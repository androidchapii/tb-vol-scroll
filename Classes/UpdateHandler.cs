﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using tbvolscroll.Forms;

namespace tbvolscroll.Classes
{
    public class UpdateHandler
    {
        private readonly string releasesApiUrl = "https://api.github.com/repos/dvingerh/tb-vol-scroll/releases/latest";
        private readonly string repoUrl = "https://github.com/dvingerh/tb-vol-scroll/releases";
        private readonly string tmpBatPath = Path.Combine(Path.GetTempPath(), "tmp.bat");
        private readonly string tmpExePath = Path.Combine(Path.GetTempPath(), "tmp.exe");
        private readonly string selfExe = Assembly.GetExecutingAssembly().Location;
        private string exeUrl;
        private string curVersion;
        private string latestVersion;
        private readonly CheckForUpdatesForm callback;

        public string RepoUrl => repoUrl;

        public UpdateHandler(CheckForUpdatesForm callback)
        {
            this.callback = callback;
        }


        public async Task CheckForUpdates()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("user-agent", "okhttp");
                    var json = await wc.DownloadStringTaskAsync(releasesApiUrl);
                    var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), new System.Xml.XmlDictionaryReaderQuotas());
                    var jsonRoot = XElement.Load(jsonReader);

                    latestVersion = jsonRoot.XPathSelectElement("//tag_name").Value;
                    curVersion = Application.ProductVersion;
                    if (string.Compare(latestVersion, curVersion) == 1)
                    {
                        XElement assetsEle = jsonRoot.XPathSelectElement("//assets[1]");
                        exeUrl = assetsEle.XPathSelectElement("//browser_download_url").Value;

                        callback.CheckingForUpdatesLabel.Text = $"Newer version available: {latestVersion.ToString().Replace(",", ".")}";
                        callback.CheckingForUpdatesLabel.Image = Properties.Resources.success;
                        callback.DownloadButton.Enabled = true;
                    }
                    else
                    {
                        callback.CheckingForUpdatesLabel.Text = $"No new version available.";
                        callback.CheckingForUpdatesLabel.Image = Properties.Resources.success;
                    }
                }
            }
            catch
            {
                callback.CheckingForUpdatesLabel.Text = "Error checking for updates.";
                callback.CheckingForUpdatesLabel.Image = Properties.Resources.error;
                callback.isUpdating = false;
            }
        }
        public async Task DownloadUpdate()
        {
            using (WebClient wc = new WebClient())
            {
                wc.DownloadFileCompleted += new AsyncCompletedEventHandler(OnUpdateDownloaded);
                wc.Headers.Add("user-agent", "okhttp");
                wc.DownloadProgressChanged += UpdateDownloadProgress;
                await wc.DownloadFileTaskAsync(new Uri(exeUrl), tmpExePath);
            }
        }

        private void UpdateDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            callback.CheckingForUpdatesLabel.Text = $"Retrieving file... {e.ProgressPercentage}%";
        }

        private void OnUpdateDownloaded(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                callback.CheckingForUpdatesLabel.Text = "Error retrieving file.";
                callback.CheckingForUpdatesLabel.Image = Properties.Resources.error;
                callback.isUpdating = false;
                return;
            }
            else
            {
                string updateCmdStr = Properties.Resources.UpdateCmd;
                updateCmdStr = updateCmdStr.Replace("[SELFEXE]", Path.GetFileName(selfExe));
                updateCmdStr = updateCmdStr.Replace("[SELF]", selfExe);
                updateCmdStr = updateCmdStr.Replace("[TEMP]", tmpExePath);
                File.WriteAllText(tmpBatPath, updateCmdStr);

                ProcessStartInfo startInfo = new ProcessStartInfo(tmpBatPath)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(tmpBatPath)
                };
                Globals.MainForm.TrayNotifyIcon.Dispose();
                Process.Start(startInfo);
                
            }
        }

    }
}
