using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TabletDriverGUI
{
    public class ConfigurationManager
    {
        public static bool Pause = false;

        public static event ConfigurationChangedHandler ConfigurationChanged;
        public delegate void ConfigurationChangedHandler();

        public const string DefaultConfigPath = "config/";
        public static Configuration Current { get { return Configurations[index]; } }
        public static List<Configuration> Configurations = new List<Configuration>();
        private static int index = 0;

        private static string ForegroundApp;

        private static WinEventProc call;
        private static IntPtr hook;
        static ConfigurationManager()
        {
            ReloadConfigFiles();
            call = (c, w, l, p, n, z, g) =>
            {
                if (w == (int)EventConstants.EVENT_SYSTEM_FOREGROUND)
                {
                    string f = GetForegroundPath(l);
                    if (ForegroundApp != f)
                    {
                        ForegroundApp = f;
                        CheckChanges();
                    }
                }
            };
            hook = SetWinEventHook((int)EventConstants.EVENT_MIN, (int)EventConstants.EVENT_MAX, IntPtr.Zero, call, 0, 0, 0);
            int i = Marshal.GetLastWin32Error();
            SystemEvents.DisplaySettingsChanged += (o, p) =>
            {
                CheckChanges();
            };
        }


        public static void CheckChanges()
        {
            if (Pause) return;
            System.Drawing.Rectangle r = MainWindow.GetVirtualDesktopSize();
            string[][] e = {
                new string[]{ "App", ForegroundApp },
                new string[]{ "ScreenWidth", r.Width.ToString() },
                new string[]{ "ScreenHeight", r.Height.ToString() }
            };
            Configuration dest = null;
            Configuration def = null;
            int max = 0;
            foreach (Configuration c in Configurations)
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
            if (dest == null) { dest = def ?? Configurations[0]; }
            if (index != Configurations.IndexOf(dest))
            {
                index = Configurations.IndexOf(dest);
                Current.DesktopWidth = r.Width;
                Current.DesktopHeight = r.Height;
                if (MainWindow.driver != null && MainWindow.running)
                {
                    Current.SendToDriver(MainWindow.driver);
                    ConfigurationChanged();
                    MainWindow.driver.ConsoleAddText("Configuration File " + Current.ConfigFilename + " Loaded.");
                }
            }
        }

        public static void ReloadConfigFiles(string path = DefaultConfigPath)
        {
            Configurations.Clear();
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
                            Configurations.Add(conf);
                        }
                        catch (Exception) { }
                    }
                }
            }
            if (Configurations.Count == 0)
            {
                Configurations.Add(new Configuration());
            }
        }


        [DllImport("user32.dll")]
        public extern static int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        public static String GetForegroundPath(IntPtr window)
        {
            try
            {
                int pId = 0;
                GetWindowThreadProcessId(window, out pId);
                Process p = Process.GetProcessById(pId);
                return p.MainModule.FileName;
            }
            catch (Exception w)
            {
                Debug.WriteLine(w);
                return "";
            }
        }

        public static void QuestConfiguration(int selectedIndex)
        {
            index = selectedIndex;
            ConfigurationChanged();
        }

        #region CBT

        public enum EventConstants
        {
            EVENT_MIN = 0x00000001,
            EVENT_MAX = 0x7FFFFFFF,
            EVENT_SYSTEM_SOUND = 0x0001,
            EVENT_SYSTEM_ALERT = 0x0002,
            EVENT_SYSTEM_FOREGROUND = 0x0003,
            EVENT_SYSTEM_MENUSTART = 0x0004,
            EVENT_SYSTEM_MENUEND = 0x0005,
            EVENT_SYSTEM_MENUPOPUPSTART = 0x0006,
            EVENT_SYSTEM_MENUPOPUPEND = 0x0007,
            EVENT_SYSTEM_CAPTURESTART = 0x0008,
            EVENT_SYSTEM_CAPTUREEND = 0x0009,
            EVENT_SYSTEM_MOVESIZESTART = 0x000A,
            EVENT_SYSTEM_MOVESIZEEND = 0x000B,
            EVENT_SYSTEM_CONTEXTHELPSTART = 0x000C,
            EVENT_SYSTEM_CONTEXTHELPEND = 0x000D,
            EVENT_SYSTEM_DRAGDROPSTART = 0x000E,
            EVENT_SYSTEM_DRAGDROPEND = 0x000F,
            EVENT_SYSTEM_DIALOGSTART = 0x0010,
            EVENT_SYSTEM_DIALOGEND = 0x0011,
            EVENT_SYSTEM_SCROLLINGSTART = 0x0012,
            EVENT_SYSTEM_SCROLLINGEND = 0x0013,
            EVENT_SYSTEM_SWITCHSTART = 0x0014,
            EVENT_SYSTEM_SWITCHEND = 0x0015,
            EVENT_SYSTEM_MINIMIZESTART = 0x0016,
            EVENT_SYSTEM_MINIMIZEEND = 0x0017,
            EVENT_SYSTEM_DESKTOPSWITCH = 0x0020,
            EVENT_CONSOLE_CARET = 0x4001,
            EVENT_CONSOLE_UPDATE_REGION = 0x4002,
            EVENT_CONSOLE_UPDATE_SIMPLE = 0x4003,
            EVENT_CONSOLE_UPDATE_SCROLL = 0x4004,
            EVENT_CONSOLE_LAYOUT = 0x4005,
            EVENT_CONSOLE_START_APPLICATION = 0x4006,
            EVENT_CONSOLE_END_APPLICATION = 0x4007,
            EVENT_OBJECT_CREATE = 0x8000,
            EVENT_OBJECT_DESTROY = 0x8001,
            EVENT_OBJECT_SHOW = 0x8002,
            EVENT_OBJECT_HIDE = 0x8003,
            EVENT_OBJECT_REORDER = 0x8004,
            EVENT_OBJECT_FOCUS = 0x8005,
            EVENT_OBJECT_SELECTION = 0x8006,
            EVENT_OBJECT_SELECTIONADD = 0x8007,
            EVENT_OBJECT_SELECTIONREMOVE = 0x8008,
            EVENT_OBJECT_SELECTIONWITHIN = 0x8009,
            EVENT_OBJECT_STATECHANGE = 0x800A,
            EVENT_OBJECT_LOCATIONCHANGE = 0x800B,
            EVENT_OBJECT_NAMECHANGE = 0x800C,
            EVENT_OBJECT_DESCRIPTIONCHANGE = 0x800D,
            EVENT_OBJECT_VALUECHANGE = 0x800E,
            EVENT_OBJECT_PARENTCHANGE = 0x800F,
            EVENT_OBJECT_HELPCHANGE = 0x8010,
            EVENT_OBJECT_DEFACTIONCHANGE = 0x8011,
            EVENT_OBJECT_ACCELERATORCHANGE = 0x8012,
            EVENT_OBJECT_INVOKED = 0x8013,
            EVENT_OBJECT_TEXTSELECTIONCHANGED = 0x8014,
            EVENT_OBJECT_CONTENTSCROLLED = 0x8015
        }

        public delegate void WinEventProc(IntPtr hWinEventHook, ulong @event, IntPtr hwnd, long idObject, long idChild, ulong dwEventThread, ulong dwmsEventTime);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwflags);

        #endregion
    }
}
