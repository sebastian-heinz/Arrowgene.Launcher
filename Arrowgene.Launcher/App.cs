﻿namespace Arrowgene.Launcher
{
    using Arrowgene.Launcher.Core;
    using Arrowgene.Launcher.Translation;
    using Arrowgene.Launcher.Windows;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using System.Windows;
    using System.Windows.Threading;

    public class App : Application
    {
        private const string SETTINGS_FILE_NAME = "arrowgene_launcher.settings";
        private const string LOG_FILE_NAME = "arrowgene_launcher.log";

        public static int VERSION = 1;

        public static Logger Logger => _logger;
        public static LauncherWindow Window => _launcherWindow;

        private static Logger _logger;
        private static LauncherWindow _launcherWindow;

        [STAThread]
        public static void Main()
        {
            _logger = new Logger(GetLogPath());
            _logger.Log(string.Format("Startup - Version: {0}", VERSION), "App::Main");
            _launcherWindow = new LauncherWindow();

            string configPath = GetConfigPath();
            FileInfo configFile = CreateFileInfo(configPath);
            if (configFile == null)
            {
                DisplayError(string.Format("The path: {0} is invalid.", configPath), "LauncherController::Window_Loaded");
                Environment.Exit((int)ExitCode.CONFIG_PATH);
            }
            Configuration config = new Configuration(configPath);
            if (!config.Load())
            {
                Environment.Exit((int)ExitCode.LOAD_SETTINGS);
            }
            Translator.Instance.ChangeLanguage(config.SelectedLanguage);
            Translator.Instance.OnChange = (LanguageType languageType) =>
            {
                config.SelectedLanguage = languageType;
                DisplayMessage(Translator.Instance.Translate("please_restart"), Translator.Instance.Translate("notice"), App.Window);
            };
            LauncherController launcherController = new LauncherController(_launcherWindow, config);
            App launcher = new App();
            launcher.Run(_launcherWindow);
        }

        public static string GetApplicationDir()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public static string GetApplicationFileName()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetFileName(path);
        }

        public static IPAddress IPAddressLookup(string hostname, AddressFamily addressFamily)
        {
            IPAddress ipAdress = null;
            try
            {
                IPAddress[] ipAddresses = Dns.GetHostAddresses(hostname);
                foreach (IPAddress ipAddr in ipAddresses)
                {
                    if (ipAddr.AddressFamily == addressFamily)
                    {
                        ipAdress = ipAddr;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "LaunchController::IpAddressLookup");
            }
            return ipAdress;
        }

        public static string CreateMD5(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }

        public static FileInfo CreateFileInfo(string path)
        {
            FileInfo info;
            try
            {
                info = new FileInfo(path);
            }
            catch (Exception)
            {
                info = null;
            }
            return info;
        }

        public static string GetConfigPath()
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(folderPath, SETTINGS_FILE_NAME);
        }

        public static string GetLogPath()
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(folderPath, LOG_FILE_NAME);
        }

        public static byte[] ReadFile(string source)
        {
            if (!File.Exists(source))
            {
                DisplayError(string.Format("'{0}' does not exist or is not a file", source), "App::ReadFile");
                return null;
            }

            return File.ReadAllBytes(source);
        }

        public static void WriteFile(byte[] content, string destination)
        {
            if (content != null)
            {
                File.WriteAllBytes(destination, content);
            }
            else
            {
                DisplayError(string.Format("Content of '{0}' is null", destination), "App::WriteFile");
            }
        }

        public static void DisplayError(string error, string origin)
        {
            DisplayError(error, origin, _launcherWindow);
        }

        public static void DisplayError(string error, string origin, Window owner)
        {
            _logger.Log(error, origin);
            Dispatch(new Action(() =>
            {
                new DialogBox(owner, error, "Error").ShowDialog();
            }));
        }

        public static void DisplayMessage(string message, string title)
        {
            Dispatch(new Action(() =>
            {
                new DialogBox(Window, message, title).ShowDialog();
            }));
        }

        public static void DisplayMessage(string message, string title, Window owner)
        {
            Dispatch(new Action(() =>
            {
                new DialogBox(owner, message, title).ShowDialog();
            }));
        }

        public static void Dispatch(Action action)
        {
            if (_launcherWindow.Dispatcher.CheckAccess())
            {
                action.Invoke();
            }
            else
            {
                _launcherWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
            }
        }
    }
}
