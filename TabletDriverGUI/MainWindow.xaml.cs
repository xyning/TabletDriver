using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TabletDriverGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Version
        public string Version = "0.1.5";

        // Console stuff
        private List<string> commandHistory;
        private int commandHistoryIndex;

        // Notify icon
        private System.Windows.Forms.NotifyIcon notifyIcon;
        public bool IsRealExit;

        // Driver
        public static TabletDriver driver;
        public static bool running;

        // Timers
        private DispatcherTimer timerStatusbar;
        private DispatcherTimer timerRestart;
        private DispatcherTimer timerConsoleUpdate;

        // Config
        private bool isLoadingSettings;

        // Screen map canvas elements
        private Rectangle[] rectangleMonitors;
        private Rectangle rectangleDesktop;
        private Rectangle rectangleScreenMap;
        private TextBlock textScreenAspectRatio;

        // Tablet area canvas elements
        private Polygon polygonTabletFullArea;
        private Polygon polygonTabletArea;
        private Polygon polygonTabletAreaArrow;
        private TextBlock textTabletAspectRatio;

        // Mouse drag
        private class MouseDrag
        {
            public bool IsMouseDown;
            public object Source;
            public Point OriginMouse;
            public Point OriginDraggable;
            public MouseDrag()
            {
                IsMouseDown = false;
                Source = null;
                OriginMouse = new Point(0, 0);
                OriginDraggable = new Point(0, 0);
            }
        }
        MouseDrag mouseDrag;

        //
        // Constructor
        //
        public MainWindow()
        {
            // Set the current directory as TabletDriverGUI.exe's directory.
            try { Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory); } catch (Exception) { }

            //
            isLoadingSettings = true;

            // Initialize WPF
            InitializeComponent();

            // Version text
            textVersion.Text = this.Version;

            // Set culture to en-US to force decimal format and etc.
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");


            // Create notify icon
            notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                // Icon
                Icon = Properties.Resources.AppIcon,

                // Menu items
                ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[]
                {
                    new System.Windows.Forms.MenuItem("TabletDriverGUI " + Version),
                    new System.Windows.Forms.MenuItem("Show", NotifyShowWindow),
                    new System.Windows.Forms.MenuItem("Exit", NotifyExit)
                })
            };
            notifyIcon.ContextMenu.MenuItems[0].Enabled = false;

            notifyIcon.Text = "";
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            notifyIcon.Visible = true;
            IsRealExit = false;

            // Create command history list
            commandHistory = new List<string> { "" };
            commandHistoryIndex = 0;

            // Init tablet driver
            driver = new TabletDriver("TabletDriverService.exe");
            driver.MessageReceived += OnDriverMessageReceived;
            driver.ErrorReceived += OnDriverErrorReceived;
            driver.Started += OnDriverStarted;
            driver.Stopped += OnDriverStopped;
            running = false;


            // Restart timer
            timerRestart = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 5)
            };
            timerRestart.Tick += TimerRestart_Tick;

            // Statusbar timer
            timerStatusbar = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 5)
            };
            timerStatusbar.Tick += TimerStatusbar_Tick;

            // Timer console update
            timerConsoleUpdate = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 200)
            };
            timerConsoleUpdate.Tick += TimerConsoleUpdate_Tick;

            // Tooltip timeout
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(60000));


            //
            // Buttom Map ComboBoxes
            //
            comboBoxButton1.Items.Clear();
            comboBoxButton2.Items.Clear();
            comboBoxButton3.Items.Clear();
            comboBoxButton1.Items.Add("Disable");
            comboBoxButton2.Items.Add("Disable");
            comboBoxButton3.Items.Add("Disable");
            for (int i = 1; i <= 5; i++)
            {
                comboBoxButton1.Items.Add("Mouse " + i);
                comboBoxButton2.Items.Add("Mouse " + i);
                comboBoxButton3.Items.Add("Mouse " + i);
            }
            comboBoxButton1.Items.Add("Extra");
            comboBoxButton2.Items.Add("Extra");
            comboBoxButton3.Items.Add("Extra");
            comboBoxButton1.SelectedIndex = 0;
            comboBoxButton2.SelectedIndex = 0;
            comboBoxButton3.SelectedIndex = 0;

            extraTipEventBox.Items.Clear();
            extraBottomEventBox.Items.Clear();
            extraTopEventBox.Items.Clear();
            extraTipEventBox.Items.Add("None");
            extraBottomEventBox.Items.Add("None");
            extraTopEventBox.Items.Add("None");
            extraTipEventBox.Items.Add("Mouse Wheel");
            extraBottomEventBox.Items.Add("Mouse Wheel");
            extraTopEventBox.Items.Add("Mouse Wheel");
            extraTipEventBox.Items.Add("Disable Tablet");
            extraBottomEventBox.Items.Add("Disable Tablet");
            extraTopEventBox.Items.Add("Disable Tablet");
            extraTipEventBox.Items.Add("Keyboard");
            extraBottomEventBox.Items.Add("Keyboard");
            extraTopEventBox.Items.Add("Keyboard");

            //
            // Smoothing rate ComboBox
            //
            comboBoxSmoothingRate.Items.Clear();
            for (int i = 2; i <= 8; i++)
            {
                comboBoxSmoothingRate.Items.Add((1000.0 / i).ToString("0") + " Hz");
            }
            comboBoxSmoothingRate.SelectedIndex = 2;

            // Process command line arguments
            ProcessCommandLineArguments();

            // Events
            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;

            switch (ConfigurationManager.Method)
            {
                case ConfigurationManager.ListenMethod.Stop: { disableAutoSwitch.IsChecked = true; break; }
                case ConfigurationManager.ListenMethod.Poll_L: { fdm_Poll.IsChecked = true; break; }
                case ConfigurationManager.ListenMethod.Poll_M: { fdm_PollH.IsChecked = true; break; }
                case ConfigurationManager.ListenMethod.Poll_X: { fdm_PollL.IsChecked = true; break; }
                case ConfigurationManager.ListenMethod.Callback_WinEventHook: { fdm_WEHook.IsChecked = true; break; }
            }
            //
            isLoadingSettings = false;

            ConfigurationManager.ConfigurationChanged += () => LoadSettingsFromConfiguration();
            ConfigurationManager.ConfigurationChanged += () => driver.SendCommand("status");
        }



        #region Window events

        // Window is closing -> Stop driver
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.Visible = false;
            try
            {
                ConfigurationManager.Current.Write();
            }
            catch (Exception)
            {
            }
            Stop();
        }

        // Window loaded -> Start driver
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            // Invalid config -> Set defaults
            if (ConfigurationManager.Current.ScreenArea.Width == 0 || ConfigurationManager.Current.ScreenArea.Height == 0)
            {
                ConfigurationManager.Current.DesktopSize.Width = GetVirtualDesktopSize().Width;
                ConfigurationManager.Current.DesktopSize.Height = GetVirtualDesktopSize().Height;
                ConfigurationManager.Current.ScreenArea.Width = ConfigurationManager.Current.DesktopSize.Width;
                ConfigurationManager.Current.ScreenArea.Height = ConfigurationManager.Current.DesktopSize.Height;
                ConfigurationManager.Current.ScreenArea.X = 0;
                ConfigurationManager.Current.ScreenArea.Y = 0;
            }

            // Create canvas elements
            CreateCanvasElements();

            // Load settings from configuration
            LoadSettingsFromConfiguration();

            // Update the settings back to the configuration
            UpdateSettingsToConfiguration();


            // Set run at startup
            SetRunAtStartup(ConfigurationManager.Current.RunAtStartup);

            // Hide the window if the GUI is started as minimized
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }

            // Start the driver
            Start();
        }


        //
        // Process command line arguments
        //
        void ProcessCommandLineArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                // Skip values
                if (!args[i].StartsWith("-") && !args[i].StartsWith("/")) continue;

                // Remove '-' and '/' characters at the start of the argument
                string parameter = Regex.Replace(args[i], "^[\\-/]+", "").ToLower();

                //
                // Parameter: --hide
                //
                if (parameter == "hide")
                {
                    WindowState = WindowState.Minimized;
                }
            }
        }

        #endregion



        #region Notify icon stuff

        // Notify icon double click -> show window
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                NotifyShowWindow(sender, e);
            }
            else
            {
                NotifyHideWindow(sender, e);
            }
        }

        // Window minimizing -> minimize to taskbar
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        // 'Hide' handler for taskbar menu
        void NotifyHideWindow(object sender, EventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // 'Show' handler for taskbar menu
        void NotifyShowWindow(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        // 'Exit' handler for taskbar menu
        void NotifyExit(object sender, EventArgs e)
        {
            IsRealExit = true;
            Application.Current.Shutdown();
        }

        #endregion



        #region Setting handlers

        //
        // Load settings from configuration
        //
        private void LoadSettingsFromConfiguration()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                extraTipEventBox.SelectionChanged -= ExtraSelectionChanged;
                extraBottomEventBox.SelectionChanged -= ExtraSelectionChanged;
                extraTopEventBox.SelectionChanged -= ExtraSelectionChanged;

                isLoadingSettings = true;

                //
                // Tablet area
                //
                textTabletAreaWidth.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Width);
                textTabletAreaHeight.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Height);
                textTabletAreaX.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.X);
                textTabletAreaY.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Y);
                checkBoxForceAspect.IsChecked = ConfigurationManager.Current.ForceAspectRatio;
                checkBoxForceFullArea.IsChecked = ConfigurationManager.Current.ForceFullArea;
                switch (ConfigurationManager.Current.OutputMode)
                {
                    case Configuration.OutputModes.Absolute:
                        radioModeAbsolute.IsChecked = true;
                        break;
                    case Configuration.OutputModes.Relative:
                        radioModeRelative.IsChecked = true;
                        break;
                    case Configuration.OutputModes.Digitizer:
                        radioModeDigitizer.IsChecked = true;
                        break;
                }
                textTabletAreaRotation.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Rotation);
                checkBoxInvert.IsChecked = ConfigurationManager.Current.Invert;


                //
                // Force full area
                //
                if (ConfigurationManager.Current.ForceFullArea)
                {
                    textTabletAreaWidth.IsEnabled = false;
                    textTabletAreaHeight.IsEnabled = false;
                    textTabletAreaX.IsEnabled = false;
                    textTabletAreaY.IsEnabled = false;
                }
                else
                {
                    textTabletAreaWidth.IsEnabled = true;
                    textTabletAreaHeight.IsEnabled = true;
                    textTabletAreaX.IsEnabled = true;
                    textTabletAreaY.IsEnabled = true;
                }


                //
                // Screen area
                //
                textScreenAreaWidth.Text = Utils.GetNumberString(ConfigurationManager.Current.ScreenArea.Width, "0");
                textScreenAreaHeight.Text = Utils.GetNumberString(ConfigurationManager.Current.ScreenArea.Height, "0");
                textScreenAreaX.Text = Utils.GetNumberString(ConfigurationManager.Current.ScreenArea.X, "0");
                textScreenAreaY.Text = Utils.GetNumberString(ConfigurationManager.Current.ScreenArea.Y, "0");


                //
                // Desktop size
                //
                if (ConfigurationManager.Current.AutomaticDesktopSize)
                {
                    textDesktopWidth.Text = Utils.GetNumberString(GetVirtualDesktopSize().Width);
                    textDesktopHeight.Text = Utils.GetNumberString(GetVirtualDesktopSize().Height);
                    ConfigurationManager.Current.DesktopSize.Width = GetVirtualDesktopSize().Width;
                    ConfigurationManager.Current.DesktopSize.Height = GetVirtualDesktopSize().Height;
                    textDesktopWidth.IsEnabled = false;
                    textDesktopHeight.IsEnabled = false;
                }
                else
                {
                    textDesktopWidth.Text = Utils.GetNumberString(ConfigurationManager.Current.DesktopSize.Width);
                    textDesktopHeight.Text = Utils.GetNumberString(ConfigurationManager.Current.DesktopSize.Height);
                }
                checkBoxAutomaticDesktopSize.IsChecked = ConfigurationManager.Current.AutomaticDesktopSize;


                // Force aspect ratio
                if (ConfigurationManager.Current.ForceAspectRatio)
                {
                    ConfigurationManager.Current.TabletArea.Height = ConfigurationManager.Current.TabletArea.Width / (ConfigurationManager.Current.ScreenArea.Width / ConfigurationManager.Current.ScreenArea.Height);
                    textTabletAreaHeight.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Height);
                    textTabletAreaHeight.IsEnabled = false;
                }


                //
                // Move tablet area to a valid position
                //
                ConfigurationManager.Current.TabletArea.MoveInside(ConfigurationManager.Current.TabletFullArea);
                textTabletAreaX.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.X);
                textTabletAreaY.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Y);


                //
                // Buttons
                //
                if (ConfigurationManager.Current.ButtonMap.Count() == 3)
                {
                    comboBoxButton1.SelectedIndex = ConfigurationManager.Current.ButtonMap[0] == 8 ? 6 : ConfigurationManager.Current.ButtonMap[0];
                    comboBoxButton2.SelectedIndex = ConfigurationManager.Current.ButtonMap[1] == 7 ? 6 : ConfigurationManager.Current.ButtonMap[1];
                    comboBoxButton3.SelectedIndex = ConfigurationManager.Current.ButtonMap[2] == 6 ? 6 : ConfigurationManager.Current.ButtonMap[2];
                    extraTipEventBox.SelectedIndex = (int)ConfigurationManager.Current.ExtraButtonEvents[0];
                    extraBottomEventBox.SelectedIndex = (int)ConfigurationManager.Current.ExtraButtonEvents[1];
                    extraTopEventBox.SelectedIndex = (int)ConfigurationManager.Current.ExtraButtonEvents[2];
                }
                else
                {
                    ConfigurationManager.Current.ButtonMap = new int[] { 1, 2, 3 };
                }
                checkBoxDisableButtons.IsChecked = ConfigurationManager.Current.DisableButtons;


                //
                // Smoothing filter
                //
                checkBoxSmoothing.IsChecked = ConfigurationManager.Current.SmoothingEnabled;
                textSmoothingLatency.Text = Utils.GetNumberString(ConfigurationManager.Current.SmoothingLatency);
                comboBoxSmoothingRate.SelectedIndex = ConfigurationManager.Current.SmoothingInterval - 2;
                if (ConfigurationManager.Current.SmoothingEnabled)
                {
                    textSmoothingLatency.IsEnabled = true;
                    comboBoxSmoothingRate.IsEnabled = true;
                }
                else
                {
                    textSmoothingLatency.IsEnabled = false;
                    comboBoxSmoothingRate.IsEnabled = false;
                }

                //
                // Run at startup
                //
                checkRunAtStartup.IsChecked = ConfigurationManager.Current.RunAtStartup;


                //
                // Custom commands
                //
                string tmp = "";
                foreach (string command in ConfigurationManager.Current.CommandsBefore)
                {
                    if (command.Trim().Length > 0)
                        tmp += command.Trim() + "\n";
                }
                textCommandsBefore.Text = tmp;

                tmp = "";
                foreach (string command in ConfigurationManager.Current.CommandsAfter)
                {
                    if (command.Trim().Length > 0)
                        tmp += command.Trim() + "\n";
                }
                textCommandsAfter.Text = tmp;


                // Update canvases
                UpdateCanvasElements();


                isLoadingSettings = false;

                extraTipEventBox.SelectionChanged += ExtraSelectionChanged;
                extraBottomEventBox.SelectionChanged += ExtraSelectionChanged;
                extraTopEventBox.SelectionChanged += ExtraSelectionChanged;

                UpdateTitle();
            }));
        }

        //
        // Update settings to configuration
        //
        private void UpdateSettingsToConfiguration()
        {
            if (isLoadingSettings)
                return;

            bool oldValue;

            // Tablet area
            if (Utils.ParseNumber(textTabletAreaWidth.Text, out double val))
                ConfigurationManager.Current.TabletArea.Width = val;
            if (Utils.ParseNumber(textTabletAreaHeight.Text, out val))
                ConfigurationManager.Current.TabletArea.Height = val;
            if (Utils.ParseNumber(textTabletAreaX.Text, out val))
                ConfigurationManager.Current.TabletArea.X = val;
            if (Utils.ParseNumber(textTabletAreaY.Text, out val))
                ConfigurationManager.Current.TabletArea.Y = val;
            if (Utils.ParseNumber(textTabletAreaRotation.Text, out val))
                ConfigurationManager.Current.TabletArea.Rotation = val;

            ConfigurationManager.Current.Invert = (bool)checkBoxInvert.IsChecked;
            ConfigurationManager.Current.ForceAspectRatio = (bool)checkBoxForceAspect.IsChecked;
            ConfigurationManager.Current.ForceFullArea = (bool)checkBoxForceFullArea.IsChecked;

            // Output Mode
            if (radioModeAbsolute.IsChecked == true) ConfigurationManager.Current.OutputMode = Configuration.OutputModes.Absolute;
            if (radioModeRelative.IsChecked == true) ConfigurationManager.Current.OutputMode = Configuration.OutputModes.Relative;
            if (radioModeDigitizer.IsChecked == true) ConfigurationManager.Current.OutputMode = Configuration.OutputModes.Digitizer;


            // Force full area
            if (ConfigurationManager.Current.ForceFullArea)
            {
                // Set tablet area size to full area
                ConfigurationManager.Current.TabletArea.Width = ConfigurationManager.Current.TabletFullArea.Width;
                ConfigurationManager.Current.TabletArea.Height = ConfigurationManager.Current.TabletFullArea.Height;

                // Force aspect
                if (ConfigurationManager.Current.ForceAspectRatio)
                    ConfigurationManager.Current.TabletArea.Height = ConfigurationManager.Current.TabletArea.Width / (ConfigurationManager.Current.ScreenArea.Width / ConfigurationManager.Current.ScreenArea.Height);

                // Fit area to full area
                ConfigurationManager.Current.TabletArea.ScaleInside(ConfigurationManager.Current.TabletFullArea);

                textTabletAreaWidth.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Width);
                textTabletAreaHeight.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Height);

            }

            // Force the tablet area to be inside of the full area
            ConfigurationManager.Current.TabletArea.MoveInside(ConfigurationManager.Current.TabletFullArea);

            // Screen area
            if (Utils.ParseNumber(textScreenAreaWidth.Text, out val))
                ConfigurationManager.Current.ScreenArea.Width = val;
            if (Utils.ParseNumber(textScreenAreaHeight.Text, out val))
                ConfigurationManager.Current.ScreenArea.Height = val;
            if (Utils.ParseNumber(textScreenAreaX.Text, out val))
                ConfigurationManager.Current.ScreenArea.X = val;
            if (Utils.ParseNumber(textScreenAreaY.Text, out val))
                ConfigurationManager.Current.ScreenArea.Y = val;


            // Desktop size
            if (Utils.ParseNumber(textDesktopWidth.Text, out val))
                ConfigurationManager.Current.DesktopSize.Width = val;
            if (Utils.ParseNumber(textDesktopHeight.Text, out val))
                ConfigurationManager.Current.DesktopSize.Height = val;
            ConfigurationManager.Current.AutomaticDesktopSize = (bool)checkBoxAutomaticDesktopSize.IsChecked;
            if (ConfigurationManager.Current.AutomaticDesktopSize == true)
            {
                textDesktopWidth.Text = Utils.GetNumberString(GetVirtualDesktopSize().Width);
                textDesktopHeight.Text = Utils.GetNumberString(GetVirtualDesktopSize().Height);
                ConfigurationManager.Current.DesktopSize.Width = GetVirtualDesktopSize().Width;
                ConfigurationManager.Current.DesktopSize.Height = GetVirtualDesktopSize().Height;
            }


            // Force aspect ratio
            if (ConfigurationManager.Current.ForceAspectRatio)
            {
                ConfigurationManager.Current.TabletArea.Height = ConfigurationManager.Current.TabletArea.Width / (ConfigurationManager.Current.ScreenArea.Width / ConfigurationManager.Current.ScreenArea.Height);
                textTabletAreaHeight.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Height);
            }


            // Button map 
            ConfigurationManager.Current.ButtonMap[0] = comboBoxButton1.SelectedIndex == 6 ? 8 : comboBoxButton1.SelectedIndex;
            ConfigurationManager.Current.ButtonMap[1] = comboBoxButton2.SelectedIndex == 6 ? 7 : comboBoxButton2.SelectedIndex;
            ConfigurationManager.Current.ButtonMap[2] = comboBoxButton3.SelectedIndex == 6 ? 6 : comboBoxButton3.SelectedIndex;
            ConfigurationManager.Current.DisableButtons = (bool)checkBoxDisableButtons.IsChecked;
            ConfigurationManager.Current.ExtraButtonEvents[0] = (Configuration.ExtraEvents)extraTipEventBox.SelectedIndex;
            ConfigurationManager.Current.ExtraButtonEvents[1] = (Configuration.ExtraEvents)extraBottomEventBox.SelectedIndex;
            ConfigurationManager.Current.ExtraButtonEvents[2] = (Configuration.ExtraEvents)extraTopEventBox.SelectedIndex;

            int.TryParse(textDesktopWidth.Text, out ConfigurationManager.Current.DesktopWidth);
            int.TryParse(textDesktopHeight.Text, out ConfigurationManager.Current.DesktopHeight);

            // Filter
            ConfigurationManager.Current.SmoothingEnabled = (bool)checkBoxSmoothing.IsChecked;
            ConfigurationManager.Current.SmoothingInterval = comboBoxSmoothingRate.SelectedIndex + 2;
            if (Utils.ParseNumber(textSmoothingLatency.Text, out val))
                ConfigurationManager.Current.SmoothingLatency = val;

            if (ConfigurationManager.Current.SmoothingEnabled)
            {
                textSmoothingLatency.IsEnabled = true;
                comboBoxSmoothingRate.IsEnabled = true;
            }
            else
            {
                textSmoothingLatency.IsEnabled = false;
                comboBoxSmoothingRate.IsEnabled = false;
            }

            //
            // Run at startup
            //
            oldValue = ConfigurationManager.Current.RunAtStartup;
            ConfigurationManager.Current.RunAtStartup = (bool)checkRunAtStartup.IsChecked;
            if (ConfigurationManager.Current.RunAtStartup != oldValue)
                SetRunAtStartup(ConfigurationManager.Current.RunAtStartup);


            // Custom commands
            List<string> commandList = new List<string>();
            foreach (string command in textCommandsBefore.Text.Split('\n'))
                if (command.Trim().Length > 0)
                    commandList.Add(command.Trim());
            ConfigurationManager.Current.CommandsBefore = commandList.ToArray();

            commandList.Clear();
            foreach (string command in textCommandsAfter.Text.Split('\n'))
                if (command.Trim().Length > 0)
                    commandList.Add(command.Trim());
            ConfigurationManager.Current.CommandsAfter = commandList.ToArray();



            UpdateCanvasElements();

        }

        //
        // Set run at startup
        //
        private void SetRunAtStartup(bool enabled)
        {
            try
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string entryName = "TabletDriverGUI";
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (enabled)
                    rk.SetValue(entryName, "\"" + path + "\" --hide");
                else
                    rk.DeleteValue(entryName, false);

                rk.Close();
            }
            catch (Exception)
            {
            }
        }

        //
        // Get desktop size
        //
        public static System.Drawing.Rectangle GetVirtualDesktopSize()
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle();

            // Windows 8 or greater needed for the multiscreen absolute mode
            if (VersionHelper.IsWindows8OrGreater() || ConfigurationManager.Current.OutputMode == Configuration.OutputModes.Digitizer)
            {
                rect.Width = System.Windows.Forms.SystemInformation.VirtualScreen.Width;
                rect.Height = System.Windows.Forms.SystemInformation.VirtualScreen.Height;
            }
            else
            {
                rect.Width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                rect.Height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

            }
            return rect;
        }


        //
        // Get available screens
        //
        System.Windows.Forms.Screen[] GetAvailableScreens()
        {
            System.Windows.Forms.Screen[] screens;

            // Windows 8 or greater needed for the multiscreen absolute mode
            if (VersionHelper.IsWindows8OrGreater() || ConfigurationManager.Current.OutputMode == Configuration.OutputModes.Digitizer)
                screens = System.Windows.Forms.Screen.AllScreens;
            else
                screens = new System.Windows.Forms.Screen[] { System.Windows.Forms.Screen.PrimaryScreen };
            return screens;
        }


        //
        // Create canvas elements
        //
        void CreateCanvasElements()
        {
            //
            // Screen map canvas
            //
            // Clear canvas
            canvasScreenMap.Children.Clear();


            // Monitor rectangles
            rectangleMonitors = new Rectangle[16];
            for (int i = 0; i < 16; i++)
            {
                rectangleMonitors[i] = new Rectangle
                {
                    Width = 10,
                    Height = 10,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1.0,
                    Fill = Brushes.Transparent,
                    Visibility = Visibility.Hidden
                };
                canvasScreenMap.Children.Add(rectangleMonitors[i]);
            }

            //
            // Desktop area rectangle
            //
            rectangleDesktop = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2.0,
                Fill = Brushes.Transparent
            };
            canvasScreenMap.Children.Add(rectangleDesktop);


            //
            // Screen map area rectangle
            //
            Brush brushScreenMap = new SolidColorBrush(Color.FromArgb(50, 20, 20, 20));
            rectangleScreenMap = new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2.0,
                Fill = brushScreenMap
            };
            canvasScreenMap.Children.Add(rectangleScreenMap);

            //
            // Screen aspect ratio text
            //
            textScreenAspectRatio = new TextBlock
            {
                Text = "",
                Foreground = Brushes.Black,
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            canvasScreenMap.Children.Add(textScreenAspectRatio);



            //
            // Tablet area canvas
            //
            //
            // Clear
            canvasTabletArea.Children.Clear();

            //
            // Tablet full area polygon
            //
            polygonTabletFullArea = new Polygon
            {
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 2.0,
                Points = new PointCollection
                {
                    new Point(0,0),
                    new Point(0,0),
                    new Point(0,0),
                    new Point(0,0)
                },
            };
            canvasTabletArea.Children.Add(polygonTabletFullArea);

            //
            // Tablet area polygon
            //
            polygonTabletArea = new Polygon
            {
                Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                Fill = new SolidColorBrush(Color.FromArgb(50, 20, 20, 20)),
                StrokeThickness = 2.0,
                Points = new PointCollection
                {
                    new Point(0,0),
                    new Point(0,0),
                    new Point(0,0),
                    new Point(0,0)
                },
            };
            canvasTabletArea.Children.Add(polygonTabletArea);


            //
            // Tablet area arrow polygon
            //
            polygonTabletAreaArrow = new Polygon
            {
                Fill = new SolidColorBrush(Color.FromArgb(50, 20, 20, 20)),
                Points = new PointCollection
                {
                    new Point(0,0),
                    new Point(0,0),
                    new Point(0,0)
                },
            };
            canvasTabletArea.Children.Add(polygonTabletAreaArrow);


            //
            // Tablet area aspect ratio text
            //
            textTabletAspectRatio = new TextBlock
            {
                Text = "",
                Foreground = Brushes.Black,
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            canvasTabletArea.Children.Add(textTabletAspectRatio);




            //
            // Canvas mouse drag
            //
            mouseDrag = new MouseDrag();
        }

        //
        // Update canvas elements
        //
        void UpdateCanvasElements()
        {
            if (canvasScreenMap == null || canvasScreenMap.ActualWidth == 0)
            {
                return;
            }
            UpdateScreenMapCanvas();
            UpdateTabletAreaCanvas();
        }

        //
        // Update screen map canvas elements
        //
        void UpdateScreenMapCanvas()
        {
            // Canvas element scaling
            double scaleX = (canvasScreenMap.ActualWidth - 2) / ConfigurationManager.Current.DesktopSize.Width;
            double scaleY = (canvasScreenMap.ActualHeight - 2) / ConfigurationManager.Current.DesktopSize.Height;
            double scale = scaleX;
            if (scaleX > scaleY)
                scale = scaleY;

            // Centered offset
            double offsetX = canvasScreenMap.ActualWidth / 2.0 - ConfigurationManager.Current.DesktopSize.Width * scale / 2.0;
            double offsetY = canvasScreenMap.ActualHeight / 2.0 - ConfigurationManager.Current.DesktopSize.Height * scale / 2.0;


            // Full desktop area
            rectangleDesktop.Width = ConfigurationManager.Current.DesktopSize.Width * scale;
            rectangleDesktop.Height = ConfigurationManager.Current.DesktopSize.Height * scale;
            Canvas.SetLeft(rectangleDesktop, offsetX);
            Canvas.SetTop(rectangleDesktop, offsetY);


            // Screen map area
            rectangleScreenMap.Width = ConfigurationManager.Current.ScreenArea.Width * scale;
            rectangleScreenMap.Height = ConfigurationManager.Current.ScreenArea.Height * scale;
            Canvas.SetLeft(rectangleScreenMap, offsetX + ConfigurationManager.Current.ScreenArea.X * scale);
            Canvas.SetTop(rectangleScreenMap, offsetY + ConfigurationManager.Current.ScreenArea.Y * scale);

            // Screen aspect ratio text
            textScreenAspectRatio.Text = Utils.GetNumberString(ConfigurationManager.Current.ScreenArea.Width / ConfigurationManager.Current.ScreenArea.Height, "0.###") + ":1";
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Canvas.SetLeft(textScreenAspectRatio, offsetX +
                              (ConfigurationManager.Current.ScreenArea.X + ConfigurationManager.Current.ScreenArea.Width / 2.0) * scale -
                              textScreenAspectRatio.ActualWidth / 2.0
                          );
                Canvas.SetTop(textScreenAspectRatio, offsetY +
                    (ConfigurationManager.Current.ScreenArea.Y + ConfigurationManager.Current.ScreenArea.Height / 2.0) * scale -
                    textScreenAspectRatio.ActualHeight / 2.0
                );
            }));






            // Screens
            System.Windows.Forms.Screen[] screens = GetAvailableScreens();

            // Monitor minimums
            double minX = 99999;
            double minY = 99999;
            foreach (System.Windows.Forms.Screen screen in screens)
            {
                if (screen.Bounds.X < minX) minX = screen.Bounds.X;
                if (screen.Bounds.Y < minY) minY = screen.Bounds.Y;
            }


            // Monitor rectangles
            int rectangeIndex = 0;
            foreach (System.Windows.Forms.Screen screen in screens)
            {
                double x = screen.Bounds.X - minX;
                double y = screen.Bounds.Y - minY;

                rectangleMonitors[rectangeIndex].Visibility = Visibility.Visible;
                rectangleMonitors[rectangeIndex].Width = screen.Bounds.Width * scale;
                rectangleMonitors[rectangeIndex].Height = screen.Bounds.Height * scale;
                Canvas.SetLeft(rectangleMonitors[rectangeIndex], offsetX + x * scale);
                Canvas.SetTop(rectangleMonitors[rectangeIndex], offsetY + y * scale);

                rectangeIndex++;
                if (rectangeIndex >= 16) break;
            }

        }

        //
        // Update tablet area canvas elements
        //
        void UpdateTabletAreaCanvas()
        {
            double fullWidth = ConfigurationManager.Current.TabletFullArea.Width;
            double fullHeight = ConfigurationManager.Current.TabletFullArea.Height;

            // Canvas element scaling
            double scaleX = (canvasTabletArea.ActualWidth - 2) / fullWidth;
            double scaleY = (canvasTabletArea.ActualHeight - 2) / fullHeight;
            double scale = scaleX;
            if (scaleX > scaleY)
                scale = scaleY;


            double offsetX = canvasTabletArea.ActualWidth / 2.0 - fullWidth * scale / 2.0;
            double offsetY = canvasTabletArea.ActualHeight / 2.0 - fullHeight * scale / 2.0;

            //
            // Tablet full area
            //
            Point[] corners = ConfigurationManager.Current.TabletFullArea.Corners;
            for (int i = 0; i < 4; i++)
            {
                Point p = corners[i];
                p.X *= scale;
                p.Y *= scale;
                p.X += ConfigurationManager.Current.TabletFullArea.X * scale + offsetX;
                p.Y += ConfigurationManager.Current.TabletFullArea.Y * scale + offsetY;
                polygonTabletFullArea.Points[i] = p;
            }


            //
            // Tablet area
            //
            corners = ConfigurationManager.Current.TabletArea.Corners;
            for (int i = 0; i < 4; i++)
            {
                Point p = corners[i];
                p.X *= scale;
                p.Y *= scale;
                p.X += ConfigurationManager.Current.TabletArea.X * scale + offsetX;
                p.Y += ConfigurationManager.Current.TabletArea.Y * scale + offsetY;
                polygonTabletArea.Points[i] = p;
            }

            //
            // Tablet area arrow
            //
            polygonTabletAreaArrow.Points[0] = new Point(
                offsetX + ConfigurationManager.Current.TabletArea.X * scale,
                offsetY + ConfigurationManager.Current.TabletArea.Y * scale
            );

            polygonTabletAreaArrow.Points[1] = new Point(
                offsetX + corners[2].X * scale + ConfigurationManager.Current.TabletArea.X * scale,
                offsetY + corners[2].Y * scale + ConfigurationManager.Current.TabletArea.Y * scale
            );

            polygonTabletAreaArrow.Points[2] = new Point(
                offsetX + corners[3].X * scale + ConfigurationManager.Current.TabletArea.X * scale,
                offsetY + corners[3].Y * scale + ConfigurationManager.Current.TabletArea.Y * scale
            );


            //
            // Tablet area aspect ratio text
            //
            textTabletAspectRatio.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Width / ConfigurationManager.Current.TabletArea.Height, "0.###") + ":1";
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Canvas.SetLeft(textTabletAspectRatio, offsetX + (ConfigurationManager.Current.TabletArea.X) * scale - textTabletAspectRatio.ActualWidth / 2.0);
                Canvas.SetTop(textTabletAspectRatio, offsetY + (ConfigurationManager.Current.TabletArea.Y) * scale - textTabletAspectRatio.ActualHeight / 2.0);
            }));

        }




        //
        // Canvas mouse events
        //
        //
        // Canvas mouse down
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

            mouseDrag.IsMouseDown = true;
            mouseDrag.Source = (UIElement)sender;
            mouseDrag.OriginMouse = e.GetPosition((UIElement)mouseDrag.Source);

            // Screen Map
            if (mouseDrag.Source == canvasScreenMap)
            {
                // Reset monitor selection
                comboBoxMonitor.SelectedIndex = -1;

                mouseDrag.OriginDraggable = new Point(ConfigurationManager.Current.ScreenArea.X, ConfigurationManager.Current.ScreenArea.Y);
                canvasScreenMap.CaptureMouse();
            }

            // Tablet Area
            else if (mouseDrag.Source == canvasTabletArea)
            {
                mouseDrag.OriginDraggable = new Point(ConfigurationManager.Current.TabletArea.X, ConfigurationManager.Current.TabletArea.Y);
                canvasTabletArea.CaptureMouse();
            }
        }

        // Canvas mouse up
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mouseDrag.IsMouseDown = false;
            LoadSettingsFromConfiguration();
            isLoadingSettings = true;
            textScreenAreaX.Text = Utils.GetNumberString(ConfigurationManager.Current.ScreenArea.X, "0");
            textScreenAreaY.Text = Utils.GetNumberString(ConfigurationManager.Current.ScreenArea.Y, "0");
            textTabletAreaX.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.X);
            textTabletAreaY.Text = Utils.GetNumberString(ConfigurationManager.Current.TabletArea.Y);
            isLoadingSettings = false;
            canvasScreenMap.ReleaseMouseCapture();
            canvasTabletArea.ReleaseMouseCapture();
        }

        // Canvas mouse move
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point position;
            double dx, dy;
            double scaleX = 0, scaleY = 0, scale = 0;

            // Canvas mouse drag
            if (mouseDrag.IsMouseDown && mouseDrag.Source == sender)
            {
                position = e.GetPosition((UIElement)mouseDrag.Source);

                dx = position.X - mouseDrag.OriginMouse.X;
                dy = position.Y - mouseDrag.OriginMouse.Y;

                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    dx = 0;
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    dy = 0;

                // Screen map canvas
                if (mouseDrag.Source == canvasScreenMap)
                {
                    scaleX = ConfigurationManager.Current.DesktopSize.Width / canvasScreenMap.ActualWidth;
                    scaleY = ConfigurationManager.Current.DesktopSize.Height / canvasScreenMap.ActualHeight;
                    scale = scaleY;
                    if (scaleX > scaleY)
                        scale = scaleX;

                    ConfigurationManager.Current.ScreenArea.X = mouseDrag.OriginDraggable.X + dx * scale;
                    ConfigurationManager.Current.ScreenArea.Y = mouseDrag.OriginDraggable.Y + dy * scale;
                    UpdateScreenMapCanvas();
                }

                // Tablet area canvas
                else if (mouseDrag.Source == canvasTabletArea)
                {
                    scaleX = ConfigurationManager.Current.TabletFullArea.Width / canvasTabletArea.ActualWidth;
                    scaleY = ConfigurationManager.Current.TabletFullArea.Height / canvasTabletArea.ActualHeight;
                    scale = scaleY;
                    if (scaleX > scaleY)
                        scale = scaleX;

                    ConfigurationManager.Current.TabletArea.X = mouseDrag.OriginDraggable.X + dx * scale;
                    ConfigurationManager.Current.TabletArea.Y = mouseDrag.OriginDraggable.Y + dy * scale;

                    UpdateTabletAreaCanvas();
                }


            }
        }


        // TextBox setting changed
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSettingsToConfiguration();
        }

        // Checkbox setting changed
        private void CheckboxChanged(object sender, RoutedEventArgs e)
        {
            if (isLoadingSettings) return;


            // Disable tablet area settings when full area is forced
            if (checkBoxForceFullArea.IsChecked == true)
            {
                textTabletAreaWidth.IsEnabled = false;
                textTabletAreaHeight.IsEnabled = false;
                textTabletAreaX.IsEnabled = false;
                textTabletAreaY.IsEnabled = false;
            }
            else
            {
                textTabletAreaWidth.IsEnabled = true;
                textTabletAreaX.IsEnabled = true;
                textTabletAreaY.IsEnabled = true;

                // Disable tablet area height when aspect ratio is forced
                if (checkBoxForceAspect.IsChecked == true)
                    textTabletAreaHeight.IsEnabled = false;
                else
                    textTabletAreaHeight.IsEnabled = true;


            }

            // Disable button map selection when buttons are disabled
            if (checkBoxDisableButtons.IsChecked == true)
            {
                comboBoxButton1.IsEnabled = false;
                comboBoxButton2.IsEnabled = false;
                comboBoxButton3.IsEnabled = false;
            }
            else
            {
                comboBoxButton1.IsEnabled = true;
                comboBoxButton2.IsEnabled = true;
                comboBoxButton3.IsEnabled = true;
            }

            // Disable desktop size settings when automatic is checked
            if (checkBoxAutomaticDesktopSize.IsChecked == true)
            {
                textDesktopWidth.Text = Utils.GetNumberString(GetVirtualDesktopSize().Width);
                textDesktopHeight.Text = Utils.GetNumberString(GetVirtualDesktopSize().Height);
                textDesktopWidth.IsEnabled = false;
                textDesktopHeight.IsEnabled = false;
            }
            else
            {
                textDesktopWidth.IsEnabled = true;
                textDesktopHeight.IsEnabled = true;
            }

            UpdateSettingsToConfiguration();

            if (sender == checkBoxForceFullArea)
            {
                LoadSettingsFromConfiguration();
                UpdateSettingsToConfiguration();
            }
        }

        // Selection settings changed
        private void ItemSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSettingsToConfiguration();
        }

        private void ExtraSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox c = sender as ComboBox;
            Configuration.ExtraEvents extra = Configuration.ExtraEvents.None;
            string tag = "";
            switch ((Configuration.ExtraEvents)c.SelectedIndex)
            {
                case Configuration.ExtraEvents.None: { break; }
                case Configuration.ExtraEvents.MouseWheel:
                    {
                        ExtraButtonConfig.MouseWheel mouseWheel = new ExtraButtonConfig.MouseWheel();
                        mouseWheel.ShowDialog();
                        int i = mouseWheel.Value;
                        extra = Configuration.ExtraEvents.MouseWheel;
                        tag = i.ToString();
                        break;
                    }
                case Configuration.ExtraEvents.DisableTablet: { break; }
                case Configuration.ExtraEvents.Keyboard:
                    {
                        ExtraButtonConfig.Keyboard ex = new ExtraButtonConfig.Keyboard();
                        ex.ShowDialog();
                        string r = ex.Result;
                        extra = Configuration.ExtraEvents.Keyboard;
                        tag = r;
                        break;
                    }
            }
            if (c == extraTipEventBox) { ConfigurationManager.Current.ExtraButtonEvents[0] = extra; ConfigurationManager.Current.ExtraButtonEventTag[0] = tag; }
            else if (c == extraBottomEventBox) { ConfigurationManager.Current.ExtraButtonEvents[1] = extra; ConfigurationManager.Current.ExtraButtonEventTag[1] = tag; }
            else if (c == extraTopEventBox) { ConfigurationManager.Current.ExtraButtonEvents[2] = extra; ConfigurationManager.Current.ExtraButtonEventTag[2] = tag; }
            UpdateSettingsToConfiguration();
        }

        // Window size changed
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded || isLoadingSettings) return;
            if (WindowState != WindowState.Maximized)
            {
                ConfigurationManager.Current.WindowWidth = (int)e.NewSize.Width;
                ConfigurationManager.Current.WindowHeight = (int)e.NewSize.Height;
            }
        }

        // Monitor combobox clicked -> create new monitor list
        private void ComboBoxMonitor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            comboBoxMonitor.Items.Clear();


            System.Windows.Forms.Screen[] screens = GetAvailableScreens();
            if (screens.Length > 1)
            {
                comboBoxMonitor.Items.Add("Full desktop");
                foreach (System.Windows.Forms.Screen screen in screens)
                {
                    string name = screen.DeviceName;
                    if (screen.Primary)
                        name += " Main";

                    comboBoxMonitor.Items.Add(name);
                }
            }
            else
            {
                comboBoxMonitor.Items.Add(System.Windows.Forms.Screen.PrimaryScreen.DeviceName);
            }

        }

        // Monitor selected -> change screen map
        private void ComboBoxMonitor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Forms.Screen[] screens = GetAvailableScreens();
            double minX = 99999;
            double minY = 99999;
            foreach (System.Windows.Forms.Screen screen in screens)
            {
                if (screen.Bounds.X < minX) minX = screen.Bounds.X;
                if (screen.Bounds.Y < minY) minY = screen.Bounds.Y;
            }

            int index = comboBoxMonitor.SelectedIndex;
            if (index == 0)
            {
                textScreenAreaX.Text = "0";
                textScreenAreaY.Text = "0";
                textScreenAreaWidth.Text = Utils.GetNumberString(ConfigurationManager.Current.DesktopSize.Width);
                textScreenAreaHeight.Text = Utils.GetNumberString(ConfigurationManager.Current.DesktopSize.Height);
            }
            else if (index > 0)
            {
                index--;
                if (index >= 0 && index < screens.Length)
                {
                    textScreenAreaX.Text = Utils.GetNumberString(screens[index].Bounds.X - minX);
                    textScreenAreaY.Text = Utils.GetNumberString(screens[index].Bounds.Y - minY);
                    textScreenAreaWidth.Text = Utils.GetNumberString(screens[index].Bounds.Width);
                    textScreenAreaHeight.Text = Utils.GetNumberString(screens[index].Bounds.Height);
                }
            }
            UpdateSettingsToConfiguration();
        }

        //
        // Save settings
        //
        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigurationManager.Current.Write();
                ConfigurationManager.Current.SendToDriver(driver);
                SetStatus("Settings saved!");
            }
            catch (Exception)
            {
                string dir = Directory.GetCurrentDirectory();
                MessageBox.Show("Error occured while saving the configuration.\n" +
                    "Make sure that it is possible to create and edit files in the '" + dir + "' directory.\n",
                    "ERROR!", MessageBoxButton.OK, MessageBoxImage.Error
                );
            }
        }

        //
        // Apply settings
        //
        private void ApplySettings(object sender, RoutedEventArgs e)
        {
            ConfigurationManager.Current.SendToDriver(driver);
            SetStatus("Settings applied!");
        }


        //
        // Main Menu Click
        //
        private void MainMenuClick(object sender, RoutedEventArgs e)
        {

            // Import
            if (sender == mainMenuImport)
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    InitialDirectory = Directory.GetCurrentDirectory(),
                    Filter = "XML File|*.xml"
                };
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        File.Copy(dialog.FileName, Configuration.DefaultConfigFilename);
                        ConfigurationManager.ReloadConfigFiles();
                        LoadSettingsFromConfiguration();
                        SetStatus("Settings imported!");
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Settings import failed!", "ERROR!",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            // Export
            else if (sender == mainMenuExport)
            {
                SaveFileDialog dialog = new SaveFileDialog
                {
                    InitialDirectory = Directory.GetCurrentDirectory(),
                    AddExtension = true,
                    DefaultExt = "xml",
                    Filter = "XML File|*.xml"

                };
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        UpdateSettingsToConfiguration();
                        ConfigurationManager.Current.Write(dialog.FileName);
                        SetStatus("Settings exported!");
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Settings export failed!", "ERROR!",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            }

            // Exit
            else if (sender == mainMenuExit)
            {
                Close();
            }

        }

        #endregion



        #region Statusbar

        //
        // Update statusbar
        //
        private void SetStatus(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                textStatus.Text = text;
            });
            timerStatusbar.Stop();
            timerStatusbar.Start();
        }

        //
        // Update statusbar
        //
        private void SetStatusWarning(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                textStatusWarning.Text = text;
            });
            timerStatusbar.Stop();
            timerStatusbar.Start();
        }


        //
        // Statusbar warning text click
        //
        private void StatusWarning_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Open Task Manager 
            if (textStatusWarning.Text.ToLower().Contains("priority"))
            {
                try { Process.Start("taskmgr.exe"); } catch (Exception) { }
            }
        }


        //
        // Statusbar timer tick
        //
        private void TimerStatusbar_Tick(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                textStatus.Text = "";
                textStatusWarning.Text = "";
            });
            timerStatusbar.Stop();
        }

        #endregion



        #region Tablet Driver

        //
        // Driver event handlers
        //
        //
        // Message
        private void OnDriverMessageReceived(object sender, TabletDriver.DriverEventArgs e)
        {
            //ConsoleAddText(e.Message);
            Application.Current.Dispatcher.Invoke(() =>
            {
                ParseDriverStatus(e.Message);
            });
        }
        // Error
        private void OnDriverErrorReceived(object sender, TabletDriver.DriverEventArgs e)
        {
            SetStatusWarning(e.Message);

        }
        // Started
        private void OnDriverStarted(object sender, EventArgs e)
        {
            driver.SendCommand("HIDList");
            driver.SendCommand("Echo");
            driver.SendCommand("Echo   Driver version: " + Version);
            try { driver.SendCommand("echo   Windows version: " + Environment.OSVersion.VersionString); } catch (Exception) { }
            try
            {
                driver.SendCommand("Echo   Windows product: " +
                    Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString());
                driver.SendCommand("Echo   Windows release: " +
                    Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString());
            }
            catch (Exception)
            {
            }
            driver.SendCommand("Echo");
            driver.SendCommand("CheckTablet");
            ConfigurationManager.Current.SendToDriver(driver);
            driver.SendCommand("Info");
            driver.SendCommand("Start");
            driver.SendCommand("Log Off");
            driver.SendCommand("LogDirect False");
            driver.SendCommand("Echo");
            driver.SendCommand("Echo Driver started!");
        }
        // Stopped
        private void OnDriverStopped(object sender, EventArgs e)
        {
            if (running)
            {
                SetStatus("Driver stopped. Restarting! Check console !!!");
                driver.ConsoleAddText("Driver stopped. Restarting!");

                // Run in the main application thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Title = "TabletDriverGUI";
                    notifyIcon.Text = "";
                    driver.Stop();
                    timerRestart.Start();
                });

            }
        }
        // Driver restart timer
        private void TimerRestart_Tick(object sender, EventArgs e)
        {
            if (running)
            {
                driver.Start(ConfigurationManager.Current.DriverPath, ConfigurationManager.Current.DriverArguments);
            }
            timerRestart.Stop();
        }



        string TabletName;
        //
        // Parse driver status messages
        //
        private void ParseDriverStatus(string line)
        {
            // Status line?
            if (!line.Contains("[STATUS]")) return;

            // Parse status variable and value
            Match match = Regex.Match(line, "^.+\\[STATUS\\] ([^ ]+) (.*?)$");
            if (!match.Success) return;

            string variableName = match.Groups[1].ToString().ToLower();
            string stringValue = match.Groups[2].ToString();

            //
            // Tablet Name
            //
            if (variableName == "tablet")
            {
                TabletName = stringValue;
                UpdateTitle();
                SetStatus("Connected to " + stringValue);
            }

            //
            // Tablet width
            //
            if (variableName == "width")
            {
                if (Utils.ParseNumber(stringValue, out double val))
                {
                    ConfigurationManager.Current.TabletFullArea.Width = val;
                    ConfigurationManager.Current.TabletFullArea.X = val / 2.0;
                    LoadSettingsFromConfiguration();
                    UpdateSettingsToConfiguration();
                    //if (ConfigurationManager.Current.isFirstStart)
                    ConfigurationManager.Current.SendToDriver(driver);
                }
            }

            //
            // Tablet height
            //
            if (variableName == "height")
            {
                if (Utils.ParseNumber(stringValue, out double val))
                {
                    ConfigurationManager.Current.TabletFullArea.Height = val;
                    ConfigurationManager.Current.TabletFullArea.Y = val / 2.0;
                    LoadSettingsFromConfiguration();
                    UpdateSettingsToConfiguration();
                    //if (ConfigurationManager.Current.isFirstStart)
                    ConfigurationManager.Current.SendToDriver(driver);

                }
            }
        }

        private void UpdateTitle()
        {
            string title = "TabletDriverGUI - " + TabletName + " - " + ConfigurationManager.Current.ConfigFilename;
            Title = title;

            // Limit notify icon text length
            if (title.Length > 63)
            {
                notifyIcon.Text = title.Substring(0, 63);
            }
            else
            {
                notifyIcon.Text = title;
            }
        }


        //
        // Start driver
        //
        void Start()
        {
            if (running) return;

            // Try to start the driver
            try
            {
                running = true;

                // Console timer
                timerConsoleUpdate.Start();

                driver.Start(ConfigurationManager.Current.DriverPath, ConfigurationManager.Current.DriverArguments);
                if (!driver.IsRunning)
                {
                    SetStatus("Can't start the driver! Check the console!");
                    driver.ConsoleAddText("ERROR! Can't start the driver!");
                }
            }

            // Start failed
            catch (Exception e)
            {
                SetStatus("Can't start the driver! Check the console!");
                driver.ConsoleAddText("ERROR! Can't start the driver!\n  " + e.Message);
            }
        }


        //
        // Stop driver
        //
        void Stop()
        {
            if (!running) return;
            running = false;
            driver.Stop();
            timerConsoleUpdate.Stop();
        }

        //
        // Restart Driver button click
        //
        private void RestartDriverClick(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                Stop();
            }
            Start();
        }

        #endregion



        #region Console stuff

        //
        // Console buffer to text
        //
        private void ConsoleBufferToText()
        {
            StringBuilder stringBuilder = new StringBuilder();

            // Lock console
            driver.ConsoleLock();

            // Get console status
            if (!driver.HasConsoleUpdated)
            {
                driver.ConsoleUnlock();
                return;
            }
            driver.HasConsoleUpdated = false;

            // Create a string from buffer
            foreach (string line in driver.ConsoleBuffer)
            {
                stringBuilder.Append(line);
                stringBuilder.Append("\r\n");
            }

            // Unlock console
            driver.ConsoleUnlock();

            // Set output
            textConsole.Text = stringBuilder.ToString();

            // Scroll to end
            scrollConsole.ScrollToEnd();

        }

        // Console update timer tick
        private void TimerConsoleUpdate_Tick(object sender, EventArgs e)
        {
            ConsoleBufferToText();
        }


        //
        // Send a command to driver
        //
        private void ConsoleSendCommand(string line)
        {
            if (commandHistory.Last<string>() != line)
            {
                commandHistory.Add(line);
            }
            commandHistoryIndex = commandHistory.Count();
            textConsoleInput.Text = "";
            textConsoleInput.ScrollToEnd();
            try
            {
                driver.SendCommand(line);
            }
            catch (Exception e)
            {
                driver.ConsoleAddText("Error! " + e.Message);
            }
        }

        //
        // Console input key down
        //
        private void TextConsoleInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string line = textConsoleInput.Text;
                ConsoleSendCommand(line);
            }
        }

        //
        // Console input preview key down
        //
        private void TextConsoleInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                commandHistoryIndex--;
                if (commandHistoryIndex < 0) commandHistoryIndex = 0;
                textConsoleInput.Text = commandHistory[commandHistoryIndex];
                textConsoleInput.CaretIndex = textConsoleInput.Text.Length;
            }
            if (e.Key == Key.Down)
            {
                commandHistoryIndex++;
                if (commandHistoryIndex > commandHistory.Count() - 1)
                {
                    commandHistoryIndex = commandHistory.Count();
                    textConsoleInput.Text = "";
                }
                else
                {
                    textConsoleInput.Text = commandHistory[commandHistoryIndex];
                    textConsoleInput.CaretIndex = textConsoleInput.Text.Length;
                }
            }
        }


        //
        // Search rows
        //
        private List<string> SearchRows(List<string> rows, string search, int rowsBefore, int rowsAfter)
        {
            List<string> buffer = new List<string>(rowsBefore);
            List<string> output = new List<string>();
            int rowCounter = 0;

            foreach (string row in rows)
            {
                if (row.Contains(search))
                {
                    if (buffer.Count > 0)
                    {
                        foreach (string bufferLine in buffer)
                        {
                            output.Add(bufferLine);
                        }
                        buffer.Clear();
                    }
                    output.Add(row.Trim());
                    rowCounter = rowsAfter;
                }
                else if (rowCounter > 0)
                {
                    output.Add(row.Trim());
                    rowCounter--;
                }
                else
                {
                    buffer.Add(row);
                    if (buffer.Count > rowsBefore)
                    {
                        buffer.RemoveAt(0);
                    }
                }
            }
            return output;
        }


        //
        // Console output context menu
        //
        private void ConsoleMenuClick(object sender, RoutedEventArgs e)
        {


            // Copy all
            if (sender == menuCopyAll)
            {
                Clipboard.SetDataObject(textConsole.Text);
                SetStatus("Console output copied to clipboard");
            }

            // Copy debug messages
            else if (sender == menuCopyDebug)
            {
                string clipboard = "";
                List<string> rows;
                driver.ConsoleLock();
                rows = SearchRows(driver.ConsoleBuffer, "[DEBUG]", 0, 0);
                driver.ConsoleUnlock();
                foreach (string row in rows)
                    clipboard += row + "\r\n";
                Clipboard.SetDataObject(clipboard);
                SetStatus("Debug message copied to clipboard");
            }

            // Copy error messages
            else if (sender == menuCopyErrors)
            {
                string clipboard = "";
                List<string> rows;
                driver.ConsoleLock();
                rows = SearchRows(driver.ConsoleBuffer, "[ERROR]", 1, 1);
                driver.ConsoleUnlock();
                foreach (string row in rows)
                    clipboard += row + "\r\n";
                Clipboard.SetDataObject(clipboard);
                SetStatus("Error message copied to clipboard");
            }

            // Start debug log
            else if (sender == menuStartDebug)
            {
                string logFilename = "debug_" + DateTime.Now.ToString("yyyy-MM-dd_hh_mm_ss") + ".txt";
                ConsoleSendCommand("log " + logFilename);
                ConsoleSendCommand("debug 1");
            }

            // Stop debug log
            else if (sender == menuStopDebug)
            {
                ConsoleSendCommand("log off");
                ConsoleSendCommand("debug 0");
            }

            // Open latest debug log
            else if (sender == menuOpenDebug)
            {
                try
                {
                    var files = Directory.GetFiles(".", "debug_*.txt").OrderBy(a => File.GetCreationTime(a));
                    if (files.Count() > 0)
                    {
                        string file = files.Last().ToString();
                        Process.Start(file);
                    }
                }
                catch (Exception)
                {
                }
            }

            // Run benchmark
            else if (sender == menuRunBenchmark)
            {
                ConsoleSendCommand("Benchmark");
            }

            // Copy Benchmark
            else if (sender == menuCopyBenchmark)
            {
                string clipboard = "";
                List<string> rows;
                driver.ConsoleLock();
                rows = SearchRows(driver.ConsoleBuffer, " [STATUS] BENCHMARK ", 0, 0);
                driver.ConsoleUnlock();
                foreach (string row in rows)
                {
                    Match m = Regex.Match(row, " BENCHMARK ([0-9\\.]+) ([0-9\\.]+) ([0-9\\.]+) (.*)$");
                    if (m.Success)
                    {
                        string tabletName = m.Groups[4].ToString();
                        string totalPackets = m.Groups[1].ToString();
                        string noiseWidth = m.Groups[2].ToString();
                        string noiseHeight = m.Groups[3].ToString();
                        clipboard =
                            "Tablet(" + tabletName + ") " +
                            "Noise(" + noiseWidth + " mm x " + noiseHeight + " mm) " +
                            "Packets(" + totalPackets + ")\r\n";
                    }
                }

                if (clipboard.Length > 0)
                {
                    Clipboard.SetDataObject(clipboard);
                    SetStatus("Benchmark result copied to clipboard");
                }
            }

            // Open startup log
            else if (sender == menuOpenStartup)
            {
                if (File.Exists("startuplog.txt"))
                {
                    try { Process.Start("startuplog.txt"); } catch (Exception) { }
                }
                else
                {
                    MessageBox.Show(
                        "Startup log not found!\n" +
                        "Make sure that it is possible to create and edit files in the '" + Directory.GetCurrentDirectory() + "' directory.\n",
                        "Error!", MessageBoxButton.OK, MessageBoxImage.Error
                    );
                }
            }

            // Open driver folder
            else if (sender == menuOpenFolder)
            {
                try { Process.Start("."); } catch (Exception) { }
            }

            // Open GitHub page
            else if (sender == menuOpenGithub)
            {
                try { Process.Start("https://github.com/hawku/TabletDriver"); } catch (Exception) { }
            }

            // Open Latest URL
            else if (sender == menuOpenLatestURL)
            {
                Regex regex = new Regex("(http[s]?://.+?)($|\\s)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(textConsole.Text);
                if (matches.Count > 0)
                {
                    string url = matches[matches.Count - 1].Groups[0].ToString().Trim();
                    try { Process.Start(url); } catch (Exception) { }
                }
            }

            // Report a problem
            else if (sender == menuReportProblem)
            {
                try { Process.Start("https://github.com/hawku/TabletDriver/wiki/FAQ"); } catch (Exception) { }
            }


        }



        #endregion



        #region Wacom

        //
        // Wacom Area
        //
        private void ButtonWacomArea_Click(object sender, RoutedEventArgs e)
        {
            WacomArea wacom = new WacomArea();
            wacom.textWacomLeft.Text = Utils.GetNumberString((ConfigurationManager.Current.TabletArea.X - ConfigurationManager.Current.TabletArea.Width / 2) * 100.0, "0");
            wacom.textWacomRight.Text = Utils.GetNumberString((ConfigurationManager.Current.TabletArea.X + ConfigurationManager.Current.TabletArea.Width / 2) * 100.0, "0");

            wacom.textWacomTop.Text = Utils.GetNumberString((ConfigurationManager.Current.TabletArea.Y - ConfigurationManager.Current.TabletArea.Height / 2) * 100.0, "0");
            wacom.textWacomBottom.Text = Utils.GetNumberString((ConfigurationManager.Current.TabletArea.Y + ConfigurationManager.Current.TabletArea.Height / 2) * 100.0, "0");

            wacom.ShowDialog();

            // Set button clicked
            if (wacom.DialogResult == true)
            {
                if (
                    Utils.ParseNumber(wacom.textWacomLeft.Text, out double left) &&
                    Utils.ParseNumber(wacom.textWacomRight.Text, out double right) &&
                    Utils.ParseNumber(wacom.textWacomTop.Text, out double top) &&
                    Utils.ParseNumber(wacom.textWacomBottom.Text, out double bottom)
                )
                {
                    double width, height;
                    width = right - left;
                    height = bottom - top;
                    ConfigurationManager.Current.ForceAspectRatio = false;
                    ConfigurationManager.Current.ForceFullArea = false;
                    ConfigurationManager.Current.TabletArea.X = (left + width / 2.0) / 100.0;
                    ConfigurationManager.Current.TabletArea.Y = (top + height / 2.0) / 100.0;
                    ConfigurationManager.Current.TabletArea.Width = width / 100.0;
                    ConfigurationManager.Current.TabletArea.Height = height / 100.0;
                    LoadSettingsFromConfiguration();
                }
                else
                {
                    MessageBox.Show("Invalid values!", "Wacom area error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            wacom.Close();
        }

        #endregion



        #region WndProc

        //
        // Add WndProc hook
        //
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        //
        // Process Windows messages
        //
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {

            // Show TabletDriverGUI
            if (msg == NativeMethods.WM_SHOWTABLETDRIVERGUI)
            {
                if (WindowState == WindowState.Minimized)
                {
                    NotifyShowWindow(null, null);
                }
                else
                {
                    Activate();
                }
            }

            return IntPtr.Zero;
        }



        #endregion

        private void canvasLoaded(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedItem == tabScreenMapping)
                UpdateCanvasElements();
        }

        private void Window_Activated(object sender, EventArgs e)
        {

        }

        #region profile

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ConfigurationManager.ReloadConfigFiles();
            SyncProfilesList();
        }

        private void SyncProfilesList()
        {
            profilesList.Items.Clear();
            ecList.Items.Clear();
            foreach (Configuration c in ConfigurationManager.Configurations)
            {
                profilesList.Items.Add(c);
            }
            profilesList.Items.Refresh();
        }

        private void profilesList_Loaded(object sender, RoutedEventArgs e)
        {
            SyncProfilesList();
        }

        private void profilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (profilesList.SelectedIndex == -1)
            {
                ecEdit.IsEnabled = ecAdd.IsEnabled = ecRemove.IsEnabled = profileDelete.IsEnabled = profileLoad.IsEnabled = profileNew.IsEnabled = false;
                return;
            }
            ecEdit.IsEnabled = ecAdd.IsEnabled = ecRemove.IsEnabled = profileDelete.IsEnabled = profileLoad.IsEnabled = profileNew.IsEnabled = true;
            Configuration c = ConfigurationManager.Configurations[profilesList.SelectedIndex];
            LoadEC(c);
        }

        private void LoadEC(Configuration c)
        {
            string[] s = c.EffectiveConditions.Split('|');
            ecList.Items.Clear();
            foreach (string st in s)
            {
                if (st.Trim() != "")
                    ecList.Items.Add(new Configuration.EffectiveCondition(st));
            }
        }

        private void editEC_Click(object sender, RoutedEventArgs e)
        {
            if (ecList.SelectedIndex < 0) return;
            new EffectiveConditionEditor(ecList.SelectedItem as Configuration.EffectiveCondition).ShowDialog();
            Configuration c = ConfigurationManager.Configurations[profilesList.SelectedIndex];
            SaveECToConfiguration(c);
            SaveProfile();
            LoadEC(c);
            ConfigurationManager.CheckChanges();
        }

        private void SaveECToConfiguration(Configuration c)
        {
            string b = "";
            foreach (object o in ecList.Items)
            {
                b += (o as Configuration.EffectiveCondition).ToFormattedString() + "|";
            }
            c.EffectiveConditions = b == "" ? "" : b.Substring(0, b.Length - 1);
        }

        private void addEC_Click(object sender, RoutedEventArgs e)
        {
            var v = new Configuration.EffectiveCondition();
            ecList.Items.Add(v);
            ecList.SelectedItem = v;
            editEC_Click(sender, e);
        }

        private void deleteEC_Click(object sender, RoutedEventArgs e)
        {
            if (ecList.SelectedIndex < 0) return;
            ecList.Items.RemoveAt(ecList.SelectedIndex);
            Configuration c = ConfigurationManager.Configurations[profilesList.SelectedIndex];
            SaveECToConfiguration(c);
            SaveProfile();
        }

        private void SaveProfile()
        {
            Configuration c = ConfigurationManager.Configurations[profilesList.SelectedIndex];
            c.Write();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                fdmGroup.IsEnabled = false;
                ConfigurationManager.Method = ConfigurationManager.ListenMethod.Stop;
            }
            else
            {
                fdmGroup.IsEnabled = true;
                fdm_Checked(sender, e);
            }
        }

        private void fdm_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ConfigurationManager.ListenMethod m = ConfigurationManager.ListenMethod.Poll_L;
            if (fdm_Poll.IsChecked == true) m = ConfigurationManager.ListenMethod.Poll_L;
            if (fdm_PollH.IsChecked == true) m = ConfigurationManager.ListenMethod.Poll_M;
            if (fdm_PollL.IsChecked == true) m = ConfigurationManager.ListenMethod.Poll_X;
            if (fdm_WEHook.IsChecked == true) m = ConfigurationManager.ListenMethod.Callback_WinEventHook;
            ConfigurationManager.Method = m;
        }

        private void profileLoad_Click(object sender, RoutedEventArgs e)
        {
            if (profilesList.SelectedIndex < 0) return;
            ConfigurationManager.QuestConfiguration(profilesList.SelectedIndex);
            LoadSettingsFromConfiguration();
            ConfigurationManager.Current.SendToDriver(driver);
        }

        private void profileNew_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = (Environment.CurrentDirectory + "/" + ConfigurationManager.DefaultConfigPath).Replace('/', '\\');
            saveFileDialog.Filter = "eXtensible Markup Language|*.xml";
            if (saveFileDialog.ShowDialog() == true)
            {
                Configuration c = new Configuration();
                System.Drawing.Rectangle r = MainWindow.GetVirtualDesktopSize();
                c.DesktopWidth = r.Width;
                c.DesktopHeight = r.Height;
                c.DesktopSize.Width = r.Width;
                c.DesktopSize.Height = r.Height;
                c.ScreenArea.Width = c.DesktopSize.Width;
                c.ScreenArea.Height = c.DesktopSize.Height;
                c.ScreenArea.X = 0;
                c.ScreenArea.Y = 0;
                string f = saveFileDialog.FileName;
                c.ConfigFilename = "config/" + f.Split('\\').Last();
                c.Write();
                Button_Click(sender, e);
            }
        }

        private void profileDelete_Click(object sender, RoutedEventArgs e)
        {
            File.Delete(profilesList.SelectedItem.ToString());
            Button_Click(sender, e);
        }

        #endregion

    }
}