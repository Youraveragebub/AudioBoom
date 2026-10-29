using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace AudioBoom
{
    enum ApoState
    {
        Ready,
        NotFound,
        NoAccess
    }

    class ApoWriter
    {
        const string FILE_NAME = "audioboom.txt";
        const string INCLUDE_LINE = "Include: audioboom.txt";

        public string ConfigPath = "";

        public ApoWriter(string overridePath)
        {
            if (overridePath != "")
            {
                ConfigPath = overridePath;
            }
            else
            {
                ConfigPath = Find();
            }
        }

        static string Find()
        {
            RegistryView[] views = { RegistryView.Registry64, RegistryView.Registry32 };
            foreach (RegistryView view in views)
            {
                try
                {
                    using (RegistryKey machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey key = machine.OpenSubKey(@"SOFTWARE\EqualizerAPO"))
                    {
                        if (key == null)
                        {
                            continue;
                        }
                        string config = key.GetValue("ConfigPath") as string;
                        if ((config != null) && Directory.Exists(config))
                        {
                            return config;
                        }
                        string install = key.GetValue("InstallPath") as string;
                        if (install != null)
                        {
                            config = Path.Combine(install, "config");
                            if (Directory.Exists(config))
                            {
                                return config;
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            string[] roots = { Environment.GetEnvironmentVariable("ProgramW6432"), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) };
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root))
                {
                    continue;
                }
                string config = Path.Combine(root, "EqualizerAPO", "config");
                if (Directory.Exists(config))
                {
                    return config;
                }
            }
            return "";
        }

        static string Db(double db)
        {
            return db.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static string BuildText(int micPercent, int speakerPercent, bool enabled)
        {
            double micDb = 0;
            double speakerDb = 0;
            if (enabled)
            {
                micDb = Gain.ToDb(micPercent);
                speakerDb = Gain.ToDb(speakerPercent);
            }

            return "# AudioBoom\r\n" +
                "Device: all\r\n" +
                "Stage: capture\r\n" +
                "Preamp: " + Db(micDb) + " dB\r\n" +
                "Stage: post-mix\r\n" +
                "Preamp: " + Db(speakerDb) + " dB\r\n" +
                "Stage: post-mix capture\r\n";
        }

        public ApoState Apply(int micPercent, int speakerPercent, bool enabled)
        {
            if ((ConfigPath == "") || (Directory.Exists(ConfigPath) == false))
            {
                return ApoState.NotFound;
            }

            string text = BuildText(micPercent, speakerPercent, enabled);
            string file = Path.Combine(ConfigPath, FILE_NAME);

            for (int tries = 0; tries < 4; tries++)
            {
                try
                {
                    AddInclude();
                    if (File.Exists(file) && (File.ReadAllText(file) == text))
                    {
                        return ApoState.Ready;
                    }
                    File.WriteAllText(file, text, Encoding.ASCII);
                    return ApoState.Ready;
                }
                catch (UnauthorizedAccessException)
                {
                    return ApoState.NoAccess;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
            return ApoState.NoAccess;
        }

        void AddInclude()
        {
            string main = Path.Combine(ConfigPath, "config.txt");
            string old = "";
            if (File.Exists(main))
            {
                old = File.ReadAllText(main);
            }

            foreach (string line in old.Split('\n'))
            {
                if (line.Trim().Equals(INCLUDE_LINE, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            string add = INCLUDE_LINE + "\r\n";
            if ((old != "") && (old.EndsWith("\n") == false))
            {
                add = "\r\n" + add;
            }
            File.AppendAllText(main, add, Encoding.ASCII);
        }
    }
}
