using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace MetaMe.Sensors
{
    class ProcessInfo
    {
        public string Name { get; set; }
        public uint Id { get; set; }
        public string Path { get; set; }

        //yeap not the most elegant I know
        static readonly object _lockObject = new object();
        private static Dictionary<string, string> _cache = new Dictionary<string, string>();

        public static ProcessInfo GetProcessInfo(IntPtr windowHandle)
        {
            NativeMethods.GetWindowThreadProcessId(windowHandle, out uint pid);

            try
            {
                using (Process p = Process.GetProcessById((int)pid)) //todo: optimize this is taking along time
                {
                    string name = p.ProcessName;
                    string path = GetProcessPathCached(p);
                    return new ProcessInfo
                    {
                        Id = pid,
                        Name = name,
                        Path = path
                    };
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is InvalidOperationException)
                {
                    return null;
                }
                throw;
            }
        }

        static string GetProcessPathCached(Process process)
        {
            //processIds get recycled, must tag processName in addition to Id
            string key = string.Format("{0}+{1}", process.ProcessName, process.Id);

            if (!_cache.ContainsKey(key))
            {
                string processPath = GetProcessPath(process);
                lock (_lockObject)
                {
                    if (!_cache.ContainsKey(key))
                    {
                        _cache.Add(key, processPath);
                    }
                }
            }
            return _cache[key];
        }

        private static string GetProcessPath(Process process)
        {
            var task = GetProcessPathAsync(process);
            task.Wait();
            string path = task.Result;
            return path;
        }

        //GetProcessPath must not run in STA thread.
        //https://stackoverflow.com/questions/12162797/wmi-managementobjectsearcher-hanging-on-query
        private static System.Threading.Tasks.Task<string> GetProcessPathAsync(Process process)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";

                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                    foreach (ManagementObject item in searcher.Get())
                    {
                        object id = item["ProcessID"];
                        object path = item["ExecutablePath"];

                        if (path != null && id.ToString() == process.Id.ToString())
                        {
                            return path.ToString();
                        }
                    }
                }
                catch { }
                return string.Empty;
            });

        }
    }
}
