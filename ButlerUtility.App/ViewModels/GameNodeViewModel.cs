using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ItchioButlerUtility.Models;

namespace ItchioButlerUtility.ViewModels;

public partial class GameNodeViewModel : ViewModelBase
{
    private readonly LibraryViewModel _library;

    public GameConfig Game { get; }
    public List<BuildNodeViewModel> Builds { get; } = new();
    public ObservableCollection<BuildNodeViewModel> FilteredBuilds { get; } = new();

    public GameNodeViewModel(GameConfig game, LibraryViewModel library)
    {
        Game = game;
        _library = library;
        foreach (var build in game.Builds)
        {
            var node = new BuildNodeViewModel(build, this, library);
            Builds.Add(node);
            FilteredBuilds.Add(node); // nothing is filtered out until a filter is typed
        }
    }

    public string Name
    {
        get
        {
            string name = !string.IsNullOrWhiteSpace(Game.DisplayName) ? Game.DisplayName : Game.GameSlug;
            if (string.IsNullOrWhiteSpace(name)) name = "(new game)";
            return name;
        }
    }

    /// <summary>Re-reads <see cref="Name"/> after the underlying config changed.</summary>
    public void RefreshName() => OnPropertyChanged(nameof(Name));

    /// <summary>Recomputes FilteredBuilds for the given filter; returns whether this game should be shown at all.</summary>
    public bool ApplyFilter(string filter)
    {
        FilteredBuilds.Clear();
        bool noFilter = string.IsNullOrWhiteSpace(filter);
        bool gameMatches = noFilter
            || Matches(Name, filter)
            || Matches(Game.GameSlug, filter)
            || Matches(Game.Username, filter);

        foreach (var build in Builds)
        {
            if (gameMatches || Matches(build.Name, filter))
                FilteredBuilds.Add(build);
        }
        return gameMatches || FilteredBuilds.Count > 0;
    }

    private static bool Matches(string text, string filter) =>
        !string.IsNullOrEmpty(text) && text.Contains(filter, StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private void AddBuild() => _library.AddBuildTo(this);

    [RelayCommand]
    private void Duplicate() => _library.DuplicateGame(this);

    [RelayCommand]
    private Task DeleteAsync() => _library.DeleteGameAsync(this);
}
