using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AudioBoom
{
    class ToggleSwitch : Control
    {
        public event EventHandler CheckedChanged;

        bool on = false;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            Size = new Size(Ui.Px(46), Ui.Px(26));
            Cursor = Cursors.Hand;
        }

        public bool Checked
        {
            get
            {
                return on;
            }
            set
            {
                if (value == on)
                {
                    return;
                }
                on = value;
                Invalidate();
                if (CheckedChanged != null)
                {
                    CheckedChanged(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Checked = (on == false);
            }
            base.OnMouseClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                Checked = (on == false);
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            Color fill = Palette.Off;
            if (on)
            {
                fill = Palette.Accent;
            }

            RectangleF pill = new RectangleF(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Ui.Round(pill, pill.Height / 2))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
            }

            float gap = Ui.F(3);
            float knob = pill.Height - gap * 2;
            float x = pill.X + gap;
            if (on)
            {
                x = pill.Right - gap - knob;
            }
            using (SolidBrush white = new SolidBrush(Color.White))
            {
                g.FillEllipse(white, x, pill.Y + gap, knob, knob);
            }
        }
    }

    class GainSlider : Control
    {
        public event EventHandler ValueChanged;

        public int Minimum = 100;
        public int Maximum = 1000;
        public bool Warn = false;
        public bool Dim = false;

        int current = 100;
        bool dragging = false;

        public GainSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public int Value
        {
            get
            {
                return current;
            }
            set
            {
                current = value;
                Invalidate();
            }
        }

        float Radius()
        {
            return Ui.F(10);
        }

        float TrackStart()
        {
            return Radius() + 1;
        }

        float TrackEnd()
        {
            return Width - Radius() - 1;
        }

        float ThumbX()
        {
            int shown = Math.Min(Math.Max(current, Minimum), Maximum);
            float ratio = (shown - Minimum) / (float)(Maximum - Minimum);
            return TrackStart() + ratio * (TrackEnd() - TrackStart());
        }

        void SetByUser(int picked)
        {
            if (picked == current)
            {
                return;
            }
            current = picked;
            Invalidate();
            if (ValueChanged != null)
            {
                ValueChanged(this, EventArgs.Empty);
            }
        }

        void SetFromX(int x)
        {
            float ratio = (x - TrackStart()) / (TrackEnd() - TrackStart());
            ratio = Math.Min(Math.Max(ratio, 0f), 1f);
            int picked = Minimum + (int)Math.Round(ratio * (Maximum - Minimum) / 10.0) * 10;
            SetByUser(picked);
        }

        void Step(int amount)
        {
            int shown = Math.Min(Math.Max(current, Minimum), Maximum);
            int picked = Math.Min(Math.Max(shown + amount, Minimum), Maximum);
            if ((amount > 0) && (current > picked))
            {
                return;
            }
            SetByUser(picked);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            Color fill = Palette.Accent;
            if (Warn)
            {
                fill = Palette.Warn;
            }
            if (Dim)
            {
                fill = Palette.Dim;
            }

            float mid = Height / 2f;
            float bar = Ui.F(6);
            float x = ThumbX();

            RectangleF track = new RectangleF(TrackStart(), mid - bar / 2, TrackEnd() - TrackStart(), bar);
            using (GraphicsPath path = Ui.Round(track, bar / 2))
            using (SolidBrush brush = new SolidBrush(Palette.Track))
            {
                g.FillPath(brush, path);
            }
            if (x - TrackStart() > 1)
            {
                RectangleF done = new RectangleF(TrackStart(), mid - bar / 2, x - TrackStart(), bar);
                using (GraphicsPath path = Ui.Round(done, bar / 2))
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
            }

            float r = Radius();
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
            {
                g.FillEllipse(shadow, x - r, mid - r + Ui.F(1), r * 2, r * 2);
            }
            using (SolidBrush white = new SolidBrush(Color.White))
            using (Pen edge = new Pen(Palette.Border, 1))
            {
                g.FillEllipse(white, x - r, mid - r, r * 2, r * 2);
                g.DrawEllipse(edge, x - r, mid - r, r * 2, r * 2);
            }
            float dot = r * 0.55f;
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillEllipse(brush, x - dot, mid - dot, dot * 2, dot * 2);
            }

            if (Focused && ShowFocusCues)
            {
                using (Pen ring = new Pen(Color.FromArgb(90, fill), Ui.F(2)))
                {
                    float big = r + Ui.F(3);
                    g.DrawEllipse(ring, x - big, mid - big, big * 2, big * 2);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                if (Math.Abs(e.X - ThumbX()) > Radius())
                {
                    SetFromX(e.X);
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                SetFromX(e.X);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                Step(10);
            }
            else
            {
                Step(-10);
            }
            base.OnMouseWheel(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if ((keyData == Keys.Left) | (keyData == Keys.Right) | (keyData == Keys.Up) | (keyData == Keys.Down))
            {
                return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Left) | (e.KeyCode == Keys.Down))
            {
                Step(-10);
            }
            if ((e.KeyCode == Keys.Right) | (e.KeyCode == Keys.Up))
            {
                Step(10);
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

    class DevicePicker : Control
    {
        string shown = "";
        bool hover = false;
        Font iconFont;
        ToolTip tip = new ToolTip();

        public DevicePicker()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Font = new Font("Segoe UI", 9F);
            iconFont = Ui.IconFont(7F);
            Cursor = Cursors.Hand;
        }

        public void SetText(string text)
        {
            shown = text;
            tip.SetToolTip(this, text);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            Color color = Palette.SubText;
            if (hover)
            {
                color = Palette.Accent;
            }

            int arrow = Ui.Px(14);
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            int full = TextRenderer.MeasureText(shown, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            int textWidth = Math.Min(full, Width - arrow);
            TextRenderer.DrawText(e.Graphics, shown, Font, new Rectangle(0, 0, textWidth, Height), color, flags);
            if (iconFont != null)
            {
                Rectangle chevron = new Rectangle(textWidth + Ui.Px(4), 0, arrow, Height);
                TextRenderer.DrawText(e.Graphics, "", iconFont, chevron, color, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
            }
        }
    }

    class ValueBox : Control
    {
        public event EventHandler ValueChanged;

        TextBox text;
        Font signFont;
        int current = 100;

        public ValueBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(Ui.Px(96), Ui.Px(36));
            Cursor = Cursors.IBeam;
            signFont = new Font("Segoe UI", 11F);

            text = new TextBox();
            text.BorderStyle = BorderStyle.None;
            text.BackColor = Palette.Pill;
            text.ForeColor = Palette.Text;
            text.Font = new Font("Segoe UI Semibold", 12F);
            text.TextAlign = HorizontalAlignment.Right;
            text.MaxLength = 5;
            text.Text = "100";
            text.Width = Width - Ui.Px(12) - Ui.Px(28);
            text.Location = new Point(Ui.Px(12), (Height - text.Height) / 2);
            text.KeyPress += Text_KeyPress;
            text.KeyDown += Text_KeyDown;
            text.Leave += delegate { Commit(); };
            text.Enter += delegate { BeginInvoke(new MethodInvoker(text.SelectAll)); };
            text.GotFocus += delegate { Invalidate(); };
            text.LostFocus += delegate { Invalidate(); };
            Controls.Add(text);
        }

        public int Value
        {
            get
            {
                return current;
            }
            set
            {
                current = Gain.Clamp(value);
                text.Text = current.ToString();
            }
        }

        public void NumberColor(Color color)
        {
            text.ForeColor = color;
        }

        public void Tip(string message)
        {
            ToolTip tip = new ToolTip();
            tip.SetToolTip(this, message);
            tip.SetToolTip(text, message);
        }

        public void Commit()
        {
            int typed = current;
            try
            {
                typed = Gain.Clamp(int.Parse(text.Text));
            }
            catch
            {
            }
            text.Text = typed.ToString();
            if (typed != current)
            {
                current = typed;
                if (ValueChanged != null)
                {
                    ValueChanged(this, EventArgs.Empty);
                }
            }
        }

        void Text_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((char.IsDigit(e.KeyChar) == false) && (char.IsControl(e.KeyChar) == false))
            {
                e.Handled = true;
            }
        }

        void Text_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Commit();
                text.SelectAll();
                e.SuppressKeyPress = true;
            }
            if ((e.KeyCode == Keys.Up) | (e.KeyCode == Keys.Down))
            {
                Commit();
                int amount = 10;
                if (e.KeyCode == Keys.Down)
                {
                    amount = -10;
                }
                text.Text = Gain.Clamp(current + amount).ToString();
                Commit();
                text.SelectAll();
                e.SuppressKeyPress = true;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            text.Focus();
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            Color edge = Palette.Track;
            float width = 1;
            if (text.Focused)
            {
                edge = Palette.Accent;
                width = Ui.F(1.5f);
            }

            RectangleF box = new RectangleF(width / 2, width / 2, Width - width - 1, Height - width - 1);
            using (GraphicsPath path = Ui.Round(box, Ui.F(8)))
            using (SolidBrush brush = new SolidBrush(Palette.Pill))
            using (Pen pen = new Pen(edge, width))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            Rectangle sign = new Rectangle(text.Right + Ui.Px(3), 0, Width - text.Right - Ui.Px(3), Height);
            TextRenderer.DrawText(g, "%", signFont, sign, Palette.SubText, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
        }
    }
}
