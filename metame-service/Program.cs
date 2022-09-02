using log4net;
using MetaMe.Core;
using Microsoft.Owin.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace MetaMe.WindowsClient
{
    [Obfuscation(Exclude=true)]
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string url, bool dev)
        {
            SetupLog4Net(dev);

            if (string.IsNullOrEmpty(url))
            {
                log.Error("Url was not provided. Exiting...");
                return;
            }

            string mutexName = dev ? "35492D7A-4C7F-4CF6-B881-129551367E84" : "F066F6FC-2A8F-40CB-B787-9DDEDA565F4A";
            Mutex mutex = new Mutex(true, mutexName);

            //exit if already running
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                log.InfoFormat("Existing process detected. Exiting...");
                return;
            }

            try
            {
                log.InfoFormat("Starting OWIN host: {0}", url);
                // Start OWIN host 
                using (WebApp.Start<Startup>(url: url))
                {
                    log.InfoFormat("Initializing ClientApplication...");
                    ClientApplication.Instance.Initialize(dev);

                    Application.ThreadException += Application_ThreadException;
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new ClientApplicationContext());
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        static void SetupLog4Net(bool devMode)
        {
            var appFolder = PathUtils.GetApplicationFolder(devMode);
            var logFilePath = Path.Combine(appFolder, "logs", "service-win.log");
            Directory.CreateDirectory(Path.GetDirectoryName(appFolder));

            var appender = (log4net.Appender.RollingFileAppender)LogManager.GetRepository().GetAppenders()[1];
            appender.File = logFilePath;
            appender.ActivateOptions();
            log.Info("Logger initialized");
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            log.Error(e.Exception);
            Environment.Exit(1);
        }
    }
}
