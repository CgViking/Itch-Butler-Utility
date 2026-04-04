using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoUpdaterDotNET;

namespace ItchioButlerUtility
{
    public partial class MainForm : Form
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private System.Windows.Forms.Timer versionFetchTimer;
        private const int CollapsedHeight = 360;
        private const int ExpandedHeight = 555;

        private static readonly string CredentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "itch", "butler_creds");

        private static readonly string ButlerPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "butler.exe");

        public MainForm()
        {
            InitializeComponent();
            SetupToolTips();
            SetupVersionFetch();
            CheckLoginStatus();
            SetIconFromButler();
            appVersionLabel.Text = $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(2)}";
            this.Shown += (s, e) => CheckForUpdates();
        }

        private void SetupToolTips()
        {
            toolTip.SetToolTip(pathBox, "Enter the folder path where your game files are located.");
            toolTip.SetToolTip(usernameBox, "Enter your username for the Butler login.");
            toolTip.SetToolTip(gameNameBox, "Enter the name of your game. Has to be the same name as seen in the URL of the game page");
            toolTip.SetToolTip(tagBox, "Enter the tag for the platform (e.g., win-64, linux). Optional.");
            toolTip.SetToolTip(versionBox, "Optional version number (e.g., 1.0.0). Leave empty to skip.");
            toolTip.SetToolTip(pushButton, "Click to do a butler push command.");
            toolTip.SetToolTip(loginButton, "Click to log in with Butler using the provided username.");
            toolTip.SetToolTip(generateButton, "Click to generate a batch file for the Butler command.");
        }

        private void SetupVersionFetch()
        {
            versionFetchTimer = new System.Windows.Forms.Timer();
            versionFetchTimer.Interval = 500;
            versionFetchTimer.Tick += (s, ev) => { versionFetchTimer.Stop(); _ = FetchLatestVersion(); };

            usernameBox.TextChanged += (s, ev) => RestartVersionFetchTimer();
            gameNameBox.TextChanged += (s, ev) => RestartVersionFetchTimer();
            tagBox.TextChanged += (s, ev) => RestartVersionFetchTimer();
        }

        private void CheckForUpdates()
        {
            updateLabel.Text = "Checking for updates...";
            updateLabel.ForeColor = System.Drawing.SystemColors.ControlText;

            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;

            bool isInstalled;
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{8D7EB7C8-2639-45B7-8676-70F0BC01498B}_is1"))
            {
                isInstalled = key != null;
            }

            string updateUrl = isInstalled
                ? "https://raw.githubusercontent.com/CgViking/Butler-Utility/main/updater-installed.xml"
                : "https://raw.githubusercontent.com/CgViking/Butler-Utility/main/updater-portable.xml";

            AutoUpdater.Start(updateUrl);
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;

            void UpdateUI(string text, System.Drawing.Color color)
            {
                updateLabel.Text = text;
                updateLabel.ForeColor = color;
            }

            if (args == null || args.Error != null)
            {
                Action action = () => UpdateUI("Update check failed", System.Drawing.Color.Red);
                if (InvokeRequired) Invoke(action); else action();
                return;
            }

            if (args.IsUpdateAvailable)
            {
                Action action = () => UpdateUI("Update available!", System.Drawing.Color.Orange);
                if (InvokeRequired) Invoke(action); else action();
            }
            else
            {
                Action action = () => UpdateUI("Up to date", System.Drawing.Color.Green);
                if (InvokeRequired) Invoke(action); else action();
            }
        }

        private void SetIconFromButler()
        {
            string butlerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "butler.exe");
            if (File.Exists(butlerPath))
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(butlerPath);
        }

        private void CheckLoginStatus()
        {
            if (File.Exists(CredentialsPath))
            {
                statusLabel.Text = "Status: Logged in";
                statusLabel.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                statusLabel.Text = "Status: Not logged in";
                statusLabel.ForeColor = System.Drawing.Color.Red;
            }
        }

        private void RestartVersionFetchTimer()
        {
            versionFetchTimer.Stop();
            versionFetchTimer.Start();
        }

        private async Task FetchLatestVersion()
        {
            string username = usernameBox.Text;
            string gameName = gameNameBox.Text;
            string tag = tagBox.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(tag))
            {
                versionHintLabel.Text = "";
                return;
            }

            try
            {
                string url = $"https://itch.io/api/1/x/wharf/latest?target={Uri.EscapeDataString(username)}/{Uri.EscapeDataString(gameName)}&channel_name={Uri.EscapeDataString(tag)}";
                string response = await httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("latest", out var latest))
                    versionHintLabel.Text = $"Latest: {latest.GetString()}";
                else
                    versionHintLabel.Text = "No version history";
            }
            catch
            {
                versionHintLabel.Text = "";
            }
        }

        private void ShowOutputPanel()
        {
            outputBox.Clear();
            outputBox.Visible = true;
            progressBar.Visible = true;
            progressBar.Value = 0;
            this.ClientSize = new System.Drawing.Size(535, ExpandedHeight);
        }

        private void AppendOutput(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (outputBox.InvokeRequired)
                outputBox.Invoke(new Action(() => AppendOutput(text)));
            else
                outputBox.AppendText(text + Environment.NewLine);
        }

        private void UpdateProgress(string text)
        {
            var match = Regex.Match(text, @"(\d+(?:\.\d+)?)%");
            if (!match.Success) return;
            int percent = (int)double.Parse(match.Groups[1].Value);
            if (percent > 100) percent = 100;
            if (progressBar.InvokeRequired)
                progressBar.Invoke(new Action(() => progressBar.Value = percent));
            else
                progressBar.Value = percent;
        }

        private async void PushButton_Click(object sender, EventArgs e)
        {
            string path = pathBox.Text;
            string username = usernameBox.Text;
            string gameName = gameNameBox.Text;
            string tag = tagBox.Text;
            string version = versionBox.Text;

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(gameName))
            {
                MessageBox.Show("Please fill in the build folder path, username, and game name.", "Missing Fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string target = string.IsNullOrWhiteSpace(tag) ? $"{username}/{gameName}" : $"{username}/{gameName}:{tag}";
            string arguments = $"push \"{path}\" {target}";
            if (!string.IsNullOrWhiteSpace(version))
                arguments += $" --userversion {version}";

            ShowOutputPanel();
            pushButton.Enabled = false;

            try
            {
                int exitCode = await Task.Run(() =>
                {
                    using var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = ButlerPath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += (s, ev) =>
                    {
                        if (ev.Data == null) return;
                        AppendOutput(ev.Data);
                        UpdateProgress(ev.Data);
                    };
                    process.ErrorDataReceived += (s, ev) =>
                    {
                        if (ev.Data == null) return;
                        AppendOutput(ev.Data);
                        UpdateProgress(ev.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    return process.ExitCode;
                });

                if (exitCode == 0)
                {
                    progressBar.Value = 100;
                    AppendOutput("Push completed successfully.");
                }
                else
                {
                    AppendOutput($"Push failed with exit code {exitCode}.");
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"Error: {ex.Message}");
            }
            finally
            {
                pushButton.Enabled = true;
            }
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            string path = pathBox.Text;
            string username = usernameBox.Text;
            string gameName = gameNameBox.Text;
            string tag = tagBox.Text;
            string version = versionBox.Text;

            string target = string.IsNullOrWhiteSpace(tag) ? $"{username}/{gameName}" : $"{username}/{gameName}:{tag}";
            string versionArg = string.IsNullOrWhiteSpace(version) ? "" : $" --userversion {version}";
            string batchContent = $"@echo off\nbutler push \"{path}\" {target}{versionArg}\necho Push command executed.\npause";

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Batch files (*.bat)|*.bat|All files (*.*)|*.*";
                saveFileDialog.Title = "Save Batch File";
                saveFileDialog.FileName = "butler_push.bat";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveFileDialog.FileName, batchContent);
                        MessageBox.Show("Batch file created successfully.", "Success", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            DialogResult result = this.folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
                this.pathBox.Text = this.folderBrowserDialog.SelectedPath;
        }

        private async void LoginButton_Click(object sender, EventArgs e)
        {
            string username = usernameBox.Text;
            string arguments = $"login {username}";

            loginButton.Enabled = false;

            try
            {
                int exitCode = await Task.Run(() =>
                {
                    using var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = ButlerPath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = false;

                    process.Start();
                    process.WaitForExit();
                    return process.ExitCode;
                });

                if (exitCode == 0)
                {
                    MessageBox.Show("Login successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CheckLoginStatus();
                }
                else
                {
                    MessageBox.Show($"Login failed with exit code {exitCode}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                loginButton.Enabled = true;
            }
        }
    }
}