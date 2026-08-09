using System;

namespace ItchioButlerUtility.Services;

public enum ButlerErrorCategory
{
    None,
    Auth,
    Path,
    Network
}

/// <summary>Turns butler's raw output into a human-readable failure headline.</summary>
public static class ButlerErrors
{
    private static readonly string[] AuthMarkers =
    {
        "401", "403", "api key", "authenticat", "unauthorized", "forbidden", "invalid key"
    };

    private static readonly string[] PathMarkers =
    {
        "no such file", "does not exist", "cannot find", "access is denied",
        "permission denied", "not a directory", "is a directory"
    };

    private static readonly string[] NetworkMarkers =
    {
        "dial tcp", "tls", "timeout", "timed out", "connection refused",
        "no such host", "network is unreachable", "connection reset"
    };

    public static ButlerErrorCategory Categorize(string line)
    {
        if (string.IsNullOrEmpty(line)) return ButlerErrorCategory.None;
        if (Matches(line, AuthMarkers)) return ButlerErrorCategory.Auth;
        if (Matches(line, PathMarkers)) return ButlerErrorCategory.Path;
        if (Matches(line, NetworkMarkers)) return ButlerErrorCategory.Network;
        return ButlerErrorCategory.None;
    }

    public static string Describe(ButlerErrorCategory category, string operation) => category switch
    {
        ButlerErrorCategory.Auth =>
            $"{operation} failed: itch.io rejected your credentials. Click Login and try again.",
        ButlerErrorCategory.Path =>
            $"{operation} failed: a file or folder could not be found or accessed. Check the build path.",
        ButlerErrorCategory.Network =>
            $"{operation} failed: could not reach itch.io. Check your internet connection and try again.",
        _ => $"{operation} failed. See the output below for details."
    };

    private static bool Matches(string line, string[] markers)
    {
        foreach (var marker in markers)
        {
            if (line.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
