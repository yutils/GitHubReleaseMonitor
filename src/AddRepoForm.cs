using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace GitHubReleaseMonitor
{
    public sealed class AddRepoForm : Form
    {
        private readonly AppConfig _config;
        private readonly RepoConfig _editing;
        private readonly string _originalSlug;

        private readonly TextBox _nameBox;
        private readonly TextBox _urlBox;
        private readonly CheckBox _prereleaseBox;
        private readonly CheckBox _notifyBox;
        private readonly Label _statusLabel;
        private readonly RoundedButton _saveButton;
        private readonly RoundedButton _cancelButton;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Label _nameHint;
        private readonly Label _urlHint;
        private readonly Label _baselineHint;
        private readonly Panel _header;
        private readonly Panel _separator;

        private bool _verifying;

        public string ResultName = "";
        public string ResultOwner = "";
        public string ResultRepo = "";
        public bool ResultIncludePrerelease;
        public bool ResultNotify = true;

        public AddRepoForm(AppConfig config, RepoConfig editing)
        {
            _config = config;
            _editing = editing;
            _originalSlug = editing == null ? "" : editing.Slug;

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            ClientSize = new Size(560, 452);
            Font = Theme.Ui(9F);
            Text = editing == null ? L10n.T("add.windowTitleAdd") : L10n.T("add.windowTitleEdit");

            _header = new Panel();
            _header.Dock = DockStyle.Top;
            _header.Height = 82;
            _header.BackColor = Color.White;
            _header.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(Theme.Border))
                {
                    e.Graphics.DrawLine(pen, 0, _header.Height - 1, _header.Width, _header.Height - 1);
                }
            };

            _titleLabel = new Label();
            _titleLabel.AutoSize = true;
            _titleLabel.Text = editing == null ? L10n.T("add.titleAdd") : L10n.T("add.titleEdit");
            _titleLabel.Font = Theme.Ui(14F, FontStyle.Bold);
            _titleLabel.ForeColor = Theme.Text;
            _titleLabel.Location = new Point(24, 18);

            _subtitleLabel = new Label();
            _subtitleLabel.AutoSize = true;
            _subtitleLabel.Text = L10n.T("add.subtitle");
            _subtitleLabel.Font = Theme.Ui(8.5F);
            _subtitleLabel.ForeColor = Theme.Muted;
            _subtitleLabel.Location = new Point(25, 49);

            _header.Controls.Add(_titleLabel);
            _header.Controls.Add(_subtitleLabel);

            Label nameLabel = new Label();
            nameLabel.Text = L10n.T("add.name");
            nameLabel.AutoSize = true;
            nameLabel.Font = Theme.Ui(9F);
            nameLabel.ForeColor = Theme.Text;
            nameLabel.Location = new Point(24, 102);

            _nameBox = new TextBox();
            _nameBox.BorderStyle = BorderStyle.FixedSingle;
            _nameBox.Font = Theme.Ui(9.5F);
            _nameBox.Location = new Point(24, 123);
            _nameBox.Width = 512;
            _nameBox.MaxLength = 60;
            if (editing != null) _nameBox.Text = editing.Name;

            _nameHint = new Label();
            _nameHint.Text = L10n.T("add.nameHint");
            _nameHint.AutoSize = true;
            _nameHint.Font = Theme.Ui(8F);
            _nameHint.ForeColor = Theme.Faint;
            _nameHint.Location = new Point(24, 150);

            Label urlLabel = new Label();
            urlLabel.Text = L10n.T("add.url");
            urlLabel.AutoSize = true;
            urlLabel.Font = Theme.Ui(9F);
            urlLabel.ForeColor = Theme.Text;
            urlLabel.Location = new Point(24, 180);

            _urlBox = new TextBox();
            _urlBox.BorderStyle = BorderStyle.FixedSingle;
            _urlBox.Font = Theme.Ui(9.5F);
            _urlBox.Location = new Point(24, 201);
            _urlBox.Width = 512;
            _urlBox.MaxLength = 300;
            if (editing != null)
            {
                _urlBox.Text = "https://github.com/" + editing.Slug;
            }

            _urlHint = new Label();
            _urlHint.Text = L10n.T("add.urlHint");
            _urlHint.AutoSize = true;
            _urlHint.Font = Theme.Ui(8F);
            _urlHint.ForeColor = Theme.Faint;
            _urlHint.Location = new Point(24, 228);

            _prereleaseBox = new CheckBox();
            _prereleaseBox.Text = L10n.T("add.prerelease");
            _prereleaseBox.AutoSize = true;
            _prereleaseBox.Font = Theme.Ui(9F);
            _prereleaseBox.ForeColor = Theme.Text;
            _prereleaseBox.Location = new Point(24, 262);
            if (editing != null) _prereleaseBox.Checked = editing.IncludePrerelease;

            _notifyBox = new CheckBox();
            _notifyBox.Text = L10n.T("add.notify");
            _notifyBox.AutoSize = true;
            _notifyBox.Font = Theme.Ui(9F);
            _notifyBox.ForeColor = Theme.Text;
            _notifyBox.Location = new Point(24, 290);
            _notifyBox.Checked = editing == null || editing.Notify;

            _baselineHint = new Label();
            _baselineHint.Text = editing == null
                ? L10n.T("add.baselineAdd")
                : L10n.T("add.baselineEdit");
            _baselineHint.AutoSize = false;
            _baselineHint.Font = Theme.Ui(8F);
            _baselineHint.ForeColor = Theme.Muted;
            _baselineHint.Location = new Point(24, 322);
            _baselineHint.Size = new Size(512, 22);

            _separator = new Panel();
            _separator.BackColor = Theme.Border;
            _separator.Location = new Point(0, 372);
            _separator.Size = new Size(560, 1);

            _statusLabel = new Label();
            _statusLabel.AutoSize = false;
            _statusLabel.AutoEllipsis = true;
            _statusLabel.Font = Theme.Ui(8.5F);
            _statusLabel.ForeColor = Theme.Muted;
            _statusLabel.Location = new Point(24, 388);
            _statusLabel.Size = new Size(320, 24);
            _statusLabel.Text = "";

            _saveButton = new RoundedButton();
            _saveButton.Text = L10n.T("common.save");
            _saveButton.Kind = ButtonKind.Primary;
            _saveButton.Size = new Size(88, 32);
            _saveButton.SurfaceColor = Color.White;
            _saveButton.Location = new Point(448, 384);
            _saveButton.Click += delegate { TrySave(); };

            _cancelButton = new RoundedButton();
            _cancelButton.Text = L10n.T("common.cancel");
            _cancelButton.Kind = ButtonKind.Secondary;
            _cancelButton.Size = new Size(80, 32);
            _cancelButton.SurfaceColor = Color.White;
            _cancelButton.Location = new Point(360, 384);
            _cancelButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(_header);
            Controls.Add(nameLabel);
            Controls.Add(_nameBox);
            Controls.Add(_nameHint);
            Controls.Add(urlLabel);
            Controls.Add(_urlBox);
            Controls.Add(_urlHint);
            Controls.Add(_prereleaseBox);
            Controls.Add(_notifyBox);
            Controls.Add(_baselineHint);
            Controls.Add(_separator);
            Controls.Add(_statusLabel);
            Controls.Add(_cancelButton);
            Controls.Add(_saveButton);

            AcceptButton = _saveButton;
            CancelButton = _cancelButton;

            Shown += delegate
            {
                if (_editing == null) _urlBox.Focus();
                else _nameBox.Focus();
                _nameBox.SelectAll();
            };
        }

        private void TrySave()
        {
            if (_verifying) return;

            string url = _urlBox.Text.Trim();
            string owner;
            string repo;
            if (!GitHubSlug.TryParse(url, out owner, out repo))
            {
                ShowStatus(L10n.T("add.invalidUrl"), true);
                _urlBox.Focus();
                return;
            }

            foreach (RepoConfig existing in _config.Repos)
            {
                if (_editing != null && existing.Id == _editing.Id) continue;
                if (string.Equals(existing.Owner, owner, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.Repo, repo, StringComparison.OrdinalIgnoreCase))
                {
                    ShowStatus(L10n.T("add.duplicate"), true);
                    _urlBox.Focus();
                    return;
                }
            }

            ResultOwner = owner;
            ResultRepo = repo;
            ResultName = _nameBox.Text.Trim();
            if (ResultName.Length == 0) ResultName = repo;
            ResultIncludePrerelease = _prereleaseBox.Checked;
            ResultNotify = _notifyBox.Checked;

            string slug = owner + "/" + repo;
            if (_editing != null && string.Equals(slug, _originalSlug, StringComparison.OrdinalIgnoreCase))
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            StartVerification(owner, repo);
        }

        private void StartVerification(string owner, string repo)
        {
            _verifying = true;
            SetBusy(true);
            ShowStatus(L10n.T("add.verifyStatus"), false);

            AppSettings settings = _config.Settings.Clone();
            string token = _config.GithubToken;
            bool includePrerelease = _prereleaseBox.Checked;

            ThreadPool.QueueUserWorkItem(delegate
            {
                GitHubClient client = new GitHubClient(settings, token);
                ReleaseLookupResult result;
                try
                {
                    result = client.GetLatestRelease(owner, repo, includePrerelease);
                }
                catch (Exception ex)
                {
                    result = ReleaseLookupResult.Failure(L10n.F("add.verifyFailed", ex.Message));
                }

                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed || Disposing) return;
                        FinishVerification(result);
                    });
                }
                catch
                {
                }
            });
        }

        private void FinishVerification(ReleaseLookupResult result)
        {
            _verifying = false;
            SetBusy(false);

            if (result.Success || result.NoRelease)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (result.NotFound || result.Unauthorized)
            {
                ShowStatus(result.Error, true);
                return;
            }

            ShowStatus(result.Error, true);
            string message = L10n.F("add.verifyFailedMessage", result.Error);
            if (MessageBox.Show(this, message, L10n.T("add.verifyFailedTitle"), MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void SetBusy(bool busy)
        {
            _saveButton.Enabled = !busy;
            _saveButton.Text = busy ? L10n.T("add.verifying") : L10n.T("common.save");
            _nameBox.Enabled = !busy;
            _urlBox.Enabled = !busy;
            _prereleaseBox.Enabled = !busy;
            _notifyBox.Enabled = !busy;
            _cancelButton.Enabled = true;
            UseWaitCursor = busy;
        }

        private void ShowStatus(string message, bool isError)
        {
            _statusLabel.Text = message ?? "";
            _statusLabel.ForeColor = isError ? Theme.Red : Theme.Muted;
        }
    }
}
