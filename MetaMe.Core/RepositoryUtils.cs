using Newtonsoft.Json;
using Sodium;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MetaMe.Core
{
    class RepositoryUtils
    {
        public static T ReadFromFile<T>(string dataPath, bool devMode)
        {
            if (!File.Exists(dataPath))
            {
                return default;
            }

            byte[] combined = File.ReadAllBytes(dataPath);
            byte[] privateKey = CryptoUtils.GetPrivateKey(devMode);
            T data = Unpack<T>(combined, privateKey);
            return data;
        }

        public static T Unpack<T>(byte[] encrypted, byte[] privateKey)
        {
            //decrypt
            int nonceLength = 24;
            //nonce is 24 in length
            byte[] nonce = encrypted.Take(nonceLength).ToArray();
            byte[] cipherText = encrypted.Skip(nonceLength).ToArray();
            byte[] decrypted = SecretBox.Open(cipherText, nonce, privateKey);

            //unzip
            var decompressed = Unzip(decrypted);
            string dataJson = Encoding.UTF8.GetString(decompressed);

            T item = JsonConvert.DeserializeObject<T>(dataJson);
            return item;
        }

        //encrypts and zips
        public static byte[] Pack<T>(T item, byte[] privateKey)
        {
            var dataJson = JsonConvert.SerializeObject(item);
            var compressed = Zip(Encoding.UTF8.GetBytes(dataJson));
            byte[] encrypted = Encrypt(compressed, privateKey);
            return encrypted;
        }

        public static void FileWriteSafe(string path, byte[] contents)
        {
            var tempPath = Path.ChangeExtension(path, ".temp");
            File.WriteAllBytes(tempPath, contents);
            File.Delete(path);
            File.Move(tempPath, path);
        }

        public static ImmutableArray<T> GetArrayDataFromRollingBackupFiles<T>(bool devMode)
        {
            int backupIndex = 1;
            var buffer = ImmutableArray<T>.Empty;

            while (File.Exists(PathUtils.GetDeviceDataPath<T>(backupIndex, devMode)))
            {
                //load data
                var dataArray = GetArrayDataFromFile<T>(backupIndex, devMode);
                buffer = buffer.AddRange(dataArray);
                backupIndex++;
            }

            return buffer;
        }

        public static ImmutableArray<T> GetArrayDataFromFile<T>(int backupIndex, bool devMode)
        {
            string dataPath = PathUtils.GetDeviceDataPath<T>(backupIndex, devMode);
            if (!File.Exists(dataPath))
            {
                return ImmutableArray.Create<T>();
            }

            try
            {
                byte[] privateKey = CryptoUtils.GetPrivateKey(devMode);
                var dataArray = DecryptFromFile<ImmutableArray<T>>(dataPath, privateKey);
                return dataArray;
            }
            catch (Exception ex)
            {
                var errorMessage = String.Format("Decryption failed: {0}", dataPath);
                throw new Exception(errorMessage, ex);
            }
        }

        public static T DecryptFromFile<T>(string path, byte[] privateKey)
        {
            byte[] combined = File.ReadAllBytes(path);
            var dataArray = Unpack<T>(combined, privateKey);
            return dataArray;
        }

        public static byte[] Zip(byte[] data)
        {
            using (var msi = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }
                return mso.ToArray();
            }
        }

        public static byte[] Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }
                return mso.ToArray();
            }
        }

        public static byte[] Encrypt(byte[] data, byte[] privateKey)
        {
            byte[] nonce = SecretBox.GenerateNonce();
            byte[] cipherText1 = SecretBox.Create(data, nonce, privateKey);
            IEnumerable<byte> combinedArray = nonce.Concat(cipherText1);
            return combinedArray.ToArray();
        }

        //From https://stackoverflow.com/a/7343623
        static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
    }
}
