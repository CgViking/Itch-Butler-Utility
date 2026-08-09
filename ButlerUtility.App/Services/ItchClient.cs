using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ItchioButlerUtility.Services;

/// <summary>
/// Where butler keeps its own credentials. Only existence is interesting: those creds are
/// wharf-scoped, so listing games needs a separate personal key (see <see cref="ItchClient"/>).
/// </summary>
public static class ItchCreds
{
    public static readonly string CredentialsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "itch", "butler_creds");
}

public class ItchGame
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Username { get; set; } = "";
}

public class ItchChannel
{
    public string Name { get; set; } = "";
    public string UserVersion { get; set; } = "";
}

public static class ItchClient
{
    public static async Task<List<ItchGame>> FetchGamesAsync(HttpClient http, string apiKey, CancellationToken ct)
    {
        // Requires a personal API key with `profile:games` scope (generated from
        // https://itch.io/user/settings/api-keys). butler's own credentials are
        // wharf-scoped only and cannot list games.
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.itch.io/profile/games");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var resp = await http.SendAsync(req, ct);

        var result = new List<ItchGame>();
        using var doc = await ReadJsonAsync(resp, "/profile/games", ct);
        if (!doc.RootElement.TryGetProperty("games", out var games) || games.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var g in games.EnumerateArray())
        {
            var game = new ItchGame
            {
                Title = g.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : ""
            };

            string gameUrl = g.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "";
            (game.Username, game.Slug) = ParseUserAndSlug(gameUrl);

            if (string.IsNullOrEmpty(game.Username) &&
                g.TryGetProperty("user", out var userEl) &&
                userEl.ValueKind == JsonValueKind.Object &&
                userEl.TryGetProperty("username", out var unameEl))
            {
                game.Username = unameEl.GetString() ?? "";
            }

            if (!string.IsNullOrEmpty(game.Slug) && !string.IsNullOrEmpty(game.Username))
                result.Add(game);
        }
        return result;
    }

    public static async Task<List<ItchChannel>> FetchChannelsAsync(HttpClient http, string apiKey, string user, string slug, CancellationToken ct)
    {
        string url = $"https://itch.io/api/1/{Uri.EscapeDataString(apiKey)}/wharf/channels?target={Uri.EscapeDataString(user)}/{Uri.EscapeDataString(slug)}";
        using var resp = await http.GetAsync(url, ct);

        var result = new List<ItchChannel>();
        using var doc = await ReadJsonAsync(resp, "wharf/channels", ct);
        if (!doc.RootElement.TryGetProperty("channels", out var channels) || channels.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in channels.EnumerateObject())
        {
            var ch = new ItchChannel { Name = prop.Name };
            if (prop.Value.ValueKind == JsonValueKind.Object &&
                prop.Value.TryGetProperty("head", out var head) &&
                head.ValueKind == JsonValueKind.Object &&
                head.TryGetProperty("user_version", out var uv) &&
                uv.ValueKind == JsonValueKind.String)
            {
                ch.UserVersion = uv.GetString() ?? "";
            }
            result.Add(ch);
        }
        return result;
    }

    /// <summary>The newest user_version pushed to a channel, or null when it has no history.</summary>
    public static async Task<string?> FetchLatestVersionAsync(
        HttpClient http, string user, string slug, string channel, CancellationToken ct)
    {
        string url = "https://itch.io/api/1/x/wharf/latest" +
                     $"?target={Uri.EscapeDataString(user)}/{Uri.EscapeDataString(slug)}" +
                     $"&channel_name={Uri.EscapeDataString(channel)}";
        using var resp = await http.GetAsync(url, ct);
        using var doc = await ReadJsonAsync(resp, "wharf/latest", ct);
        return doc.RootElement.TryGetProperty("latest", out var latest) ? latest.GetString() : null;
    }

    /// <summary>
    /// Shared response handling: itch.io signals failure both by status code and by an
    /// "errors" array in an otherwise-200 body, so every endpoint has to check both.
    /// </summary>
    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage resp, string endpoint, CancellationToken ct)
    {
        string body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"GET {endpoint} returned {(int)resp.StatusCode}: {Truncate(body, 200)}");

        var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("errors", out var errs) &&
            errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0)
        {
            string message = errs[0].GetString() ?? "itch.io returned an error.";
            doc.Dispose();
            throw new HttpRequestException(message);
        }
        return doc;
    }

    // Parses "https://username.itch.io/game-slug" → ("username", "game-slug").
    private static (string user, string slug) ParseUserAndSlug(string gameUrl)
    {
        if (string.IsNullOrWhiteSpace(gameUrl)) return ("", "");
        if (!Uri.TryCreate(gameUrl, UriKind.Absolute, out var uri)) return ("", "");

        string host = uri.Host;
        string user = "";
        const string itchHost = ".itch.io";
        if (host.EndsWith(itchHost, StringComparison.OrdinalIgnoreCase))
            user = host.Substring(0, host.Length - itchHost.Length);

        string slug = uri.AbsolutePath.Trim('/');
        int firstSlash = slug.IndexOf('/');
        if (firstSlash >= 0) slug = slug.Substring(0, firstSlash);

        return (user, slug);
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
