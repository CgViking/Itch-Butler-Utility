using System;
using System.IO;
using System.Text.Json;
using ItchioButlerUtility.Models;

namespace ItchioButlerUtility.Services;

public static class ConfigStore
{
    public static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ItchioButlerUtility");

    public static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    public static string LastLoadError { get; private set; } = "";

    public static AppConfig Load()
    {
        LastLoadError = "";
        AppConfig cfg;
        try
        {
            if (!File.Exists(ConfigPath)) return new AppConfig();
            string json = File.ReadAllText(ConfigPath);
            if (string.IsNullOrWhiteSpace(json)) return new AppConfig();
            cfg = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            LastLoadError = $"Failed to load config from {ConfigPath}: {ex.Message}";
            return new AppConfig();
        }

        MigrateIfNeeded(cfg);
        return cfg;
    }

    /// <summary>Schema 1 → 2: move the plaintext API key into DPAPI-protected storage.</summary>
    private static void MigrateIfNeeded(AppConfig cfg)
    {
        if (string.IsNullOrEmpty(cfg.ItchApiKey)) return;

        if (string.IsNullOrEmpty(cfg.ItchApiKeyProtected))
            cfg.ItchApiKeyProtected = ApiKeyProtector.Protect(cfg.ItchApiKey);
        cfg.ItchApiKey = "";
        cfg.SchemaVersion = 2;
        if (!TrySave(cfg, out string error))
            LastLoadError = error;
    }

    /// <summary>
    /// The only way to write the config. Saves happen from property-change handlers and
    /// commands on the UI thread, where an escaping IOException would kill the app, so the
    /// failure is returned rather than thrown.
    /// </summary>
    public static bool TrySave(AppConfig cfg, out string error)
    {
        try
        {
            Save(cfg);
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to save config to {ConfigPath}: {ex.Message}";
            return false;
        }
    }

    private static void Save(AppConfig cfg)
    {
        if (cfg.SchemaVersion < 2) cfg.SchemaVersion = 2;
        Directory.CreateDirectory(ConfigDir);
        string tmp = ConfigPath + ".tmp";
        string json = JsonSerializer.Serialize(cfg, WriteOptions);
        File.WriteAllText(tmp, json);
        File.Move(tmp, ConfigPath, overwrite: true);

        if (!OperatingSystem.IsWindows())
        {
            // No DPAPI on this OS — at least keep the config user-only.
            try { File.SetUnixFileMode(ConfigPath, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
            catch (PlatformNotSupportedException) { }
        }
    }
}
