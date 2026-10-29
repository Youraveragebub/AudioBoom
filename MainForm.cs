using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace AudioBoom
{
    class MainForm : Form
    {
        const string SETUP_URL = "https://sourceforge.net/projects/equalizerapo/";

        Settings settings;
        ApoWriter apo;
        ApoState state = ApoState.Ready;

        ToggleSwitch onSwitch;
        GainPanel micPanel;
        GainPanel speakerPanel;
        Button resetButton;
        Panel banner;
        Label bannerLabel;
        LinkLabel bannerLink;
        Font titleFont;
        Icon logo;

        NotifyIcon tray;
        ToolStripMenuItem onItem;
        ToolStripMenuItem startupItem;
        Timer applyTimer;
        Icon iconOn;
        Icon iconOff;

        int pad;
        int formWidth;
        bool allowShow;
        bool leaving = false;
        bool loading = true;
        bool rounded = false;
        bool bannerOn = false;
        DateTime hiddenAt = DateTime.MinValue;

        public MainForm(bool hidden, string config)
        {
            allowShow = (hidden == false);
            settings = Settings.Load();
            apo = new ApoWriter(config);

            int small = SystemInformation.SmallIconSize.Width;
            iconOn = TrayIcon.Make(small, true);
            iconOff = TrayIcon.Make(small, false);

            applyTimer = new Timer();
            applyTimer.Interval = 250;
            applyTimer.Tick += ApplyTimer_Tick;

            BuildForm();
            BuildTray();

            micPanel.Percent = settings.MicPercent;
            speakerPanel.Percent = settings.SpeakerPercent;
            onSwitch.Checked = settings.Enabled;
            ShowOnOff();
            startupItem.Checked = AutoStart.IsOn();
            if (startupItem.Checked)
            {
                AutoStart.Set(true);
            }
            loading = false;

            Apply();
        }

        void BuildForm()
        {
            Text = "AudioBoom";
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Palette.Back;
            DoubleBuffered = true;
            Icon = TrayIcon.Make(32, true);

            pad = Ui.Px(22);
            formWidth = Ui.Px(360);
            int inner = formWidth - pad * 2;
            titleFont = new Font("Segoe UI Semibold", 13F);
            logo = TrayIcon.Make(Ui.Px(26), true);

            onSwitch = new ToggleSwitch();
            onSwitch.BackColor = Palette.Back;
            onSwitch.Location = new Point(formWidth - pad - onSwitch.Width, Ui.Px(22));
            onSwitch.CheckedChanged += OnSwitch_CheckedChanged;
            Controls.Add(onSwitch);

            micPanel = new GainPanel("Microphone", "", true, 1000, inner);
            micPanel.Location = new Point(pad, Ui.Px(76));
            micPanel.PercentChanged += Panel_PercentChanged;
            Controls.Add(micPanel);

            speakerPanel = new GainPanel("Speakers", "", false, 400, inner);
            speakerPanel.Location = new Point(pad, micPanel.Bottom + Ui.Px(18));
            speakerPanel.PercentChanged += Panel_PercentChanged;
            Controls.Add(speakerPanel);

            banner = new Panel();
            banner.BackColor = Palette.Back;
            banner.Size = new Size(inner, Ui.Px(40));
            banner.Location = new Point(pad, speakerPanel.Bottom + Ui.Px(14));
            banner.Paint += Banner_Paint;
            banner.Visible = false;

            bannerLabel = new Label();
            bannerLabel.AutoSize = true;
            bannerLabel.Font = new Font("Segoe UI", 9.5F);
            bannerLabel.ForeColor = Palette.ErrorText;
            bannerLabel.BackColor = Palette.ErrorBack;
            banner.Controls.Add(bannerLabel);

            bannerLink = new LinkLabel();
            bannerLink.AutoSize = true;
            bannerLink.Font = new Font("Segoe UI Semibold", 9.5F);
            bannerLink.LinkColor = Palette.ErrorText;
            bannerLink.ActiveLinkColor = Palette.Text;
            bannerLink.BackColor = Palette.ErrorBack;
            bannerLink.LinkClicked += BannerLink_LinkClicked;
            banner.Controls.Add(bannerLink);
            Controls.Add(banner);

            resetButton = new Button();
            resetButton.Text = "Reset";
            resetButton.Font = new Font("Segoe UI", 9.5F);
            resetButton.ForeColor = Palette.SubText;
            resetButton.BackColor = Palette.Back;
            resetButton.FlatStyle = FlatStyle.Flat;
            resetButton.FlatAppearance.BorderSize = 0;
            resetButton.FlatAppearance.MouseOverBackColor = Palette.Pill;
            resetButton.FlatAppearance.MouseDownBackColor = Palette.Track;
            resetButton.Cursor = Cursors.Hand;
            resetButton.AutoSize = true;
            resetButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            resetButton.Padding = new Padding(Ui.Px(6), Ui.Px(2), Ui.Px(6), Ui.Px(2));
            resetButton.Click += ResetButton_Click;
            Controls.Add(resetButton);

            Relayout();
        }

        void BuildTray()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.RenderMode = ToolStripRenderMode.System;

            ToolStripMenuItem openItem = new ToolStripMenuItem("Open");
            openItem.Font = new Font(openItem.Font, FontStyle.Bold);
            openItem.Click += delegate { ShowWindow(); };

            onItem = new ToolStripMenuItem("Boost on");
            onItem.Click += delegate { onSwitch.Checked = (onSwitch.Checked == false); };

            startupItem = new ToolStripMenuItem("Start with Windows");
            startupItem.Click += delegate
            {
                AutoStart.Set(startupItem.Checked == false);
                startupItem.Checked = AutoStart.IsOn();
            };

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += delegate { ExitApp(); };

            menu.Items.Add(openItem);
            menu.Items.Add(onItem);
            menu.Items.Add(startupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            tray = new NotifyIcon();
            tray.ContextMenuStrip = menu;
            tray.Icon = iconOn;
            tray.Text = "AudioBoom";
            tray.MouseUp += Tray_MouseUp;
            tray.Visible = true;
        }

        void Relayout()
        {
            int top = speakerPanel.Bottom + Ui.Px(14);
            banner.Visible = bannerOn;
            if (bannerOn)
            {
                banner.Top = top;
                bannerLabel.Location = new Point(Ui.Px(12), (banner.Height - bannerLabel.PreferredHeight) / 2);
                bannerLink.Location = new Point(bannerLabel.Left + bannerLabel.PreferredWidth + Ui.Px(4), bannerLabel.Top);
                top = banner.Bottom + Ui.Px(8);
            }

            resetButton.Size = resetButton.PreferredSize;
            resetButton.Location = new Point(formWidth - pad - resetButton.Width + Ui.Px(6), top);
            ClientSize = new Size(formWidth, resetButton.Bottom + Ui.Px(14));
            if (Visible)
            {
                Place();
            }
            Invalidate();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle = cp.ClassStyle | 0x20000;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            rounded = Ui.RoundCorners(Handle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            int size = Ui.Px(26);
            int middle = onSwitch.Top + onSwitch.Height / 2;
            g.DrawIcon(logo, new Rectangle(pad, middle - size / 2, size, size));
            Rectangle title = new Rectangle(pad + size + Ui.Px(10), middle - Ui.Px(20), Ui.Px(200), Ui.Px(40));
            TextRenderer.DrawText(g, "AudioBoom", titleFont, title, Palette.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);

            if (rounded == false)
            {
                using (Pen pen = new Pen(Palette.Border))
                {
                    g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
                }
            }
        }

        void Banner_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            RectangleF box = new RectangleF(0, 0, banner.Width - 1, banner.Height - 1);
            using (System.Drawing.Drawing2D.GraphicsPath path = Ui.Round(box, Ui.F(8)))
            using (SolidBrush brush = new SolidBrush(Palette.ErrorBack))
            {
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (allowShow == false)
            {
                value = false;
                if (IsHandleCreated == false)
                {
                    CreateHandle();
                }
            }
            base.SetVisibleCore(value);
        }

        public void ShowWindow()
        {
            allowShow = true;
            Show();
            Activate();
        }

        void HideWindow()
        {
            if (Visible == false)
            {
                return;
            }
            micPanel.Commit();
            speakerPanel.Commit();
            hiddenAt = DateTime.Now;
            Hide();
        }

        void Place()
        {
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(area.Right - Width - Ui.Px(12), area.Bottom - Height - Ui.Px(12));
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)
            {
                micPanel.RefreshDevice();
                speakerPanel.RefreshDevice();
                Place();
                ActiveControl = onSwitch;
            }
            base.OnVisibleChanged(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            HideWindow();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                HideWindow();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void ExitApp()
        {
            leaving = true;
            tray.Visible = false;
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            bool really = leaving | (e.CloseReason == CloseReason.WindowsShutDown) | (e.CloseReason == CloseReason.TaskManagerClosing) | (e.CloseReason == CloseReason.ApplicationExitCall);
            if (really == false)
            {
                e.Cancel = true;
                HideWindow();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            tray.Visible = false;
            tray.Dispose();
            base.OnFormClosed(e);
        }

        void Tray_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            if (Visible)
            {
                HideWindow();
                return;
            }
            if ((DateTime.Now - hiddenAt).TotalMilliseconds < 400)
            {
                return;
            }
            ShowWindow();
        }

        void Panel_PercentChanged(object sender, EventArgs e)
        {
            settings.MicPercent = micPanel.Percent;
            settings.SpeakerPercent = speakerPanel.Percent;
            QueueApply();
        }

        void OnSwitch_CheckedChanged(object sender, EventArgs e)
        {
            ShowOnOff();
            if (loading)
            {
                return;
            }
            settings.Enabled = onSwitch.Checked;
            QueueApply();
        }

        void ResetButton_Click(object sender, EventArgs e)
        {
            micPanel.Percent = 100;
            speakerPanel.Percent = 100;
            settings.MicPercent = 100;
            settings.SpeakerPercent = 100;
            QueueApply();
        }

        void BannerLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(SETUP_URL);
            }
            catch
            {
            }
        }

        void QueueApply()
        {
            applyTimer.Stop();
            applyTimer.Start();
        }

        void ApplyTimer_Tick(object sender, EventArgs e)
        {
            applyTimer.Stop();
            Apply();
        }

        void Apply()
        {
            state = apo.Apply(settings.MicPercent, settings.SpeakerPercent, settings.Enabled);
            settings.Save();
            ShowStatus();
            UpdateTray();
        }

        void ShowOnOff()
        {
            micPanel.SetDim(onSwitch.Checked == false);
            speakerPanel.SetDim(onSwitch.Checked == false);
            onItem.Checked = onSwitch.Checked;
        }

        void UpdateTray()
        {
            if (settings.Enabled)
            {
                tray.Icon = iconOn;
                tray.Text = "AudioBoom - mic " + settings.MicPercent + "%, speakers " + settings.SpeakerPercent + "%";
            }
            else
            {
                tray.Icon = iconOff;
                tray.Text = "AudioBoom - off";
            }
        }

        void ShowStatus()
        {
            bool wasOn = bannerOn;
            if (state == ApoState.Ready)
            {
                bannerOn = false;
            }
            else if (state == ApoState.NoAccess)
            {
                bannerLabel.Text = "Can't save. Run as administrator.";
                bannerLink.Visible = false;
                bannerOn = true;
            }
            else
            {
                bannerLabel.Text = "Equalizer APO not found.";
                bannerLink.Text = "Get it";
                bannerLink.Visible = true;
                bannerOn = true;
            }
            if ((bannerOn != wasOn) | bannerOn)
            {
                Relayout();
            }
        }
    }
}
