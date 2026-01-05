using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SignalLost
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        static extern int ShowCursor(bool bShow);

        public struct Point { public int X; public int Y; }

        static void Main(string[] args)
        {
            string mode = args.Length > 0 ? args[0].ToLower().Substring(0, 2) : "/s";

            if (mode == "/p" || mode == "/c") return;

            RunScreensaver();
        }

        static void RunScreensaver()
        {
            string vlcPath = @"C:\Program Files\VideoLAN\VLC\vlc.exe";
            if (!File.Exists(vlcPath)) vlcPath = @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe";
            if (!File.Exists(vlcPath)) return;

            string tempVideoPath = Path.Combine(Path.GetTempPath(), "signallost.mp4");
            try
            {
                ExtractEmbeddedVideo("SignalLost.assets.signallost.mp4", tempVideoPath);
            }
            catch { return; }

            List<Process> vlcProcesses = new List<Process>();
            ShowCursor(false);

            foreach (var screen in Screen.AllScreens)
            {
                string audioFlag = screen.Primary ? "" : "--no-audio";

                string arguments = $"--fullscreen --no-video-title-show --mouse-hide-timeout 0 --loop " +
                                   $"--video-x={screen.Bounds.X} --video-y={screen.Bounds.Y} " +
                                   $"{audioFlag} \"{tempVideoPath}\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = vlcPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process? p = Process.Start(psi);
                if (p != null) vlcProcesses.Add(p);
            }

            MonitorMouseMovement(vlcProcesses);

            ShowCursor(true);
        }

        static void MonitorMouseMovement(List<Process> processes)
        {
            GetCursorPos(out Point startPos);
            bool shouldExit = false;

            while (!shouldExit)
            {
                GetCursorPos(out Point currentPos);

                if (Math.Abs(currentPos.X - startPos.X) > 5 || Math.Abs(currentPos.Y - startPos.Y) > 5)
                {
                    shouldExit = true;
                }

                foreach (var p in processes)
                {
                    if (p.HasExited) shouldExit = true;
                }

                if (shouldExit)
                {
                    foreach (var p in processes)
                    {
                        try { if (!p.HasExited) p.Kill(); } catch { }
                    }
                }

                Thread.Sleep(100);
            }
        }

        static void ExtractEmbeddedVideo(string resourceName, string tempPath)
        {
            if (!File.Exists(tempPath))
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream? resource = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resource == null) throw new FileNotFoundException();
                    using (FileStream fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                    {
                        resource.CopyTo(fileStream);
                    }
                }
            }
        }
    }
}