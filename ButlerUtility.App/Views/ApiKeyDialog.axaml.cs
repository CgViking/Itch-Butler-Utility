using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ItchioButlerUtility.Views;

public partial class ApiKeyDialog : Window
{
    private const string KeysUrl = "https://itch.io/user/settings/api-keys";

    public ApiKeyDialog()
    {
        InitializeComponent();
    }

    private async void OpenKeysPage(object? sender, RoutedEventArgs e)
    {
        bool opened = await Launcher.LaunchUriAsync(new Uri(KeysUrl));
        if (!opened)
        {
            ErrorText.Text = $"Could not open a browser. Visit {KeysUrl} manually.";
            ErrorText.IsVisible = true;
        }
    }

    private void SaveClick(object? sender, RoutedEventArgs e)
    {
        string key = KeyBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(key))
        {
            ErrorText.Text = "Please paste an API key, or click Cancel.";
            ErrorText.IsVisible = true;
            return;
        }
        Close(key);
    }

    private void CancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
