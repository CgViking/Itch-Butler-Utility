using System;
using System.Collections.Generic;

namespace ItchioButlerUtility.Models;

public class AppConfig
{
    public List<GameConfig> Games { get; set; } = new();
    public string LastSelectedBuildId { get; set; } = "";

    /// <summary>Legacy plaintext key (schema 1). Migrated to <see cref="ItchApiKeyProtected"/> on load.</summary>
    public string ItchApiKey { get; set; } = "";

    /// <summary>DPAPI-protected (base64) on Windows, "plain:"-prefixed elsewhere.</summary>
    public string ItchApiKeyProtected { get; set; } = "";

    public bool LibraryCollapsed { get; set; } = false;
    public int SchemaVersion { get; set; } = 2;
}

public class GameConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DisplayName { get; set; } = "";
    public string Username { get; set; } = "";
    public string GameSlug { get; set; } = "";
    public List<BuildConfig> Builds { get; set; } = new();
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
    public List<string> IgnorePatterns { get; set; } = new();
}
