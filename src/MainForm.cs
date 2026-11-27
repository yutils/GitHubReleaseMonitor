using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace GitHubReleaseMonitor
{
    public sealed class MainForm : Form
    {
        public const string WindowTitle = "GitHub Release Monitor";

        private readonly AppConfig _config;
        private readonly bool _suppressAutoStart;
        private readonly bool _startMinimized;

        private readonly HeaderBar _header;
        private readonly RoundedButton _languageButton;
        private readonly GlyphButton _settingsButton;
        private readonly GlyphButton _minimizeButton;
        private readonly GlyphButton _closeButton;

        private readonly Panel _content;
        private readonly Panel _toolbar;
        private readonly Label _listTitle;
        private readonly Label _listCount;
        private readonly RoundedButton _addButton;
        private readonly Panel _listHost;
        private readonly Panel _listPanel;
        private readonly EmptyView _emptyView;

        private readonly Panel _statusBar;
        private readonly Label _statusLabel;
        private readonly LinkLabel _versionLabel;
        private readonly Label _updateDot;
        private readonly Label _rateLabel;
        private readonly RoundedButton _checkButton;

        private readonly NotifyIcon _tray;
        private readonly ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _trayOpenItem;
        private ToolStripMenuItem _trayCheckItem;
        private ToolStripMenuItem _trayUpdateItem;
        private ToolStripMenuItem _trayPauseItem;
        private ToolStripMenuItem _trayExitItem;

        private readonly System.Windows.Forms.Timer _timer;
        private readonly ToolTip _toolTip;
        private readonly Dictionary<string, RepoCard> _cards;
        private readonly List<string> _balloonRepoIds;

        private bool _exiting;
        private bool _checking;
        private bool _paused;
        private bool _checkOnFinish;
        private bool _loaded;
        private bool _layingOutCards;
        private bool _trayShowsUnread;
        private bool _appUpdateChecking;
        private bool _appUpdateAvailable;
        private bool _appUpdateUpToDate;
        private string _appUpdateVersion = "";
        private string _appUpdateUrl = "";

        private DateTime _nextCheckAt = DateTime.MinValue;
        private DateTime _lastCheckAt = DateTime.MinValue;
        private int _apiLimit = -1;
        private int _apiRemaining = -1;
        private long _apiResetUnix;
        private Color _statusDotColor = Theme.Green;

        public MainForm(AppConfig config, bool suppressAutoStart, bool startMinimized)
        {
            _config = config ?? new AppConfig();
            _suppressAutoStart = suppressAutoStart;
            _startMinimized = startMinimized;
            _cards = new Dictionary<string, RepoCard>();
            _balloonRepoIds = new List<string>();

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            Text = WindowTitle;
            Icon = AppIcon.Large;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 620);
            BackColor = Theme.Background;
            Font = Theme.Ui(9F);
            DoubleBuffered = true;
            KeyPreview = true;

            _toolTip = new ToolTip();

            _header = new HeaderBar();
            _header.Dock = DockStyle.Top;

            _languageButton = new RoundedButton();
            _languageButton.Kind = ButtonKind.Secondary;
            _languageButton.Size = new Size(48, 28);
            _languageButton.SurfaceColor = Color.White;
            _languageButton.Font = Theme.Ui(8.5F);
            _languageButton.Click += delegate { ToggleLanguage(); };

            _settingsButton = new GlyphButton();
            _settingsButton.Glyph = "\uE713";
            _settingsButton.Click += delegate { OpenSettings(); };

            _minimizeButton = new GlyphButton();
            _minimizeButton.Glyph = "\uE921";
            _minimizeButton.Click += delegate { WindowState = FormWindowState.Minimized; };

            _closeButton = new GlyphButton();
            _closeButton.Glyph = "\uE8BB";
            _closeButton.HoverBackground = Theme.Red;
            _closeButton.HoverGlyphColor = Color.White;
            _closeButton.Click += delegate { Close(); };

            _header.Controls.Add(_languageButton);
            _header.Controls.Add(_settingsButton);
            _header.Controls.Add(_minimizeButton);
            _header.Controls.Add(_closeButton);
            _header.Resize += delegate { LayoutHeaderButtons(); };

            _content = new Panel();
            _content.Dock = DockStyle.Fill;
            _content.BackColor = Theme.Background;
            _content.Padding = new Padding(24, 14, 24, 8);

            _toolbar = new Panel();
            _toolbar.Dock = DockStyle.Top;
            _toolbar.Height = 46;
            _toolbar.BackColor = Theme.Background;

            _listTitle = new Label();
            _listTitle.Text = L10n.T("main.title");
            _listTitle.AutoSize = true;
            _listTitle.Font = Theme.Ui(10.5F, FontStyle.Bold);
            _listTitle.ForeColor = Theme.Text;
            _listTitle.Location = new Point(0, 6);

            _listCount = new Label();
            _listCount.Text = "";
            _listCount.AutoSize = true;
            _listCount.Font = Theme.Ui(8.5F);
            _listCount.ForeColor = Theme.Muted;
            _listCount.Location = new Point(0, 10);

            _addButton = new RoundedButton();
            _addButton.Text = L10n.T("common.addRepo");
            _addButton.Kind = ButtonKind.Primary;
            _addButton.SurfaceColor = Theme.Background;
            _addButton.Size = new Size(112, 32);
            _addButton.Click += delegate { AddRepo(); };

            _toolbar.Controls.Add(_listTitle);
            _toolbar.Controls.Add(_listCount);
            _toolbar.Controls.Add(_addButton);
            _toolbar.Resize += delegate { LayoutToolbar(); };

            _listHost = new Panel();
            _listHost.Dock = DockStyle.Fill;
            _listHost.BackColor = Theme.Background;

            _listPanel = new Panel();
            _listPanel.Dock = DockStyle.Fill;
            _listPanel.BackColor = Theme.Background;
            _listPanel.AutoScroll = true;
            _listPanel.Resize += delegate { RelayoutCards(); };

            _emptyView = new EmptyView();
            _emptyView.Dock = DockStyle.Fill;
            _emptyView.AddRequested += delegate { AddRepo(); };

            _listHost.Controls.Add(_listPanel);
            _listHost.Controls.Add(_emptyView);

            _content.Controls.Add(_listHost);
            _content.Controls.Add(_toolbar);

            _statusBar = new Panel();
            _statusBar.Dock = DockStyle.Bottom;
            _statusBar.Height = 54;
            _statusBar.BackColor = Color.White;
            _statusBar.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen border = new Pen(Theme.Border))
                {
                    e.Graphics.DrawLine(border, 0, 0, _statusBar.Width, 0);
                }
                if (_config.Repos.Count == 0)
                {
                    return;
                }
                using (SolidBrush brush = new SolidBrush(_statusDotColor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(brush, 24, 23, 7, 7);
                }
            };
            _statusBar.Resize += delegate { LayoutStatusBar(); };

            _statusLabel = new Label();
            _statusLabel.AutoSize = false;
            _statusLabel.AutoEllipsis = true;
            _statusLabel.Font = Theme.Ui(8.5F);
            _statusLabel.ForeColor = Theme.Muted;
            _statusLabel.Location = new Point(36, 17);
            _statusLabel.Size = new Size(500, 20);

            _versionLabel = new LinkLabel();
            _versionLabel.AutoSize = true;
            _versionLabel.Font = Theme.Ui(8.5F);
            _versionLabel.LinkColor = Theme.Accent;
            _versionLabel.ActiveLinkColor = Theme.AccentHover;
            _versionLabel.VisitedLinkColor = Theme.Accent;
            _versionLabel.LinkBehavior = LinkBehavior.HoverUnderline;
            _versionLabel.Cursor = Cursors.Hand;
            _versionLabel.Location = new Point(500, 17);
            _versionLabel.LinkClicked += delegate { OnVersionLabelClicked(); };

            _updateDot = new Label();
            _updateDot.AutoSize = false;
            _updateDot.Font = Theme.Ui(7F);
            _updateDot.ForeColor = Theme.Red;
            _updateDot.BackColor = Color.White;
            _updateDot.Text = "●";
            _updateDot.TextAlign = ContentAlignment.MiddleCenter;
            _updateDot.Size = new Size(12, 20);
            _updateDot.Visible = false;

            _rateLabel = new Label();
            _rateLabel.AutoSize = true;
            _rateLabel.Font = Theme.Ui(8.5F);
            _rateLabel.ForeColor = Theme.Faint;
            _rateLabel.Location = new Point(600, 20);
            _rateLabel.Text = "API —";

            _checkButton = new RoundedButton();
            _checkButton.Text = L10n.T("main.checkNow");
            _checkButton.Kind = ButtonKind.Secondary;
            _checkButton.SurfaceColor = Color.White;
            _checkButton.Size = new Size(104, 32);
            _checkButton.Click += delegate { StartCheck(true); };

            _statusBar.Controls.Add(_statusLabel);
            _statusBar.Controls.Add(_versionLabel);
            _statusBar.Controls.Add(_updateDot);
            _statusBar.Controls.Add(_rateLabel);
            _statusBar.Controls.Add(_checkButton);

            Controls.Add(_content);
            Controls.Add(_statusBar);
            Controls.Add(_header);

            _trayMenu = BuildTrayMenu();
            _tray = new NotifyIcon();
            _tray.Icon = AppIcon.Small;
            _tray.Text = AppInfo.Name;
            _tray.Visible = true;
            _tray.ContextMenuStrip = _trayMenu;
            _tray.DoubleClick += delegate { ShowMainWindow(); };
            _tray.BalloonTipClicked += delegate { OnBalloonTipClicked(); };

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 1000;
            _timer.Tick += delegate { OnTimerTick(); };

            EnableWindowDrag(_content);
            EnableWindowDrag(_toolbar);
            EnableWindowDrag(_listHost);
            EnableWindowDrag(_listPanel);
            EnableWindowDrag(_statusBar);
            EnableWindowDrag(_emptyView);

            RebuildCards();
            ApplyLanguage();

            KeyDown += OnMainKeyDown;

            Load += OnMainLoad;
            Shown += OnMainShown;
            Resize += delegate { LayoutHeaderButtons(); LayoutToolbar(); LayoutStatusBar(); };
            FormClosing += OnMainFormClosing;
            FormClosed += delegate { _timer.Stop(); };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.TryEnableRoundedCorners(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == AppMessages.ShowWindow)
            {
                ShowMainWindow();
                return;
            }
            base.WndProc(ref m);
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = Theme.Ui(9F);
            menu.ShowImageMargin = false;

            _trayOpenItem = new ToolStripMenuItem(L10n.T("main.trayOpen"));
            _trayOpenItem.Font = Theme.Ui(9F, FontStyle.Bold);
            _trayOpenItem.Click += delegate { ShowMainWindow(); };

            _trayCheckItem = new ToolStripMenuItem(L10n.T("main.trayCheck"));
            _trayCheckItem.Click += delegate { StartCheck(true); };

            _trayUpdateItem = new ToolStripMenuItem(L10n.T("main.trayUpdate"));
            _trayUpdateItem.Click += delegate { OnTrayUpdateClicked(); };

            _trayPauseItem = new ToolStripMenuItem(L10n.T("main.trayPause"));
            _trayPauseItem.Click += delegate { TogglePause(); };

            _trayExitItem = new ToolStripMenuItem(L10n.T("main.trayExit"));
            _trayExitItem.Click += delegate { ExitApplication(); };

            menu.Items.Add(_trayOpenItem);
            menu.Items.Add(_trayCheckItem);
            menu.Items.Add(_trayUpdateItem);
            menu.Items.Add(_trayPauseItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_trayExitItem);
            return menu;
        }

        private void OnMainLoad(object sender, EventArgs e)
        {
            ClampToWorkingArea();
            if (!_suppressAutoStart)
            {
                StartupManager.Apply(_config.Settings.AutoStart);
            }

            LoadLastCheckTime();
            _loaded = true;
            _timer.Start();

            if (_config.Settings.CheckOnStartup && EnabledRepoCount() > 0 && !_paused)
            {
                _nextCheckAt = DateTime.Now.AddSeconds(2);
            }
            else
            {
                ScheduleNext();
            }

            UpdateAllChrome();
            CheckAppUpdate();
        }

        private void OnMainShown(object sender, EventArgs e)
        {
            if (_startMinimized && _config.Repos.Count > 0)
            {
                HideToTray(false);
            }
        }

        private void ClampToWorkingArea()
        {
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int maxWidth = Math.Max(700, area.Width - 40);
            int maxHeight = Math.Max(480, area.Height - 48);
            int newWidth = Math.Min(Width, maxWidth);
            int newHeight = Math.Min(Height, maxHeight);
            if (newWidth != Width || newHeight != Height)
            {
                Size = new Size(newWidth, newHeight);
            }
            Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
            Top = area.Top + Math.Max(0, (area.Height - Height) / 2);
        }

        private void LoadLastCheckTime()
        {
            DateTime latest = DateTime.MinValue;
            foreach (RepoConfig repo in _config.Repos)
            {
                DateTime utc;
                if (TimeText.TryParseUtc(repo.LastCheckedAt, out utc))
                {
                    DateTime local = utc.ToLocalTime();
                    if (local > latest) latest = local;
                }
            }
            _lastCheckAt = latest;
        }

        private void OnMainKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.N)
            {
                AddRepo();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if ((e.Control && e.KeyCode == Keys.R) || e.KeyCode == Keys.F5)
            {
                StartCheck(true);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Oemcomma)
            {
                OpenSettings();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void ToggleLanguage()
        {
            L10n.SetLanguage(L10n.IsEnglish ? "zh-CN" : "en-US");
            _config.Settings.Language = L10n.Current;
            ConfigStore.Save(_config);
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            if (_languageButton == null) return;

            _languageButton.Text = L10n.IsEnglish ? "中文" : "EN";
            string languageTip = L10n.IsEnglish
                ? L10n.T("header.languageToChinese")
                : L10n.T("header.languageToEnglish");
            _languageButton.AccessibleName = languageTip;
            _toolTip.SetToolTip(_languageButton, languageTip);
            _toolTip.SetToolTip(_settingsButton, L10n.T("common.settingsTooltip"));
            _toolTip.SetToolTip(_minimizeButton, L10n.T("common.minimize"));
            _toolTip.SetToolTip(_closeButton, L10n.T("common.closeToTray"));
            _settingsButton.AccessibleName = L10n.T("common.settings");
            _minimizeButton.AccessibleName = L10n.T("common.minimize");
            _closeButton.AccessibleName = L10n.T("common.closeToTray");

            _listTitle.Text = L10n.T("main.title");
            _addButton.Text = L10n.T("common.addRepo");
            _addButton.Width = L10n.IsEnglish ? 122 : 112;
            _emptyView.ApplyLanguage();

            foreach (RepoCard card in _cards.Values)
            {
                card.RefreshView();
            }

            _trayOpenItem.Text = L10n.T("main.trayOpen");
            _trayCheckItem.Text = L10n.T("main.trayCheck");
            _trayPauseItem.Text = _paused ? L10n.T("main.trayResume") : L10n.T("main.trayPause");
            _trayExitItem.Text = L10n.T("main.trayExit");

            UpdateAppUpdateUi();
            LayoutHeaderButtons();
            LayoutToolbar();
            UpdateAllChrome();
        }

        private void OnVersionLabelClicked()
        {
            if (_appUpdateAvailable)
            {
                OpenUrl(_appUpdateUrl);
                return;
            }
            CheckAppUpdate();
        }

        private void OnTrayUpdateClicked()
        {
            if (_appUpdateAvailable)
            {
                OpenUrl(_appUpdateUrl);
                return;
            }
            CheckAppUpdate();
        }

        private void CheckAppUpdate()
        {
            if (_appUpdateChecking) return;

            _appUpdateChecking = true;
            _appUpdateUpToDate = false;
            UpdateAppUpdateUi();
            UpdateTray();

            AppSettings settings = _config.Settings.Clone();
            string token = _config.GithubToken;

            ThreadPool.QueueUserWorkItem(delegate
            {
                AppUpdateResult result = AppUpdateChecker.Check(settings, token);
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed || Disposing) return;

                        _appUpdateChecking = false;
                        if (result.Status == AppUpdateStatus.Available)
                        {
                            _appUpdateAvailable = true;
                            _appUpdateVersion = result.Version;
                            _appUpdateUrl = result.Url;
                            _appUpdateUpToDate = false;
                        }
                        else if (result.Status == AppUpdateStatus.UpToDate)
                        {
                            _appUpdateAvailable = false;
                            _appUpdateUpToDate = true;
                        }
                        else if (!_appUpdateAvailable)
                        {
                            _appUpdateUpToDate = false;
                        }

                        UpdateAppUpdateUi();
                        UpdateTray();
                        UpdateStatusBar();
                    });
                }
                catch
                {
                }
            });
        }

        private void UpdateAppUpdateUi()
        {
            if (_versionLabel == null) return;

            string current = "v" + AppInfo.Version;
            if (_appUpdateAvailable)
            {
                _versionLabel.Text = current + " → v" + _appUpdateVersion;
                _versionLabel.LinkColor = Theme.Red;
                _versionLabel.ActiveLinkColor = Theme.Orange;
                _updateDot.Visible = true;
                string tip = L10n.F("main.updateReadyTooltip", _appUpdateVersion);
                _toolTip.SetToolTip(_versionLabel, tip);
                _toolTip.SetToolTip(_updateDot, tip);
            }
            else
            {
                _versionLabel.LinkColor = Theme.Accent;
                _versionLabel.ActiveLinkColor = Theme.AccentHover;
                _updateDot.Visible = false;

                if (_appUpdateChecking)
                {
                    _versionLabel.Text = current + " · " + L10n.T("main.checkingUpdateShort");
                    string tip = L10n.T("main.checkingUpdateTooltip");
                    _toolTip.SetToolTip(_versionLabel, tip);
                    _toolTip.SetToolTip(_updateDot, tip);
                }
                else if (_appUpdateUpToDate)
                {
                    _versionLabel.Text = current + " · " + L10n.T("main.upToDateShort");
                    string tip = L10n.T("main.upToDateTooltip");
                    _toolTip.SetToolTip(_versionLabel, tip);
                    _toolTip.SetToolTip(_updateDot, tip);
                }
                else
                {
                    _versionLabel.Text = current + " · " + L10n.T("main.checkUpdate");
                    string tip = L10n.F("main.checkUpdateTooltip", AppInfo.Version);
                    _toolTip.SetToolTip(_versionLabel, tip);
                    _toolTip.SetToolTip(_updateDot, tip);
                }
            }

            _versionLabel.LinkArea = new LinkArea(0, _versionLabel.Text.Length);
            if (_trayUpdateItem != null)
            {
                _trayUpdateItem.Text = _appUpdateAvailable
                    ? L10n.F("main.trayUpdateAvailable", _appUpdateVersion)
                    : L10n.T("main.trayUpdate");
                _trayUpdateItem.ForeColor = _appUpdateAvailable ? Theme.Red : SystemColors.ControlText;
            }
            LayoutStatusBar();
        }

        private void EnableWindowDrag(Control control)
        {
            if (control == null) return;
            control.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left) return;
                Form form = FindForm();
                if (form != null)
                {
                    NativeMethods.StartWindowDrag(form.Handle);
                }
            };
        }

        private void LayoutHeaderButtons()
        {
            if (_header == null) return;
            int right = _header.ClientSize.Width - 18;
            _closeButton.SetBounds(right - 38, 16, 38, 32);
            _minimizeButton.SetBounds(_closeButton.Left - 4 - 38, 16, 38, 32);
            _settingsButton.SetBounds(_minimizeButton.Left - 8 - 38, 16, 38, 32);
            _languageButton.SetBounds(_settingsButton.Left - 10 - _languageButton.Width, 18,
                _languageButton.Width, _languageButton.Height);
        }

        private void LayoutToolbar()
        {
            if (_toolbar == null) return;
            int right = _toolbar.ClientSize.Width;
            _addButton.SetBounds(right - _addButton.Width, 2, _addButton.Width, _addButton.Height);
            _listCount.Location = new Point(_listTitle.Right + 10, 10);
        }

        private void LayoutStatusBar()
        {
            if (_statusBar == null) return;
            _checkButton.SetBounds(_statusBar.ClientSize.Width - 24 - _checkButton.Width, 11,
                _checkButton.Width, _checkButton.Height);
            int rateRight = _checkButton.Left - 16;
            _rateLabel.Location = new Point(rateRight - _rateLabel.Width, 20);
            _updateDot.SetBounds(_rateLabel.Left - 18 - _updateDot.Width, 17,
                _updateDot.Width, _updateDot.Height);
            _versionLabel.Location = new Point(
                Math.Max(220, _updateDot.Left - 4 - _versionLabel.Width), 16);
            _statusLabel.SetBounds(36, 17, Math.Max(100, _versionLabel.Left - 48), 20);
        }

        private void RebuildCards()
        {
            List<RepoCard> oldCards = new List<RepoCard>(_cards.Values);
            _cards.Clear();
            _listPanel.Controls.Clear();
            foreach (RepoCard old in oldCards)
            {
                old.Dispose();
            }

            foreach (RepoConfig repo in _config.Repos)
            {
                RepoCard card = new RepoCard(repo);
                card.ActionRequested += OnCardActionRequested;
                card.ToggleChanged += OnCardToggleChanged;
                _cards[repo.Id] = card;
                _listPanel.Controls.Add(card);
            }

            _listPanel.Visible = _config.Repos.Count > 0;
            _emptyView.Visible = _config.Repos.Count == 0;
            RelayoutCards();
            UpdateAllChrome();
        }

        private void RelayoutCards()
        {
            if (_layingOutCards) return;
            _layingOutCards = true;
            try
            {
                int width = Math.Max(320, _listPanel.ClientSize.Width - 2);
                int top = 0;
                foreach (RepoConfig repo in _config.Repos)
                {
                    RepoCard card;
                    if (!_cards.TryGetValue(repo.Id, out card)) continue;
                    card.SetBounds(0, top, width, card.Height);
                    top += card.Height + 10;
                }
            }
            finally
            {
                _layingOutCards = false;
            }
        }

        private void UpdateAllChrome()
        {
            UpdateHeader();
            UpdateToolbar();
            UpdateStatusBar();
            UpdateTray();
        }

        private void UpdateHeader()
        {
            if (_header == null) return;
            int total = _config.Repos.Count;
            int unread = EnabledUnreadCount();
            string subtitle;

            if (total == 0)
            {
                subtitle = L10n.T("header.noRepos");
            }
            else if (_checking)
            {
                int enabled = EnabledRepoCount();
                subtitle = L10n.F(enabled == 1 ? "header.checkingOne" : "header.checking", enabled);
            }
            else if (_paused)
            {
                subtitle = L10n.F(total == 1 ? "header.pausedOne" : "header.paused", total);
            }
            else if (unread > 0)
            {
                subtitle = L10n.F(unread == 1 ? "header.unreadOne" : "header.unread", unread, total);
            }
            else
            {
                subtitle = L10n.F(total == 1 ? "header.intervalOne" : "header.interval",
                    TimeText.Interval(_config.Settings.IntervalMinutes), total);
            }
            _header.SetSubtitle(subtitle);
        }

        private void UpdateToolbar()
        {
            if (_listCount == null) return;
            int total = _config.Repos.Count;
            int unread = EnabledUnreadCount();
            if (total == 0)
            {
                _listCount.Text = "";
            }
            else if (unread > 0)
            {
                _listCount.Text = L10n.F(total == 1 ? "main.countUnreadOne" : "main.countUnread", total, unread);
            }
            else
            {
                _listCount.Text = L10n.F(total == 1 ? "main.countOne" : "main.count", total);
            }
            _listCount.Location = new Point(_listTitle.Right + 10, 10);
        }

        private void UpdateStatusBar()
        {
            if (_statusLabel == null) return;

            int enabled = EnabledRepoCount();
            int total = _config.Repos.Count;
            _checkButton.Enabled = !_checking && enabled > 0;
            _checkButton.Text = _checking ? L10n.T("main.checkingButton") : L10n.T("main.checkNow");

            string text;
            if (_checking)
            {
                text = L10n.T("main.checkingStatus");
                if (_lastCheckAt != DateTime.MinValue)
                {
                    text += " · " + L10n.F("main.lastStatus", TimeText.RelativeFromLocal(_lastCheckAt));
                }
            }
            else if (_paused)
            {
                text = L10n.T("main.pausedStatus");
                if (_lastCheckAt != DateTime.MinValue)
                {
                    text += " · " + L10n.F("main.lastStatus", TimeText.RelativeFromLocal(_lastCheckAt));
                }
            }
            else if (total == 0)
            {
                text = L10n.T("main.noReposStatus");
            }
            else if (enabled == 0)
            {
                text = L10n.T("main.allPausedStatus");
            }
            else
            {
                TimeSpan left = _nextCheckAt - DateTime.Now;
                string nextPart = left.TotalSeconds <= 0
                    ? L10n.T("main.dueNow")
                    : L10n.F("main.nextStatus", _nextCheckAt.ToString("HH:mm"), TimeText.Countdown(left));
                string lastPart = _lastCheckAt == DateTime.MinValue
                    ? L10n.T("main.neverStatus")
                    : L10n.F("main.lastStatus", TimeText.RelativeFromLocal(_lastCheckAt));
                text = nextPart + " · " + lastPart;
            }
            _statusLabel.Text = text;

            if (_apiRemaining >= 0)
            {
                _rateLabel.Text = "API " + _apiRemaining + (_apiLimit > 0 ? "/" + _apiLimit : "");
                _rateLabel.ForeColor = _apiRemaining <= 5 ? Theme.Red : Theme.Faint;
                string tip = L10n.F("main.apiTooltip", _apiRemaining,
                    _apiLimit > 0 ? _apiLimit.ToString() : "?");
                if (_apiResetUnix > 0)
                {
                    try
                    {
                        DateTime reset = DateTimeOffset.FromUnixTimeSeconds(_apiResetUnix).LocalDateTime;
                        tip += "\r\n" + L10n.F("main.apiReset", reset.ToString("HH:mm"));
                    }
                    catch
                    {
                    }
                }
                _toolTip.SetToolTip(_rateLabel, tip);
            }
            else
            {
                _rateLabel.Text = "API —";
                _rateLabel.ForeColor = Theme.Faint;
                _toolTip.SetToolTip(_rateLabel, L10n.T("main.apiHint"));
            }

            if (_paused)
            {
                _statusDotColor = Theme.Faint;
            }
            else if (_checking)
            {
                _statusDotColor = Theme.Accent;
            }
            else if (HasAnyError())
            {
                _statusDotColor = Theme.Red;
            }
            else if (EnabledUnreadCount() > 0)
            {
                _statusDotColor = Theme.Orange;
            }
            else if (total == 0)
            {
                _statusDotColor = Theme.Faint;
            }
            else
            {
                _statusDotColor = Theme.Green;
            }

            LayoutStatusBar();
            _statusBar.Invalidate();
        }

        private void UpdateTray()
        {
            if (_tray == null) return;

            int total = _config.Repos.Count;
            int unread = EnabledUnreadCount();
            string text = AppInfo.Name;
            if (total > 0) text += " · " + L10n.F(total == 1 ? "main.trayReposOne" : "main.trayRepos", total);
            if (unread > 0) text += " · " + L10n.F("main.trayUpdates", unread);
            if (_appUpdateAvailable) text += " · " + L10n.F("main.trayAppUpdate", _appUpdateVersion);
            if (text.Length > 62) text = text.Substring(0, 62);
            if (_tray.Text != text) _tray.Text = text;

            bool hasUnread = unread > 0 || _appUpdateAvailable;
            if (_tray.Icon == null || _trayShowsUnread != hasUnread)
            {
                _tray.Icon = hasUnread ? AppIcon.Unread : AppIcon.Small;
                _trayShowsUnread = hasUnread;
            }

            _trayCheckItem.Enabled = !_checking && EnabledRepoCount() > 0;
            _trayUpdateItem.Enabled = !_appUpdateChecking;
            _trayPauseItem.Text = _paused ? L10n.T("main.trayResume") : L10n.T("main.trayPause");
        }

        private void OnTimerTick()
        {
            if (!_loaded || _exiting) return;
            if (!_checking && !_paused && EnabledRepoCount() > 0 && DateTime.Now >= _nextCheckAt)
            {
                StartCheck(false);
                return;
            }
            UpdateStatusBar();
        }

        private void ScheduleNext()
        {
            _nextCheckAt = DateTime.Now.AddMinutes(Math.Max(1, _config.Settings.IntervalMinutes));
        }

        private void StartCheck(bool manual)
        {
            if (_checking) return;

            List<CheckTarget> targets = new List<CheckTarget>();
            foreach (RepoConfig repo in _config.Repos)
            {
                if (!repo.Enabled) continue;
                CheckTarget target = new CheckTarget();
                target.RepoId = repo.Id;
                target.Owner = repo.Owner;
                target.Repo = repo.Repo;
                target.IncludePrerelease = repo.IncludePrerelease;
                targets.Add(target);
            }

            if (targets.Count == 0)
            {
                UpdateStatusBar();
                return;
            }

            _checking = true;
            UpdateHeader();
            UpdateStatusBar();
            UpdateTray();

            AppSettings settings = _config.Settings.Clone();
            string token = _config.GithubToken;
            CheckTarget[] targetArray = targets.ToArray();

            ThreadPool.QueueUserWorkItem(delegate
            {
                CheckSummary summary;
                try
                {
                    summary = CheckRunner.Run(targetArray, settings, token);
                }
                catch (Exception ex)
                {
                    Log.Error("检查更新时发生异常", ex);
                    summary = new CheckSummary();
                }

                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed || Disposing) return;
                        FinishCheck(summary);
                    });
                }
                catch
                {
                }
            });
        }

        private void FinishCheck(CheckSummary summary)
        {
            _checking = false;
            _lastCheckAt = DateTime.Now;

            if (summary == null) summary = new CheckSummary();
            if (summary.RateRemaining >= 0)
            {
                _apiLimit = summary.RateLimit;
                _apiRemaining = summary.RateRemaining;
                _apiResetUnix = summary.RateResetUnix;
            }

            List<RepoConfig> updates = new List<RepoConfig>();
            List<RepoConfig> newErrors = new List<RepoConfig>();

            foreach (RepoCheckOutcome outcome in summary.Outcomes)
            {
                RepoConfig repo = FindRepo(outcome.RepoId);
                if (repo == null) continue;

                string previousStatus = repo.LastStatus;
                bool isNew = ApplyOutcome(repo, outcome);
                if (isNew && repo.Notify)
                {
                    updates.Add(repo);
                }
                if (repo.LastStatus == "error" && previousStatus != "error")
                {
                    newErrors.Add(repo);
                }
            }

            ConfigStore.Save(_config);
            foreach (RepoCard card in _cards.Values)
            {
                card.RefreshView();
            }

            UpdateAllChrome();
            ShowCheckResultBalloon(updates, newErrors);

            if (_paused)
            {
                _nextCheckAt = DateTime.MinValue;
            }
            else
            {
                ScheduleNext();
            }
            UpdateStatusBar();

            if (_checkOnFinish && !_exiting)
            {
                _checkOnFinish = false;
                StartCheck(true);
            }
        }

        private bool ApplyOutcome(RepoConfig repo, RepoCheckOutcome outcome)
        {
            repo.LastCheckedAt = outcome.CheckedAtUtc.ToString("o");
            ReleaseLookupResult result = outcome.Result;

            if (result == null)
            {
                repo.LastStatus = "error";
                repo.LastError = L10n.T("common.unknownError");
                return false;
            }

            if (!result.Success)
            {
                repo.LastStatus = "error";
                repo.LastError = string.IsNullOrEmpty(result.Error) ? L10n.T("common.checkFailed") : result.Error;
                return false;
            }

            repo.LastError = "";

            if (result.NoRelease || result.Release == null)
            {
                repo.LatestReleaseId = 0;
                repo.LatestTag = "";
                repo.LatestName = "";
                repo.LatestUrl = "";
                repo.LatestPublishedAt = "";
                if (!repo.BaselineInitialized)
                {
                    repo.BaselineInitialized = true;
                    repo.SeenReleaseId = 0;
                }
                ClearUnread(repo);
                repo.LastStatus = "no-release";
                return false;
            }

            ReleaseInfo release = result.Release;
            bool firstCheck = !repo.BaselineInitialized;
            bool hasNewRelease = !firstCheck && repo.SeenReleaseId != release.Id;

            repo.LatestReleaseId = release.Id;
            repo.LatestTag = release.TagName ?? "";
            repo.LatestName = string.IsNullOrEmpty(release.Name) ? repo.LatestTag : release.Name;
            repo.LatestUrl = release.HtmlUrl ?? "";
            repo.LatestPublishedAt = release.PublishedAt ?? "";

            if (firstCheck)
            {
                repo.BaselineInitialized = true;
                repo.SeenReleaseId = release.Id;
                repo.LastStatus = repo.HasUnread ? "new" : "baseline";
                return false;
            }

            if (hasNewRelease)
            {
                repo.SeenReleaseId = release.Id;
                repo.UnreadReleaseId = release.Id;
                repo.UnreadTag = repo.LatestTag;
                repo.UnreadName = repo.LatestName;
                repo.UnreadUrl = repo.LatestUrl;
                repo.LastStatus = "new";
                return true;
            }

            repo.LastStatus = repo.HasUnread ? "new" : "uptodate";
            return false;
        }

        private void ShowCheckResultBalloon(List<RepoConfig> updates, List<RepoConfig> newErrors)
        {
            _balloonRepoIds.Clear();

            if (updates.Count == 1)
            {
                RepoConfig repo = updates[0];
                _balloonRepoIds.Add(repo.Id);
                string title = L10n.F("main.updateTitle", repo.Name);
                string tag = string.IsNullOrEmpty(repo.UnreadTag) ? L10n.T("main.newVersion") : repo.UnreadTag;
                string text = L10n.F("main.updateText", tag);
                _tray.ShowBalloonTip(10000, title, text, ToolTipIcon.Info);
                return;
            }

            if (updates.Count > 1)
            {
                List<string> labels = new List<string>();
                for (int i = 0; i < updates.Count && i < 3; i++)
                {
                    labels.Add(updates[i].Name + " " + updates[i].UnreadTag);
                }
                if (updates.Count > 3) labels.Add(L10n.F("main.multiEllipsis", updates.Count));
                string title = L10n.F("main.multiTitle", updates.Count);
                string text = L10n.F("main.multiText", string.Join(L10n.T("main.listSeparator"), labels.ToArray()));
                _tray.ShowBalloonTip(10000, title, CutText(text, 240), ToolTipIcon.Info);
                return;
            }

            if (_config.Settings.NotifyOnError && newErrors.Count > 0)
            {
                string text;
                if (newErrors.Count == 1)
                {
                    text = L10n.F("main.errorOne", newErrors[0].Name, newErrors[0].LastError);
                }
                else
                {
                    text = L10n.F("main.errorMany", newErrors.Count);
                }
                _tray.ShowBalloonTip(8000, L10n.T("main.errorTitle"), CutText(text, 240), ToolTipIcon.Warning);
            }
        }

        private void OnBalloonTipClicked()
        {
            if (_balloonRepoIds.Count == 1)
            {
                RepoConfig repo = FindRepo(_balloonRepoIds[0]);
                _balloonRepoIds.Clear();
                if (repo != null && !string.IsNullOrEmpty(repo.UnreadUrl))
                {
                    OpenUrl(repo.UnreadUrl);
                    ClearUnread(repo);
                    ConfigStore.Save(_config);
                    RefreshRepoCard(repo);
                    UpdateAllChrome();
                    return;
                }
            }
            _balloonRepoIds.Clear();
            ShowMainWindow();
        }

        private void OnCardActionRequested(RepoCard card, RepoCardAction action)
        {
            if (card == null) return;
            RepoConfig repo = card.Repo;
            switch (action)
            {
                case RepoCardAction.OpenRelease:
                    OpenRelease(repo);
                    break;
                case RepoCardAction.OpenRepository:
                    OpenUrl(repo.RepoUrl);
                    break;
                case RepoCardAction.MarkRead:
                    MarkRead(repo);
                    break;
                case RepoCardAction.Edit:
                    EditRepo(repo);
                    break;
                case RepoCardAction.Delete:
                    DeleteRepo(repo);
                    break;
            }
        }

        private void OnCardToggleChanged(object sender, EventArgs e)
        {
            RepoCard card = sender as RepoCard;
            if (card == null) return;
            card.Repo.Enabled = card.SwitchChecked;
            ConfigStore.Save(_config);
            card.RefreshView();
            UpdateAllChrome();
        }

        private void OpenRelease(RepoConfig repo)
        {
            if (string.IsNullOrEmpty(repo.LatestUrl)) return;
            OpenUrl(repo.LatestUrl);
            if (repo.HasUnread)
            {
                ClearUnread(repo);
                ConfigStore.Save(_config);
                RefreshRepoCard(repo);
                UpdateAllChrome();
            }
        }

        private void MarkRead(RepoConfig repo)
        {
            if (!repo.HasUnread) return;
            ClearUnread(repo);
            ConfigStore.Save(_config);
            RefreshRepoCard(repo);
            UpdateAllChrome();
        }

        private void ClearUnread(RepoConfig repo)
        {
            repo.UnreadReleaseId = 0;
            repo.UnreadTag = "";
            repo.UnreadName = "";
            repo.UnreadUrl = "";
            if (repo.LastStatus == "new")
            {
                repo.LastStatus = string.IsNullOrEmpty(repo.LatestTag) ? "" : "uptodate";
            }
        }

        private void RefreshRepoCard(RepoConfig repo)
        {
            RepoCard card;
            if (_cards.TryGetValue(repo.Id, out card))
            {
                card.RefreshView();
            }
        }

        private void AddRepo()
        {
            using (AddRepoForm dialog = new AddRepoForm(_config, null))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                RepoConfig repo = new RepoConfig();
                repo.Id = Guid.NewGuid().ToString("N");
                repo.Name = dialog.ResultName;
                repo.Owner = dialog.ResultOwner;
                repo.Repo = dialog.ResultRepo;
                repo.IncludePrerelease = dialog.ResultIncludePrerelease;
                repo.Notify = dialog.ResultNotify;
                repo.Enabled = true;
                repo.BaselineInitialized = false;
                repo.LastStatus = "";

                _config.Repos.Add(repo);
                ConfigStore.Save(_config);
                RebuildCards();
                StartCheckOrDefer();
            }
        }

        private void EditRepo(RepoConfig repo)
        {
            using (AddRepoForm dialog = new AddRepoForm(_config, repo))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string oldSlug = repo.Owner + "/" + repo.Repo;
                string newSlug = dialog.ResultOwner + "/" + dialog.ResultRepo;
                bool slugChanged = !string.Equals(oldSlug, newSlug, StringComparison.OrdinalIgnoreCase);

                repo.Name = dialog.ResultName;
                repo.Owner = dialog.ResultOwner;
                repo.Repo = dialog.ResultRepo;
                repo.IncludePrerelease = dialog.ResultIncludePrerelease;
                repo.Notify = dialog.ResultNotify;

                if (slugChanged)
                {
                    repo.ResetTracking();
                }

                ConfigStore.Save(_config);
                RebuildCards();
                StartCheckOrDefer();
            }
        }

        private void DeleteRepo(RepoConfig repo)
        {
            string message = L10n.F("main.deleteMessage", repo.Name);
            if (MessageBox.Show(this, message, L10n.T("main.deleteTitle"), MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            _config.Repos.Remove(repo);
            ConfigStore.Save(_config);
            RebuildCards();
        }

        private void StartCheckOrDefer()
        {
            if (_checking)
            {
                _checkOnFinish = true;
                return;
            }
            StartCheck(true);
        }

        private void OpenSettings()
        {
            using (SettingsForm dialog = new SettingsForm(_config))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                _config.Settings = dialog.ResultSettings;
                _config.GithubToken = dialog.ResultToken;
                ConfigStore.Save(_config);

                if (!_suppressAutoStart)
                {
                    StartupManager.Apply(_config.Settings.AutoStart);
                }

                if (!_paused)
                {
                    ScheduleNext();
                }
                UpdateAllChrome();
            }
        }

        private void TogglePause()
        {
            _paused = !_paused;
            if (!_paused)
            {
                _nextCheckAt = DateTime.Now.AddSeconds(1);
            }
            UpdateAllChrome();
        }

        private void ShowMainWindow()
        {
            if (IsDisposed || Disposing) return;
            ShowInTaskbar = true;
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Show();
            Activate();
            try
            {
                NativeMethods.SetForegroundWindow(Handle);
            }
            catch
            {
            }
        }

        private void HideToTray(bool showTip)
        {
            Hide();
            ShowInTaskbar = false;
            if (showTip && !_config.Settings.CloseToTrayTipShown)
            {
                _balloonRepoIds.Clear();
                _tray.ShowBalloonTip(6000, L10n.T("main.minimizedTitle"),
                    L10n.T("main.minimizedText"), ToolTipIcon.Info);
                _config.Settings.CloseToTrayTipShown = true;
                ConfigStore.Save(_config);
            }
        }

        private void ExitApplication()
        {
            _exiting = true;
            Close();
        }

        private void OnMainFormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Info("关闭主窗口: reason=" + e.CloseReason + ", closeToTray=" + _config.Settings.CloseToTray +
                     ", exiting=" + _exiting);
            if (!_exiting && _config.Settings.CloseToTray &&
                e.CloseReason != CloseReason.WindowsShutDown &&
                e.CloseReason != CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                HideToTray(true);
                return;
            }

            _exiting = true;
            _timer.Stop();
            ConfigStore.Save(_config);
            if (_tray != null)
            {
                _tray.Visible = false;
            }
        }

        private int EnabledRepoCount()
        {
            int count = 0;
            foreach (RepoConfig repo in _config.Repos)
            {
                if (repo.Enabled) count++;
            }
            return count;
        }

        private int EnabledUnreadCount()
        {
            int count = 0;
            foreach (RepoConfig repo in _config.Repos)
            {
                if (repo.Enabled && repo.HasUnread) count++;
            }
            return count;
        }

        private bool HasAnyError()
        {
            foreach (RepoConfig repo in _config.Repos)
            {
                if (repo.Enabled && repo.LastStatus == "error") return true;
            }
            return false;
        }

        private RepoConfig FindRepo(string id)
        {
            foreach (RepoConfig repo in _config.Repos)
            {
                if (repo.Id == id) return repo;
            }
            return null;
        }

        private static string CutText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Replace("\r", " ").Replace("\n", " ").Trim();
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        private static void OpenUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error("打开链接失败: " + url, ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_timer != null) _timer.Dispose();
                if (_tray != null) _tray.Dispose();
                if (_trayMenu != null) _trayMenu.Dispose();
                if (_toolTip != null) _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
