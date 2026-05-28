using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ItchioButlerUtility
{
    public class AppConfig
    {
        public List<GameConfig> Games { get; set; } = new List<GameConfig>();
        public string LastSelectedBuildId { get; set; } = "";
        public string ItchApiKey { get; set; } = "";
        public bool LibraryCollapsed { get; set; } = false;
        public int SchemaVersion { get; set; } = 1;
    }

    public class GameConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DisplayName { get; set; } = "";
        public string Username { get; set; } = "";
        public string GameSlug { get; set; } = "";
        public List<BuildConfig> Builds { get; set; } = new List<BuildConfig>();
    }

    public class BuildConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DisplayName { get; set; } = "";
        public string Path { get; set; } = "";
        public string Tag { get; set; } = "";
        public string Version { get; set; } = "";
        public bool Hidden { get; set; }
        public bool IfChanged { get; set; }
        public List<string> IgnorePatterns { get; set; } = new List<string>();
    }

    public static class ConfigStore
    {
        public static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ItchioButlerUtility");

        public static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static string LastLoadError { get; private set; } = "";

        public static AppConfig Load()
        {
            LastLoadError = "";
            try
            {
                if (!File.Exists(ConfigPath)) return new AppConfig();
                string json = File.ReadAllText(ConfigPath);
                if (string.IsNullOrWhiteSpace(json)) return new AppConfig();
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                return cfg ?? new AppConfig();
            }
            catch (Exception ex)
            {
                LastLoadError = $"Failed to load config from {ConfigPath}: {ex.Message}";
                return new AppConfig();
            }
        }

        public static void Save(AppConfig cfg)
        {
            Directory.CreateDirectory(ConfigDir);
            string tmp = ConfigPath + ".tmp";
            string json = JsonSerializer.Serialize(cfg, WriteOptions);
            File.WriteAllText(tmp, json);
            File.Move(tmp, ConfigPath, overwrite: true);
        }
    }
}
