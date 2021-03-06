﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Toast;

namespace ImgurSniper.UI {
    public class InstallerHelper {

        private string _path;
        private string _downloads;
        private Toasty _error, _success;
        private MainWindow invoker;

        private string _docPath {
            get {
                string value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ImgurSniper");
                if(!Directory.Exists(value))
                    Directory.CreateDirectory(value);
                return value;
            }
        }

        public InstallerHelper(string path, Toasty errorToast, Toasty successToast, MainWindow invoker) {
            _path = path;
            try {
                SHGetKnownFolderPath(KnownFolder.Downloads, 0, IntPtr.Zero, out _downloads);
            } catch { }

            this.invoker = invoker;
            _error = errorToast;
            _success = successToast;
        }

        public void Install(object senderButton) {
            Download(senderButton);
        }

        public void AddToContextMenu() {
            string addPath = Path.Combine(_path, "AddToContextMenu.exe");

            if(!File.Exists(addPath))
                return;

            Process add = new Process();
            add.StartInfo = new ProcessStartInfo {
                FileName = addPath
            };
            add.Start();
            add.WaitForExit();
        }

        /// <summary>
        /// Download the ImgurSniper Archive from github
        /// </summary>
        /// <param name="path">The path to save the zip to</param>
        private void Download(object sender) {
            string file = Path.Combine(_path, "ImgurSniperSetup.zip");
            using(WebClient client = new WebClient()) {
                client.DownloadFileCompleted += DownloadCompleted;

                client.DownloadFileCompleted += delegate {
                    (sender as Button).Content = "Done!";
                    (sender as Button).IsEnabled = false;
                    (sender as Button).Tag = new object();
                    _success.Show("Successfully Installed ImgurSniper!", TimeSpan.FromSeconds(3));
                };

                _success.Show("Downloading from github.com/mrousavy/ImgurSniper...", TimeSpan.FromSeconds(2));

                client.DownloadFileAsync(new Uri(@"https://github.com/mrousavy/ImgurSniper/blob/master/Downloads/ImgurSniperSetup.zip?raw=true"),
                    file);
            }
        }

        private void DownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e) {
            string file = Path.Combine(_path, "ImgurSniperSetup.zip");
            string extractTo = Path.Combine(_path, "ImgurSniperInstaller");

            if(!File.Exists(file)) {
                _error.Show("Could not download ZIP Archive from github.com!",
                    TimeSpan.FromSeconds(5));
                Process.Start("https://mrousavy.github.io/ImgurSniper");
            } else {
                Extract(file, extractTo);
                Process.Start(Path.Combine(extractTo, "ImgurSniperSetup.msi"));
                Application.Current.Shutdown();
            }
        }

        private void KillTasks() {
            List<Process> processes = new List<Process>(Process.GetProcesses().Where(p => p.ProcessName.Contains("ImgurSniper")));
            foreach(Process p in processes) {
                if(p.ProcessName != Process.GetCurrentProcess().ProcessName)
                    p.Kill();
            }
        }

        /// <summary>
        /// Extract the downloaded ImgurSniper Messenger Archive
        /// </summary>
        /// <param name="file">The path of the Archive</param>
        /// <param name="path">The path of the Folder</param>
        private void Extract(string file, string path) {
            using(ZipArchive archive = new ZipArchive(new FileStream(file, FileMode.Open))) {
                archive.ExtractToDirectory(path);
            }
        }

        public async void Uninstall() {
            await _success.ShowAsync("Removing ImgurSniper and Cleaning up junk...",
                TimeSpan.FromSeconds(2.5));

            string path = Path.Combine(Path.GetTempPath(), "Cleanup.exe");
            File.WriteAllBytes(path, Properties.Resources.Cleanup);
            Process.Start(path);

            Application.Current.Shutdown();
        }

        //private void CreateUninstaller() {
        //    try {
        //        using(RegistryKey parent = Registry.LocalMachine.OpenSubKey(
        //                     @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true)) {
        //            if(parent == null) {
        //                return;
        //            }
        //            try {
        //                RegistryKey key = null;

        //                try {
        //                    string appName = "ImgurSniper";

        //                    key = parent.CreateSubKey(appName);

        //                    Assembly asm = GetType().Assembly;
        //                    Version v = asm.GetName().Version;
        //                    string exe = "\"" + asm.CodeBase.Substring(8).Replace("/", "\\\\") + "\"";

        //                    key.SetValue("DisplayName", "ImgurSniper");
        //                    key.SetValue("ApplicationVersion", v.ToString());
        //                    key.SetValue("Publisher", "mrousavy");
        //                    key.SetValue("DisplayIcon", exe);
        //                    key.SetValue("DisplayVersion", v.ToString(2));
        //                    key.SetValue("URLInfoAbout", "http://www.github.com/mrousavy/ImgurSniper");
        //                    key.SetValue("Contact", "");
        //                    key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
        //                    key.SetValue("UninstallString", exe + " /uninstall");
        //                } finally {
        //                    key?.Close();
        //                }
        //            } catch  {
        //                _error.Show("Could not create Uninstaller for ImgurSniper! You will have to remove the Files manually (from " + _path + ")",
        //                    TimeSpan.FromSeconds(5));
        //            }
        //        }
        //    } catch  { }
        //}

        public void Autostart(bool? boxIsChecked) {
            try {
                string path = Path.Combine(_path, "ImgurSniper.exe -autostart");

                using(RegistryKey baseKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run")) {
                    if(boxIsChecked == true) {
                        baseKey.SetValue("ImgurSniper", path);
                    } else {
                        baseKey.DeleteValue("ImgurSniper");
                    }
                }
            } catch {
                //Not authorized
            }
        }

        public static class KnownFolder {
            public static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);
    }
}
