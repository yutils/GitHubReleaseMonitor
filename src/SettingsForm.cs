using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace GitHubReleaseMonitor
{
    public sealed class SettingsForm : Form
    {
        private static readonly int[] IntervalValues = new int[] { 5, 10, 15, 30, 60, 120, 360, 720, 1440 };

        private readonly AppSettings _settings;
        private readonly string _token;

        private readonly ComboBox _intervalCombo;
        private readonly Panel _customIntervalPanel;
        private readonly NumericUpDown _customMinutes;
        private readonly CheckBox _autoStartBox;
        private readonly CheckBox _checkOnStartupBox;
        private readonly CheckBox _closeToTrayBox;
        private readonly TextBox _tokenBox;
        private readonly CheckBox _showTokenBox;

        private readonly RadioButton _systemProxyRadio;
        private readonly RadioButton _noProxyRadio;
        private readonly RadioButton _customProxyRadio;
        private readonly Panel _customProxyPanel;
        private readonly TextBox _proxyAddressBox;
        private readonly TextBox _proxyUserBox;
        private readonly TextBox _proxyPassBox;
        private readonly RoundedButton _testButton;
        private readonly Label _testResultLabel;

        private readonly CheckBox _notifyOnErrorBox;

        private readonly RoundedButton _saveButton;
        private readonly RoundedButton _cancelButton;
        private readonly Label _errorLabel;

        private bool _testing;

        public AppSettings ResultSettings;
        public string ResultToken = "";

        public SettingsForm(AppConfig config)
        {
            _settings = config.Settings.Clone();
            _token = config.GithubToken ?? "";

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            ClientSize = new Size(640, 660);
            Font = Theme.Ui(9F);
            Text = L10n.T("settings.window");

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 82;
            header.BackColor = Color.White;
            header.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(Theme.Border))
                {
                    e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
                }
            };

            Label title = new Label();
            title.AutoSize = true;
            title.Text = L10n.T("settings.title");
            title.Font = Theme.Ui(14F, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.Location = new Point(24, 18);

            Label subtitle = new Label();
            subtitle.AutoSize = true;
            subtitle.Text = L10n.T("settings.subtitle");
            subtitle.Font = Theme.Ui(8.5F);
            subtitle.ForeColor = Theme.Muted;
            subtitle.Location = new Point(25, 49);

            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            TabControl tabs = new TabControl();
            tabs.Font = Theme.Ui(9F);
            tabs.Location = new Point(20, 94);
            tabs.Size = new Size(600, 486);

            TabPage generalPage = new TabPage(L10n.T("settings.tabGeneral"));
            generalPage.BackColor = Color.White;
            generalPage.AutoScroll = true;
            BuildGeneralPage(generalPage);

            TabPage networkPage = new TabPage(L10n.T("settings.tabNetwork"));
            networkPage.BackColor = Color.White;
            networkPage.AutoScroll = true;
            BuildNetworkPage(networkPage);

            TabPage notifyPage = new TabPage(L10n.T("settings.tabNotify"));
            notifyPage.BackColor = Color.White;
            notifyPage.AutoScroll = true;
            BuildNotifyPage(notifyPage);

            TabPage aboutPage = new TabPage(L10n.T("settings.tabAbout"));
            aboutPage.BackColor = Color.White;
            aboutPage.AutoScroll = true;
            BuildAboutPage(aboutPage);

            tabs.TabPages.Add(generalPage);
            tabs.TabPages.Add(networkPage);
            tabs.TabPages.Add(notifyPage);
            tabs.TabPages.Add(aboutPage);

            // These readonly fields are assigned in the page builders via local controls.
            _intervalCombo = (ComboBox)FindPageControl(generalPage, "intervalCombo");
            _customIntervalPanel = (Panel)FindPageControl(generalPage, "customIntervalPanel");
            _customMinutes = (NumericUpDown)FindPageControl(generalPage, "customMinutes");
            _autoStartBox = (CheckBox)FindPageControl(generalPage, "autoStartBox");
            _checkOnStartupBox = (CheckBox)FindPageControl(generalPage, "checkOnStartupBox");
            _closeToTrayBox = (CheckBox)FindPageControl(generalPage, "closeToTrayBox");
            _tokenBox = (TextBox)FindPageControl(generalPage, "tokenBox");
            _showTokenBox = (CheckBox)FindPageControl(generalPage, "showTokenBox");

            _systemProxyRadio = (RadioButton)FindPageControl(networkPage, "systemProxyRadio");
            _noProxyRadio = (RadioButton)FindPageControl(networkPage, "noProxyRadio");
            _customProxyRadio = (RadioButton)FindPageControl(networkPage, "customProxyRadio");
            _customProxyPanel = (Panel)FindPageControl(networkPage, "customProxyPanel");
            _proxyAddressBox = (TextBox)FindPageControl(networkPage, "proxyAddressBox");
            _proxyUserBox = (TextBox)FindPageControl(networkPage, "proxyUserBox");
            _proxyPassBox = (TextBox)FindPageControl(networkPage, "proxyPassBox");
            _testButton = (RoundedButton)FindPageControl(networkPage, "testButton");
            _testResultLabel = (Label)FindPageControl(networkPage, "testResultLabel");

            _notifyOnErrorBox = (CheckBox)FindPageControl(notifyPage, "notifyOnErrorBox");

            Panel separator = new Panel();
            separator.BackColor = Theme.Border;
            separator.Location = new Point(0, 590);
            separator.Size = new Size(640, 1);

            _errorLabel = new Label();
            _errorLabel.AutoSize = false;
            _errorLabel.AutoEllipsis = true;
            _errorLabel.Font = Theme.Ui(8.5F);
            _errorLabel.ForeColor = Theme.Red;
            _errorLabel.Location = new Point(24, 612);
            _errorLabel.Size = new Size(400, 24);

            _cancelButton = new RoundedButton();
            _cancelButton.Text = L10n.T("common.cancel");
            _cancelButton.Kind = ButtonKind.Secondary;
            _cancelButton.SurfaceColor = Color.White;
            _cancelButton.Size = new Size(80, 32);
            _cancelButton.Location = new Point(432, 606);
            _cancelButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            _saveButton = new RoundedButton();
            _saveButton.Text = L10n.T("common.save");
            _saveButton.Kind = ButtonKind.Primary;
            _saveButton.SurfaceColor = Color.White;
            _saveButton.Size = new Size(96, 32);
            _saveButton.Location = new Point(520, 606);
            _saveButton.Click += delegate { TrySave(); };

            Controls.Add(header);
            Controls.Add(tabs);
            Controls.Add(separator);
            Controls.Add(_errorLabel);
            Controls.Add(_cancelButton);
            Controls.Add(_saveButton);

            tabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            separator.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _errorLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            AcceptButton = _saveButton;
            CancelButton = _cancelButton;

            _intervalCombo.SelectedIndexChanged += delegate { UpdateCustomIntervalVisibility(); };
            _showTokenBox.CheckedChanged += delegate
            {
                _tokenBox.PasswordChar = _showTokenBox.Checked ? '\0' : '●';
            };
            _systemProxyRadio.CheckedChanged += delegate { UpdateProxyFields(); };
            _noProxyRadio.CheckedChanged += delegate { UpdateProxyFields(); };
            _customProxyRadio.CheckedChanged += delegate { UpdateProxyFields(); };
            _testButton.Click += delegate { TestConnection(); };
            tabs.SelectedIndex = 0;
            UpdateCustomIntervalVisibility();
            UpdateProxyFields();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int maxWidth = Math.Max(520, area.Width - 40);
            int maxHeight = Math.Max(560, area.Height - 48);
            int newWidth = Math.Min(Width, maxWidth);
            int newHeight = Math.Min(Height, maxHeight);
            if (newWidth != Width || newHeight != Height)
            {
                Size = new Size(newWidth, newHeight);
            }
            if (Owner != null)
            {
                Left = Owner.Left + Math.Max(0, (Owner.Width - Width) / 2);
                Top = Owner.Top + Math.Max(0, (Owner.Height - Height) / 2);
            }
            else
            {
                Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
                Top = area.Top + Math.Max(0, (area.Height - Height) / 2);
            }
        }

        private void BuildGeneralPage(TabPage page)
        {
            Label freqTitle = MakeSectionTitle(L10n.T("settings.frequency"), 22, 16);
            Label intervalLabel = MakeLabel(L10n.T("settings.interval"), 22, 52, 120, 22);
            ComboBox combo = new ComboBox();
            combo.Name = "intervalCombo";
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Font = Theme.Ui(9F);
            combo.Location = new Point(150, 48);
            combo.Width = 200;
            for (int i = 0; i < IntervalValues.Length; i++)
            {
                combo.Items.Add(TimeText.Interval(IntervalValues[i]));
            }
            combo.Items.Add(L10n.T("settings.custom"));
            int selectedIndex = -1;
            for (int i = 0; i < IntervalValues.Length; i++)
            {
                if (IntervalValues[i] == _settings.IntervalMinutes)
                {
                    selectedIndex = i;
                    break;
                }
            }
            combo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : IntervalValues.Length;

            Panel customPanel = new Panel();
            customPanel.Name = "customIntervalPanel";
            customPanel.Location = new Point(150, 80);
            customPanel.Size = new Size(230, 30);
            NumericUpDown numeric = new NumericUpDown();
            numeric.Name = "customMinutes";
            numeric.Minimum = 1;
            numeric.Maximum = 1440;
            numeric.Font = Theme.Ui(9F);
            numeric.Location = new Point(0, 2);
            numeric.Width = 100;
            numeric.Value = _settings.IntervalMinutes >= 1 && _settings.IntervalMinutes <= 1440
                ? _settings.IntervalMinutes
                : 10;
            Label minuteLabel = new Label();
            minuteLabel.Text = L10n.T("settings.minutes");
            minuteLabel.AutoSize = true;
            minuteLabel.Font = Theme.Ui(9F);
            minuteLabel.ForeColor = Theme.Muted;
            minuteLabel.Location = new Point(108, 6);
            customPanel.Controls.Add(numeric);
            customPanel.Controls.Add(minuteLabel);

            Label freqHint = MakeNote(L10n.T("settings.freqHint"), 22, 116, 540, 34);

            Panel sep1 = MakeSeparator(22, 158, 540);

            Label startupTitle = MakeSectionTitle(L10n.T("settings.startup"), 22, 176);
            CheckBox autoStart = MakeCheckBox(L10n.T("settings.autoStart"), 22, 208, 260);
            autoStart.Name = "autoStartBox";
            autoStart.Checked = _settings.AutoStart;
            CheckBox checkOnStartup = MakeCheckBox(L10n.T("settings.checkOnStartup"), 22, 236, 260);
            checkOnStartup.Name = "checkOnStartupBox";
            checkOnStartup.Checked = _settings.CheckOnStartup;
            CheckBox closeToTray = MakeCheckBox(L10n.T("settings.closeToTray"), 22, 264, 360);
            closeToTray.Name = "closeToTrayBox";
            closeToTray.Checked = _settings.CloseToTray;

            Panel sep2 = MakeSeparator(22, 302, 540);

            Label tokenTitle = MakeSectionTitle(L10n.T("settings.tokenTitle"), 22, 318);
            TextBox tokenBox = new TextBox();
            tokenBox.Name = "tokenBox";
            tokenBox.BorderStyle = BorderStyle.FixedSingle;
            tokenBox.Font = Theme.Ui(9.5F);
            tokenBox.Location = new Point(22, 346);
            tokenBox.Width = 420;
            tokenBox.PasswordChar = '●';
            tokenBox.MaxLength = 300;
            tokenBox.Text = _token;

            CheckBox showToken = new CheckBox();
            showToken.Name = "showTokenBox";
            showToken.Text = L10n.T("settings.show");
            showToken.AutoSize = true;
            showToken.Font = Theme.Ui(9F);
            showToken.ForeColor = Theme.Text;
            showToken.Location = new Point(452, 349);

            Label tokenHint = MakeNote(L10n.T("settings.tokenHint"), 22, 380, 540, 38);

            LinkLabel tokenLink = new LinkLabel();
            tokenLink.Text = L10n.T("settings.createToken");
            tokenLink.AutoSize = true;
            tokenLink.Font = Theme.Ui(8.5F);
            tokenLink.Location = new Point(22, 424);
            tokenLink.LinkColor = Theme.Accent;
            tokenLink.ActiveLinkColor = Theme.AccentHover;
            tokenLink.LinkClicked += delegate
            {
                OpenUrl("https://github.com/settings/tokens/new?scopes=repo&description=GitHub%20Release%20Monitor");
            };

            page.Controls.Add(freqTitle);
            page.Controls.Add(intervalLabel);
            page.Controls.Add(combo);
            page.Controls.Add(customPanel);
            page.Controls.Add(freqHint);
            page.Controls.Add(sep1);
            page.Controls.Add(startupTitle);
            page.Controls.Add(autoStart);
            page.Controls.Add(checkOnStartup);
            page.Controls.Add(closeToTray);
            page.Controls.Add(sep2);
            page.Controls.Add(tokenTitle);
            page.Controls.Add(tokenBox);
            page.Controls.Add(showToken);
            page.Controls.Add(tokenHint);
            page.Controls.Add(tokenLink);
        }

        private void BuildNetworkPage(TabPage page)
        {
            Label proxyTitle = MakeSectionTitle(L10n.T("settings.proxyTitle"), 22, 16);

            RadioButton systemRadio = new RadioButton();
            systemRadio.Name = "systemProxyRadio";
            systemRadio.Text = L10n.T("settings.proxySystem");
            systemRadio.AutoSize = true;
            systemRadio.Font = Theme.Ui(9F);
            systemRadio.Location = new Point(22, 50);

            RadioButton noneRadio = new RadioButton();
            noneRadio.Name = "noProxyRadio";
            noneRadio.Text = L10n.T("settings.proxyNone");
            noneRadio.AutoSize = true;
            noneRadio.Font = Theme.Ui(9F);
            noneRadio.Location = new Point(22, 82);

            RadioButton customRadio = new RadioButton();
            customRadio.Name = "customProxyRadio";
            customRadio.Text = L10n.T("settings.proxyCustom");
            customRadio.AutoSize = true;
            customRadio.Font = Theme.Ui(9F);
            customRadio.Location = new Point(22, 114);

            if (_settings.ProxyMode == "none") noneRadio.Checked = true;
            else if (_settings.ProxyMode == "custom") customRadio.Checked = true;
            else systemRadio.Checked = true;

            Panel customPanel = new Panel();
            customPanel.Name = "customProxyPanel";
            customPanel.Location = new Point(44, 148);
            customPanel.Size = new Size(530, 195);

            Label addressLabel = MakeLabel(L10n.T("settings.proxyAddress"), 0, 4, 70, 22);
            TextBox addressBox = new TextBox();
            addressBox.Name = "proxyAddressBox";
            addressBox.BorderStyle = BorderStyle.FixedSingle;
            addressBox.Font = Theme.Ui(9.5F);
            addressBox.Location = new Point(80, 0);
            addressBox.Width = 430;
            addressBox.Text = _settings.ProxyAddress;

            Label userLabel = MakeLabel(L10n.T("settings.proxyUser"), 0, 42, 70, 22);
            TextBox userBox = new TextBox();
            userBox.Name = "proxyUserBox";
            userBox.BorderStyle = BorderStyle.FixedSingle;
            userBox.Font = Theme.Ui(9.5F);
            userBox.Location = new Point(80, 38);
            userBox.Width = 220;
            userBox.Text = _settings.ProxyUsername;

            Label passLabel = MakeLabel(L10n.T("settings.proxyPass"), 0, 80, 70, 22);
            TextBox passBox = new TextBox();
            passBox.Name = "proxyPassBox";
            passBox.BorderStyle = BorderStyle.FixedSingle;
            passBox.Font = Theme.Ui(9.5F);
            passBox.Location = new Point(80, 76);
            passBox.Width = 220;
            passBox.PasswordChar = '●';
            passBox.Text = _settings.ProxyPassword;

            Label customHint = MakeNote(L10n.T("settings.proxyHint"), 0, 116, 490, 34);

            customPanel.Controls.Add(addressLabel);
            customPanel.Controls.Add(addressBox);
            customPanel.Controls.Add(userLabel);
            customPanel.Controls.Add(userBox);
            customPanel.Controls.Add(passLabel);
            customPanel.Controls.Add(passBox);
            customPanel.Controls.Add(customHint);

            RoundedButton testButton = new RoundedButton();
            testButton.Name = "testButton";
            testButton.Text = L10n.T("settings.test");
            testButton.Kind = ButtonKind.Secondary;
            testButton.SurfaceColor = Color.White;
            testButton.Size = new Size(132, 32);
            testButton.Location = new Point(22, 358);

            Label testResult = new Label();
            testResult.Name = "testResultLabel";
            testResult.AutoSize = false;
            testResult.Font = Theme.Ui(8.5F);
            testResult.ForeColor = Theme.Muted;
            testResult.Location = new Point(166, 362);
            testResult.Size = new Size(398, 40);

            Label note = MakeNote(L10n.T("settings.proxyNote"), 22, 412, 540, 34);

            page.Controls.Add(proxyTitle);
            page.Controls.Add(systemRadio);
            page.Controls.Add(noneRadio);
            page.Controls.Add(customRadio);
            page.Controls.Add(customPanel);
            page.Controls.Add(testButton);
            page.Controls.Add(testResult);
            page.Controls.Add(note);
        }

        private void BuildNotifyPage(TabPage page)
        {
            Label title = MakeSectionTitle(L10n.T("settings.notifyTitle"), 22, 16);

            CheckBox errorBox = MakeCheckBox(L10n.T("settings.notifyOnError"), 22, 52, 320);
            errorBox.Name = "notifyOnErrorBox";
            errorBox.Checked = _settings.NotifyOnError;

            Label note = MakeNote(L10n.T("settings.notifyNote"), 22, 92, 540, 68);
            Label note2 = MakeNote(L10n.T("settings.notifyNote2"), 22, 176, 540, 54);

            page.Controls.Add(title);
            page.Controls.Add(errorBox);
            page.Controls.Add(note);
            page.Controls.Add(note2);
        }

        private void BuildAboutPage(TabPage page)
        {
            PictureBox icon = new PictureBox();
            icon.Image = AppIcon.Bitmap64;
            icon.SizeMode = PictureBoxSizeMode.Zoom;
            icon.Location = new Point(22, 20);
            icon.Size = new Size(64, 64);

            Label appName = new Label();
            appName.Text = AppInfo.Name;
            appName.AutoSize = true;
            appName.Font = Theme.Ui(13F, FontStyle.Bold);
            appName.ForeColor = Theme.Text;
            appName.Location = new Point(100, 24);

            Label version = new Label();
            version.Text = L10n.F("settings.aboutVersion", AppInfo.Version);
            version.AutoSize = true;
            version.Font = Theme.Ui(8.5F);
            version.ForeColor = Theme.Muted;
            version.Location = new Point(101, 52);

            Label description = new Label();
            description.Text = L10n.T("settings.aboutDescription");
            description.AutoSize = false;
            description.Font = Theme.Ui(9F);
            description.ForeColor = Theme.Text;
            description.Location = new Point(22, 108);
            description.Size = new Size(540, L10n.IsEnglish ? 58 : 44);

            Label configTitle = MakeSectionTitle(L10n.T("settings.configTitle"), 22, 170);

            Label configPath = new Label();
            configPath.Text = ConfigStore.BaseDir;
            configPath.AutoSize = false;
            configPath.Font = Theme.Ui(8.5F);
            configPath.ForeColor = Theme.Muted;
            configPath.Location = new Point(22, 206);
            configPath.Size = new Size(540, 38);

            RoundedButton openConfig = new RoundedButton();
            openConfig.Text = L10n.T("settings.openConfig");
            openConfig.Kind = ButtonKind.Secondary;
            openConfig.SurfaceColor = Color.White;
            openConfig.Size = new Size(L10n.IsEnglish ? 148 : 116, 32);
            openConfig.Location = new Point(22, 250);
            openConfig.Click += delegate { OpenPath(ConfigStore.BaseDir); };

            RoundedButton openLog = new RoundedButton();
            openLog.Text = L10n.T("settings.openLog");
            openLog.Kind = ButtonKind.Secondary;
            openLog.SurfaceColor = Color.White;
            openLog.Size = new Size(100, 32);
            openLog.Location = new Point(openConfig.Right + 8, 250);
            openLog.Click += delegate { OpenLog(); };

            Label footer = MakeNote(L10n.T("settings.aboutFooter"), 22, 304, 540, 24);

            Label openSourceTitle = MakeSectionTitle(L10n.T("settings.openSourceTitle"), 22, 340);
            LinkLabel openSourceLink = new LinkLabel();
            openSourceLink.Text = "https://github.com/yutils/GitHubReleaseMonitor";
            openSourceLink.AutoSize = true;
            openSourceLink.Font = Theme.Ui(8.5F);
            openSourceLink.LinkColor = Theme.Accent;
            openSourceLink.ActiveLinkColor = Theme.AccentHover;
            openSourceLink.Location = new Point(22, 374);
            openSourceLink.LinkClicked += delegate
            {
                OpenUrl("https://github.com/yutils/GitHubReleaseMonitor");
            };

            Label freeLabel = new Label();
            freeLabel.Text = L10n.T("settings.openSourceFree");
            freeLabel.AutoSize = true;
            freeLabel.Font = Theme.Ui(9F, FontStyle.Bold);
            freeLabel.ForeColor = Theme.Green;
            freeLabel.Location = new Point(22, 400);

            page.Controls.Add(icon);
            page.Controls.Add(appName);
            page.Controls.Add(version);
            page.Controls.Add(description);
            page.Controls.Add(configTitle);
            page.Controls.Add(configPath);
            page.Controls.Add(openConfig);
            page.Controls.Add(openLog);
            page.Controls.Add(footer);
            page.Controls.Add(openSourceTitle);
            page.Controls.Add(openSourceLink);
            page.Controls.Add(freeLabel);
        }

        private static Control FindPageControl(Control root, string name)
        {
            Control[] found = root.Controls.Find(name, true);
            return found.Length > 0 ? found[0] : null;
        }

        private void UpdateCustomIntervalVisibility()
        {
            bool custom = _intervalCombo.SelectedIndex == IntervalValues.Length;
            _customIntervalPanel.Visible = custom;
        }

        private void UpdateProxyFields()
        {
            _customProxyPanel.Enabled = _customProxyRadio.Checked;
        }

        private void TestConnection()
        {
            if (_testing) return;
            _errorLabel.Text = "";

            if (_customProxyRadio.Checked)
            {
                string proxyError;
                if (!ValidateProxy(out proxyError))
                {
                    SetTestResult(false, proxyError);
                    return;
                }
            }

            AppSettings testSettings = BuildSettings();
            string token = _tokenBox.Text.Trim();
            _testing = true;
            _testButton.Enabled = false;
            _testButton.Text = L10n.T("settings.testing");
            _testResultLabel.ForeColor = Theme.Muted;
            _testResultLabel.Text = L10n.T("settings.connecting");
            UseWaitCursor = true;

            ThreadPool.QueueUserWorkItem(delegate
            {
                ConnectionTestResult result;
                try
                {
                    GitHubClient client = new GitHubClient(testSettings, token);
                    result = client.TestConnection();
                }
                catch (Exception ex)
                {
                    result = new ConnectionTestResult();
                    result.Success = false;
                    result.Message = L10n.F("settings.testFailed", ex.Message);
                }

                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed || Disposing) return;
                        _testing = false;
                        _testButton.Enabled = true;
                        _testButton.Text = L10n.T("settings.test");
                        UseWaitCursor = false;
                        SetTestResult(result.Success, result.Message);
                    });
                }
                catch
                {
                }
            });
        }

        private void SetTestResult(bool success, string message)
        {
            _testResultLabel.ForeColor = success ? Theme.Green : Theme.Red;
            _testResultLabel.Text = message;
        }

        private void TrySave()
        {
            _errorLabel.Text = "";

            if (_customProxyRadio.Checked)
            {
                string proxyError;
                if (!ValidateProxy(out proxyError))
                {
                    _errorLabel.Text = proxyError;
                    return;
                }
            }

            ResultSettings = BuildSettings();
            ResultToken = _tokenBox.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }

        private AppSettings BuildSettings()
        {
            AppSettings result = _settings.Clone();
            if (_intervalCombo.SelectedIndex >= 0 && _intervalCombo.SelectedIndex < IntervalValues.Length)
            {
                result.IntervalMinutes = IntervalValues[_intervalCombo.SelectedIndex];
            }
            else
            {
                result.IntervalMinutes = (int)_customMinutes.Value;
            }

            if (_noProxyRadio.Checked) result.ProxyMode = "none";
            else if (_customProxyRadio.Checked) result.ProxyMode = "custom";
            else result.ProxyMode = "system";

            result.ProxyAddress = _proxyAddressBox.Text.Trim();
            result.ProxyUsername = _proxyUserBox.Text.Trim();
            result.ProxyPassword = _proxyPassBox.Text;
            result.AutoStart = _autoStartBox.Checked;
            result.CheckOnStartup = _checkOnStartupBox.Checked;
            result.CloseToTray = _closeToTrayBox.Checked;
            result.NotifyOnError = _notifyOnErrorBox.Checked;
            return result;
        }

        private bool ValidateProxy(out string error)
        {
            error = "";
            string address = _proxyAddressBox.Text.Trim();
            if (address.Length == 0)
            {
                error = L10n.T("settings.proxyRequired");
                return false;
            }
            if (address.IndexOf("://", StringComparison.Ordinal) < 0)
            {
                address = "http://" + address;
            }
            Uri uri;
            if (!Uri.TryCreate(address, UriKind.Absolute, out uri) || string.IsNullOrEmpty(uri.Host))
            {
                error = L10n.T("settings.proxyInvalid");
                return false;
            }
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                error = L10n.T("settings.proxyScheme");
                return false;
            }
            return true;
        }

        private static Label MakeSectionTitle(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Font = Theme.Ui(10F, FontStyle.Bold);
            label.ForeColor = Theme.Text;
            label.Location = new Point(x, y);
            return label;
        }

        private static Label MakeLabel(string text, int x, int y, int width, int height)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.Font = Theme.Ui(9F);
            label.ForeColor = Theme.Text;
            label.Location = new Point(x, y);
            label.Size = new Size(width, height);
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static Label MakeNote(string text, int x, int y, int width, int height)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.Font = Theme.Ui(8F);
            label.ForeColor = Theme.Muted;
            label.Location = new Point(x, y);
            label.Size = new Size(width, height);
            return label;
        }

        private static CheckBox MakeCheckBox(string text, int x, int y, int width)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Font = Theme.Ui(9F);
            box.ForeColor = Theme.Text;
            box.AutoSize = false;
            box.Location = new Point(x, y);
            box.Size = new Size(width, 24);
            return box;
        }

        private static Panel MakeSeparator(int x, int y, int width)
        {
            Panel panel = new Panel();
            panel.BackColor = Theme.Border;
            panel.Location = new Point(x, y);
            panel.Size = new Size(width, 1);
            return panel;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error("打开链接失败: " + url, ex);
            }
        }

        private static void OpenPath(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error("打开目录失败: " + path, ex);
            }
        }

        private static void OpenLog()
        {
            try
            {
                Directory.CreateDirectory(ConfigStore.BaseDir);
                if (!File.Exists(ConfigStore.LogPath))
                {
                    File.WriteAllText(ConfigStore.LogPath, "");
                }
                Process.Start(new ProcessStartInfo("notepad.exe", "\"" + ConfigStore.LogPath + "\"")
                {
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                Log.Error("打开日志失败", ex);
            }
        }
    }
}
