using System;
using System.IO;

namespace AudioBoom
{
    static class Gain
    {
        public const int MIN_PERCENT = 1;
        public const int MAX_PERCENT = 10000;

        public static int Clamp(int percent)
        {
            if (percent < MIN_PERCENT)
            {
                return MIN_PERCENT;
            }
            if (percent > MAX_PERCENT)
            {
                return MAX_PERCENT;
            }
            return percent;
        }

        public static double ToDb(int percent)
        {
            return 20 * Math.Log10(percent / 100.0);
        }
    }

    class Settings
    {
        public int MicPercent = 300;
        public int SpeakerPercent = 150;
        public bool Enabled = true;

        static string FilePath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioBoom");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "settings.txt");
        }

        static int ReadPercent(string text, int fallback)
        {
            try
            {
                return Gain.Clamp(int.Parse(text));
            }
            catch
            {
                return fallback;
            }
        }

        public static Settings Load()
        {
            Settings loaded = new Settings();
            string[] lines = new string[0];
            try
            {
                lines = File.ReadAllLines(FilePath());
            }
            catch
            {
            }

            foreach (string line in lines)
            {
                int cut = line.IndexOf('=');
                if (cut < 0)
                {
                    continue;
                }
                string key = line.Substring(0, cut).Trim();
                string text = line.Substring(cut + 1).Trim();

                if (key == "MicPercent")
                {
                    loaded.MicPercent = ReadPercent(text, loaded.MicPercent);
                }
                if (key == "SpeakerPercent")
                {
                    loaded.SpeakerPercent = ReadPercent(text, loaded.SpeakerPercent);
                }
                if (key == "Enabled")
                {
                    loaded.Enabled = (text != "0");
                }
            }
            return loaded;
        }

        public void Save()
        {
            string on = "0";
            if (Enabled)
            {
                on = "1";
            }
            string text = "MicPercent=" + MicPercent + "\r\n" + "SpeakerPercent=" + SpeakerPercent + "\r\n" + "Enabled=" + on + "\r\n";
            try
            {
                File.WriteAllText(FilePath(), text);
            }
            catch
            {
            }
        }
    }
}
