using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using ItchioButlerUtility.ViewModels;

namespace ItchioButlerUtility.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _boundViewModel;

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            // Drop the previous subscription first — otherwise a second DataContext keeps the
            // old view model alive and every console flush posts one ScrollToEnd per binding.
            if (_boundViewModel != null)
                _boundViewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _boundViewModel = DataContext as MainWindowViewModel;
            if (_boundViewModel != null)
            {
                _boundViewModel.OwnerWindow = this;
                _boundViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.ConsoleText))
        {
            // Defer until after layout so the new line is measurable.
            Dispatcher.UIThread.Post(() => ConsoleScroll.ScrollToEnd(), DispatcherPriority.Background);
        }
    }
}
