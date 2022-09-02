using log4net;
using System;
using System.Diagnostics;
using UIAutomationBlockingCoreLib;

namespace MetaMe.Sensors
{
    class ChromeUrlExtractor : IUrlExtractor
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool CanExtract(IntPtr windowHandle, string processName, string className, string windowText)
        {
            if (processName != Chrome.ProcessName
                || className != "Chrome_WidgetWin_1"
                || String.IsNullOrWhiteSpace(windowText))
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
            IUIAutomationElement addressElement = GetAddressElement(windowHandle); //todo: optimize. taking 10% of app CPU usage
            stopWatch.Stop();
            log.DebugFormat("GetAddressElement duration: {0} ms", stopWatch.ElapsedMilliseconds);

            if (addressElement == null)
            {
                return string.Empty;
            }

            var url = (string)addressElement.GetCachedPropertyValue(UIAProperties.UIA_ValueValuePropertyId); //4% app CPU usage

            //Chrome trims the protocol. Adjust the url to make it fully formed
            url = PostProcessChromeUrl(url);
            return url;
        }

        static IUIAutomationElement GetAddressElement(IntPtr windowHandle)
        {
            try
            {
                CUIAutomation automation = new CUIAutomation();

                IUIAutomationElement root = automation.ElementFromHandle(windowHandle);

                IUIAutomationTreeWalker walker = automation.ControlViewWalker;

                IUIAutomationElement temp;

                temp = root.FindFirst(TreeScope.TreeScope_Children, automation.CreatePropertyCondition(UIAProperties.UIA_NamePropertyId, "Google Chrome"));

                if (temp == null) return null;

                var addressBarCondition = automation.CreateAndCondition(
                    automation.CreatePropertyCondition(UIAProperties.UIA_ControlTypePropertyId, UIAControlTypes.UIA_EditControlTypeId),
                    automation.CreatePropertyCondition(UIAProperties.UIA_AccessKeyPropertyId, "Ctrl+L"));

                IUIAutomationCacheRequest valueCacheRequest = automation.CreateCacheRequest();
                valueCacheRequest.AddProperty(UIAProperties.UIA_ValueValuePropertyId);
                valueCacheRequest.AutomationElementMode = AutomationElementMode.AutomationElementMode_None;

                temp = temp.FindFirstBuildCache(TreeScope.TreeScope_Descendants, addressBarCondition, valueCacheRequest);

                return temp;
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

        static string PostProcessChromeUrl(string webUrl)
        {
            //chrome current behavior strips the http:// when there is no ssl present.
            if (string.IsNullOrEmpty(webUrl))
            {
                return webUrl;
            }

            if (!webUrl.StartsWith("https://")
                && !webUrl.StartsWith("http://"))
            {
                return string.Format("http://{0}", webUrl);
            }

            return webUrl;
        }
    }
}
