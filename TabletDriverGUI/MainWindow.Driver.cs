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

        //
        // Start the driver
        //
        void StartDriver()
        {
            if (running) return;

            // Try to start the driver
            try
            {
                running = true;

                // Console timer
                timerConsoleUpdate.Start();

                driver.Start(config.DriverPath, config.DriverArguments);
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
        // Stop the driver
        //
        void StopDriver()
        {
            if (!running) return;
            running = false;
            driver.Stop();
            timerConsoleUpdate.Stop();
        }


        //
        // Driver event handlers
        //
        private void OnDriverMessageReceived(object sender, TabletDriver.DriverEventArgs e)
        {
            //ConsoleAddText(e.Message);
        }


        //
        // Driver error received
        //
        private void OnDriverErrorReceived(object sender, TabletDriver.DriverEventArgs e)
        {
            SetStatusWarning(e.Message);
        }


        //
        // Driver status message received
        //
        private void OnDriverStatusReceived(object sender, TabletDriver.DriverEventArgs e)
        {
            string variableName = e.Message;
            string parameters = e.Parameters;
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProcessStatusMessage(variableName, parameters);
            });
        }
        // Process driver status message
        private void ProcessStatusMessage(string variableName, string parameters)
        {

            //
            // Tablet Name
            //
            if (variableName == "tablet")
            {
                TabletName = parameters;
                UpdateTitle();
                SetStatus("Connected to " + parameters);
            }

            //
            // Tablet width
            //
            if (variableName == "width")
            {
                if (Utils.ParseNumber(parameters, out double val))
                {
                    config.TabletFullArea.Width = val;
                    config.TabletFullArea.X = val / 2.0;
                    LoadSettingsFromConfiguration();
                    UpdateSettingsToConfiguration();
                    //if (config.isFirstStart)
                    config.SendToDriver(driver);
                }
            }

            //
            // Tablet height
            //
            if (variableName == "height")
            {
                if (Utils.ParseNumber(parameters, out double val))
                {
                    config.TabletFullArea.Height = val;
                    config.TabletFullArea.Y = val / 2.0;
                    LoadSettingsFromConfiguration();
                    UpdateSettingsToConfiguration();
                    //if (isFirstStart)
                    config.SendToDriver(driver);

                }
            }


            //
            // Tablet measurement to tablet area
            //
            if (variableName == "measurement" && isEnabledMeasurementToArea)
            {
                string[] stringValues = parameters.Split(' ');
                int valueCount = stringValues.Count();
                if (valueCount >= 4)
                {
                    double minimumX = 10000;
                    double minimumY = 10000;
                    double maximumX = -10000;
                    double maximumY = -10000;
                    for (int i = 0; i < valueCount; i += 2)
                    {
                        if (
                            Utils.ParseNumber(stringValues[i], out double x)
                            &&
                            Utils.ParseNumber(stringValues[i + 1], out double y)
                        )
                        {
                            // Find limits
                            if (x > maximumX) maximumX = x;
                            if (x < minimumX) minimumX = x;
                            if (y > maximumY) maximumY = y;
                            if (y < minimumY) minimumY = y;
                        }
                    }

                    double areaWidth = maximumX - minimumX;
                    double areaHeight = maximumY - minimumY;
                    double centerX = minimumX + areaWidth / 2.0;
                    double centerY = minimumY + areaHeight / 2.0;

                    config.TabletArea.Width = areaWidth;
                    config.TabletArea.Height = areaHeight;
                    config.TabletArea.X = centerX;
                    config.TabletArea.Y = centerY;
                    LoadSettingsFromConfiguration();
                    UpdateSettingsToConfiguration();


                }
                isEnabledMeasurementToArea = false;
                buttonDrawArea.IsEnabled = true;
                SetStatus("");
            }



        }


        //
        // Driver Started
        //
        private void OnDriverStarted(object sender, EventArgs e)
        {
            // Debugging commands
            if(config.DebuggingEnabled)
            {
                driver.SendCommand("HIDList");
            }

            driver.SendCommand("GetCommands");
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
            config.SendToDriver(driver);
            driver.SendCommand("Info");
            driver.SendCommand("Start");
            driver.SendCommand("Log Off");
            driver.SendCommand("LogDirect False");
            driver.SendCommand("Echo");
            driver.SendCommand("Echo Driver started!");
            driver.SendCommand("Echo");
        }


        //
        // Driver Stopped
        //
        private void OnDriverStopped(object sender, EventArgs e)
        {
            if (running)
            {

                // Automatic restart?
                if (config.AutomaticRestart)
                {
                    SetStatus("Driver stopped. Restarting! Check console !!!");
                    driver.ConsoleAddText("Driver stopped. Restarting!");

                    // Run in the main application thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        driver.Stop();
                        timerRestart.Start();
                    });

                }
                else
                {
                    SetStatus("Driver stopped!");
                    driver.ConsoleAddText("Driver stopped!");
                }

                // Run in the main application thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Title = "TabletDriverGUI";
                    notifyIcon.Text = "No tablet found";
                });

            }
        }


        //
        // Driver restart timer tick
        //
        private void TimerRestart_Tick(object sender, EventArgs e)
        {
            if (running)
            {
                driver.Start(config.DriverPath, config.DriverArguments);
            }
            timerRestart.Stop();
        }


        //
        // Restart Driver button click
        //
        private void RestartDriverClick(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                StopDriver();
            }
            StartDriver();
        }


    }
}
