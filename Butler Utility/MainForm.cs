using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
        private const int CollapsedHeight = 430;
        private const int ExpandedHeight = 625;
        private const int ClientWidth = 770;
        private const int ExpandedWidth = 770;
        private const int CollapsedWidth = 540;

        private static readonly string CredentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "itch", "butler_creds");

        private static readonly string ButlerPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "butler.exe");

        private AppConfig config = new AppConfig();
        private GameConfig selectedGame;
        private BuildConfig selectedBuild;

        public MainForm()
        {
            InitializeComponent();
            SetupToolTips();
            SetupVersionFetch();
            CheckLoginStatus();
            SetIconFromButler();
            appVersionLabel.Text = $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(2)}";
            LoadConfigIntoTree();
            gamesTree.AfterSelect += GamesTree_AfterSelect;
            UpdateLibraryButtons();
            sidebarToggle.Click += (s, e) => ToggleLibrary();
            toolTip.SetToolTip(sidebarToggle, "Show or hide the saved-games sidebar.");
            ApplyLibraryCollapsed(config.LibraryCollapsed);
            this.Shown += (s, e) => CheckForUpdates();
        }

        private void SetupToolTips()
        {
            toolTip.SetToolTip(pathBox, "Enter the folder path where your game files are located.");
            toolTip.SetToolTip(usernameBox, "Itch.io username (lives on the Game — change here cascades to every build under it).");
            toolTip.SetToolTip(gameNameBox, "Game slug exactly as it appears in the URL of the game page.");
            toolTip.SetToolTip(tagBox, "Channel tag (e.g., win-64, linux, win-beta). Optional.");
            toolTip.SetToolTip(versionBox, "Optional version number (e.g., 1.0.0). Leave empty to skip.");
            toolTip.SetToolTip(ifChangedCheck, "Adds --if-changed: butler skips the upload if contents match the latest build.");
            toolTip.SetToolTip(hiddenCheck, "Adds --hidden: new channel stays hidden on the game page. Great for betas / dev pushes.");
            toolTip.SetToolTip(ignoreBox, "Comma-separated patterns. Each becomes a --ignore flag (e.g., *.pdb,*.dSYM).");
            toolTip.SetToolTip(pushButton, "Run butler push with the current fields and flags.");
            toolTip.SetToolTip(saveBuildButton, "Save the form contents into the selected library entry.");
            toolTip.SetToolTip(generateButton, "Write a .bat file equivalent to the current Push command.");
            toolTip.SetToolTip(validateButton, "Run butler validate on the build folder.");
            toolTip.SetToolTip(statusButton, "Run butler status for the current user/game.");
            toolTip.SetToolTip(loginButton, "Log in with butler using the provided username.");
            toolTip.SetToolTip(addGameButton, "Add a new game entry.");
            toolTip.SetToolTip(addBuildButton, "Add a new build under the selected game.");
            toolTip.SetToolTip(duplicateButton, "Duplicate the selected game or build.");
            toolTip.SetToolTip(deleteButton, "Delete the selected game or build.");
            toolTip.SetToolTip(syncButton, "Fetch your games and channels from itch.io. Local paths and flags are preserved.");
        }

        private void ToggleLibrary()
        {
            config.LibraryCollapsed = !config.LibraryCollapsed;
            ApplyLibraryCollapsed(config.LibraryCollapsed);
            ConfigStore.Save(config);
        }

        private void ApplyLibraryCollapsed(bool collapsed)
        {
            libraryGroup.Visible = !collapsed;
            gamesTree.Visible = !collapsed;
            syncButton.Visible = !collapsed;
            addGameButton.Visible = !collapsed;
            addBuildButton.Visible = !collapsed;
            duplicateButton.Visible = !collapsed;
            deleteButton.Visible = !collapsed;

            sidebarToggle.Text = collapsed ? ">>" : "<<";

            int contentWidth = collapsed ? 520 : 750;
            progressBar.Size = new System.Drawing.Size(contentWidth, progressBar.Size.Height);
            progressStatusLabel.Size = new System.Drawing.Size(contentWidth, progressStatusLabel.Size.Height);
            outputBox.Size = new System.Drawing.Size(contentWidth, outputBox.Size.Height);

            int newWidth = collapsed ? CollapsedWidth : ExpandedWidth;
            this.ClientSize = new System.Drawing.Size(newWidth, this.ClientSize.Height);
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
                ? "https://raw.githubusercontent.com/CgViking/Itch.io-Butler-Utility/main/updater-installed.xml"
                : "https://raw.githubusercontent.com/CgViking/Itch.io-Butler-Utility/main/updater-portable.xml";

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

                try
                {
                    if (AutoUpdater.DownloadUpdate(args))
                    {
                        Application.Exit();
                    }
                }
                catch (Exception)
                {
                    Action errorAction = () => UpdateUI("Update failed", System.Drawing.Color.Red);
                    if (InvokeRequired) Invoke(errorAction); else errorAction();
                }
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
            bool loggedIn = File.Exists(CredentialsPath);
            if (loggedIn)
            {
                statusLabel.Text = "Status: Logged in";
                statusLabel.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                statusLabel.Text = "Status: Not logged in";
                statusLabel.ForeColor = System.Drawing.Color.Red;
            }
            syncButton.Enabled = loggedIn;
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

        // --- Library / tree ---

        private void LoadConfigIntoTree()
        {
            config = ConfigStore.Load();
            if (!string.IsNullOrEmpty(ConfigStore.LastLoadError))
            {
                ShowOutputPanel();
                AppendOutput(ConfigStore.LastLoadError);
                AppendOutput("Starting with an empty library. Fix the file or re-add entries.");
            }
            RebuildTree(config.LastSelectedBuildId);
        }

        private void RebuildTree(string selectBuildId)
        {
            gamesTree.BeginUpdate();
            gamesTree.Nodes.Clear();
            TreeNode toSelect = null;
            foreach (var game in config.Games)
            {
                var gameNode = new TreeNode(GameNodeText(game)) { Tag = game };
                foreach (var build in game.Builds)
                {
                    var buildNode = new TreeNode(BuildNodeText(build)) { Tag = build };
                    gameNode.Nodes.Add(buildNode);
                    if (!string.IsNullOrEmpty(selectBuildId) && build.Id == selectBuildId)
                        toSelect = buildNode;
                }
                gameNode.Expand();
                gamesTree.Nodes.Add(gameNode);
            }
            if (toSelect != null) gamesTree.SelectedNode = toSelect;
            gamesTree.EndUpdate();
            UpdateLibraryButtons();
        }

        private static string GameNodeText(GameConfig g)
        {
            string name = !string.IsNullOrWhiteSpace(g.DisplayName) ? g.DisplayName : g.GameSlug;
            if (string.IsNullOrWhiteSpace(name)) name = "(new game)";
            return name;
        }

        private static string BuildNodeText(BuildConfig b)
        {
            string name = !string.IsNullOrWhiteSpace(b.DisplayName) ? b.DisplayName : b.Tag;
            if (string.IsNullOrWhiteSpace(name)) name = "(no tag)";
            if (b.Hidden) name += " (hidden)";
            return name;
        }

        private GameConfig FindParentGame(BuildConfig build)
        {
            return config.Games.FirstOrDefault(g => g.Builds.Contains(build));
        }

        private void GamesTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is BuildConfig build)
            {
                selectedBuild = build;
                selectedGame = FindParentGame(build);
                LoadBuildIntoForm(selectedGame, build);
            }
            else if (e.Node?.Tag is GameConfig game)
            {
                selectedBuild = null;
                selectedGame = game;
                LoadGameIntoForm(game);
            }
            else
            {
                selectedBuild = null;
                selectedGame = null;
            }
            config.LastSelectedBuildId = selectedBuild?.Id ?? "";
            UpdateLibraryButtons();
        }

        private void LoadGameIntoForm(GameConfig game)
        {
            usernameBox.Text = game.Username;
            gameNameBox.Text = game.GameSlug;
            pathBox.Text = "";
            tagBox.Text = "";
            versionBox.Text = "";
            ifChangedCheck.Checked = false;
            hiddenCheck.Checked = false;
            ignoreBox.Text = "";
        }

        private void LoadBuildIntoForm(GameConfig game, BuildConfig build)
        {
            usernameBox.Text = game?.Username ?? "";
            gameNameBox.Text = game?.GameSlug ?? "";
            pathBox.Text = build.Path;
            tagBox.Text = build.Tag;
            versionBox.Text = build.Version;
            ifChangedCheck.Checked = build.IfChanged;
            hiddenCheck.Checked = build.Hidden;
            ignoreBox.Text = string.Join(", ", build.IgnorePatterns);
        }

        private void UpdateLibraryButtons()
        {
            bool hasSelection = selectedGame != null || selectedBuild != null;
            addBuildButton.Enabled = selectedGame != null || selectedBuild != null;
            duplicateButton.Enabled = hasSelection;
            deleteButton.Enabled = hasSelection;
            saveBuildButton.Enabled = hasSelection;
        }

        private void AddGameButton_Click(object sender, EventArgs e)
        {
            var game = new GameConfig { DisplayName = "New game" };
            config.Games.Add(game);
            ConfigStore.Save(config);
            RebuildTree(null);
            SelectGame(game);
        }

        private void AddBuildButton_Click(object sender, EventArgs e)
        {
            var game = selectedGame ?? (selectedBuild != null ? FindParentGame(selectedBuild) : null);
            if (game == null) return;
            var build = new BuildConfig { Tag = "win" };
            game.Builds.Add(build);
            ConfigStore.Save(config);
            RebuildTree(build.Id);
        }

        private void DuplicateButton_Click(object sender, EventArgs e)
        {
            if (selectedBuild != null)
            {
                var game = FindParentGame(selectedBuild);
                if (game == null) return;
                var copy = new BuildConfig
                {
                    DisplayName = selectedBuild.DisplayName,
                    Path = selectedBuild.Path,
                    Tag = selectedBuild.Tag + "-copy",
                    Version = selectedBuild.Version,
                    Hidden = selectedBuild.Hidden,
                    IfChanged = selectedBuild.IfChanged,
                    IgnorePatterns = new List<string>(selectedBuild.IgnorePatterns)
                };
                game.Builds.Add(copy);
                ConfigStore.Save(config);
                RebuildTree(copy.Id);
            }
            else if (selectedGame != null)
            {
                var copy = new GameConfig
                {
                    DisplayName = selectedGame.DisplayName + " (copy)",
                    Username = selectedGame.Username,
                    GameSlug = selectedGame.GameSlug,
                    Builds = selectedGame.Builds.Select(b => new BuildConfig
                    {
                        DisplayName = b.DisplayName,
                        Path = b.Path,
                        Tag = b.Tag,
                        Version = b.Version,
                        Hidden = b.Hidden,
                        IfChanged = b.IfChanged,
                        IgnorePatterns = new List<string>(b.IgnorePatterns)
                    }).ToList()
                };
                config.Games.Add(copy);
                ConfigStore.Save(config);
                RebuildTree(null);
                SelectGame(copy);
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (selectedBuild != null)
            {
                var game = FindParentGame(selectedBuild);
                if (game == null) return;
                var label = BuildNodeText(selectedBuild);
                if (MessageBox.Show($"Delete build '{label}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                game.Builds.Remove(selectedBuild);
                selectedBuild = null;
                ConfigStore.Save(config);
                RebuildTree(null);
            }
            else if (selectedGame != null)
            {
                var label = GameNodeText(selectedGame);
                if (MessageBox.Show($"Delete game '{label}' and all its builds?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                config.Games.Remove(selectedGame);
                selectedGame = null;
                ConfigStore.Save(config);
                RebuildTree(null);
            }
        }

        private void SelectGame(GameConfig game)
        {
            foreach (TreeNode node in gamesTree.Nodes)
            {
                if (node.Tag == game)
                {
                    gamesTree.SelectedNode = node;
                    return;
                }
            }
        }

        private void SaveBuildButton_Click(object sender, EventArgs e)
        {
            if (selectedBuild == null && selectedGame == null)
            {
                MessageBox.Show("Select a game or build in the sidebar first, or click '+ Game' to start a new one.", "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var game = selectedGame ?? FindParentGame(selectedBuild);
            if (game != null)
            {
                game.Username = usernameBox.Text.Trim();
                game.GameSlug = gameNameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(game.DisplayName) || game.DisplayName == "New game")
                    game.DisplayName = game.GameSlug;
            }

            if (selectedBuild != null)
            {
                selectedBuild.Path = pathBox.Text;
                selectedBuild.Tag = tagBox.Text.Trim();
                selectedBuild.Version = versionBox.Text.Trim();
                selectedBuild.Hidden = hiddenCheck.Checked;
                selectedBuild.IfChanged = ifChangedCheck.Checked;
                selectedBuild.IgnorePatterns = ParseIgnorePatterns(ignoreBox.Text);
                if (string.IsNullOrWhiteSpace(selectedBuild.DisplayName))
                    selectedBuild.DisplayName = selectedBuild.Tag;
            }

            ConfigStore.Save(config);
            RebuildTree(selectedBuild?.Id);
            saveBuildButton.Text = "Saved ✓";
            var revert = new System.Windows.Forms.Timer { Interval = 1200 };
            revert.Tick += (s, ev) => { saveBuildButton.Text = "Save to library"; revert.Stop(); revert.Dispose(); };
            revert.Start();
        }

        private static List<string> ParseIgnorePatterns(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return text.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
        }

        // --- Sync from itch.io ---

        private async void SyncButton_Click(object sender, EventArgs e)
        {
            syncButton.Enabled = false;
            ShowOutputPanel();

            string apiKey = config.ItchApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                using var dlg = new ApiKeyDialog(apiKey);
                var result = dlg.ShowDialog(this);
                if (result != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.ApiKey))
                {
                    AppendOutput("Sync cancelled: no API key provided.");
                    syncButton.Enabled = File.Exists(CredentialsPath);
                    return;
                }
                apiKey = dlg.ApiKey;
                config.ItchApiKey = apiKey;
                ConfigStore.Save(config);
            }

            AppendOutput("Syncing from itch.io...");

            int newGames = 0, newBuilds = 0, updatedVersions = 0, skipped = 0;

            try
            {
                var remoteGames = await ItchClient.FetchGamesAsync(httpClient, apiKey, default);
                AppendOutput($"Found {remoteGames.Count} game(s) on itch.io.");

                foreach (var rg in remoteGames)
                {
                    var localGame = config.Games.FirstOrDefault(g =>
                        string.Equals(g.Username, rg.Username, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(g.GameSlug, rg.Slug, StringComparison.OrdinalIgnoreCase));

                    if (localGame == null)
                    {
                        localGame = new GameConfig
                        {
                            Username = rg.Username,
                            GameSlug = rg.Slug,
                            DisplayName = rg.Title
                        };
                        config.Games.Add(localGame);
                        newGames++;
                    }
                    else if (IsUncustomizedDisplayName(localGame.DisplayName, localGame.GameSlug))
                    {
                        localGame.DisplayName = rg.Title;
                    }

                    List<ItchChannel> channels;
                    try
                    {
                        channels = await ItchClient.FetchChannelsAsync(httpClient, apiKey, rg.Username, rg.Slug, default);
                    }
                    catch (Exception ex)
                    {
                        AppendOutput($"Skipped {rg.Username}/{rg.Slug}: {ex.Message}");
                        skipped++;
                        continue;
                    }

                    foreach (var rc in channels)
                    {
                        var localBuild = localGame.Builds.FirstOrDefault(b =>
                            string.Equals(b.Tag, rc.Name, StringComparison.OrdinalIgnoreCase));

                        if (localBuild == null)
                        {
                            localGame.Builds.Add(new BuildConfig
                            {
                                Tag = rc.Name,
                                Version = rc.UserVersion ?? "",
                                DisplayName = rc.Name
                            });
                            newBuilds++;
                        }
                        else if (!string.IsNullOrEmpty(rc.UserVersion) && localBuild.Version != rc.UserVersion)
                        {
                            localBuild.Version = rc.UserVersion;
                            updatedVersions++;
                        }
                    }
                }

                ConfigStore.Save(config);
                RebuildTree(selectedBuild?.Id);

                AppendOutput($"Sync complete: {newGames} new game(s), {newBuilds} new build(s), {updatedVersions} version(s) refreshed" +
                             (skipped > 0 ? $", {skipped} skipped." : "."));
            }
            catch (Exception ex)
            {
                AppendOutput($"Sync failed: {ex.Message}");
                if (ex.Message.Contains(" 401") || ex.Message.Contains(" 403"))
                {
                    config.ItchApiKey = "";
                    ConfigStore.Save(config);
                    AppendOutput("Cleared stored API key. Click Sync again to enter a new one.");
                }
            }
            finally
            {
                syncButton.Enabled = File.Exists(CredentialsPath);
            }
        }

        private static bool IsUncustomizedDisplayName(string displayName, string slug)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return true;
            if (string.Equals(displayName, slug, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(displayName, "New game", StringComparison.Ordinal)) return true;
            return false;
        }

        // --- Output panel + butler process plumbing ---

        private static readonly Regex ProgressRegex = new Regex(
            @"(\d+(?:\.\d+)?)%\s*(?:[-@]\s*(.*?))?\s*$",
            RegexOptions.Compiled);

        private void ShowOutputPanel()
        {
            outputBox.Clear();
            outputBox.Visible = true;
            progressBar.Visible = true;
            progressBar.Value = 0;
            progressStatusLabel.Visible = true;
            progressStatusLabel.Text = "";
            this.ClientSize = new System.Drawing.Size(this.ClientSize.Width, ExpandedHeight);
        }

        private static bool IsProgressLine(string text)
        {
            return text.IndexOf('▌') >= 0;
        }

        private void HandleOutputLine(string text)
        {
            if (text == null) return;
            if (IsProgressLine(text))
            {
                UpdateProgressFromBar(text);
                return;
            }
            AppendOutput(text);
        }

        private void AppendOutput(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (outputBox.InvokeRequired)
                outputBox.Invoke(new Action(() => AppendOutput(text)));
            else
                outputBox.AppendText(text + Environment.NewLine);
        }

        private void UpdateProgressFromBar(string text)
        {
            var match = ProgressRegex.Match(text);
            if (!match.Success) return;

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double percentD))
                return;

            int percent = (int)Math.Min(100, Math.Max(0, percentD));
            string detail = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "";
            string statusText = detail.Length > 0
                ? $"{percentD.ToString("0.0", CultureInfo.InvariantCulture)}%  —  {detail}"
                : $"{percentD.ToString("0.0", CultureInfo.InvariantCulture)}%";

            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action(() =>
                {
                    progressBar.Value = percent;
                    progressStatusLabel.Text = statusText;
                }));
            }
            else
            {
                progressBar.Value = percent;
                progressStatusLabel.Text = statusText;
            }
        }

        private Task<int> RunButlerAsync(string arguments)
        {
            return Task.Run(() =>
            {
                using var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = ButlerPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.StartInfo.CreateNoWindow = true;

                process.OutputDataReceived += (s, ev) => HandleOutputLine(ev.Data);
                process.ErrorDataReceived += (s, ev) => HandleOutputLine(ev.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode;
            });
        }

        // --- Push / generate / validate / status ---

        private string BuildPushArguments(string path, string username, string gameName, string tag, string version,
            bool hidden, bool ifChanged, IEnumerable<string> ignorePatterns)
        {
            string target = string.IsNullOrWhiteSpace(tag) ? $"{username}/{gameName}" : $"{username}/{gameName}:{tag}";
            var sb = new StringBuilder();
            sb.Append($"push \"{path}\" {target}");
            if (!string.IsNullOrWhiteSpace(version))
                sb.Append($" --userversion {version}");
            if (ifChanged) sb.Append(" --if-changed");
            if (hidden) sb.Append(" --hidden");
            foreach (var raw in ignorePatterns)
            {
                var pattern = raw.Trim();
                if (pattern.Length == 0) continue;
                sb.Append($" --ignore \"{pattern}\"");
            }
            return sb.ToString();
        }

        private async void PushButton_Click(object sender, EventArgs e)
        {
            string path = pathBox.Text;
            string username = usernameBox.Text.Trim();
            string gameName = gameNameBox.Text.Trim();
            string tag = tagBox.Text.Trim();
            string version = versionBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(gameName))
            {
                MessageBox.Show("Please fill in the build folder path, username, and game name.", "Missing Fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string arguments = BuildPushArguments(path, username, gameName, tag, version,
                hiddenCheck.Checked, ifChangedCheck.Checked, ParseIgnorePatterns(ignoreBox.Text));

            ShowOutputPanel();
            pushButton.Enabled = false;

            try
            {
                int exitCode = await RunButlerAsync(arguments);
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
            string username = usernameBox.Text.Trim();
            string gameName = gameNameBox.Text.Trim();
            string tag = tagBox.Text.Trim();
            string version = versionBox.Text.Trim();

            string commandTail = BuildPushArguments(path, username, gameName, tag, version,
                hiddenCheck.Checked, ifChangedCheck.Checked, ParseIgnorePatterns(ignoreBox.Text));
            // commandTail starts with "push ..."; turn into "butler ..."
            string batchContent = $"@echo off\nbutler {commandTail}\necho Push command executed.\npause";

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

        private async void ValidateButton_Click(object sender, EventArgs e)
        {
            string path = pathBox.Text;
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("Pick a build folder first.", "Missing path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowOutputPanel();
            validateButton.Enabled = false;
            try
            {
                int exitCode = await RunButlerAsync($"validate \"{path}\"");
                AppendOutput(exitCode == 0 ? "Validate OK." : $"Validate failed with exit code {exitCode}.");
            }
            catch (Exception ex)
            {
                AppendOutput($"Error: {ex.Message}");
            }
            finally
            {
                validateButton.Enabled = true;
            }
        }

        private async void StatusButton_Click(object sender, EventArgs e)
        {
            string username = usernameBox.Text.Trim();
            string gameName = gameNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(gameName))
            {
                MessageBox.Show("Fill in username and game name first.", "Missing target", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowOutputPanel();
            statusButton.Enabled = false;
            try
            {
                int exitCode = await RunButlerAsync($"status {username}/{gameName}");
                if (exitCode != 0) AppendOutput($"Status failed with exit code {exitCode}.");
            }
            catch (Exception ex)
            {
                AppendOutput($"Error: {ex.Message}");
            }
            finally
            {
                statusButton.Enabled = true;
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
            string username = usernameBox.Text.Trim();
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
