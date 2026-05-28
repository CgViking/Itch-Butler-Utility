namespace ItchioButlerUtility
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                versionFetchTimer?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.buildGroup = new System.Windows.Forms.GroupBox();
            this.libraryGroup = new System.Windows.Forms.GroupBox();
            this.sidebarToggle = new System.Windows.Forms.Button();
            this.gamesTree = new System.Windows.Forms.TreeView();
            this.addGameButton = new System.Windows.Forms.Button();
            this.addBuildButton = new System.Windows.Forms.Button();
            this.duplicateButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.syncButton = new System.Windows.Forms.Button();
            this.pathLabel = new System.Windows.Forms.Label();
            this.pathBox = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.usernameLabel = new System.Windows.Forms.Label();
            this.usernameBox = new System.Windows.Forms.TextBox();
            this.gameNameLabel = new System.Windows.Forms.Label();
            this.gameNameBox = new System.Windows.Forms.TextBox();
            this.tagLabel = new System.Windows.Forms.Label();
            this.tagBox = new System.Windows.Forms.TextBox();
            this.versionLabel = new System.Windows.Forms.Label();
            this.versionBox = new System.Windows.Forms.TextBox();
            this.versionHintLabel = new System.Windows.Forms.Label();
            this.ifChangedCheck = new System.Windows.Forms.CheckBox();
            this.hiddenCheck = new System.Windows.Forms.CheckBox();
            this.ignoreLabel = new System.Windows.Forms.Label();
            this.ignoreBox = new System.Windows.Forms.TextBox();
            this.pushButton = new System.Windows.Forms.Button();
            this.saveBuildButton = new System.Windows.Forms.Button();
            this.generateButton = new System.Windows.Forms.Button();
            this.validateButton = new System.Windows.Forms.Button();
            this.statusButton = new System.Windows.Forms.Button();
            this.loginButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.updateLabel = new System.Windows.Forms.Label();
            this.appVersionLabel = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressStatusLabel = new System.Windows.Forms.Label();
            this.outputBox = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // buildGroup
            //
            this.buildGroup.Location = new System.Drawing.Point(5, 5);
            this.buildGroup.Name = "buildGroup";
            this.buildGroup.Size = new System.Drawing.Size(490, 350);
            this.buildGroup.Text = "Build details";
            this.buildGroup.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.buildGroup.TabStop = false;
            //
            // libraryGroup
            //
            this.libraryGroup.Location = new System.Drawing.Point(535, 5);
            this.libraryGroup.Name = "libraryGroup";
            this.libraryGroup.Size = new System.Drawing.Size(230, 355);
            this.libraryGroup.Text = "Saved games";
            this.libraryGroup.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.libraryGroup.TabStop = false;
            //
            // sidebarToggle
            //
            this.sidebarToggle.Location = new System.Drawing.Point(500, 14);
            this.sidebarToggle.Name = "sidebarToggle";
            this.sidebarToggle.Size = new System.Drawing.Size(30, 40);
            this.sidebarToggle.Text = "<<";
            this.sidebarToggle.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.sidebarToggle.UseVisualStyleBackColor = true;
            //
            // gamesTree
            //
            this.gamesTree.Location = new System.Drawing.Point(540, 28);
            this.gamesTree.Name = "gamesTree";
            this.gamesTree.Size = new System.Drawing.Size(220, 230);
            this.gamesTree.HideSelection = false;
            //
            // addGameButton
            //
            this.addGameButton.Location = new System.Drawing.Point(540, 296);
            this.addGameButton.Name = "addGameButton";
            this.addGameButton.Size = new System.Drawing.Size(108, 26);
            this.addGameButton.Text = "Add Game";
            this.addGameButton.Click += new System.EventHandler(this.AddGameButton_Click);
            //
            // addBuildButton
            //
            this.addBuildButton.Location = new System.Drawing.Point(652, 296);
            this.addBuildButton.Name = "addBuildButton";
            this.addBuildButton.Size = new System.Drawing.Size(108, 26);
            this.addBuildButton.Text = "Add Build";
            this.addBuildButton.Click += new System.EventHandler(this.AddBuildButton_Click);
            //
            // duplicateButton
            //
            this.duplicateButton.Location = new System.Drawing.Point(540, 327);
            this.duplicateButton.Name = "duplicateButton";
            this.duplicateButton.Size = new System.Drawing.Size(108, 26);
            this.duplicateButton.Text = "Duplicate";
            this.duplicateButton.Click += new System.EventHandler(this.DuplicateButton_Click);
            //
            // deleteButton
            //
            this.deleteButton.Location = new System.Drawing.Point(652, 327);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(108, 26);
            this.deleteButton.Text = "Delete";
            this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            //
            // syncButton
            //
            this.syncButton.Location = new System.Drawing.Point(540, 263);
            this.syncButton.Name = "syncButton";
            this.syncButton.Size = new System.Drawing.Size(220, 28);
            this.syncButton.Text = "Sync from itch.io";
            this.syncButton.Click += new System.EventHandler(this.SyncButton_Click);
            //
            // pathLabel
            //
            this.pathLabel.Location = new System.Drawing.Point(10, 20);
            this.pathLabel.Name = "pathLabel";
            this.pathLabel.Size = new System.Drawing.Size(110, 23);
            this.pathLabel.Text = "Build Folder Path:";
            //
            // pathBox
            //
            this.pathBox.Location = new System.Drawing.Point(120, 17);
            this.pathBox.Name = "pathBox";
            this.pathBox.Size = new System.Drawing.Size(295, 23);
            //
            // browseButton
            //
            this.browseButton.Location = new System.Drawing.Point(420, 17);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(75, 23);
            this.browseButton.Text = "Browse";
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            //
            // usernameLabel
            //
            this.usernameLabel.Location = new System.Drawing.Point(10, 55);
            this.usernameLabel.Name = "usernameLabel";
            this.usernameLabel.Size = new System.Drawing.Size(110, 23);
            this.usernameLabel.Text = "Username:";
            //
            // usernameBox
            //
            this.usernameBox.Location = new System.Drawing.Point(120, 52);
            this.usernameBox.Name = "usernameBox";
            this.usernameBox.Size = new System.Drawing.Size(375, 23);
            //
            // gameNameLabel
            //
            this.gameNameLabel.Location = new System.Drawing.Point(10, 90);
            this.gameNameLabel.Name = "gameNameLabel";
            this.gameNameLabel.Size = new System.Drawing.Size(110, 23);
            this.gameNameLabel.Text = "Game Name:";
            //
            // gameNameBox
            //
            this.gameNameBox.Location = new System.Drawing.Point(120, 87);
            this.gameNameBox.Name = "gameNameBox";
            this.gameNameBox.Size = new System.Drawing.Size(375, 23);
            //
            // tagLabel
            //
            this.tagLabel.Location = new System.Drawing.Point(10, 125);
            this.tagLabel.Name = "tagLabel";
            this.tagLabel.Size = new System.Drawing.Size(110, 23);
            this.tagLabel.Text = "Tag (optional):";
            //
            // tagBox
            //
            this.tagBox.Location = new System.Drawing.Point(120, 122);
            this.tagBox.Name = "tagBox";
            this.tagBox.Size = new System.Drawing.Size(375, 23);
            //
            // versionLabel
            //
            this.versionLabel.Location = new System.Drawing.Point(10, 160);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(110, 23);
            this.versionLabel.Text = "Version (optional):";
            //
            // versionBox
            //
            this.versionBox.Location = new System.Drawing.Point(120, 157);
            this.versionBox.Name = "versionBox";
            this.versionBox.Size = new System.Drawing.Size(150, 23);
            //
            // versionHintLabel
            //
            this.versionHintLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.versionHintLabel.Location = new System.Drawing.Point(275, 160);
            this.versionHintLabel.Name = "versionHintLabel";
            this.versionHintLabel.Size = new System.Drawing.Size(220, 23);
            this.versionHintLabel.Text = "";
            //
            // ifChangedCheck
            //
            this.ifChangedCheck.Location = new System.Drawing.Point(120, 188);
            this.ifChangedCheck.Name = "ifChangedCheck";
            this.ifChangedCheck.Size = new System.Drawing.Size(375, 20);
            this.ifChangedCheck.Text = "Skip if unchanged (--if-changed)";
            this.ifChangedCheck.UseVisualStyleBackColor = true;
            //
            // hiddenCheck
            //
            this.hiddenCheck.Location = new System.Drawing.Point(120, 210);
            this.hiddenCheck.Name = "hiddenCheck";
            this.hiddenCheck.Size = new System.Drawing.Size(375, 20);
            this.hiddenCheck.Text = "Hidden channel (--hidden) — for betas / dev pushes";
            this.hiddenCheck.UseVisualStyleBackColor = true;
            //
            // ignoreLabel
            //
            this.ignoreLabel.Location = new System.Drawing.Point(10, 240);
            this.ignoreLabel.Name = "ignoreLabel";
            this.ignoreLabel.Size = new System.Drawing.Size(110, 23);
            this.ignoreLabel.Text = "Ignore (comma):";
            //
            // ignoreBox
            //
            this.ignoreBox.Location = new System.Drawing.Point(120, 237);
            this.ignoreBox.Name = "ignoreBox";
            this.ignoreBox.Size = new System.Drawing.Size(375, 23);
            //
            // pushButton
            //
            this.pushButton.Location = new System.Drawing.Point(10, 275);
            this.pushButton.Name = "pushButton";
            this.pushButton.Size = new System.Drawing.Size(155, 30);
            this.pushButton.Text = "Push";
            this.pushButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.pushButton.Click += new System.EventHandler(this.PushButton_Click);
            //
            // saveBuildButton
            //
            this.saveBuildButton.Location = new System.Drawing.Point(175, 275);
            this.saveBuildButton.Name = "saveBuildButton";
            this.saveBuildButton.Size = new System.Drawing.Size(155, 30);
            this.saveBuildButton.Text = "Save to library";
            this.saveBuildButton.Click += new System.EventHandler(this.SaveBuildButton_Click);
            //
            // generateButton
            //
            this.generateButton.Location = new System.Drawing.Point(340, 275);
            this.generateButton.Name = "generateButton";
            this.generateButton.Size = new System.Drawing.Size(155, 30);
            this.generateButton.Text = "Generate .bat file";
            this.generateButton.Click += new System.EventHandler(this.GenerateButton_Click);
            //
            // validateButton
            //
            this.validateButton.Location = new System.Drawing.Point(10, 312);
            this.validateButton.Name = "validateButton";
            this.validateButton.Size = new System.Drawing.Size(155, 30);
            this.validateButton.Text = "Validate folder";
            this.validateButton.Click += new System.EventHandler(this.ValidateButton_Click);
            //
            // statusButton
            //
            this.statusButton.Location = new System.Drawing.Point(175, 312);
            this.statusButton.Name = "statusButton";
            this.statusButton.Size = new System.Drawing.Size(155, 30);
            this.statusButton.Text = "View status";
            this.statusButton.Click += new System.EventHandler(this.StatusButton_Click);
            //
            // loginButton
            //
            this.loginButton.Location = new System.Drawing.Point(340, 312);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(155, 30);
            this.loginButton.Text = "Login to Itch";
            this.loginButton.Click += new System.EventHandler(this.LoginButton_Click);
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(10, 360);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(250, 20);
            this.statusLabel.Text = "Status: Checking...";
            //
            // updateLabel
            //
            this.updateLabel.Location = new System.Drawing.Point(265, 360);
            this.updateLabel.Name = "updateLabel";
            this.updateLabel.Size = new System.Drawing.Size(200, 20);
            this.updateLabel.Text = "";
            this.updateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // appVersionLabel
            //
            this.appVersionLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.appVersionLabel.Location = new System.Drawing.Point(470, 360);
            this.appVersionLabel.Name = "appVersionLabel";
            this.appVersionLabel.Size = new System.Drawing.Size(60, 20);
            this.appVersionLabel.Text = "";
            this.appVersionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // progressBar
            //
            this.progressBar.Location = new System.Drawing.Point(10, 385);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(750, 20);
            this.progressBar.Visible = false;
            //
            // progressStatusLabel
            //
            this.progressStatusLabel.AutoSize = false;
            this.progressStatusLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.progressStatusLabel.Location = new System.Drawing.Point(10, 408);
            this.progressStatusLabel.Name = "progressStatusLabel";
            this.progressStatusLabel.Size = new System.Drawing.Size(750, 16);
            this.progressStatusLabel.Text = "";
            this.progressStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.progressStatusLabel.Visible = false;
            //
            // outputBox
            //
            this.outputBox.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.outputBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.outputBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.outputBox.ForeColor = System.Drawing.Color.Gainsboro;
            this.outputBox.Location = new System.Drawing.Point(10, 430);
            this.outputBox.Multiline = true;
            this.outputBox.Name = "outputBox";
            this.outputBox.ReadOnly = true;
            this.outputBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.outputBox.Size = new System.Drawing.Size(750, 180);
            this.outputBox.Visible = false;
            //
            // toolTip
            //
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.InitialDelay = 1000;
            this.toolTip.ReshowDelay = 500;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(770, 430);
            this.Controls.Add(this.sidebarToggle);
            this.Controls.Add(this.gamesTree);
            this.Controls.Add(this.addGameButton);
            this.Controls.Add(this.addBuildButton);
            this.Controls.Add(this.duplicateButton);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.syncButton);
            this.Controls.Add(this.pathLabel);
            this.Controls.Add(this.pathBox);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.usernameLabel);
            this.Controls.Add(this.usernameBox);
            this.Controls.Add(this.gameNameLabel);
            this.Controls.Add(this.gameNameBox);
            this.Controls.Add(this.tagLabel);
            this.Controls.Add(this.tagBox);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.versionBox);
            this.Controls.Add(this.versionHintLabel);
            this.Controls.Add(this.ifChangedCheck);
            this.Controls.Add(this.hiddenCheck);
            this.Controls.Add(this.ignoreLabel);
            this.Controls.Add(this.ignoreBox);
            this.Controls.Add(this.pushButton);
            this.Controls.Add(this.saveBuildButton);
            this.Controls.Add(this.generateButton);
            this.Controls.Add(this.validateButton);
            this.Controls.Add(this.statusButton);
            this.Controls.Add(this.loginButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.updateLabel);
            this.Controls.Add(this.appVersionLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.progressStatusLabel);
            this.Controls.Add(this.outputBox);
            this.Controls.Add(this.buildGroup);
            this.Controls.Add(this.libraryGroup);
            this.buildGroup.SendToBack();
            this.libraryGroup.SendToBack();
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Itch.io Butler Utility";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.GroupBox buildGroup;
        private System.Windows.Forms.GroupBox libraryGroup;
        private System.Windows.Forms.Button sidebarToggle;
        private System.Windows.Forms.TreeView gamesTree;
        private System.Windows.Forms.Button addGameButton;
        private System.Windows.Forms.Button addBuildButton;
        private System.Windows.Forms.Button duplicateButton;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button syncButton;
        private System.Windows.Forms.Label pathLabel;
        private System.Windows.Forms.TextBox pathBox;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.Label usernameLabel;
        private System.Windows.Forms.TextBox usernameBox;
        private System.Windows.Forms.Label gameNameLabel;
        private System.Windows.Forms.TextBox gameNameBox;
        private System.Windows.Forms.Label tagLabel;
        private System.Windows.Forms.TextBox tagBox;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.TextBox versionBox;
        private System.Windows.Forms.Label versionHintLabel;
        private System.Windows.Forms.CheckBox ifChangedCheck;
        private System.Windows.Forms.CheckBox hiddenCheck;
        private System.Windows.Forms.Label ignoreLabel;
        private System.Windows.Forms.TextBox ignoreBox;
        private System.Windows.Forms.Button pushButton;
        private System.Windows.Forms.Button saveBuildButton;
        private System.Windows.Forms.Button generateButton;
        private System.Windows.Forms.Button validateButton;
        private System.Windows.Forms.Button statusButton;
        private System.Windows.Forms.Button loginButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label updateLabel;
        private System.Windows.Forms.Label appVersionLabel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label progressStatusLabel;
        private System.Windows.Forms.TextBox outputBox;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
