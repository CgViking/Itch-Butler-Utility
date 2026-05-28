using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ItchioButlerUtility
{
    public class ApiKeyDialog : Form
    {
        private const string KeysUrl = "https://itch.io/user/settings/api-keys";

        private TextBox keyBox = new TextBox();
        private Button okButton = new Button();
        private Button cancelButton = new Button();
        private LinkLabel keysLink = new LinkLabel();

        public string ApiKey { get; private set; } = "";

        public ApiKeyDialog(string existingKey)
        {
            Text = "itch.io API key";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 200);

            var info = new Label
            {
                Text = "Syncing games requires a personal itch.io API key with profile:games scope.\r\n" +
                       "butler's own credentials don't have this scope.\r\n\r\n" +
                       "Generate or copy one from your itch.io account settings, then paste it below.",
                Location = new Point(12, 12),
                Size = new Size(436, 70),
                AutoSize = false
            };

            keysLink.Text = "Open itch.io API keys page";
            keysLink.Location = new Point(12, 82);
            keysLink.AutoSize = true;
            keysLink.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(KeysUrl) { UseShellExecute = true }); }
                catch { /* ignore launch failures */ }
            };

            var label = new Label
            {
                Text = "API key:",
                Location = new Point(12, 112),
                Size = new Size(60, 20),
                AutoSize = true
            };

            keyBox.Location = new Point(72, 109);
            keyBox.Size = new Size(376, 23);
            keyBox.Text = existingKey ?? "";
            keyBox.UseSystemPasswordChar = false;

            okButton.Text = "Save";
            okButton.Location = new Point(292, 156);
            okButton.Size = new Size(75, 28);
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += (s, e) =>
            {
                ApiKey = keyBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(ApiKey))
                {
                    MessageBox.Show(this, "Please paste an API key, or click Cancel.",
                        "API key required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            cancelButton.Text = "Cancel";
            cancelButton.Location = new Point(373, 156);
            cancelButton.Size = new Size(75, 28);
            cancelButton.DialogResult = DialogResult.Cancel;

            AcceptButton = okButton;
            CancelButton = cancelButton;

            Controls.Add(info);
            Controls.Add(keysLink);
            Controls.Add(label);
            Controls.Add(keyBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
        }
    }
}
