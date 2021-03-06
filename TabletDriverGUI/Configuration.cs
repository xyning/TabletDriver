﻿using System;
using System.Linq;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TabletDriverGUI
{
    [XmlRootAttribute("Configuration", IsNullable = true)]
    public class Configuration
    {
        public class EffectiveCondition
        {
            public string K = "", V = "";
            public override string ToString()
            {
                return K + "=\"" + V + "\"";
            }
            public EffectiveCondition(string cfg)
            {
                string[] s = cfg.Split('>');
                K = s[0]; V = s[1];
            }
            public EffectiveCondition()
            {
            }
            public string ToFormattedString()
            {
                return K + ">" + V;
            }
        }
        public enum OutputModes
        {
            Absolute = 0,
            Relative = 1,
            Digitizer = 2,
            SendInput = 3
        }
        public enum ExtraEvents
        {
            None = 0,
            MouseWheel = 1,
            DisableTablet = 2,
            Keyboard = 3
        }
        public enum Buttons
        {
            Tip = 0,
            Bottom = 1,
            Top = 2
        }

        public int ConfigVersion = 1;
        public string EffectiveConditions = "";

        public Area TabletArea = new Area(80, 45, 40, 22.5);
        public Area TabletFullArea = new Area(100, 50, 50, 25);
        public bool ForceAspectRatio = true;
        public double Rotation = 0;
        public bool Invert;
        public bool ForceFullArea = true;
        public OutputModes OutputMode = OutputModes.Absolute;
        public Area ScreenArea = new Area(0, 0, 0, 0);
        public int DesktopWidth, DesktopHeight;

        public double SmoothingLatency = 0;
        public int SmoothingInterval = 4;
        public bool SmoothingEnabled = false;
        // Noise filter
        public int NoiseFilterBuffer = 10;
        public double NoiseFilterThreshold = 0.5;
        public bool NoiseFilterEnabled = false;

        // Anti-smoothing filter
        public double AntiSmoothingShape = 0.5;
        public double AntiSmoothingCompensation = 4.0;
        public bool AntiSmoothingIgnoreWhenDragging;
        public bool AntiSmoothingEnabled = false;

        public Area DesktopSize = new Area(0, 0, 0, 0);
        public bool AutomaticDesktopSize = true;

        [XmlArray("ButtonMap")]
        [XmlArrayItem("Button")]
        public int[] ButtonMap = new int[] { 1, 7, 2 };
        public bool DisableButtons = false;

        [XmlArray("ExtraButtonEvents")]
        [XmlArrayItem("Event")]
        public ExtraEvents[] ExtraButtonEvents = new ExtraEvents[3] { ExtraEvents.None, ExtraEvents.MouseWheel, ExtraEvents.None };

        [XmlArray("ExtraButtonEventTags")]
        [XmlArrayItem("Tag")]
        public string[] ExtraButtonEventTag = new string[3] { "", "20", "" };

        [XmlArray("CommandsAfter")]
        [XmlArrayItem("Command")]
        public string[] CommandsAfter = new string[] { "" };

        [XmlArray("CommandsBefore")]
        [XmlArrayItem("Command")]
        public string[] CommandsBefore = new string[] { "" };

        public int WindowWidth = 800;
        public int WindowHeight = 710;

        public bool AutomaticRestart = true;
        public bool RunAtStartup = false;

        public string DriverPath = "bin/TabletDriverService.exe";
        public string DriverArguments = "config/init.cfg";

        public bool DeveloperMode = false;
        public static string DefaultConfigFilename = "config/config.xml";
        public string ConfigFilename = DefaultConfigFilename;
        public bool DebuggingEnabled = false;

        public override string ToString()
        {
            return ConfigFilename;
        }

        //
        // Send settings to the driver
        //
        public void SendToDriver(TabletDriver driver)
        {
            if (!driver.IsRunning) return;

            // Commands before settings
            if (CommandsBefore.Length > 0)
            {
                foreach (string command in CommandsBefore)
                {
                    string tmp = command.Trim();
                    if (tmp.Length > 0)
                    {
                        driver.SendCommand(tmp);
                    }
                }
            }


            // Desktop size
            driver.SendCommand("DesktopSize " + DesktopWidth + " " + DesktopHeight);


            // Screen area
            driver.SendCommand("ScreenArea " +
                Utils.GetNumberString(ScreenArea.Width) + " " + Utils.GetNumberString(ScreenArea.Height) + " " +
                Utils.GetNumberString(ScreenArea.X) + " " + Utils.GetNumberString(ScreenArea.Y)
            );


            //
            // Tablet area
            //
            // Inverted
            if (Invert)
            {
                driver.SendCommand("TabletArea " +
                    Utils.GetNumberString(TabletArea.Width) + " " +
                    Utils.GetNumberString(TabletArea.Height) + " " +
                    Utils.GetNumberString(TabletFullArea.Width - TabletArea.X) + " " +
                    Utils.GetNumberString(TabletFullArea.Height - TabletArea.Y)
                );
                driver.SendCommand("Rotate " + Utils.GetNumberString(TabletArea.Rotation + 180));
            }
            // Normal
            else
            {
                driver.SendCommand("TabletArea " +
                    Utils.GetNumberString(TabletArea.Width) + " " +
                    Utils.GetNumberString(TabletArea.Height) + " " +
                    Utils.GetNumberString(TabletArea.X) + " " +
                    Utils.GetNumberString(TabletArea.Y)
                );
                driver.SendCommand("Rotate " + Utils.GetNumberString(TabletArea.Rotation));
            }


            // Output Mode
            switch (OutputMode)
            {
                case Configuration.OutputModes.Absolute:
                    driver.SendCommand("Mode Absolute");
                    break;
                case Configuration.OutputModes.Relative:
                    driver.SendCommand("Mode Relative");
                    driver.SendCommand("Sensitivity " + Utils.GetNumberString(ScreenArea.Width / TabletArea.Width));
                    break;
                case Configuration.OutputModes.Digitizer:
                    driver.SendCommand("Mode Digitizer");
                    break;
                case Configuration.OutputModes.SendInput:
                    driver.SendCommand("Mode sendinputabs");
                    break;
            }


            // Button map
            if (DisableButtons)
            {
                driver.SendCommand("ButtonMap 0 0 0");
            }
            else
            {
                driver.SendCommand("ButtonMap " + String.Join(" ", ButtonMap));
            }

            // Smoothing filter
            if (SmoothingEnabled)
            {
                driver.SendCommand("FilterTimerInterval " + Utils.GetNumberString(SmoothingInterval));
                driver.SendCommand("Smoothing " + Utils.GetNumberString(SmoothingLatency));
            }
            else
            {
                driver.SendCommand("Smoothing 0");
            }

            // Noise filter
            if (NoiseFilterEnabled)
            {
                driver.SendCommand("Noise " + Utils.GetNumberString(NoiseFilterBuffer) + " " + Utils.GetNumberString(NoiseFilterThreshold));
            }
            else
            {
                driver.SendCommand("Noise 0");
            }

            // Anti-smoothing filter
            if (AntiSmoothingEnabled)
            {
                driver.SendCommand("AntiSmoothing " + Utils.GetNumberString(AntiSmoothingShape) + " " +
                    Utils.GetNumberString(AntiSmoothingCompensation) + " " +
                    (AntiSmoothingIgnoreWhenDragging ? "true" : "false"));
            }
            else
            {
                driver.SendCommand("AntiSmoothing 0");
            }

            // Extra Buttons
            driver.SendCommand("Extra 0 " + ExtraButtonEvents[0].ToString() + " " + ExtraButtonEventTag[0]);
            driver.SendCommand("Extra 1 " + ExtraButtonEvents[1].ToString() + " " + ExtraButtonEventTag[1]);
            driver.SendCommand("Extra 2 " + ExtraButtonEvents[2].ToString() + " " + ExtraButtonEventTag[2]);

            // Commands after settings
            if (CommandsAfter.Length > 0)
            {
                foreach (string command in CommandsAfter)
                {
                    string tmp = command.Trim();
                    if (tmp.Length > 0)
                    {
                        driver.SendCommand(tmp);
                    }
                }
            }

        }

        public void Write()
        {
            Write(ConfigFilename);
        }

        public void Write(string filename)
        {
            var fileWriter = new StreamWriter(filename);

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings() { Indent = true };
            XmlWriter writer = XmlWriter.Create(fileWriter, xmlWriterSettings);
            try
            {
                serializer.Serialize(writer, this);
            }
            catch (Exception)
            {
                fileWriter.Close();
                throw;
            }
            fileWriter.Close();
        }

        public static Configuration CreateFromFile()
        {
            return CreateFromFile(DefaultConfigFilename);
        }

        public static Configuration CreateFromFile(string filename)
        {
            Configuration config = null;
            var serializer = new XmlSerializer(typeof(Configuration));
            var settings = new XmlWriterSettings() { Indent = true };
            var reader = XmlReader.Create(filename);

            try
            {
                config = (Configuration)serializer.Deserialize(reader);
            }
            catch (Exception)
            {
                reader.Close();
                throw;
            }
            reader.Close();
            return config;
        }
    }


}
