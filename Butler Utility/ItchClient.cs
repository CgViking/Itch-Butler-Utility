using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ItchioButlerUtility
{
    public static class ItchCreds
    {
        public static readonly string CredentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "itch", "butler_creds");

        private static readonly Regex ApiKeyLike = new Regex(@"^[A-Za-z0-9]{20,}$", RegexOptions.Compiled);

        public static string ReadApiKey()
        {
            if (!File.Exists(CredentialsPath))
                throw new InvalidOperationException(
                    $"butler credentials not found at {CredentialsPath}. Click 'Login to Itch' first.");

            string raw = File.ReadAllText(CredentialsPath).Trim();
            if (string.IsNullOrEmpty(raw))
                throw new InvalidOperationException("butler_creds is empty.");

            // butler stores credentials as JSON. Field name varies across butler versions —
            // try the common shapes, then fall back to scanning string values.
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var name in new[] { "key", "api_key", "apiKey", "Key" })
                    {
                        if (doc.RootElement.TryGetProperty(name, out var v) &&
                            v.ValueKind == JsonValueKind.String)
                        {
                            string val = v.GetString();
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                    }
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            string val = prop.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(val) && ApiKeyLike.IsMatch(val))
                                return val;
                        }
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.String)
                {
                    string val = doc.RootElement.GetString();
                    if (!string.IsNullOrWhiteSpace(val)) return val;
                }
            }
            catch (JsonException)
            {
                // Not JSON — some butler builds store the bare key string.
                if (ApiKeyLike.IsMatch(raw)) return raw;
            }

            throw new InvalidOperationException(
                "Could not find an API key inside butler_creds. Try logging in again.");
        }
    }

    public class ItchGame
    {
        public long Id { get; set; }
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
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"GET /profile/games returned {(int)resp.StatusCode}: {Truncate(body, 200)}");

            var result = new List<ItchGame>();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0)
                throw new HttpRequestException(errs[0].GetString() ?? "itch.io returned an error.");
            if (!doc.RootElement.TryGetProperty("games", out var games) || games.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var g in games.EnumerateArray())
            {
                var game = new ItchGame
                {
                    Id = g.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.Number ? idEl.GetInt64() : 0,
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
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"GET wharf/channels returned {(int)resp.StatusCode}: {Truncate(body, 200)}");

            var result = new List<ItchChannel>();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0)
                throw new HttpRequestException(errs[0].GetString() ?? "itch.io returned an error.");

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
}