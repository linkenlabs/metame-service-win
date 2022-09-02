using log4net;
using System;
using System.Diagnostics;
using UIAutomationBlockingCoreLib;

namespace MetaMe.Sensors
{
    class FirefoxUrlExtractor : IUrlExtractor
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool CanExtract(IntPtr windowHandle, string processName, string className, string windowText)
        {
            if (processName != Firefox.ProcessName
                || className != "MozillaWindowClass")
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
            stopWatch.Start();
            log.DebugFormat("GetAddressElement duration: {0} ms", stopWatch.ElapsedMilliseconds);

            if (addressElement == null)
            {
                return String.Empty;
            }


            var url = (string)addressElement.GetCachedPropertyValue(UIAProperties.UIA_ValueValuePropertyId);
            url = PostProcessFirefoxUrl(url);
            return url;
        }

        private static IUIAutomationElement GetAddressElement(IntPtr windowHandle)
        {
            try
            {
                CUIAutomation automation = new CUIAutomation();

                IUIAutomationElement root = automation.ElementFromHandle(windowHandle);

                IUIAutomationElement temp = root;

                //although not needed, increases performance significantly
                temp = temp.FindFirst(TreeScope.TreeScope_Children, automation.CreatePropertyCondition(UIAProperties.UIA_AutomationIdPropertyId, "nav-bar"));
                if (temp == null) return null;

                var addressBarCondition = automation.CreateAndCondition(
                    automation.CreatePropertyCondition(UIAProperties.UIA_ControlTypePropertyId, UIAControlTypes.UIA_EditControlTypeId),
                    automation.CreatePropertyCondition(UIAProperties.UIA_AutomationIdPropertyId, "urlbar-input"));

                //cached request
                IUIAutomationCacheRequest valueCacheRequest = automation.CreateCacheRequest();
                valueCacheRequest.AddProperty(UIAProperties.UIA_ValueValuePropertyId);
                valueCacheRequest.AutomationElementMode = AutomationElementMode.AutomationElementMode_None;

                temp = temp.FindFirstBuildCache(TreeScope.TreeScope_Descendants, addressBarCondition, valueCacheRequest);

                return temp;
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                return null;
            }
        }

        private static string PostProcessFirefoxUrl(string webUrl)
        {
            //chrome current behavior strips the http:// when there is no ssl present.
            if (String.IsNullOrEmpty(webUrl))
            {
                return webUrl;
            }

            if (!webUrl.StartsWith("https://")
                && !webUrl.StartsWith("http://"))
            {
                return String.Format("http://{0}", webUrl);
            }

            return webUrl;
        }
    }
}
