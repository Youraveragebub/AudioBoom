using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AudioBoom
{
    class GainPanel : Control
    {
        public event EventHandler PercentChanged;

        string title;
        string glyph;
        bool mic;
        int recommendedMax;
        bool dim = false;
        int header;
        GainSlider slider;
        ValueBox box;
        DevicePicker picker;
        ContextMenuStrip menu;
        Font titleFont;
        Font iconFont;

        public GainPanel(string title, string glyph, bool mic, int recommendedMax, int width)
        {
            this.title = title;
            this.glyph = glyph;
            this.mic = mic;
            this.recommendedMax = recommendedMax;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Palette.Back;
            titleFont = new Font("Segoe UI", 11.5F);
            iconFont = Ui.IconFont(14F);
            header = Ui.Px(46);

            box = new ValueBox();
            box.BackColor = Palette.Back;
            box.Location = new Point(width - box.Width, (header - box.Height) / 2);
            box.ValueChanged += Box_Changed;
            box.Tip("Recommended up to " + recommendedMax + "%");
            Controls.Add(box);

            picker = new DevicePicker();
            picker.BackColor = Palette.Back;
            picker.Bounds = new Rectangle(TextLeft(), Ui.Px(24), box.Left - TextLeft() - Ui.Px(8), Ui.Px(20));
            picker.Click += Picker_Click;
            Controls.Add(picker);

            menu = new ContextMenuStrip();
            menu.RenderMode = ToolStripRenderMode.System;
            menu.Font = new Font("Segoe UI", 9.5F);

            slider = new GainSlider();
            slider.Minimum = 100;
            slider.Maximum = recommendedMax;
            slider.BackColor = Palette.Back;
            slider.Bounds = new Rectangle(0, header + Ui.Px(4), width, Ui.Px(30));
            slider.ValueChanged += Slider_Changed;
            Controls.Add(slider);

            Size = new Size(width, slider.Bottom);
        }

        int TextLeft()
        {
            if (iconFont == null)
            {
                return 0;
            }
            return Ui.Px(34);
        }

        public int Percent
        {
            get
            {
                return box.Value;
            }
            set
            {
                box.Value = value;
                slider.Value = box.Value;
                UpdateColors();
            }
        }

        public void SetDim(bool dim)
        {
            this.dim = dim;
            UpdateColors();
            Invalidate();
        }

        public void Commit()
        {
            box.Commit();
        }

        public void RefreshDevice()
        {
            AudioDevice current = AudioDevices.Default(mic);
            if (current == null)
            {
                picker.SetText("Nothing plugged in");
                return;
            }

            string name = current.Name;
            string start = title + " (";
            if (name.StartsWith(start) && name.EndsWith(")"))
            {
                name = name.Substring(start.Length, name.Length - start.Length - 1);
            }
            picker.SetText(name);
        }

        void Picker_Click(object sender, EventArgs e)
        {
            while (menu.Items.Count > 0)
            {
                menu.Items[0].Dispose();
            }

            AudioDevice current = AudioDevices.Default(mic);
            List<AudioDevice> all = AudioDevices.List(mic);
            foreach (AudioDevice device in all)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(device.Name);
                item.Tag = device.Id;
                item.Checked = (current != null) && (device.Id == current.Id);
                item.Click += Device_Click;
                menu.Items.Add(item);
            }
            if (all.Count == 0)
            {
                ToolStripMenuItem none = new ToolStripMenuItem("Nothing plugged in");
                none.Enabled = false;
                menu.Items.Add(none);
            }
            menu.Show(picker, new Point(0, picker.Height));
        }

        void Device_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            AudioDevices.MakeDefault((string)item.Tag);
            RefreshDevice();
        }

        void Slider_Changed(object sender, EventArgs e)
        {
            box.Value = slider.Value;
            UpdateColors();
            Raise();
        }

        void Box_Changed(object sender, EventArgs e)
        {
            slider.Value = box.Value;
            UpdateColors();
            Raise();
        }

        void Raise()
        {
            if (PercentChanged != null)
            {
                PercentChanged(this, EventArgs.Empty);
            }
        }

        void UpdateColors()
        {
            bool over = box.Value > recommendedMax;
            slider.Warn = over;
            slider.Dim = dim;
            slider.Invalidate();

            Color number = Palette.Text;
            if (over)
            {
                number = Palette.Warn;
            }
            if (dim)
            {
                number = Palette.Dim;
            }
            box.NumberColor(number);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);

            Color iconColor = Palette.Accent;
            Color textColor = Palette.Text;
            if (dim)
            {
                iconColor = Palette.Dim;
                textColor = Palette.Dim;
            }

            if (iconFont != null)
            {
                Rectangle icon = new Rectangle(0, 0, Ui.Px(24), header);
                TextRenderer.DrawText(e.Graphics, glyph, iconFont, icon, iconColor, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
            }
            Rectangle name = new Rectangle(TextLeft(), 0, box.Left - TextLeft(), Ui.Px(24));
            TextRenderer.DrawText(e.Graphics, title, titleFont, name, textColor, TextFormatFlags.Bottom | TextFormatFlags.Left | TextFormatFlags.NoPadding);
        }
    }
}
