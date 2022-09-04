using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIAutomationBlockingCoreLib;

namespace MetaMe.Sensors
{
    class EdgeUrlExtractor : IUrlExtractor
    {
        public EdgeUrlExtractor()
        {
        }

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool CanExtract(IntPtr windowHandle, string processName, string className, string windowText)
        {
            if (processName != Edge.ProcessName
                || className != "Chrome_WidgetWin_1")
            {
                return false;
            }

            //filter out alert panes E.g. Translate this page?, popups, etc
            if (!WindowsApiUtils.IsProcessRootWindowHandle(windowHandle))
            {
                log.DebugFormat("Non process root window handle detected: {0}", windowHandle);
                return false;
            }

            return true;
        }

        public string ExtractUrl(IntPtr windowHandle)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            IUIAutomationElement addressElement = GetAddressElement(windowHandle);
            stopWatch.Stop();

            if (addressElement == null)
            {
                log.DebugFormat("AddressElement not found");
                //check if edge app
                var settingsButtonElement = GetSettingsWebAppMenuButton(windowHandle);
                if (settingsButtonElement == null)
                {
                    return string.Empty;
                }
                var name = (string)settingsButtonElement.GetCachedPropertyValue(UIAProperties.UIA_NamePropertyId);
                EdgeWebAppInfo[] webApps = GetEdgeWebAppInfo();

                var matchingApp = (from app in webApps
                                   where app.Name == name
                                   select app).FirstOrDefault();

                return matchingApp != null ? matchingApp.WebUrl : string.Empty;
            }
            else
            {
                log.DebugFormat("GetAddressElement duration: {0} ms", stopWatch.ElapsedMilliseconds);
                //check if its an Edge app
                var url = (string)addressElement.GetCachedPropertyValue(UIAProperties.UIA_ValueValuePropertyId);
                return url;
            }
        }

        static IUIAutomationElement GetSettingsWebAppMenuButton(IntPtr windowHandle)
        {
            try
            {
                CUIAutomation automation = new CUIAutomation();

                IUIAutomationElement root = automation.ElementFromHandle(windowHandle);

                IUIAutomationCacheRequest nameRequest = automation.CreateCacheRequest();
                nameRequest.AddProperty(UIAProperties.UIA_NamePropertyId);
                nameRequest.AutomationElementMode = AutomationElementMode.AutomationElementMode_None;

                var webAppMenuButtonCondition = automation.CreateAndCondition(
                    automation.CreatePropertyCondition(UIAProperties.UIA_ClassNamePropertyId, "WebAppMenuButton"),
                    automation.CreatePropertyCondition(UIAProperties.UIA_AutomationIdPropertyId, "view_1014"));

                IUIAutomationElement result = root.FindFirstBuildCache(TreeScope.TreeScope_Descendants,
                                                webAppMenuButtonCondition,
                                                nameRequest);

                return result;
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                var errorContext = ExtractErrorContext(windowHandle);
                log.Debug(errorContext);
                return null;
            }

        }
        static IUIAutomationElement GetAddressElement(IntPtr windowHandle)
        {
            try
            {
                CUIAutomation automation = new CUIAutomation();

                IUIAutomationElement root = automation.ElementFromHandle(windowHandle);

                IUIAutomationCacheRequest valueCacheRequest = automation.CreateCacheRequest();
                valueCacheRequest.AddProperty(UIAProperties.UIA_ValueValuePropertyId);
                valueCacheRequest.AutomationElementMode = AutomationElementMode.AutomationElementMode_None;

                var addressBarCondition = automation.CreateAndCondition(
                    automation.CreatePropertyCondition(UIAProperties.UIA_ControlTypePropertyId, UIAControlTypes.UIA_EditControlTypeId),
                    automation.CreatePropertyCondition(UIAProperties.UIA_AcceleratorKeyPropertyId, "Ctrl+L"));

                IUIAutomationElement result = root.FindFirstBuildCache(TreeScope.TreeScope_Descendants,
                                                addressBarCondition,
                                                valueCacheRequest);

                return result;
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                var errorContext = ExtractErrorContext(windowHandle);
                log.Debug(errorContext);
                return null;
            }
        }
        static string ExtractErrorContext(IntPtr windowHandle)
        {
            try
            {
                CUIAutomation automation = new CUIAutomation();
                IUIAutomationElement root = automation.ElementFromHandle(windowHandle);
                var className = root.CurrentClassName;
                var name = root.CurrentName;
                var localizedControlType = root.CurrentLocalizedControlType;

                var values = new string[] { localizedControlType, className, name };
                return string.Join(", ", values);
            }
            catch (Exception ex)
            {
                return string.Format("Unable to ExtractErrorContext: {0}", ex);
            }
        }

        static string[] GetExtensionFolders()
        {
            var extensionsPath = GetEdgeExtensionsPath();
            string[] directories = Directory.GetDirectories(extensionsPath);
            //remove temp

            return directories;
        }

        static string[] GetLatestWebAppManifests()
        {
            var extensionFolders = GetExtensionFolders().ToList();
            var manifests = extensionFolders.ConvertAll(GetLatestWebAppManifestFile);

            manifests = (from item in manifests
                         where !String.IsNullOrEmpty(item)
                         select item).ToList();

            return manifests.ToArray();

        }

        static EdgeWebAppInfo[] GetEdgeWebAppInfo()
        {
            var manifests = GetLatestWebAppManifests();

            List<EdgeWebAppInfo> webApps = new List<EdgeWebAppInfo>();
            foreach (var item in manifests)
            {
                var webApp = ReadManifest(item);
                if (webApp != null)
                {
                    webApps.Add(webApp);
                }
            }
            return webApps.ToArray();
        }

        static EdgeWebAppInfo ReadManifest(string manifestPath)
        {
            //JsonConvert
            var jsonContent = File.ReadAllText(manifestPath);

            var jsonObject = JObject.Parse(jsonContent);

            var name = jsonObject.SelectToken("name");
            var webUrl = jsonObject.SelectToken("app.launch.web_url");

            if (name == null
                || webUrl == null)
            {
                return null;
            }

            return new EdgeWebAppInfo
            {
                Name = name.Value<string>(),
                WebUrl = webUrl.Value<string>()
            };
        }

        static string GetLatestWebAppManifestFile(string extensionPath)
        {
            string[] manifestFiles = Directory.GetFiles(extensionPath, "manifest.json", SearchOption.AllDirectories);

            if (manifestFiles.Length == 0)
            {
                return null;
            }

            return manifestFiles.Last();
        }

        static string GetEdgeExtensionsPath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var extensionsPath = Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Extensions");
            return extensionsPath;
        }

    }

}
