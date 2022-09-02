using log4net;
using System;
using System.Diagnostics;
using UIAutomationBlockingCoreLib;

namespace MetaMe.Sensors
{
    class BraveUrlExtractor : IUrlExtractor
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string ProcessName = "Brave";

        public bool CanExtract(IntPtr windowHandle, string processName, string className, string windowText)
        {
            if (!processName.Equals(ProcessName, StringComparison.CurrentCultureIgnoreCase)
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
            stopWatch.Start();
            log.DebugFormat("GetAddressElement duration: {0} ms", stopWatch.ElapsedMilliseconds);

            if (addressElement == null)
            {
                return String.Empty;
            }

            var url = (string)addressElement.GetCachedPropertyValue(UIAProperties.UIA_ValueValuePropertyId);
            url = PostProcessBraveUrl(url);
            return url;
        }

        static string PostProcessBraveUrl(string webUrl)
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


        private static IUIAutomationElement GetAddressElement(IntPtr windowHandle)
        {
            try
            {
                CUIAutomation automation = new CUIAutomation();

                IUIAutomationElement root = automation.ElementFromHandle(windowHandle);

                IUIAutomationElement temp;
                IUIAutomationTreeWalker walker = automation.ControlViewWalker;

                var addressBarCondition = automation.CreateAndCondition(
                  automation.CreatePropertyCondition(UIAProperties.UIA_ControlTypePropertyId, UIAControlTypes.UIA_EditControlTypeId),
                  automation.CreatePropertyCondition(UIAProperties.UIA_AccessKeyPropertyId, "Ctrl+L"));

                IUIAutomationCacheRequest valueCacheRequest = automation.CreateCacheRequest();
                valueCacheRequest.AddProperty(UIAProperties.UIA_ValueValuePropertyId);
                valueCacheRequest.AutomationElementMode = AutomationElementMode.AutomationElementMode_None;

                temp = root.FindFirstBuildCache(TreeScope.TreeScope_Descendants, addressBarCondition, valueCacheRequest);

                return temp;

            }
            catch (Exception ex)
            {
                log.Warn(ex);
                return null;
            }
        }
    }
}
