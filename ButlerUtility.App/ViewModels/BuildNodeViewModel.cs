using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ItchioButlerUtility.Models;

namespace ItchioButlerUtility.ViewModels;

public partial class BuildNodeViewModel : ViewModelBase
{
    private readonly LibraryViewModel _library;

    public BuildConfig Build { get; }
    public GameNodeViewModel Parent { get; }

    public BuildNodeViewModel(BuildConfig build, GameNodeViewModel parent, LibraryViewModel library)
    {
        Build = build;
        Parent = parent;
        _library = library;
    }

    public string Name
    {
        get
        {
            string name = !string.IsNullOrWhiteSpace(Build.DisplayName) ? Build.DisplayName : Build.Tag;
            if (string.IsNullOrWhiteSpace(name)) name = "(no tag)";
            if (Build.Hidden) name += " (hidden)";
            return name;
        }
    }

    /// <summary>Re-reads <see cref="Name"/> after the underlying config changed.</summary>
    public void RefreshName() => OnPropertyChanged(nameof(Name));

    [RelayCommand]
    private Task PushAsync() => _library.PushBuildAsync(this);

    [RelayCommand]
    private void Duplicate() => _library.DuplicateBuild(this);

    [RelayCommand]
    private Task DeleteAsync() => _library.DeleteBuildAsync(this);
}
