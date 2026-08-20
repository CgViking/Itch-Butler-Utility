using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ItchioButlerUtility.Models;
using ItchioButlerUtility.Services;

namespace ItchioButlerUtility.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#A3BE8C"));
    private static readonly IBrush FailureBrush = new SolidColorBrush(Color.Parse("#E08585"));
    private static readonly IBrush InfoBrush = new SolidColorBrush(Color.Parse("#CFCEC4"));

    private readonly ButlerService _butler;
    private readonly HttpClient _http;
    private readonly UpdateService _updates;

    private CancellationTokenSource? _operationCts;
    private CancellationTokenSource? _versionFetchCts;
    private bool _versionHintErrorLogged;

    // butler push emits a line per file and redraws its progress bar constantly, far faster
    // than the UI can usefully repaint. Both stage into fields that a timer publishes, so the
    // repaint rate is capped no matter how fast butler talks. The console buffer is capped too:
    // the box shows ~10 lines, so retaining more than this only costs layout time and LOH churn.
    private const int MaxConsoleChars = 40_000;
    private const int TrimConsoleTo = 30_000;
    private static readonly TimeSpan UiFlushInterval = TimeSpan.FromMilliseconds(100);

    private readonly StringBuilder _consoleBuffer = new();
    private readonly DispatcherTimer _uiFlushTimer;
    private bool _consoleDirty;
    private ButlerProgress? _pendingProgress;

    public AppConfig Config { get; }
    public LibraryViewModel Library { get; }

    /// <summary>Set by the view once it opens; used for pickers and dialogs.</summary>
    public Window? OwnerWindow { get; set; }

    // --- form fields ---
    [ObservableProperty] private string _buildPath = "";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _gameSlug = "";
    [ObservableProperty] private string _tag = "";
    [ObservableProperty] private string _version = "";
    [ObservableProperty] private bool _ifChanged;
    [ObservableProperty] private bool _hidden;
    [ObservableProperty] private string _ignoreText = "";
    [ObservableProperty] private string _versionHint = "";

    // --- output / progress ---
    [ObservableProperty] private string _consoleText = "";

    // Collapsed on launch: an empty console is 170px of nothing. Every operation reopens
    // it (see PrepareOutputPanel / ReportProblem), so it is only ever shut when idle.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputGlyph))]
    private bool _isOutputVisible;

    public string OutputGlyph => IsOutputVisible ? "▾" : "▸";
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _progressStatus = "";
    [ObservableProperty] private string _resultHeadline = "";
    [ObservableProperty] private IBrush _headlineBrush = InfoBrush;
    [ObservableProperty] private bool _isBusy;

    // --- status strip ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoginStatusText))]
    [NotifyPropertyChangedFor(nameof(LoginStatusBrush))]
    private bool _isLoggedIn;

    public string LoginStatusText => IsLoggedIn ? "Logged in" : "Not logged in";
    public IBrush LoginStatusBrush => IsLoggedIn ? SuccessBrush : FailureBrush;

    [ObservableProperty] private string _updateText = "";
    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private string _updateUrl = "https://github.com/CgViking/Itch.io-Butler-Utility/releases";
    [ObservableProperty] private string _saveButtonText = "Save to library";

    public string AppVersion { get; } =
        $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(2) ?? "?"}";

    public MainWindowViewModel(AppConfig config, ButlerService butler, HttpClient http)
    {
        Config = config;
        _butler = butler;
        _http = http;
        _updates = new UpdateService(http);

        _uiFlushTimer = new DispatcherTimer { Interval = UiFlushInterval };
        _uiFlushTimer.Tick += (_, _) => FlushPendingUi();

        if (!string.IsNullOrEmpty(ConfigStore.LastLoadError))
        {
            ReportProblem(ConfigStore.LastLoadError);
            AppendOutput("Starting with an empty library. Fix the file or re-add entries.");
        }

        CheckLoginStatus();
        Library = new LibraryViewModel(this, http);
        _ = CheckForUpdatesAsync();
    }

    // --- updates ---

    private async Task CheckForUpdatesAsync()
    {
        UpdateText = "Checking for updates…";
        try
        {
            var result = await _updates.CheckAsync();
            UpdateUrl = result.ReleasesUrl;
            if (result.UpdateAvailable)
            {
                IsUpdateAvailable = true;
                UpdateText = $"Update available: v{result.LatestVersion!.ToString(2)} ↗";
            }
            else
            {
                UpdateText = "Up to date";
            }
        }
        catch (Exception ex)
        {
            UpdateText = "Update check failed";
            AppendOutput($"Update check failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenReleasesAsync()
    {
        var launcher = OwnerWindow?.Launcher;
        if (launcher is null) return;
        await launcher.LaunchUriAsync(new Uri(UpdateUrl));
    }

    // --- login status ---

    public void CheckLoginStatus() => IsLoggedIn = File.Exists(ItchCreds.CredentialsPath);

    // --- library interop ---

    /// <summary>A game has no build fields of its own, so it shows a blank build.</summary>
    public void LoadGameIntoForm(GameConfig game) => LoadBuildIntoForm(game, new BuildConfig());

    public void LoadBuildIntoForm(GameConfig? game, BuildConfig build)
    {
        Username = game?.Username ?? "";
        GameSlug = game?.GameSlug ?? "";
        BuildPath = build.Path;
        Tag = build.Tag;
        Version = build.Version;
        IfChanged = build.IfChanged;
        Hidden = build.Hidden;
        IgnoreText = string.Join(", ", build.IgnorePatterns);
    }

    /// <summary>
    /// Opens an operation: clears the panel, marks the app busy, and hands back the token that
    /// Cancel() trips. Setting IsBusy is what reveals the Cancel button, so every operation has
    /// to start here or the button ends up visible but inert. Pair with <see cref="EndOperation"/>.
    /// </summary>
    public CancellationToken BeginOperation()
    {
        PrepareOutputPanel();
        _operationCts = new CancellationTokenSource();
        IsBusy = true;
        return _operationCts.Token;
    }

    public void EndOperation()
    {
        IsBusy = false;
        _operationCts?.Dispose();
        _operationCts = null;
    }

    [RelayCommand]
    private async Task SaveToLibraryAsync()
    {
        if (!Library.SaveFromForm(out string message))
        {
            ReportProblem(message);
            SetHeadline("Nothing selected — pick a library entry first.", FailureBrush);
            return;
        }

        SaveButtonText = "Saved ✓";
        await Task.Delay(1200);
        SaveButtonText = "Save to library";
    }

    // --- commands ---

    private bool CanRunOperation() => !IsBusy;
    private bool CanCancel() => IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        PushCommand.NotifyCanExecuteChanged();
        ValidateCommand.NotifyCanExecuteChanged();
        StatusCommand.NotifyCanExecuteChanged();
        LoginCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRunOperation))]
    private async Task PushAsync()
    {
        var plan = CapturePlan();
        var problems = ValidatePlan(plan);
        if (problems.Count > 0)
        {
            ReportValidationProblems(problems.ToArray());
            return;
        }

        await RunButlerOperationAsync("Push", ButlerService.BuildPushArgs(plan), trackProgress: true);
    }

    [RelayCommand(CanExecute = nameof(CanRunOperation))]
    private async Task ValidateAsync()
    {
        string? problem = CheckBuildPath(BuildPath);
        if (problem != null)
        {
            ReportValidationProblems(problem);
            return;
        }

        await RunButlerOperationAsync("Validate", new[] { "validate", BuildPath }, trackProgress: true);
    }

    [RelayCommand(CanExecute = nameof(CanRunOperation))]
    private async Task StatusAsync()
    {
        string username = Username.Trim();
        string slug = GameSlug.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(slug))
        {
            ReportValidationProblems("Fill in username and game name first.");
            return;
        }

        await RunButlerOperationAsync("Status", new[] { "status", $"{username}/{slug}" }, trackProgress: false);
    }

    [RelayCommand(CanExecute = nameof(CanRunOperation))]
    private async Task LoginAsync()
    {
        IsBusy = true;
        try
        {
            int exitCode = await _butler.RunLoginAsync(Username.Trim());
            if (exitCode == 0)
                SetHeadline("Login successful.", SuccessBrush);
            else
                SetHeadline($"Login failed with exit code {exitCode}.", FailureBrush);
        }
        catch (Exception ex)
        {
            ReportProblem($"Login error: {ex.Message}");
            SetHeadline("Login failed — see output below.", FailureBrush);
        }
        finally
        {
            IsBusy = false;
            CheckLoginStatus();
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _operationCts?.Cancel();

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var storage = OwnerWindow?.StorageProvider;
        if (storage is null) return;

        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select build folder",
            AllowMultiple = false
        });
        if (folders.Count > 0)
            BuildPath = folders[0].TryGetLocalPath() ?? BuildPath;
    }

    [RelayCommand]
    private async Task GenerateBatAsync()
    {
        var storage = OwnerWindow?.StorageProvider;
        if (storage is null) return;

        string batchContent = ButlerService.BuildPushBatch(CapturePlan());

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Batch File",
            SuggestedFileName = "butler_push.bat",
            DefaultExtension = "bat",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Batch files") { Patterns = new[] { "*.bat" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
            }
        });
        if (file is null) return;

        try
        {
            string? localPath = file.TryGetLocalPath();
            if (localPath != null)
            {
                await File.WriteAllTextAsync(localPath, batchContent);
            }
            else
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(batchContent);
            }
            SetHeadline($"Batch file saved: {file.Name}", SuccessBrush);
        }
        catch (Exception ex)
        {
            ReportProblem($"Could not write batch file: {ex.Message}");
            SetHeadline("Saving the batch file failed — see output below.", FailureBrush);
        }
    }

    // --- shared butler plumbing ---

    /// <summary>The form's current contents, normalized. The single form → data mapping.</summary>
    public BuildPlan CapturePlan() => new(
        BuildPath,
        Username.Trim(),
        GameSlug.Trim(),
        Tag.Trim(),
        Version.Trim(),
        Hidden,
        IfChanged,
        ParseIgnorePatterns(IgnoreText));

    private static readonly Regex TagCharset = new("^[A-Za-z0-9._-]*$", RegexOptions.Compiled);

    /// <summary>The build-folder rules, shared by Push's validation and the Validate command.</summary>
    private static string? CheckBuildPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "Pick a build folder first.";
        if (!Directory.Exists(path)) return $"Build folder does not exist: {path}";
        return null;
    }

    private List<string> ValidatePlan(BuildPlan plan)
    {
        var problems = new List<string>();
        if (CheckBuildPath(plan.Path) is { } pathProblem)
            problems.Add(pathProblem);
        if (string.IsNullOrWhiteSpace(plan.Username))
            problems.Add("Username is required.");
        if (string.IsNullOrWhiteSpace(plan.GameSlug))
            problems.Add("Game name (slug) is required.");
        if (!TagCharset.IsMatch(plan.Tag))
            problems.Add($"Channel tag '{plan.Tag}' contains unexpected characters (use letters, digits, '-', '_', '.').");
        if (!File.Exists(ButlerService.ButlerPath))
            problems.Add($"butler executable not found at {ButlerService.ButlerPath}.");
        return problems;
    }

    private void ReportValidationProblems(params string[] problems)
    {
        foreach (var problem in problems)
            ReportProblem($"[validation] {problem}");
        SetHeadline("Fix the problems listed below, then try again.", FailureBrush);
    }

    private async Task RunButlerOperationAsync(string operation, IReadOnlyList<string> args, bool trackProgress)
    {
        var ct = BeginOperation();

        // Constructed on the UI thread so callbacks marshal back automatically. Both sinks
        // only stage the update; the flush timer publishes it (butler talks faster than the
        // UI can usefully repaint).
        var progress = trackProgress ? new Progress<ButlerProgress>(StageProgress) : null;
        var output = new Progress<string>(AppendOutput);

        ProgressStatus = "Starting…";

        try
        {
            var result = await _butler.RunAsync(args, progress, output, ct);
            if (result.Success)
            {
                // Always call this, tracked or not: it is what clears the "Starting…"
                // label. Guarding the whole call left it on screen for good after an
                // untracked operation succeeded. Only the bar itself is tracked-only.
                SetFinalProgress(trackProgress ? 100 : null);
                SetHeadline($"{operation} completed successfully.", SuccessBrush);
            }
            else
            {
                SetFinalProgress(null);
                SetHeadline(
                    $"{ButlerErrors.Describe(result.ErrorCategory, operation)} (exit code {result.ExitCode})",
                    FailureBrush);
            }
        }
        catch (OperationCanceledException)
        {
            SetFinalProgress(null);
            SetHeadline($"{operation} cancelled.", InfoBrush);
            AppendOutput($"{operation} cancelled by user.");
        }
        catch (Exception ex)
        {
            SetFinalProgress(null);
            AppendOutput($"Error: {ex.Message}");
            SetHeadline($"{operation} failed — see output below.", FailureBrush);
        }
        finally
        {
            EndOperation();
        }
    }

    private void PrepareOutputPanel()
    {
        _uiFlushTimer.Stop();
        _consoleBuffer.Clear();
        _consoleDirty = false;
        _pendingProgress = null;
        ConsoleText = "";
        IsOutputVisible = true;
        ProgressPercent = 0;
        ProgressStatus = "";
        ResultHeadline = "";
    }

    [RelayCommand]
    private void ToggleOutput() => IsOutputVisible = !IsOutputVisible;

    /// <summary>Writes to the output console and makes sure the panel is actually showing.</summary>
    public void ReportProblem(string message)
    {
        IsOutputVisible = true;
        AppendOutput(message);
    }

    public void AppendOutput(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;

        _consoleBuffer.Append(text).Append(Environment.NewLine);
        TrimConsoleBuffer();

        _consoleDirty = true;
        EnsureFlushing();
    }

    /// <summary>Last-value-wins: butler reports progress many times a second.</summary>
    private void StageProgress(ButlerProgress p)
    {
        _pendingProgress = p;
        EnsureFlushing();
    }

    /// <summary>Publishes a terminal progress state, discarding any staged update behind it.</summary>
    private void SetFinalProgress(double? percent)
    {
        _pendingProgress = null;
        if (percent.HasValue) ProgressPercent = percent.Value;
        ProgressStatus = "";
    }

    private void EnsureFlushing()
    {
        if (!_uiFlushTimer.IsEnabled) _uiFlushTimer.Start();
    }

    private void FlushPendingUi()
    {
        bool published = false;

        if (_pendingProgress is { } p)
        {
            _pendingProgress = null;
            string percent = p.Percent.ToString("0.0", CultureInfo.InvariantCulture);
            ProgressPercent = p.Percent;
            ProgressStatus = p.Detail.Length > 0 ? $"{percent}%  —  {p.Detail}" : $"{percent}%";
            published = true;
        }

        if (_consoleDirty)
        {
            _consoleDirty = false;
            ConsoleText = _consoleBuffer.ToString();
            published = true;
        }

        // Nothing staged this tick — stop until there is something to publish again.
        if (!published) _uiFlushTimer.Stop();
    }

    private void TrimConsoleBuffer()
    {
        if (_consoleBuffer.Length <= MaxConsoleChars) return;

        int cut = _consoleBuffer.Length - TrimConsoleTo;
        // Slice on a line boundary so the first surviving line is not a fragment.
        int limit = Math.Min(_consoleBuffer.Length, cut + 1000);
        for (int i = cut; i < limit; i++)
        {
            if (_consoleBuffer[i] == '\n')
            {
                cut = i + 1;
                break;
            }
        }

        _consoleBuffer.Remove(0, cut);
        _consoleBuffer.Insert(0, "… earlier output trimmed …" + Environment.NewLine);
    }

    private void SetHeadline(string text, IBrush brush)
    {
        ResultHeadline = text;
        HeadlineBrush = brush;
    }

    private static List<string> ParseIgnorePatterns(string text) =>
        text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

    // --- latest-version hint (debounced) ---

    partial void OnUsernameChanged(string value) => RestartVersionFetch();
    partial void OnGameSlugChanged(string value) => RestartVersionFetch();
    partial void OnTagChanged(string value) => RestartVersionFetch();

    private void RestartVersionFetch()
    {
        _versionFetchCts?.Cancel();
        _versionFetchCts?.Dispose();
        var cts = _versionFetchCts = new CancellationTokenSource();
        _ = FetchLatestVersionAsync(cts.Token);
    }

    private async Task FetchLatestVersionAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(500, ct);

            string username = Username.Trim();
            string slug = GameSlug.Trim();
            string tag = Tag.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(tag))
            {
                VersionHint = "";
                return;
            }

            string? latest = await ItchClient.FetchLatestVersionAsync(_http, username, slug, tag, ct);
            VersionHint = latest != null ? $"Latest: {latest}" : "No version history";
        }
        catch (OperationCanceledException)
        {
            // superseded by newer keystrokes — nothing to do
        }
        catch (Exception ex)
        {
            VersionHint = "";
            if (!_versionHintErrorLogged)
            {
                _versionHintErrorLogged = true;
                AppendOutput($"Version hint unavailable ({ex.Message}). Will keep quiet about further failures.");
            }
        }
    }
}
