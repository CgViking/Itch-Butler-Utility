using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ItchioButlerUtility.Models;

namespace ItchioButlerUtility.Services;

public record ButlerProgress(double Percent, string Detail);

public record ButlerResult(int ExitCode, ButlerErrorCategory ErrorCategory)
{
    public bool Success => ExitCode == 0;
}

/// <summary>Invokes the bundled butler CLI and streams its output/progress back to the caller.</summary>
public class ButlerService
{
    public static readonly string ButlerPath = Path.Combine(
        AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "butler.exe" : "butler");

    // butler renders progress as a half-block bar, e.g. "▌███▌  42.50% - compressing".
    private static readonly Regex ProgressRegex = new(
        @"(\d+(?:\.\d+)?)%\s*(?:[-@]\s*(.*?))?\s*$",
        RegexOptions.Compiled);

    private static bool IsProgressLine(string text) => text.IndexOf('▌') >= 0;

    public static List<string> BuildPushArgs(BuildPlan plan)
    {
        string target = string.IsNullOrWhiteSpace(plan.Tag)
            ? $"{plan.Username}/{plan.GameSlug}"
            : $"{plan.Username}/{plan.GameSlug}:{plan.Tag}";

        var args = new List<string> { "push", plan.Path, target };
        if (!string.IsNullOrWhiteSpace(plan.Version))
        {
            args.Add("--userversion");
            args.Add(plan.Version);
        }
        if (plan.IfChanged) args.Add("--if-changed");
        if (plan.Hidden) args.Add("--hidden");
        foreach (var raw in plan.IgnorePatterns)
        {
            var pattern = raw.Trim();
            if (pattern.Length == 0) continue;
            args.Add("--ignore");
            args.Add(pattern);
        }
        return args;
    }

    /// <summary>
    /// The equivalent of <see cref="BuildPushArgs"/> as a standalone .bat. Everything cmd.exe
    /// needs lives here — argument quoting, % escaping, CRLF line endings — so callers only
    /// have to decide where to write it. Process invocation uses ArgumentList and needs none of it.
    /// </summary>
    public static string BuildPushBatch(BuildPlan plan)
    {
        string commandTail = EscapeForBatch(ToCommandLine(BuildPushArgs(plan)));
        return $"@echo off\r\nbutler {commandTail}\r\necho Push command executed.\r\npause\r\n";
    }

    private static string ToCommandLine(IEnumerable<string> args) =>
        string.Join(" ", args.Select(QuoteArgument));

    private static readonly char[] MustQuote = { ' ', '\t', '"' };

    /// <summary>
    /// Quotes one argument the way the Windows CRT parses argv: a run of backslashes is only
    /// special when it precedes a quote, so a trailing separator (C:\My Build\) has to be
    /// doubled or it escapes the closing quote and swallows the next argument.
    /// </summary>
    private static string QuoteArgument(string arg)
    {
        if (arg.Length > 0 && arg.IndexOfAny(MustQuote) < 0) return arg;

        var sb = new StringBuilder(arg.Length + 8).Append('"');
        int backslashes = 0;
        foreach (char c in arg)
        {
            if (c == '\\')
            {
                backslashes++;
            }
            else if (c == '"')
            {
                sb.Append('\\', backslashes * 2 + 1).Append('"');
                backslashes = 0;
            }
            else
            {
                sb.Append('\\', backslashes).Append(c);
                backslashes = 0;
            }
        }
        return sb.Append('\\', backslashes * 2).Append('"').ToString();
    }

    /// <summary>cmd expands %VAR% even inside double quotes; %% is the literal percent sign.</summary>
    private static string EscapeForBatch(string commandLine) => commandLine.Replace("%", "%%");

    /// <summary>
    /// Runs butler with redirected output. Progress lines (the ▌ bar) go to
    /// <paramref name="progress"/>; everything else goes to <paramref name="output"/>.
    /// Both callbacks fire on the caller's SynchronizationContext when the
    /// Progress instances are constructed on the UI thread.
    /// </summary>
    public async Task<ButlerResult> RunAsync(
        IReadOnlyList<string> args,
        IProgress<ButlerProgress>? progress,
        IProgress<string>? output,
        CancellationToken ct = default)
    {
        using var process = new Process();
        process.StartInfo.FileName = ButlerPath;
        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        // butler's progress bar uses '▌'; without explicit UTF-8 the glyph is mangled
        // and progress detection breaks.
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        process.StartInfo.CreateNoWindow = true;

        var errorCategory = ButlerErrorCategory.None;

        void HandleLine(string? line)
        {
            if (line is null) return;
            if (IsProgressLine(line))
            {
                ReportProgress(line, progress);
                return;
            }
            var category = ButlerErrors.Categorize(line);
            if (category != ButlerErrorCategory.None && errorCategory == ButlerErrorCategory.None)
                errorCategory = category;
            output?.Report(line);
        }

        process.OutputDataReceived += (_, e) => HandleLine(e.Data);
        process.ErrorDataReceived += (_, e) => HandleLine(e.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct);
            // WaitForExitAsync does not wait for redirected streams to drain; the
            // parameterless WaitForExit does, and returns immediately post-exit.
            process.WaitForExit();
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { /* already exited */ }
            throw;
        }

        return new ButlerResult(process.ExitCode, errorCategory);
    }

    /// <summary>butler login opens a console window and the browser; output is not redirected.</summary>
    public Task<int> RunLoginAsync(string username)
    {
        return Task.Run(() =>
        {
            using var process = new Process();
            process.StartInfo.FileName = ButlerPath;
            process.StartInfo.ArgumentList.Add("login");
            if (!string.IsNullOrWhiteSpace(username))
                process.StartInfo.ArgumentList.Add(username);
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = false;

            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        });
    }

    private static void ReportProgress(string line, IProgress<ButlerProgress>? progress)
    {
        if (progress is null) return;
        var match = ProgressRegex.Match(line);
        if (!match.Success) return;
        if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double percent))
            return;

        percent = Math.Clamp(percent, 0, 100);
        string detail = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "";
        progress.Report(new ButlerProgress(percent, detail));
    }
}
