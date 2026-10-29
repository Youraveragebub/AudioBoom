using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AudioBoom
{
    static class Palette
    {
        public static readonly Color Back = Color.White;
        public static readonly Color Text = Color.FromArgb(31, 41, 55);
        public static readonly Color SubText = Color.FromArgb(107, 114, 128);
        public static readonly Color Accent = Color.FromArgb(37, 99, 235);
        public static readonly Color Warn = Color.FromArgb(234, 88, 12);
        public static readonly Color Track = Color.FromArgb(229, 231, 235);
        public static readonly Color Pill = Color.FromArgb(243, 244, 246);
        public static readonly Color Border = Color.FromArgb(209, 213, 219);
        public static readonly Color Off = Color.FromArgb(180, 188, 199);
        public static readonly Color Dim = Color.FromArgb(170, 176, 186);
        public static readonly Color ErrorBack = Color.FromArgb(254, 242, 242);
        public static readonly Color ErrorText = Color.FromArgb(185, 28, 28);
    }

    static class Ui
    {
        static float factor = 0;

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);

        public static float F(float value)
        {
            if (factor == 0)
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    factor = g.DpiX / 96f;
                }
            }
            return value * factor;
        }

        public static int Px(int value)
        {
            return (int)Math.Round(F(value));
        }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Font IconFont(float size)
        {
            try
            {
                return new Font(new FontFamily("Segoe MDL2 Assets"), size);
            }
            catch
            {
                return null;
            }
        }

        public static bool RoundCorners(IntPtr handle)
        {
            try
            {
                int round = 2;
                return DwmSetWindowAttribute(handle, 33, ref round, 4) == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    static class TrayIcon
    {
        [DllImport("user32.dll")]
        static extern bool DestroyIcon(IntPtr handle);

        public static Icon Make(int size, bool on)
        {
            Color back = Palette.Accent;
            if (on == false)
            {
                back = Color.FromArgb(120, 120, 120);
            }

            using (Bitmap bmp = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bmp))
            using (SolidBrush brush = new SolidBrush(back))
            using (SolidBrush white = new SolidBrush(Color.White))
            using (Pen wave = new Pen(Color.White, size / 12f))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                g.FillEllipse(brush, 0, 0, size - 1, size - 1);

                float s = size / 16f;
                PointF[] shape =
                {
                    new PointF(3f * s, 6.2f * s),
                    new PointF(5f * s, 6.2f * s),
                    new PointF(8f * s, 3.8f * s),
                    new PointF(8f * s, 12.2f * s),
                    new PointF(5f * s, 9.8f * s),
                    new PointF(3f * s, 9.8f * s)
                };
                g.FillPolygon(white, shape);

                wave.StartCap = LineCap.Round;
                wave.EndCap = LineCap.Round;
                g.DrawArc(wave, 8f * s - 3f * s, 8f * s - 3f * s, 6f * s, 6f * s, -45, 90);
                g.DrawArc(wave, 8f * s - 5.2f * s, 8f * s - 5.2f * s, 10.4f * s, 10.4f * s, -45, 90);

                IntPtr handle = bmp.GetHicon();
                Icon copy = (Icon)Icon.FromHandle(handle).Clone();
                DestroyIcon(handle);
                return copy;
            }
        }
    }

    static class AutoStart
    {
        const string KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string NAME = "AudioBoom";

        public static bool IsOn()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KEY))
                {
                    return (key != null) && (key.GetValue(NAME) != null);
                }
            }
            catch
            {
                return false;
            }
        }

        public static void Set(bool on)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KEY, true))
                {
                    if (on)
                    {
                        key.SetValue(NAME, "\"" + Application.ExecutablePath + "\" --hidden");
                    }
                    else
                    {
                        key.DeleteValue(NAME, false);
                    }
                }
            }
            catch
            {
            }
        }
    }
}
