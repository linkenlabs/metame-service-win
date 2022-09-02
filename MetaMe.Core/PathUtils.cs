using System;
using System.IO;

namespace MetaMe.Core
{
    class PathUtils
    {
        public static string GetDeviceIdPath(bool devMode)
        {
            string outputFolder = GetApplicationFolder(devMode);
            string dataFilePath = Path.Combine(outputFolder, "deviceId.txt");
            return dataFilePath;
        }

        public static string GetSQLiteDatabasePath(bool devMode)
        {
            string outputFolder = GetApplicationDeviceFolder(devMode);
            string dataFilePath = Path.Combine(outputFolder, "database.sqlite3");
            return dataFilePath;
        }
        public static string GetGoalsPath(bool devMode)
        {
            string outputFolder = GetApplicationDeviceFolder(devMode);
            string dataFilePath = Path.Combine(outputFolder, "goals.json");
            return dataFilePath;
        }

        public static string GetSettingsPath(bool devMode)
        {
            string outputFolder = GetApplicationDeviceFolder(devMode);
            string dataFilePath = Path.Combine(outputFolder, "settings.json");
            return dataFilePath;
        }

        public static string GetGroupsPath(bool devMode)
        {
            string outputFolder = GetApplicationDeviceFolder(devMode);
            string dataFilePath = Path.Combine(outputFolder, "groups.json");
            return dataFilePath;
        }
        public static string GetHiddenAppsPath(bool devMode)
        {
            string outputFolder = GetApplicationDeviceFolder(devMode);
            string dataFilePath = Path.Combine(outputFolder, "hidden.json");
            return dataFilePath;
        }
        public static string GetMneumonicKeyPath(bool devMode)
        {
            string appFolder = GetApplicationFolder(devMode);
            string mneumonicPath = Path.Combine(appFolder, "mneumonic.key");
            return mneumonicPath;
        }

        public static string GetApplicationFolder(bool devMode)
        {
            string folderName = devMode ? "MetaMe_dev" : "MetaMe";
            string applicationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), folderName);
            return applicationFolder;
        }

        public static string GetEnvPath()
        {
            string envPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".env");
            return envPath;
        }

        public static string GetPassphraseKeyPath(bool devMode)
        {
            string appFolder = GetApplicationFolder(devMode);
            string mneumonicPath = Path.Combine(appFolder, "passphrase.key");
            return mneumonicPath;
        }

        public static string GetApplicationDeviceFolder(bool devMode)
        {
            Guid deviceId = Device.GetDeviceGuid(devMode);

            string appFolder = GetApplicationFolder(devMode);
            string deviceFolder = Path.Combine(appFolder, deviceId.ToString());

            Directory.CreateDirectory(deviceFolder);
            return deviceFolder;
        }

        [Obsolete]
        public static string GetDeviceDataPath<T>(bool devMode)
        {
            string outputFolder = GetApplicationDeviceFolder(devMode);
            string fileName = GetDataFileName<T>();
            string dataFilePath = Path.Combine(outputFolder, fileName);
            return dataFilePath;
        }

        [Obsolete]
        public static string GetDeviceDataPath<T>(int backupIndex, bool devMode)
        {
            string deviceFolder = GetApplicationDeviceFolder(devMode);
            string fileName = GetDataFileName<T>(backupIndex);
            string dataFilePath = Path.Combine(deviceFolder, fileName);
            return dataFilePath;
        }

        [Obsolete]
        public static string GetDataFileName<T>(int backupIndex)
        {
            string typeName = typeof(T).Name;
            string dataFilePath = string.Format("{0}_{1}.dat", typeName, backupIndex);
            return dataFilePath;
        }

        static string GetDataFileName<T>()
        {
            string typeName = typeof(T).Name;
            return string.Format("{0}.dat", typeName);
        }


    }
}
