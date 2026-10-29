using System;
using System.Threading;
using System.Windows.Forms;

namespace AudioBoom
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool hidden = false;
            string config = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--hidden")
                {
                    hidden = true;
                }
                if ((args[i] == "--config") && (i + 1 < args.Length))
                {
                    config = args[i + 1];
                }
            }

            bool first;
            Mutex single = new Mutex(true, "Local\\AudioBoom_Single", out first);
            EventWaitHandle showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\AudioBoom_Show");
            if (first == false)
            {
                showEvent.Set();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm form = new MainForm(hidden, config);

            Thread watcher = new Thread(delegate ()
            {
                while (true)
                {
                    showEvent.WaitOne();
                    if (form.IsHandleCreated)
                    {
                        form.BeginInvoke(new MethodInvoker(form.ShowWindow));
                    }
                }
            });
            watcher.IsBackground = true;
            watcher.Start();

            Application.Run(form);
            GC.KeepAlive(single);
        }
    }
}
