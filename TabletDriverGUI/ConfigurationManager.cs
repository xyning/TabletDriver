using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TabletDriverGUI
{
    public class ConfigurationManager
    {
        public static event ConfigurationChangedHandler ConfigurationChanged;
        public delegate void ConfigurationChangedHandler();

        public const string DefaultConfigPath = "config/";
        public static Configuration Current { get { return configurations[index]; } }
        private static List<Configuration> configurations = new List<Configuration>();
        private static int index = 0;
        public static bool isFirstStart = false;

        static System.Threading.Timer ForegroundAppTimer;
        static string ForegroundApp;
        static ConfigurationManager()
        {
            ReloadConfigFiles();
            ForegroundAppTimer = new System.Threading.Timer(o =>
            {
                string f = GetForegroundPath();
                if (ForegroundApp != f)
                {
                    ForegroundApp = f;
                    CheckChanges();
                }
            }, null, 2000, 1000);
            SystemEvents.DisplaySettingsChanged += (o, p) =>
            {
                CheckChanges();
            };
        }

        private static void CheckChanges(params string[] o)
        {
            System.Drawing.Rectangle r = MainWindow.GetVirtualDesktopSize();
            string[][] e = {
                new string[]{ "App", ForegroundApp },
                new string[]{ "Width", r.Width.ToString() },
                new string[]{ "Height",r.Height.ToString() }
            };
            Configuration dest = null;
            Configuration def = null;
            int max = 0;
            foreach (Configuration c in configurations)
            {
                int count = 0;
                string[] s = c.EffectiveConditions.Split('|');
                if (s.Length == 0 || (s.Length == 1 && s[0].Trim() == "")) { def = c; goto skip; }
                foreach (string st in s)
                {
                    string[] str = st.Split('>');
                    foreach (string[] stri in e)
                    {
                        if (stri[0] == str[0] && stri[1] != str[1]) { goto skip; }
                        if (stri[0] == str[0] && stri[1] == str[1]) { count++; }
                    }
                }
                if (count > max) { max = count; dest = c; }
                skip:;
            }
            if (dest == null) { dest = def ?? configurations[0]; }
            index = configurations.IndexOf(dest);
            Current.DesktopWidth = r.Width;
            Current.DesktopHeight = r.Height;
            if (MainWindow.driver != null && MainWindow.running)
            {
                Current.SendToDriver(MainWindow.driver);
                ConfigurationChanged();
                MainWindow.driver.ConsoleAddText("Configuration File " + Current.ConfigFilename + " Loaded.");
            }
        }

        public static void ReloadConfigFiles(string path = DefaultConfigPath)
        {
            if (Directory.Exists(DefaultConfigPath))
            {
                string[] s = Directory.GetFiles(DefaultConfigPath);
                if (s != null)
                {
                    foreach (string str in s)
                    {
                        try
                        {
                            Configuration conf = Configuration.CreateFromFile(str);
                            conf.ConfigFilename = str;
                            configurations.Add(conf);
                        }
                        catch (Exception) { }
                    }
                }
            }
            if (configurations.Count == 0)
            {
                configurations.Add(new Configuration());
                isFirstStart = true;
            }
        }


        public const int PROCESS_ALL_ACCESS = 0x000F0000 | 0x00100000 | 0xFFF;
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public extern static int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("Kernel32.dll")]
        public extern static IntPtr OpenProcess(int fdwAccess, int fInherit, int IDProcess);
        [DllImport("Kernel32.dll")]
        public extern static bool TerminateProcess(IntPtr hProcess, int uExitCode);
        [DllImport("Kernel32.dll")]
        public extern static bool CloseHandle(IntPtr hObject);
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "GetModuleFileName")]
        private static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpszFileName, int nSize);

        public static String GetPathFromHandle(IntPtr hwnd)
        {
            int pId = 0;
            IntPtr pHandle = IntPtr.Zero;
            GetWindowThreadProcessId(hwnd, out pId);
            pHandle = OpenProcess(PROCESS_ALL_ACCESS, 0, pId);
            StringBuilder sb = new StringBuilder(260);
            GetModuleFileName(pHandle, sb, sb.Capacity);
            CloseHandle(pHandle);
            return sb.ToString();
        }
        public static String GetForegroundPath()
        {
            try
            {
                int pId = 0;
                GetWindowThreadProcessId(GetForegroundWindow(), out pId);
                Process p = Process.GetProcessById(pId);
                return p.MainModule.FileName;
            }
            catch (Exception w)
            {
                Debug.WriteLine(w);
                return "";
            }
        }
    }
}
