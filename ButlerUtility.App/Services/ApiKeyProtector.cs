using System;
using System.Security.Cryptography;
using System.Text;

namespace ItchioButlerUtility.Services;

/// <summary>
/// Protects the itch.io API key at rest. Windows: DPAPI (current user) as base64.
/// Other platforms have no DPAPI; the key is stored with a "plain:" prefix and the
/// config file is restricted to user-only permissions instead.
/// </summary>
public static class ApiKeyProtector
{
    private const string PlainPrefix = "plain:";

    public static bool IsEncryptedAtRest => OperatingSystem.IsWindows();

    public static string Protect(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (OperatingSystem.IsWindows())
        {
            byte[] data = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(key), optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(data);
        }
        return PlainPrefix + key;
    }

    /// <summary>Returns "" when the stored value is missing or can't be decrypted
    /// (e.g. config copied from another user/machine) — callers re-prompt for the key.</summary>
    public static string Unprotect(string stored)
    {
        if (string.IsNullOrEmpty(stored)) return "";
        if (stored.StartsWith(PlainPrefix, StringComparison.Ordinal))
            return stored.Substring(PlainPrefix.Length);
        if (!OperatingSystem.IsWindows()) return "";
        try
        {
            byte[] data = Convert.FromBase64String(stored);
            return Encoding.UTF8.GetString(
                ProtectedData.Unprotect(data, optionalEntropy: null, DataProtectionScope.CurrentUser));
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return "";
        }
    }
}
