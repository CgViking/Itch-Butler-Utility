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
            this.pushButton = new System.Windows.Forms.Button();
            this.generateButton = new System.Windows.Forms.Button();
            this.loginButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.updateLabel = new System.Windows.Forms.Label();
            this.appVersionLabel = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.outputBox = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // pathLabel
            //
            this.pathLabel.Location = new System.Drawing.Point(20, 20);
            this.pathLabel.Name = "pathLabel";
            this.pathLabel.Size = new System.Drawing.Size(110, 23);
            this.pathLabel.Text = "Build Folder Path:";
            //
            // pathBox
            //
            this.pathBox.Location = new System.Drawing.Point(135, 17);
            this.pathBox.Name = "pathBox";
            this.pathBox.Size = new System.Drawing.Size(295, 23);
            //
            // browseButton
            //
            this.browseButton.Location = new System.Drawing.Point(440, 17);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(75, 23);
            this.browseButton.Text = "Browse";
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            //
            // usernameLabel
            //
            this.usernameLabel.Location = new System.Drawing.Point(20, 55);
            this.usernameLabel.Name = "usernameLabel";
            this.usernameLabel.Size = new System.Drawing.Size(110, 23);
            this.usernameLabel.Text = "Username:";
            //
            // usernameBox
            //
            this.usernameBox.Location = new System.Drawing.Point(135, 52);
            this.usernameBox.Name = "usernameBox";
            this.usernameBox.Size = new System.Drawing.Size(295, 23);
            //
            // gameNameLabel
            //
            this.gameNameLabel.Location = new System.Drawing.Point(20, 90);
            this.gameNameLabel.Name = "gameNameLabel";
            this.gameNameLabel.Size = new System.Drawing.Size(110, 23);
            this.gameNameLabel.Text = "Game Name:";
            //
            // gameNameBox
            //
            this.gameNameBox.Location = new System.Drawing.Point(135, 87);
            this.gameNameBox.Name = "gameNameBox";
            this.gameNameBox.Size = new System.Drawing.Size(295, 23);
            //
            // tagLabel
            //
            this.tagLabel.Location = new System.Drawing.Point(20, 125);
            this.tagLabel.Name = "tagLabel";
            this.tagLabel.Size = new System.Drawing.Size(110, 23);
            this.tagLabel.Text = "Tag (optional):";
            //
            // tagBox
            //
            this.tagBox.Location = new System.Drawing.Point(135, 122);
            this.tagBox.Name = "tagBox";
            this.tagBox.Size = new System.Drawing.Size(295, 23);
            //
            // versionLabel
            //
            this.versionLabel.Location = new System.Drawing.Point(20, 160);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(110, 23);
            this.versionLabel.Text = "Version (optional):";
            //
            // versionBox
            //
            this.versionBox.Location = new System.Drawing.Point(135, 157);
            this.versionBox.Name = "versionBox";
            this.versionBox.Size = new System.Drawing.Size(150, 23);
            //
            // versionHintLabel
            //
            this.versionHintLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.versionHintLabel.Location = new System.Drawing.Point(290, 160);
            this.versionHintLabel.Name = "versionHintLabel";
            this.versionHintLabel.Size = new System.Drawing.Size(225, 23);
            this.versionHintLabel.Text = "";
            //
            // pushButton
            //
            this.pushButton.Location = new System.Drawing.Point(200, 200);
            this.pushButton.Name = "pushButton";
            this.pushButton.Size = new System.Drawing.Size(120, 30);
            this.pushButton.Text = "Push";
            this.pushButton.Click += new System.EventHandler(this.PushButton_Click);
            //
            // generateButton
            //
            this.generateButton.Location = new System.Drawing.Point(200, 240);
            this.generateButton.Name = "generateButton";
            this.generateButton.Size = new System.Drawing.Size(120, 30);
            this.generateButton.Text = "Generate .bat file";
            this.generateButton.Click += new System.EventHandler(this.GenerateButton_Click);
            //
            // loginButton
            //
            this.loginButton.Location = new System.Drawing.Point(200, 280);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(120, 30);
            this.loginButton.Text = "Login to Itch";
            this.loginButton.Click += new System.EventHandler(this.LoginButton_Click);
            //
            // statusLabel
            //
            this.statusLabel.Location = new System.Drawing.Point(20, 325);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(250, 20);
            this.statusLabel.Text = "Status: Checking...";
            //
            // updateLabel
            //
            this.updateLabel.Location = new System.Drawing.Point(280, 325);
            this.updateLabel.Name = "updateLabel";
            this.updateLabel.Size = new System.Drawing.Size(170, 20);
            this.updateLabel.Text = "";
            this.updateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // appVersionLabel
            //
            this.appVersionLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.appVersionLabel.Location = new System.Drawing.Point(455, 325);
            this.appVersionLabel.Name = "appVersionLabel";
            this.appVersionLabel.Size = new System.Drawing.Size(60, 20);
            this.appVersionLabel.Text = "";
            this.appVersionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // progressBar
            //
            this.progressBar.Location = new System.Drawing.Point(20, 355);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(495, 20);
            this.progressBar.Visible = false;
            //
            // outputBox
            //
            this.outputBox.Location = new System.Drawing.Point(20, 385);
            this.outputBox.Multiline = true;
            this.outputBox.Name = "outputBox";
            this.outputBox.ReadOnly = true;
            this.outputBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.outputBox.Size = new System.Drawing.Size(495, 150);
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
            this.ClientSize = new System.Drawing.Size(535, 360);
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
            this.Controls.Add(this.pushButton);
            this.Controls.Add(this.generateButton);
            this.Controls.Add(this.loginButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.updateLabel);
            this.Controls.Add(this.appVersionLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.outputBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Itch.io Butler Utility";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

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
        private System.Windows.Forms.Button pushButton;
        private System.Windows.Forms.Button generateButton;
        private System.Windows.Forms.Button loginButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label updateLabel;
        private System.Windows.Forms.Label appVersionLabel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox outputBox;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
