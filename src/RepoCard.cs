using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GitHubReleaseMonitor
{
    public enum RepoCardAction
    {
        OpenRelease,
        Edit,
        Delete,
        MarkRead,
        OpenRepository
    }

    public sealed class RepoCard : Panel
    {
        private readonly RepoConfig _repo;
        private readonly Label _nameLabel;
        private readonly Label _slugLabel;
        private readonly Label _metaLabel;
        private readonly Pill _versionPill;
        private readonly Pill _newPill;
        private readonly RoundedButton _openButton;
        private readonly RoundedButton _moreButton;
        private readonly ToggleSwitch _enabledSwitch;
        private readonly ToolTip _toolTip;

        private bool _updatingSwitch;

        public RepoCard(RepoConfig repo)
        {
            _repo = repo;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            Height = 114;

            _nameLabel = new Label();
            _nameLabel.AutoSize = false;
            _nameLabel.AutoEllipsis = true;
            _nameLabel.BackColor = Color.White;
            _nameLabel.Font = Theme.Ui(10.5F, FontStyle.Bold);
            _nameLabel.ForeColor = Theme.Text;

            _slugLabel = new Label();
            _slugLabel.AutoSize = false;
            _slugLabel.AutoEllipsis = true;
            _slugLabel.BackColor = Color.White;
            _slugLabel.Font = Theme.Ui(8.5F);
            _slugLabel.ForeColor = Theme.Accent;
            _slugLabel.Cursor = Cursors.Hand;
            _slugLabel.Click += delegate { Raise(RepoCardAction.OpenRepository); };

            _metaLabel = new Label();
            _metaLabel.AutoSize = false;
            _metaLabel.AutoEllipsis = true;
            _metaLabel.BackColor = Color.White;
            _metaLabel.Font = Theme.Ui(8.5F);
            _metaLabel.ForeColor = Theme.Muted;

            _versionPill = new Pill();
            _versionPill.Height = 21;
            _versionPill.Font = Theme.Ui(8F, FontStyle.Bold);

            _newPill = new Pill();
            _newPill.Text = "NEW";
            _newPill.Font = Theme.Ui(7.5F, FontStyle.Bold);
            _newPill.Height = 20;
            _newPill.FillColor = Theme.OrangeSoft;
            _newPill.PillTextColor = Theme.Orange;
            _newPill.PillBorderColor = ColorTranslator.FromHtml("#FFD8B5");

            _openButton = new RoundedButton();
            _openButton.Kind = ButtonKind.Secondary;
            _openButton.Size = new Size(64, 30);
            _openButton.SurfaceColor = Color.White;
            _openButton.Click += delegate { Raise(RepoCardAction.OpenRelease); };

            _moreButton = new RoundedButton();
            _moreButton.Text = "⋯";
            _moreButton.Font = Theme.Ui(11F);
            _moreButton.Kind = ButtonKind.Ghost;
            _moreButton.Size = new Size(30, 30);
            _moreButton.SurfaceColor = Color.White;
            _moreButton.Click += delegate { ShowMenu(); };

            _enabledSwitch = new ToggleSwitch();
            _enabledSwitch.Checked = repo.Enabled;
            _enabledSwitch.CheckedChanged += delegate
            {
                if (_updatingSwitch) return;
                if (ToggleChanged != null) ToggleChanged(this, EventArgs.Empty);
            };

            _toolTip = new ToolTip();

            Controls.Add(_nameLabel);
            Controls.Add(_slugLabel);
            Controls.Add(_metaLabel);
            Controls.Add(_versionPill);
            Controls.Add(_newPill);
            Controls.Add(_openButton);
            Controls.Add(_moreButton);
            Controls.Add(_enabledSwitch);

            RefreshView();
        }

        public event Action<RepoCard, RepoCardAction> ActionRequested;
        public event EventHandler ToggleChanged;

        public RepoConfig Repo
        {
            get { return _repo; }
        }

        public bool SwitchChecked
        {
            get { return _enabledSwitch.Checked; }
        }

        public void RefreshView()
        {
            _updatingSwitch = true;
            _enabledSwitch.Checked = _repo.Enabled;
            _updatingSwitch = false;

            _enabledSwitch.Text = L10n.T("card.monitor");
            _enabledSwitch.Width = L10n.IsEnglish ? 88 : 76;
            _enabledSwitch.AccessibleName = L10n.T("card.monitorAccessible");
            _openButton.AccessibleName = L10n.T("card.openAccessible");
            _moreButton.AccessibleName = L10n.T("card.moreActions");

            _toolTip.SetToolTip(_openButton, L10n.T("card.openTooltip"));
            _toolTip.SetToolTip(_moreButton, L10n.T("card.moreTooltip"));
            _toolTip.SetToolTip(_enabledSwitch, L10n.T("card.monitorTooltip"));

            string displayName = string.IsNullOrEmpty(_repo.Name) ? _repo.Repo : _repo.Name;
            _nameLabel.Text = displayName;
            _slugLabel.Text = _repo.Slug;
            _toolTip.SetToolTip(_slugLabel, _repo.RepoUrl);

            bool hasTag = !string.IsNullOrEmpty(_repo.LatestTag);
            bool unread = _repo.Enabled && _repo.HasUnread;
            _newPill.Visible = unread;
            _newPill.Left = 0;

            _versionPill.Visible = hasTag;
            _versionPill.Text = hasTag ? _repo.LatestTag : "";
            _versionPill.FillColor = Theme.SoftGray;
            _versionPill.PillTextColor = ColorTranslator.FromHtml("#57606A");

            _nameLabel.ForeColor = _repo.Enabled ? Theme.Text : Theme.Muted;
            _slugLabel.ForeColor = _repo.Enabled ? Theme.Accent : Theme.Faint;
            _newPill.FillColor = Theme.OrangeSoft;
            _newPill.PillTextColor = Theme.Orange;

            string publishedPart = "";
            DateTime publishedUtc;
            if (hasTag && TimeText.TryParseUtc(_repo.LatestPublishedAt, out publishedUtc))
            {
                publishedPart = L10n.F("card.published", TimeText.Relative(publishedUtc));
            }

            string statusPart;
            Color metaColor = Theme.Muted;

            if (!_repo.Enabled)
            {
                statusPart = L10n.T("common.paused");
                metaColor = Theme.Faint;
            }
            else if (_repo.LastStatus == "error")
            {
                statusPart = L10n.F("card.error",
                    string.IsNullOrEmpty(_repo.LastError) ? L10n.T("common.unknownError") : _repo.LastError);
                metaColor = Theme.Red;
            }
            else if (unread)
            {
                statusPart = L10n.T("common.updateAvailable");
                metaColor = Theme.Orange;
            }
            else if (_repo.LastStatus == "uptodate")
            {
                statusPart = L10n.T("common.upToDate");
                metaColor = Theme.Green;
            }
            else if (_repo.LastStatus == "baseline")
            {
                statusPart = L10n.T("common.synced");
                metaColor = Theme.Muted;
            }
            else if (_repo.LastStatus == "no-release")
            {
                statusPart = L10n.T("common.noRelease");
                metaColor = Theme.Muted;
            }
            else
            {
                statusPart = L10n.T("common.waitingFirstCheck");
                metaColor = Theme.Muted;
            }

            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(publishedPart)) parts.Add(publishedPart);
            parts.Add(statusPart);
            _metaLabel.Text = string.Join(" · ", parts.ToArray());
            _metaLabel.ForeColor = metaColor;
            _toolTip.SetToolTip(_metaLabel, _metaLabel.Text);
            _toolTip.SetToolTip(_nameLabel, displayName);

            _openButton.Enabled = !string.IsNullOrEmpty(_repo.LatestUrl);
            _openButton.Kind = unread ? ButtonKind.Primary : ButtonKind.Secondary;
            _openButton.Text = L10n.T("card.open");
            _toolTip.SetToolTip(_versionPill,
                hasTag ? (_repo.LatestName + " (" + _repo.LatestTag + ")") : L10n.T("common.versionUnknown"));

            LayoutChildren();
            Invalidate();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            LayoutChildren();
        }

        private void LayoutChildren()
        {
            if (_openButton == null || _moreButton == null || _enabledSwitch == null ||
                _nameLabel == null || _slugLabel == null || _metaLabel == null ||
                _newPill == null || _versionPill == null)
            {
                return;
            }

            int right = Width - 20;
            _openButton.SetBounds(right - _openButton.Width, 17, _openButton.Width, _openButton.Height);
            _moreButton.SetBounds(_openButton.Left - 8 - _moreButton.Width, 17, _moreButton.Width, _moreButton.Height);
            _enabledSwitch.SetBounds(right - _enabledSwitch.Width, 72, _enabledSwitch.Width, _enabledSwitch.Height);

            int actionsLeft = _moreButton.Left;
            int nameWidth;
            if (_newPill.Visible)
            {
                _newPill.SetBounds(actionsLeft - 10 - _newPill.Width, 19, _newPill.Width, _newPill.Height);
                nameWidth = _newPill.Left - 10 - 20;
            }
            else
            {
                nameWidth = actionsLeft - 20 - 16;
            }
            _nameLabel.SetBounds(20, 15, Math.Max(80, nameWidth), 25);
            _slugLabel.SetBounds(20, 41, Math.Max(80, _moreButton.Left - 20 - 12), 19);

            int metaLeft = 20;
            if (_versionPill.Visible)
            {
                _versionPill.SetBounds(20, 74, _versionPill.Width, _versionPill.Height);
                metaLeft = _versionPill.Right + 10;
            }
            _metaLabel.SetBounds(metaLeft, 75, Math.Max(80, _enabledSwitch.Left - 12 - metaLeft), 20);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundedRect(rect, 10))
            {
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    graphics.FillPath(brush, path);
                }
                using (Pen pen = new Pen(Theme.Border))
                {
                    graphics.DrawPath(pen, path);
                }
            }

            if (_repo.Enabled && _repo.HasUnread)
            {
                using (SolidBrush brush = new SolidBrush(Theme.Orange))
                {
                    graphics.FillRectangle(brush, 1, 22, 3, Height - 45);
                }
            }
        }

        private void Raise(RepoCardAction action)
        {
            if (ActionRequested != null)
            {
                ActionRequested(this, action);
            }
        }

        private void ShowMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = Theme.Ui(9F);
            menu.ShowImageMargin = false;

            ToolStripMenuItem openRelease = new ToolStripMenuItem(L10n.T("card.menuOpenRelease"));
            openRelease.Enabled = !string.IsNullOrEmpty(_repo.LatestUrl);
            openRelease.Click += delegate { Raise(RepoCardAction.OpenRelease); };

            ToolStripMenuItem openRepo = new ToolStripMenuItem(L10n.T("card.menuOpenRepo"));
            openRepo.Click += delegate { Raise(RepoCardAction.OpenRepository); };

            ToolStripMenuItem markRead = new ToolStripMenuItem(L10n.T("card.menuMarkRead"));
            markRead.Enabled = _repo.HasUnread;
            markRead.Click += delegate { Raise(RepoCardAction.MarkRead); };

            ToolStripMenuItem edit = new ToolStripMenuItem(L10n.T("card.menuEdit"));
            edit.Click += delegate { Raise(RepoCardAction.Edit); };

            ToolStripMenuItem delete = new ToolStripMenuItem(L10n.T("card.menuDelete"));
            delete.ForeColor = Theme.Red;
            delete.Click += delegate { Raise(RepoCardAction.Delete); };

            menu.Items.Add(openRelease);
            menu.Items.Add(openRepo);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(markRead);
            menu.Items.Add(edit);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(delete);
            menu.Closed += delegate { menu.Dispose(); };
            menu.Show(_moreButton, new Point(0, _moreButton.Height + 2));
        }
    }
}
