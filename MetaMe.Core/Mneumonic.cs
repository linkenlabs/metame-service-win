using MetaMe.Core;
using Sodium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MetaMe.Core
{
    class Mneumonic
    {
        public static bool TryGetMneumonic(string passphrase, out string mneumonic, bool devMode)
        {
            try
            {
                mneumonic = GetMneumonic(passphrase, devMode);
                return true;
            }
            catch (Exception e) when (e is CryptographicException || e is FileNotFoundException)
            {
                mneumonic = null;
                return false;
            }
        }

        private static string GetMneumonic(string passphrase, bool devMode)
        {
            string destPath = PathUtils.GetMneumonicKeyPath(devMode);

            if (!File.Exists(destPath))
            {
                throw new FileNotFoundException();
            }

            //read the file
            byte[] combined = File.ReadAllBytes(destPath);

            int nonceLength = 24;
            //nonce is 24 in length
            byte[] nonce = combined.Take(nonceLength).ToArray();

            byte[] cipherText = combined.Skip(nonceLength).ToArray();

            byte[] privateKey = CryptoUtils.GenerateSymmetricKey(passphrase);

            byte[] mneumonicBytes = SecretBox.Open(cipherText, nonce, privateKey);

            string mneumonic = Encoding.UTF8.GetString(mneumonicBytes);

            return mneumonic;
        }
    }
}
