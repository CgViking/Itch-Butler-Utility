using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ItchioButlerUtility.Models;
using ItchioButlerUtility.Services;
using ItchioButlerUtility.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace ItchioButlerUtility.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    /// <summary>Placeholder title for a freshly added game; treated as "not yet named".</summary>
    private const string NewGameName = "New game";

    /// <summary>How many channel lookups sync runs at once.</summary>
    private const int SyncFetchConcurrency = 5;

    private readonly MainWindowViewModel _main;
    private readonly HttpClient _http;
    private readonly List<GameNodeViewModel> _allGames = new();

    private AppConfig Config => _main.Config;

    public ObservableCollection<GameNodeViewModel> FilteredGames { get; } = new();

    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private object? _selectedNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleGlyph))]
    private bool _isLibraryVisible;

    public string ToggleGlyph => IsLibraryVisible ? "»" : "«";

    public LibraryViewModel(MainWindowViewModel main, HttpClient http)
    {
        _main = main;
        _http = http;
        _isLibraryVisible = !Config.LibraryCollapsed;

        // CanSync reads _main's state, so this owns re-querying it rather than making
        // MainWindowViewModel remember to poke a command it doesn't define.
        _main.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainWindowViewModel.IsBusy) or nameof(MainWindowViewModel.IsLoggedIn))
                SyncCommand.NotifyCanExecuteChanged();
        };

        RebuildNodes(string.IsNullOrEmpty(Config.LastSelectedBuildId) ? null : Config.LastSelectedBuildId);
    }

    // --- tree state ---

    // SelectedNode is two-way bound to TreeView.SelectedItem, so emptying FilteredGames makes
    // the TreeView push a null back in before we can restore the selection. Side effects
    // (loading the form, persisting the selection) are suppressed across that churn and applied
    // once at the end — otherwise every rebuild reloads the form over what the user is typing.
    private bool _suppressSelectionSideEffects;

    /// <summary>
    /// Runs a tree restructure with selection side effects held off, then reselects. The
    /// callback receives the pre-churn selection — by the time it runs, the TreeView has
    /// already pushed a null in — and returns the node to select. Returns the pre-churn
    /// selection so the caller can tell whether the selection actually moved.
    /// </summary>
    private object? Restructure(Func<object?, object?> restructureAndReselect)
    {
        object? previous = SelectedNode;
        _suppressSelectionSideEffects = true;
        try
        {
            SelectedNode = restructureAndReselect(previous);
        }
        finally
        {
            _suppressSelectionSideEffects = false;
        }
        return previous;
    }

    private void RebuildNodes(string? selectBuildId, string? selectGameId = null)
    {
        object? previous = Restructure(_ =>
        {
            _allGames.Clear();
            foreach (var game in Config.Games)
                _allGames.Add(new GameNodeViewModel(game, this));
            RefreshFilteredGames();
            return FindNode(selectBuildId, selectGameId);
        });

        // Every node instance is new here, so re-apply even when the same model stayed selected.
        if (SelectedNode != null || previous != null)
            ApplySelectionSideEffects(SelectedNode);
    }

    private object? FindNode(string? buildId, string? gameId)
    {
        if (buildId != null)
            return _allGames.SelectMany(g => g.Builds).FirstOrDefault(b => b.Build.Id == buildId);
        if (gameId != null)
            return _allGames.FirstOrDefault(g => g.Game.Id == gameId);
        return null;
    }

    partial void OnFilterTextChanged(string value)
    {
        object? previous = Restructure(priorSelection =>
        {
            RefreshFilteredGames();
            return SurvivingSelection(priorSelection);
        });

        // Losing the selection to a filter is a view change, not a user pick — don't persist it.
        if (!ReferenceEquals(SelectedNode, previous))
            ApplySelectionSideEffects(SelectedNode, persist: SelectedNode != null);
    }

    private void RefreshFilteredGames()
    {
        FilteredGames.Clear();
        string filter = FilterText.Trim();
        foreach (var game in _allGames)
        {
            if (game.ApplyFilter(filter))
                FilteredGames.Add(game);
        }
    }

    private object? SurvivingSelection(object? previous)
    {
        if (previous is GameNodeViewModel g && FilteredGames.Contains(g))
            return g;
        if (previous is BuildNodeViewModel b && FilteredGames.Contains(b.Parent) && b.Parent.FilteredBuilds.Contains(b))
            return b;
        return null;
    }

    partial void OnSelectedNodeChanged(object? value)
    {
        if (_suppressSelectionSideEffects) return;
        ApplySelectionSideEffects(value);
    }

    private void ApplySelectionSideEffects(object? value, bool persist = true)
    {
        if (value is BuildNodeViewModel buildNode)
            _main.LoadBuildIntoForm(buildNode.Parent.Game, buildNode.Build);
        else if (value is GameNodeViewModel gameNode)
            _main.LoadGameIntoForm(gameNode.Game);

        string lastId = (value as BuildNodeViewModel)?.Build.Id ?? "";
        if (persist && Config.LastSelectedBuildId != lastId)
        {
            Config.LastSelectedBuildId = lastId;
            SaveConfig();
        }

        AddBuildCommand.NotifyCanExecuteChanged();
        DuplicateSelectedCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Config writes fire from property-change handlers on the UI thread, where an unhandled
    /// IOException would take the app down. Report the failure in the output panel instead.
    /// </summary>
    private void SaveConfig()
    {
        if (!ConfigStore.TrySave(Config, out string error))
            _main.ReportProblem(error);
    }

    /// <summary>
    /// Persist a library mutation and show it. Recording the resulting selection up front
    /// keeps this to a single config write — otherwise the rebuild notices LastSelectedBuildId
    /// changed and saves a second time.
    /// </summary>
    private void SaveAndRebuild(string? selectBuildId, string? selectGameId = null)
    {
        Config.LastSelectedBuildId = selectBuildId ?? "";
        SaveConfig();
        RebuildNodes(selectBuildId, selectGameId);
    }

    partial void OnIsLibraryVisibleChanged(bool value)
    {
        Config.LibraryCollapsed = !value;
        SaveConfig();
    }

    [RelayCommand]
    private void ToggleLibrary() => IsLibraryVisible = !IsLibraryVisible;

    private bool HasSelection() => SelectedNode != null;

    private GameNodeViewModel? SelectedOrParentGame() =>
        SelectedNode as GameNodeViewModel ?? (SelectedNode as BuildNodeViewModel)?.Parent;

    // --- CRUD ---

    [RelayCommand]
    private void AddGame()
    {
        var game = new GameConfig { DisplayName = NewGameName };
        Config.Games.Add(game);
        SaveAndRebuild(null, game.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void AddBuild()
    {
        var gameNode = SelectedOrParentGame();
        if (gameNode != null) AddBuildTo(gameNode);
    }

    public void AddBuildTo(GameNodeViewModel gameNode)
    {
        var build = new BuildConfig { Tag = "win" };
        gameNode.Game.Builds.Add(build);
        SaveAndRebuild(build.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DuplicateSelected()
    {
        if (SelectedNode is BuildNodeViewModel buildNode) DuplicateBuild(buildNode);
        else if (SelectedNode is GameNodeViewModel gameNode) DuplicateGame(gameNode);
    }

    /// <summary>Copies everything but the identity — one place, so new fields can't be missed.</summary>
    private static BuildConfig CloneBuild(BuildConfig source, string tagSuffix = "") => new()
    {
        DisplayName = source.DisplayName,
        Path = source.Path,
        Tag = source.Tag + tagSuffix,
        Version = source.Version,
        Hidden = source.Hidden,
        IfChanged = source.IfChanged,
        IgnorePatterns = new List<string>(source.IgnorePatterns)
    };

    public void DuplicateBuild(BuildNodeViewModel node)
    {
        var copy = CloneBuild(node.Build, "-copy");
        node.Parent.Game.Builds.Add(copy);
        SaveAndRebuild(copy.Id);
    }

    public void DuplicateGame(GameNodeViewModel node)
    {
        var source = node.Game;
        var copy = new GameConfig
        {
            DisplayName = source.DisplayName + " (copy)",
            Username = source.Username,
            GameSlug = source.GameSlug,
            Builds = source.Builds.Select(b => CloneBuild(b)).ToList()
        };
        Config.Games.Add(copy);
        SaveAndRebuild(null, copy.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedNode is BuildNodeViewModel buildNode) await DeleteBuildAsync(buildNode);
        else if (SelectedNode is GameNodeViewModel gameNode) await DeleteGameAsync(gameNode);
    }

    public async Task DeleteBuildAsync(BuildNodeViewModel node)
    {
        if (!await ConfirmAsync($"Delete build '{node.Name}'?")) return;

        // Right-clicking a TreeViewItem does not select it, so the node being deleted is often
        // not the selected one — the selection has to survive. Track it by model id: node
        // instances are replaced by RebuildNodes, so comparing instances misses matches and
        // can leave SelectedNode pointing at a model that is no longer in the config.
        var (buildId, gameId) = CurrentSelectionIds();
        if (buildId == node.Build.Id) (buildId, gameId) = (null, null);

        node.Parent.Game.Builds.Remove(node.Build);
        SaveAndRebuild(buildId, gameId);
    }

    public async Task DeleteGameAsync(GameNodeViewModel node)
    {
        if (!await ConfirmAsync($"Delete game '{node.Name}' and all its builds?")) return;

        var (buildId, gameId) = CurrentSelectionIds();
        if (gameId == node.Game.Id || (buildId != null && node.Game.Builds.Any(b => b.Id == buildId)))
            (buildId, gameId) = (null, null);

        Config.Games.Remove(node.Game);
        SaveAndRebuild(buildId, gameId);
    }

    private (string? BuildId, string? GameId) CurrentSelectionIds()
    {
        if (SelectedNode is BuildNodeViewModel b) return (b.Build.Id, null);
        if (SelectedNode is GameNodeViewModel g) return (null, g.Game.Id);
        return (null, null);
    }

    private async Task<bool> ConfirmAsync(string question)
    {
        var owner = _main.OwnerWindow;
        var box = MessageBoxManager.GetMessageBoxStandard(
            "Confirm", question, ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question);
        var result = owner != null
            ? await box.ShowWindowDialogAsync(owner)
            : await box.ShowAsync();
        return result == ButtonResult.Yes;
    }

    // --- push from context menu ---

    public async Task PushBuildAsync(BuildNodeViewModel node)
    {
        SelectedNode = node; // loads the form fields
        if (_main.PushCommand.CanExecute(null))
            await _main.PushCommand.ExecuteAsync(null);
    }

    // --- save form → library ---

    public bool SaveFromForm(out string message)
    {
        var buildNode = SelectedNode as BuildNodeViewModel;
        var gameNode = SelectedOrParentGame();
        if (gameNode == null)
        {
            message = "Select a game or build in the sidebar first, or click '+ Game' to start a new one.";
            return false;
        }

        var form = _main.CapturePlan();

        var game = gameNode.Game;
        game.Username = form.Username;
        game.GameSlug = form.GameSlug;
        if (IsUncustomizedDisplayName(game.DisplayName, game.GameSlug))
            game.DisplayName = game.GameSlug;

        if (buildNode != null)
        {
            var build = buildNode.Build;
            build.Path = form.Path;
            build.Tag = form.Tag;
            build.Version = form.Version;
            build.Hidden = form.Hidden;
            build.IfChanged = form.IfChanged;
            build.IgnorePatterns = form.IgnorePatterns.ToList();
            if (string.IsNullOrWhiteSpace(build.DisplayName))
                build.DisplayName = build.Tag;
        }

        // Nothing structural changed — only the labels — so refresh them in place rather than
        // rebuilding the tree and putting the selection through a restore.
        SaveConfig();
        gameNode.RefreshName();
        buildNode?.RefreshName();

        message = "";
        return true;
    }

    // --- sync from itch.io ---

    public bool CanSync() => _main.IsLoggedIn && !_main.IsBusy;

    [RelayCommand(CanExecute = nameof(CanSync))]
    private async Task SyncAsync()
    {
        var ct = _main.BeginOperation();

        try
        {
            string? apiKey = await EnsureApiKeyAsync();
            if (apiKey == null) return;

            _main.AppendOutput("Syncing from itch.io...");

            var remoteGames = await ItchClient.FetchGamesAsync(_http, apiKey, ct);
            _main.AppendOutput($"Found {remoteGames.Count} game(s) on itch.io.");

            var fetched = await FetchAllChannelsAsync(apiKey, remoteGames, ct);
            var counts = MergeIntoLibrary(fetched);

            PersistSyncedLibrary();

            _main.AppendOutput(
                $"Sync complete: {counts.NewGames} new game(s), {counts.NewBuilds} new build(s), " +
                $"{counts.UpdatedVersions} version(s) refreshed" +
                (counts.Skipped > 0 ? $", {counts.Skipped} skipped." : "."));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Games merged before the cancel are already in Config — keep them rather than
            // leaving the in-memory library out of step with what is on disk.
            PersistSyncedLibrary();
            _main.AppendOutput("Sync cancelled. Games merged before cancelling were kept.");
        }
        catch (Exception ex)
        {
            _main.AppendOutput($"Sync failed: {ex.Message}");
            if (ex.Message.Contains(" 401") || ex.Message.Contains(" 403"))
            {
                Config.ItchApiKeyProtected = "";
                SaveConfig();
                _main.AppendOutput("Cleared stored API key. Click Sync again to enter a new one.");
            }
        }
        finally
        {
            _main.EndOperation();
        }
    }

    /// <summary>
    /// The stored personal key, prompting for one if there isn't any. Returns null when the
    /// user cancels. butler's own credentials can't be used here — they're wharf-scoped.
    /// </summary>
    private async Task<string?> EnsureApiKeyAsync()
    {
        string stored = ApiKeyProtector.Unprotect(Config.ItchApiKeyProtected);
        if (!string.IsNullOrWhiteSpace(stored)) return stored;

        var owner = _main.OwnerWindow;
        if (owner == null) return null;

        string? entered = await new ApiKeyDialog().ShowDialog<string?>(owner);
        if (string.IsNullOrWhiteSpace(entered))
        {
            _main.AppendOutput("Sync cancelled: no API key provided.");
            return null;
        }

        string apiKey = entered.Trim();
        Config.ItchApiKeyProtected = ApiKeyProtector.Protect(apiKey);
        SaveConfig();
        if (!ApiKeyProtector.IsEncryptedAtRest)
            _main.AppendOutput("Note: API key stored unencrypted on this OS (config file restricted to your user).");
        return apiKey;
    }

    private record RemoteGame(ItchGame Game, List<ItchChannel>? Channels, string? Error);

    /// <summary>
    /// One round trip per game, and they don't depend on each other — so run a few at a time
    /// instead of paying the full latency N times over. Bounded to stay polite to itch.io.
    /// </summary>
    private async Task<List<RemoteGame>> FetchAllChannelsAsync(string apiKey, List<ItchGame> games, CancellationToken ct)
    {
        using var gate = new SemaphoreSlim(SyncFetchConcurrency);

        var fetches = games.Select(async game =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var channels = await ItchClient.FetchChannelsAsync(_http, apiKey, game.Username, game.Slug, ct);
                return new RemoteGame(game, channels, null);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new RemoteGame(game, null, ex.Message);
            }
            finally
            {
                gate.Release();
            }
        });

        // Merged in the original order afterwards, so concurrency doesn't reorder the report.
        return (await Task.WhenAll(fetches)).ToList();
    }

    private (int NewGames, int NewBuilds, int UpdatedVersions, int Skipped) MergeIntoLibrary(List<RemoteGame> fetched)
    {
        int newGames = 0, newBuilds = 0, updatedVersions = 0, skipped = 0;

        foreach (var (remote, channels, error) in fetched)
        {
            var localGame = Config.Games.FirstOrDefault(g =>
                string.Equals(g.Username, remote.Username, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(g.GameSlug, remote.Slug, StringComparison.OrdinalIgnoreCase));

            if (localGame == null)
            {
                localGame = new GameConfig
                {
                    Username = remote.Username,
                    GameSlug = remote.Slug,
                    DisplayName = remote.Title
                };
                Config.Games.Add(localGame);
                newGames++;
            }
            else if (IsUncustomizedDisplayName(localGame.DisplayName, localGame.GameSlug))
            {
                localGame.DisplayName = remote.Title;
            }

            if (channels == null)
            {
                _main.AppendOutput($"Skipped {remote.Username}/{remote.Slug}: {error}");
                skipped++;
                continue;
            }

            foreach (var channel in channels)
            {
                var localBuild = localGame.Builds.FirstOrDefault(b =>
                    string.Equals(b.Tag, channel.Name, StringComparison.OrdinalIgnoreCase));

                if (localBuild == null)
                {
                    localGame.Builds.Add(new BuildConfig
                    {
                        Tag = channel.Name,
                        Version = channel.UserVersion,
                        DisplayName = channel.Name
                    });
                    newBuilds++;
                }
                else if (!string.IsNullOrEmpty(channel.UserVersion) && localBuild.Version != channel.UserVersion)
                {
                    localBuild.Version = channel.UserVersion;
                    updatedVersions++;
                }
            }
        }

        return (newGames, newBuilds, updatedVersions, skipped);
    }

    private void PersistSyncedLibrary()
    {
        var (buildId, gameId) = CurrentSelectionIds();
        SaveAndRebuild(buildId, gameId);
    }

    /// <summary>True when the name is still a placeholder, so overwriting it loses nothing.</summary>
    private static bool IsUncustomizedDisplayName(string displayName, string slug) =>
        string.IsNullOrWhiteSpace(displayName)
        || string.Equals(displayName, slug, StringComparison.OrdinalIgnoreCase)
        || string.Equals(displayName, NewGameName, StringComparison.Ordinal);
}
