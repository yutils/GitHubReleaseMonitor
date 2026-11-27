using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GitHubReleaseMonitor
{
    public enum ButtonKind
    {
        Primary,
        Secondary,
        Ghost,
        DangerGhost
    }

    public sealed class RoundedButton : Button
    {
        private bool _hover;
        private bool _pressed;
        private ButtonKind _kind = ButtonKind.Secondary;
        private Color _surfaceColor = Theme.Card;
        private int _radius = 6;

        public RoundedButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            Font = Theme.Ui(9F);
            Size = new Size(88, 32);
            TabStop = true;
        }

        public ButtonKind Kind
        {
            get { return _kind; }
            set { _kind = value; Invalidate(); }
        }

        public Color SurfaceColor
        {
            get { return _surfaceColor; }
            set { _surfaceColor = value; Invalidate(); }
        }

        public int CornerRadius
        {
            get { return _radius; }
            set { _radius = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics graphics = pevent.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(_surfaceColor);

            Color fill;
            Color text;
            Color border = Color.Transparent;

            switch (_kind)
            {
                case ButtonKind.Primary:
                    fill = _hover ? Theme.AccentHover : Theme.Accent;
                    if (_pressed) fill = Theme.Mix(Theme.Accent, Color.Black, 0.18);
                    text = Color.White;
                    break;
                case ButtonKind.Ghost:
                    fill = _hover ? Theme.Hover : _surfaceColor;
                    text = _hover ? Theme.Text : Theme.Muted;
                    break;
                case ButtonKind.DangerGhost:
                    fill = _hover ? Theme.RedSoft : _surfaceColor;
                    text = Theme.Red;
                    break;
                default:
                    fill = _hover ? Theme.Hover : Color.White;
                    border = _hover ? Theme.BorderStrong : Theme.Border;
                    text = Theme.Text;
                    break;
            }

            if (!Enabled)
            {
                fill = Theme.SoftGray;
                text = Theme.Faint;
                border = Theme.Border;
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundedRect(rect, _radius))
            {
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    graphics.FillPath(brush, path);
                }
                if (border != Color.Transparent)
                {
                    using (Pen pen = new Pen(border))
                    {
                        graphics.DrawPath(pen, path);
                    }
                }
            }

            Theme.DrawCenteredText(graphics, Text, Font, text, ClientRectangle);
        }
    }

    public sealed class Pill : Control
    {
        private string _pillText = "";
        private Color _fillColor = Theme.SoftGray;
        private Color _pillTextColor = Theme.Muted;
        private Color _borderColor = Color.Transparent;

        public Pill()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.Ui(8F, FontStyle.Bold);
            Height = 21;
            Width = 40;
        }

        public Color FillColor
        {
            get { return _fillColor; }
            set { _fillColor = value; Invalidate(); }
        }

        public Color PillTextColor
        {
            get { return _pillTextColor; }
            set { _pillTextColor = value; Invalidate(); }
        }

        public Color PillBorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; Invalidate(); }
        }

        public override string Text
        {
            get { return _pillText; }
            set
            {
                _pillText = value ?? "";
                RecalculateWidth();
                Invalidate();
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            RecalculateWidth();
            base.OnFontChanged(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics graphics = pevent.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundedRect(rect, Height / 2))
            {
                using (SolidBrush brush = new SolidBrush(_fillColor))
                {
                    graphics.FillPath(brush, path);
                }
                if (_borderColor != Color.Transparent)
                {
                    using (Pen pen = new Pen(_borderColor))
                    {
                        graphics.DrawPath(pen, path);
                    }
                }
            }

            Theme.DrawCenteredText(graphics, _pillText, Font, _pillTextColor,
                new Rectangle(0, 0, Width, Height));
        }

        private void RecalculateWidth()
        {
            if (string.IsNullOrEmpty(_pillText))
            {
                Width = 0;
                return;
            }
            int width = TextRenderer.MeasureText(_pillText, Font).Width + 16;
            Width = Math.Min(Math.Max(width, 30), 220);
        }
    }

    public sealed class ToggleSwitch : Control
    {
        private bool _checked;
        private bool _hover;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Font = Theme.Ui(8.5F);
            Size = new Size(76, 24);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.CheckButton;
            AccessibleName = "启用监听";
        }

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics graphics = pevent.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int trackWidth = 32;
            int trackHeight = 18;
            int trackX = Width - trackWidth - 2;
            int trackY = (Height - trackHeight) / 2;
            Rectangle track = new Rectangle(trackX, trackY, trackWidth, trackHeight);

            Color trackColor = _checked ? Theme.Accent : Theme.Border;
            if (_hover) trackColor = _checked ? Theme.AccentHover : Theme.BorderStrong;
            if (!Enabled) trackColor = Theme.SoftGray;

            using (GraphicsPath path = new GraphicsPath())
            {
                int diameter = track.Height;
                path.AddArc(track.X, track.Y, diameter, diameter, 90, 180);
                path.AddArc(track.Right - diameter, track.Y, diameter, diameter, 270, 180);
                path.CloseFigure();
                using (SolidBrush brush = new SolidBrush(trackColor))
                {
                    graphics.FillPath(brush, path);
                }
            }

            int knobSize = 14;
            int knobX = _checked ? track.Right - knobSize - 2 : track.X + 2;
            int knobY = track.Y + (track.Height - knobSize) / 2;
            using (SolidBrush brush = new SolidBrush(Enabled ? Color.White : Theme.Faint))
            {
                graphics.FillEllipse(brush, knobX, knobY, knobSize, knobSize);
            }

            if (!string.IsNullOrEmpty(Text))
            {
                TextRenderer.DrawText(graphics, Text, Font,
                    new Rectangle(0, 0, trackX - 4, Height),
                    Enabled ? Theme.Muted : Theme.Faint,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (Enabled) Checked = !Checked;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
            {
                Checked = !Checked;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }
    }

    public sealed class GlyphButton : Control
    {
        private bool _hover;
        private bool _pressed;
        private Color _hoverBackground = Theme.Hover;
        private Color _hoverGlyphColor = Theme.Text;
        private Color _normalGlyphColor = Theme.Muted;

        public GlyphButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe MDL2 Assets", 10F, FontStyle.Regular);
            Size = new Size(40, 32);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
        }

        public string Glyph { get; set; }

        public Color HoverBackground
        {
            get { return _hoverBackground; }
            set { _hoverBackground = value; }
        }

        public Color HoverGlyphColor
        {
            get { return _hoverGlyphColor; }
            set { _hoverGlyphColor = value; }
        }

        public Color NormalGlyphColor
        {
            get { return _normalGlyphColor; }
            set { _normalGlyphColor = value; }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics graphics = pevent.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hover)
            {
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = Theme.RoundedRect(rect, 6))
                {
                    using (SolidBrush brush = new SolidBrush(_hoverBackground))
                    {
                        graphics.FillPath(brush, path);
                    }
                }
            }

            Color glyphColor = _hover ? _hoverGlyphColor : _normalGlyphColor;
            if (_pressed) glyphColor = Theme.Mix(glyphColor, Color.Black, 0.25);
            Theme.DrawCenteredText(graphics, Glyph ?? "", Font, glyphColor, ClientRectangle);
        }
    }

    public sealed class HeaderBar : Panel
    {
        private readonly Font _titleFont;
        private readonly Font _subtitleFont;
        private string _subtitle = "";

        public HeaderBar()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Height = 64;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
            _titleFont = Theme.Ui(11.5F, FontStyle.Bold);
            _subtitleFont = Theme.Ui(8.5F);
        }

        public void SetSubtitle(string subtitle)
        {
            string value = subtitle ?? "";
            if (value == _subtitle) return;
            _subtitle = value;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.Clear(Color.White);

            using (Pen border = new Pen(Theme.Border))
            {
                graphics.DrawLine(border, 0, Height - 1, Width, Height - 1);
            }

            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(AppIcon.Bitmap32, new Rectangle(22, 16, 32, 32));

            TextRenderer.DrawText(graphics, AppInfo.Name, _titleFont, new Point(66, 11), Theme.Text,
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(graphics, _subtitle, _subtitleFont, new Point(67, 36), Theme.Muted,
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Form form = FindForm();
                if (form != null)
                {
                    NativeMethods.StartWindowDrag(form.Handle);
                }
            }
            base.OnMouseDown(e);
        }
    }

    public sealed class EmptyView : Panel
    {
        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly RoundedButton _addButton;

        public EmptyView()
        {
            DoubleBuffered = true;
            BackColor = Theme.Background;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);

            _title = new Label();
            _title.Font = Theme.Ui(12F, FontStyle.Bold);
            _title.ForeColor = Theme.Text;
            _title.BackColor = Color.White;
            _title.TextAlign = ContentAlignment.MiddleCenter;
            _title.AutoSize = false;

            _subtitle = new Label();
            _subtitle.Font = Theme.Ui(8.5F);
            _subtitle.ForeColor = Theme.Muted;
            _subtitle.BackColor = Color.White;
            _subtitle.TextAlign = ContentAlignment.MiddleCenter;
            _subtitle.AutoSize = false;

            _addButton = new RoundedButton();
            _addButton.Kind = ButtonKind.Primary;
            _addButton.Size = new Size(124, 34);
            _addButton.SurfaceColor = Color.White;
            _addButton.Click += delegate
            {
                if (AddRequested != null) AddRequested(this, EventArgs.Empty);
            };

            Controls.Add(_title);
            Controls.Add(_subtitle);
            Controls.Add(_addButton);

            ApplyLanguage();
        }

        public event EventHandler AddRequested;

        public void ApplyLanguage()
        {
            if (_title == null) return;
            _title.Text = L10n.T("empty.title");
            _subtitle.Text = L10n.T("empty.subtitle");
            _addButton.Text = L10n.T("common.addRepo");
            _addButton.Width = L10n.IsEnglish ? 136 : 124;
            _addButton.Left = Math.Max(0, (Width - _addButton.Width) / 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle card = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundedRect(card, 10))
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

            int centerX = Width / 2;
            int iconY = Height / 2 - 108;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(AppIcon.Bitmap64, new Rectangle(centerX - 28, iconY, 56, 56));
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            int labelWidth = Math.Min(Math.Max(Width - 80, 200), 440);
            int centerX = Width / 2;
            int centerY = Height / 2;

            _title.SetBounds(centerX - labelWidth / 2, centerY - 44, labelWidth, 28);
            _subtitle.SetBounds(centerX - labelWidth / 2, centerY - 14, labelWidth, 22);
            _addButton.SetBounds(centerX - _addButton.Width / 2, centerY + 18, _addButton.Width, _addButton.Height);
        }
    }
}
