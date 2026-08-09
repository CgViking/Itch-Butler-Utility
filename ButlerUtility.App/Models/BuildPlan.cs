using System.Collections.Generic;

namespace ItchioButlerUtility.Models;

/// <summary>Everything butler push needs, captured from the form in one shot.</summary>
public record BuildPlan(
    string Path,
    string Username,
    string GameSlug,
    string Tag,
    string Version,
    bool Hidden,
    bool IfChanged,
    IReadOnlyList<string> IgnorePatterns);
