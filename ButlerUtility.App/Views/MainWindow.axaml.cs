using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using ItchioButlerUtility.ViewModels;

namespace ItchioButlerUtility.Views;

public partial class MainWindow : Window
{
    // Height the output row is allowed to occupy while the drawer is open: the drawer
    // header, progress row and margins, plus a console between 80 and 170 tall.
    private const double OutputRowMin = 156;
    private const double OutputRowMax = 246;

    // Everything outside the output row needs 452: 28 of window margin, a 36 header, the
    // 320-tall build form, a 42 action bar and a 26 status strip. Add the row itself —
    // OutputRowMin when open, just the 42-tall drawer header when shut.
    private const double OutsideOutputRow = 452;
    private const double WindowMinOutputOpen = OutsideOutputRow + OutputRowMin;   // 608
    private const double WindowMinOutputClosed = OutsideOutputRow + 42;           // 494

    // Avalonia only generates fields for x:Name on tree elements, not on RowDefinition,
    // so the row is looked up by index. Keep in step with RootGrid.RowDefinitions.
    private const int OutputRowIndex = 3;

    private RowDefinition OutputRow => RootGrid.RowDefinitions[OutputRowIndex];

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
                ApplyOutputLayout(_boundViewModel.IsOutputVisible);
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
        else if (e.PropertyName == nameof(MainWindowViewModel.IsOutputVisible))
        {
            ApplyOutputLayout(_boundViewModel!.IsOutputVisible);
        }
    }

    /// <summary>
    /// Reshapes the output row to match the drawer. Open, it is a star row so the console
    /// can hand height back to the build form when the window is short. Shut, it has to be
    /// Auto: a star row claims its share of the window whether or not anything is in it,
    /// which would leave the collapsed drawer holding a gap above the status strip.
    /// </summary>
    private void ApplyOutputLayout(bool open)
    {
        if (open)
        {
            OutputRow.Height = new GridLength(1, GridUnitType.Star);
            OutputRow.MinHeight = OutputRowMin;
            OutputRow.MaxHeight = OutputRowMax;
        }
        else
        {
            OutputRow.Height = GridLength.Auto;
            OutputRow.MinHeight = 0;
            OutputRow.MaxHeight = double.PositiveInfinity;
        }

        MinHeight = open ? WindowMinOutputOpen : WindowMinOutputClosed;
    }
}
