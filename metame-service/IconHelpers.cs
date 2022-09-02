using System;
using System.Drawing;
using System.IO;

namespace MetaMe.WindowsClient
{
    class IconHelpers
    {

        public static void ExtractProcessIcon(string processPath, string iconSavePath)
        {
            Icon icon = Icon.ExtractAssociatedIcon(processPath);
            using (Image image = icon.ToBitmap())
            {
                image.Save(iconSavePath);
            }
        }

        public static string GetBase64Icon(string appName)
        {
            if (String.IsNullOrEmpty(appName))
            {
                return String.Empty;
            }

            var iconPath = GetIconPath(appName);
            if (!File.Exists(iconPath))
            {
                return String.Empty;
            }

            Byte[] bytes = File.ReadAllBytes(iconPath);
            string base64string = Convert.ToBase64String(bytes);
            return base64string;
        }


        public static string GetIconPath(string appName)
        {
            byte[] hashBytes = GetMd5Hash(appName);
            string filename = Convert.ToBase64String(hashBytes).Replace("/", "_");

            string iconDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"MetaMe\Icons\");
            Directory.CreateDirectory(iconDirectory);

            string iconPath = Path.Combine(iconDirectory, filename) + ".ico";
            return iconPath;

        }

        static byte[] GetMd5Hash(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return hashBytes;
            }
        }
    }
}
