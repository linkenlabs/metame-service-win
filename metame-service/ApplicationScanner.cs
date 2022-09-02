using IWshRuntimeLibrary;
using log4net;
using Microsoft.Ccr.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;


namespace MetaMe.WindowsClient
{
    //periodically scans computer for existing / newly installed apps
    class ApplicationScanner
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly Port<EmptyValue> _port = new Port<EmptyValue>();
        private ImmutableArray<ApplicationScanInfo> _scannedApplications = ImmutableArray.Create<ApplicationScanInfo>();

        public ApplicationScanner(DispatcherQueue queue)
        {
            Arbiter.Activate(queue,
               Arbiter.Interleave(
                   new TeardownReceiverGroup(),
                   new ExclusiveReceiverGroup(
                       Arbiter.Receive(true, _port, HandleScan)),
                   new ConcurrentReceiverGroup()
            ));
        }

        public ImmutableArray<ApplicationScanInfo> GetScannedApplications()
        {
            return _scannedApplications;
        }

        public void Scan()
        {
            _port.Post(EmptyValue.SharedInstance);
        }

        void HandleScan(EmptyValue request)
        {
            //scan all shortcut files
            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            string commonStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);

            string[] startMenuLinks = TryGetFiles(startMenuPath, "*.lnk", SearchOption.AllDirectories);
            string[] commonStartMenuLinks = TryGetFiles(commonStartMenu, "*.lnk", SearchOption.AllDirectories);

            List<string> links = new List<string>();
            links.AddRange(startMenuLinks);
            links.AddRange(commonStartMenuLinks);

            var targets = links.ConvertAll(link => GetShortcutTargetPath(link));
            var validTargets = targets.Distinct().ToList();
            var scannedApps = validTargets.ConvertAll(ConvertToApplicationScanInfo);

            //only include isValid
            _scannedApplications = (from item in scannedApps
                                    where item.IsValidApp
                                    select item).ToImmutableArray();

            //now extract all icons
            foreach (var item in _scannedApplications)
            {
                try
                {
                    string iconSavePath = IconHelpers.GetIconPath(item.FileDescription);
                    if (System.IO.File.Exists(iconSavePath))
                    {
                        continue;
                    }
                    IconHelpers.ExtractProcessIcon(item.ApplicationPath, iconSavePath);
                }
                catch (Exception ex)
                {
                    string errorMessage = String.Format("Error extracting icon: {0}", item.FileDescription);
                    log.Warn("Error extracting icon:", ex);
                }
            }
        }

        //non exception version
        static string[] TryGetFiles(string path, string pattern, SearchOption searchOption)
        {
            try
            {
                string[] files = Directory.GetFiles(path, pattern, searchOption);
                return files;
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format("Error getting files from: {0}", path);
                log.Warn(errorMessage, ex);
                return new string[] { };
            }
        }


        static ApplicationScanInfo ConvertToApplicationScanInfo(string applicationPath)
        {
            string[] invalidExtensions = new string[] { ".html", ".htm", ".chm", ".ico", ".txt", ".url", ".cmd", ".pdf", "", ".msc" };

            if (!System.IO.File.Exists(applicationPath)
                || invalidExtensions.Contains(Path.GetExtension(applicationPath)))
            {
                return new ApplicationScanInfo
                {
                    ApplicationPath = applicationPath,
                    IsValidApp = false
                };
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(applicationPath);
            var processName = Path.GetFileNameWithoutExtension(applicationPath);

            string windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string installerPath = Path.Combine(windowsFolder, "Installer");
            string system32Folder = Environment.GetFolderPath(Environment.SpecialFolder.System);

            //ignore setup related, or in installer directory
            if (processName.ToLower().Contains("install")
                || processName.ToLower().Contains("setup")
                || applicationPath.ToLower().StartsWith(installerPath.ToLower())
                || applicationPath.ToLower().StartsWith(system32Folder.ToLower()))
            {
                return new ApplicationScanInfo
                {
                    ApplicationPath = applicationPath,
                    IsValidApp = false
                };
            }

            var productName = !string.IsNullOrEmpty(versionInfo.FileDescription) ? versionInfo.FileDescription : versionInfo.ProductName;

            if (!string.IsNullOrEmpty(productName))
            {
                if (productName.ToLower().Contains("install") || productName.ToLower().Contains("setup"))
                {
                    return new ApplicationScanInfo
                    {
                        ApplicationPath = applicationPath,
                        IsValidApp = false
                    };
                }

                char[] invalidChars = Path.GetInvalidFileNameChars();
                var invalidCharCount = (from c in invalidChars
                                        where productName.Contains(c)
                                        select c).Count();
                if (invalidCharCount > 0)
                {
                    return new ApplicationScanInfo
                    {
                        ApplicationPath = applicationPath,
                        IsValidApp = false
                    };
                }
            }

            return new ApplicationScanInfo
            {
                ApplicationPath = applicationPath,
                FileDescription = String.IsNullOrEmpty(productName) ? processName : productName,
                IsValidApp = true
            };
        }

        static string GetShortcutTargetPath(string shortcutPath)
        {
            IWshShell shell = new WshShell();
            if (shell.CreateShortcut(shortcutPath) is IWshShortcut lnk)
            {
                return lnk.TargetPath;
            }

            return String.Empty;
        }

    }
}
