using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MetaMe.DataRecoverer
{
    class Program
    {
        private const int ROLLING_RECORD_LIMIT = 10000;

        static void Main(string[] args)
        {
            string outputFolder = PathUtils.GetApplicationDeviceFolder();

            ProcessData<AppActivityInfo>();
            ProcessData<IdleActivityInfo>();
        }

        static void ProcessData<T>()
        {
            var scanResults = ScanRollingBackupFiles<T>();

            if (scanResults.Length == 0)
            {
                return;
            }

            Console.WriteLine("Repairing data...");

            //only repair if there are issues
            Console.WriteLine("Backing up data...");
            BackupRollingBackupFiles<T>();

            var recoveredData = GetRecoverableRollingBackupFileData<T>();

            DeleteRollingBackupFileData<T>();
            SaveDataRolling<T>(recoveredData);
        }

        static void SaveDataRolling<T>(ImmutableArray<T> data)
        {
            var buffer = data;
            int backupIndex = 1;

            while (buffer.Length > 0)
            {
                var takeCount = Math.Min(10000, buffer.Length);
                var items = buffer.Take(takeCount).ToImmutableArray();

                //now save it
                string dataPath = PathUtils.GetDeviceDataPath<T>(backupIndex);

                byte[] privateKey = CryptoUtils.GetPrivateKey();
                byte[] encrypted = RepositoryUtils.Pack(items, privateKey);

                RepositoryUtils.FileWriteSafe(dataPath, encrypted);

                buffer = buffer.RemoveRange(0, takeCount);
                backupIndex++;
            }
        }


        static int GetBackupIndex(int length)
        {
            return (length / ROLLING_RECORD_LIMIT) + 1;
        }

        static void DeleteRollingBackupFileData<T>()
        {
            int backupIndex = 1;
            while (File.Exists(PathUtils.GetDeviceDataPath<T>(backupIndex)))
            {
                string path = PathUtils.GetDeviceDataPath<T>(backupIndex);
                File.Delete(path);
                backupIndex++;
            }
        }

        static void BackupRollingBackupFiles<T>()
        {
            Console.WriteLine("Backing up {0}...", typeof(T).Name);
            string backupFolder = GetBackupFolder();
            int backupIndex = 1;

            //List<string> corruptedList = new List<string>();
            while (File.Exists(PathUtils.GetDeviceDataPath<T>(backupIndex)))
            {
                string path = PathUtils.GetDeviceDataPath<T>(backupIndex);

                //backup path
                string backupPath = Path.Combine(backupFolder, Path.GetFileName(path));
                File.Copy(path, backupPath, true);
                backupIndex++;
            }
        }

        static string GetBackupFileName<T>(int index)
        {
            string backupFolder = GetBackupFolder();

            return Path.Combine(backupFolder, PathUtils.GetDataFileName<T>(index));
        }

        static ImmutableArray<T> GetRecoverableRollingBackupFileData<T>()
        {
            int backupIndex = 1;
            ImmutableArray<T> buffer = ImmutableArray.Create<T>();

            while (File.Exists(GetBackupFileName<T>(backupIndex)))
            {
                //load data
                try
                {
                    byte[] privateKey = CryptoUtils.GetPrivateKey();
                    string backupFileName = GetBackupFileName<T>(backupIndex);
                    var dataArray = RepositoryUtils.DecryptFromFile<ImmutableArray<T>>(backupFileName, privateKey);
                    buffer = buffer.AddRange(dataArray);
                }
                catch (Exception)
                {
                }
                backupIndex++;
            }

            return buffer;
        }


        static string GetBackupFolder()
        {
            string executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string backupFolder = Path.Combine(executingDirectory, "001 Raw Data");
            Directory.CreateDirectory(backupFolder);
            return backupFolder;
        }

        static ImmutableArray<string> ScanRollingBackupFiles<T>()
        {
            Console.WriteLine("Scanning {0}...", typeof(T).Name);

            var corruptedActivityFiles = GetCorruptedRollingBackupFiles<T>();

            if (corruptedActivityFiles.Length == 0)
            {
                Console.WriteLine("No issues detected");
            }

            foreach (var item in corruptedActivityFiles)
            {
                Console.WriteLine("Corrupted file detected: {0}", item);
            }

            return corruptedActivityFiles;
        }

        static ImmutableArray<string> GetCorruptedRollingBackupFiles<T>()
        {
            int backupIndex = 1;
            List<string> corruptedList = new List<string>();

            while (File.Exists(PathUtils.GetDeviceDataPath<T>(backupIndex)))
            {
                //load data
                try
                {
                    var dataArray = RepositoryUtils.GetArrayDataFromFile<T>(backupIndex);
                }
                catch (Exception)
                {
                    corruptedList.Add(PathUtils.GetDeviceDataPath<T>(backupIndex));
                }
                backupIndex++;
            }

            return corruptedList.ToImmutableArray();
        }
    }
}
