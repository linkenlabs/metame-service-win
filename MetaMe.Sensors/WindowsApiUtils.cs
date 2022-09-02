using System;
using System.Text;

namespace MetaMe.Sensors
{
    class WindowsApiUtils
    {
        public static bool IsProcessRootWindowHandle(IntPtr windowHandle) {
            //filter out alert panes E.g. Translate this page?, popups, etc
            var parentHandle = NativeMethods.GetParent(windowHandle);
            return parentHandle == IntPtr.Zero;
        }
        public static DateTime GetUtcDateTimeFromMsEventTime(uint dwmsEventTime)
        {
            //can't use Environment.TickCount since it resets
            //https://msdn.microsoft.com/en-us/library/system.environment.tickcount.aspx
            ulong tickCount = NativeMethods.GetTickCount64();
            double elapsed = Convert.ToDouble(dwmsEventTime) - Convert.ToDouble(tickCount);
            return DateTime.UtcNow.AddMilliseconds(elapsed);
        }

        public static string GetClassNameOfWindow(IntPtr hwnd)
        {
            StringBuilder lpClassName = new StringBuilder(100);

            try
            {
                int result = NativeMethods.GetClassName(hwnd, lpClassName, 100);
                return result == 0 ? String.Empty : lpClassName.ToString();
            }
            catch (Exception)
            {
                return String.Empty;
            }
            finally
            {
                lpClassName = null;
            }
        }

        public static string GetWindowText(IntPtr hwnd)
        {
            string result = "";
            StringBuilder windowTextBuilder;
            try
            {
                int max_length = NativeMethods.GetWindowTextLength(hwnd);
                windowTextBuilder = new StringBuilder("", max_length + 5);
                NativeMethods.GetWindowText(hwnd, windowTextBuilder, max_length + 2);

                if (!string.IsNullOrEmpty(windowTextBuilder.ToString()) && !string.IsNullOrWhiteSpace(windowTextBuilder.ToString()))
                    result = windowTextBuilder.ToString();
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            finally
            {
                windowTextBuilder = null;
            }
            return result;
        }

    }
}
