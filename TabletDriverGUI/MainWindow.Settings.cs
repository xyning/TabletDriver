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
    public partial class MainWindow : Window
    {


        #region Driver configuration stuff

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
                    case Configuration.OutputModes.SendInput:
                        radioModeAbsoluteRaw.IsChecked = true;
                        break;
                }
                if (App.exp_no_vmulti) radioModeAbsoluteRaw.IsChecked = true;

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
                comboBoxSmoothingRate.SelectedIndex = ConfigurationManager.Current.SmoothingInterval - 1;
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
                // Noise filter
                //
                checkBoxNoiseFilter.IsChecked = ConfigurationManager.Current.NoiseFilterEnabled;
                textNoiseBuffer.Text = Utils.GetNumberString(ConfigurationManager.Current.NoiseFilterBuffer);
                textNoiseThreshold.Text = Utils.GetNumberString(ConfigurationManager.Current.NoiseFilterThreshold);
                if (ConfigurationManager.Current.NoiseFilterEnabled)
                {
                    textNoiseBuffer.IsEnabled = true;
                    textNoiseThreshold.IsEnabled = true;
                }
                else
                {
                    textNoiseBuffer.IsEnabled = false;
                    textNoiseThreshold.IsEnabled = false;
                }


                //
                // Anti-smoothing filter
                //
                checkBoxAntiSmoothing.IsChecked = ConfigurationManager.Current.AntiSmoothingEnabled;
                textAntiSmoothingShape.Text = Utils.GetNumberString(ConfigurationManager.Current.AntiSmoothingShape, "0.00");
                textAntiSmoothingCompensation.Text = Utils.GetNumberString(ConfigurationManager.Current.AntiSmoothingCompensation, "0.00");
                checkBoxAntiSmoothingIgnoreWhenDragging.IsChecked = ConfigurationManager.Current.AntiSmoothingIgnoreWhenDragging;
                if (ConfigurationManager.Current.AntiSmoothingEnabled)
                {
                    textAntiSmoothingShape.IsEnabled = true;
                    textAntiSmoothingCompensation.IsEnabled = true;
                    checkBoxAntiSmoothingIgnoreWhenDragging.IsEnabled = true;
                }
                else
                {
                    textAntiSmoothingShape.IsEnabled = false;
                    textAntiSmoothingCompensation.IsEnabled = false;
                    checkBoxAntiSmoothingIgnoreWhenDragging.IsEnabled = false;
                }
                //
                // Run at startup
                //
                checkBoxRunAtStartup.IsChecked = ConfigurationManager.Current.RunAtStartup;


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
            if (radioModeAbsoluteRaw.IsChecked == true) ConfigurationManager.Current.OutputMode = Configuration.OutputModes.SendInput;


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
            ConfigurationManager.Current.SmoothingInterval = comboBoxSmoothingRate.SelectedIndex + 1;
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

            // Noise filter
            ConfigurationManager.Current.NoiseFilterEnabled = (bool)checkBoxNoiseFilter.IsChecked;
            if (Utils.ParseNumber(textNoiseBuffer.Text, out val))
                ConfigurationManager.Current.NoiseFilterBuffer = (int)val;
            if (Utils.ParseNumber(textNoiseThreshold.Text, out val))
                ConfigurationManager.Current.NoiseFilterThreshold = val;
            if (ConfigurationManager.Current.NoiseFilterEnabled)
            {
                textNoiseBuffer.IsEnabled = true;
                textNoiseThreshold.IsEnabled = true;
            }
            else
            {
                textNoiseBuffer.IsEnabled = false;
                textNoiseThreshold.IsEnabled = false;
            }

            // Anti-smoothing filter
            ConfigurationManager.Current.AntiSmoothingEnabled = (bool)checkBoxAntiSmoothing.IsChecked;
            if (Utils.ParseNumber(textAntiSmoothingShape.Text, out val))
                ConfigurationManager.Current.AntiSmoothingShape = val;
            if (Utils.ParseNumber(textAntiSmoothingCompensation.Text, out val))
                ConfigurationManager.Current.AntiSmoothingCompensation = val;
            ConfigurationManager.Current.AntiSmoothingIgnoreWhenDragging = (bool)checkBoxAntiSmoothingIgnoreWhenDragging.IsChecked;
            if (ConfigurationManager.Current.AntiSmoothingEnabled)
            {
                textAntiSmoothingShape.IsEnabled = true;
                textAntiSmoothingCompensation.IsEnabled = true;
                checkBoxAntiSmoothingIgnoreWhenDragging.IsEnabled = true;
            }
            else
            {
                textAntiSmoothingShape.IsEnabled = false;
                textAntiSmoothingCompensation.IsEnabled = false;
                checkBoxAntiSmoothingIgnoreWhenDragging.IsEnabled = false;
            }



            //
            // Run at startup
            //
            oldValue = ConfigurationManager.Current.RunAtStartup;
            ConfigurationManager.Current.RunAtStartup = (bool)checkBoxRunAtStartup.IsChecked;
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

        #endregion



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
            if (VersionHelper.IsWindows8OrGreater() || config.OutputMode == Configuration.OutputModes.Digitizer)
                screens = System.Windows.Forms.Screen.AllScreens;
            else
                screens = new System.Windows.Forms.Screen[] { System.Windows.Forms.Screen.PrimaryScreen };
            return screens;
        }



        #region Canvas stuff

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

        #endregion



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
    }
}
