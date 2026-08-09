using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ItchioButlerUtility.Services;

public record UpdateCheckResult(bool UpdateAvailable, Version? LatestVersion, string ReleasesUrl);

/// <summary>
/// Checks the repo's AutoUpdater.NET-style XML feeds on the main branch; the release
/// workflow bumps them once a release is live. When an update exists the UI links to
/// the releases page.
/// </summary>
/// <remarks>
/// These URLs use the current repo name. Builds up to 1.1.0 have the pre-rename name
/// (Itch.io-Butler-Utility) compiled in and reach these files through GitHub's rename
/// redirect, so that name must never be claimed by another repository.
/// </remarks>
public class UpdateService
{
    private const string InstalledFeedUrl =
        "https://raw.githubusercontent.com/CgViking/Itch-Butler-Utility/main/updater-installed.xml";
    private const string PortableFeedUrl =
        "https://raw.githubusercontent.com/CgViking/Itch-Butler-Utility/main/updater-portable.xml";
    private const string DefaultReleasesUrl =
        "https://github.com/CgViking/Itch-Butler-Utility/releases";

    private readonly HttpClient _http;

    public UpdateService(HttpClient http) => _http = http;

    /// <summary>Inno Setup drops its uninstaller next to the app; portable zips don't have one.</summary>
    public static bool IsInstalledDistribution() =>
        File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        string feedUrl = IsInstalledDistribution() ? InstalledFeedUrl : PortableFeedUrl;
        string xml = await _http.GetStringAsync(feedUrl, ct);
        var doc = XDocument.Parse(xml);

        string versionText = doc.Root?.Element("version")?.Value.Trim() ?? "";
        string releasesUrl = doc.Root?.Element("changelog")?.Value.Trim() ?? DefaultReleasesUrl;

        if (!Version.TryParse(versionText, out var latest))
            throw new FormatException($"Update feed contains an unreadable version: '{versionText}'");

        var current = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);
        bool available = Normalize(latest) > Normalize(current);
        return new UpdateCheckResult(available, latest, releasesUrl);
    }

    // "1.2" and "1.2.0.0" should compare equal.
    private static Version Normalize(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
}
