using Sodium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace MetaMe.Core
{
    class Passphrase
    {
        const string NOT_IMPORTANT_LOL = "MetaMe";

        //public static bool Exists(bool devMode)
        //{
        //    string passphrase = PathUtils.GetPassphraseKeyPath(devMode);
        //    return File.Exists(passphrase);
        //}

        //public static void CreatePassphraseKeyFile(string passphrase, bool devMode)
        //{
        //    byte[] symmetricKey = CryptoUtils.GenerateSymmetricKey(NOT_IMPORTANT_LOL);

        //    byte[] passphraseBytes = Encoding.UTF8.GetBytes(passphrase);

        //    byte[] nonce = SecretBox.GenerateNonce();

        //    //Symmetric encryption test
        //    byte[] cipherText1 = SecretBox.Create(passphraseBytes, nonce, symmetricKey);

        //    //store the cipherText and the nonce together into mneumonic.key
        //    IEnumerable<byte> combinedArray = nonce.Concat(cipherText1);

        //    string destPath = PathUtils.GetPassphraseKeyPath(devMode);

        //    File.Delete(destPath);
        //    File.WriteAllBytes(destPath, combinedArray.ToArray());

        //}

        public static void DeletePassphraseKeyFile(bool devMode)
        {
            string destPath = PathUtils.GetPassphraseKeyPath(devMode);
            File.Delete(destPath);
        }

        public static bool TryGetPassphrase(out string passphrase, bool devMode)
        {
            try
            {
                passphrase = GetPassphrase(devMode);
                return true;
            }
            catch (Exception)
            {
                passphrase = null;
                return false;
            }
        }

        public static string GetPassphrase(bool devMode)
        {
            string destPath = PathUtils.GetPassphraseKeyPath(devMode);

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

            byte[] privateKey = CryptoUtils.GenerateSymmetricKey(NOT_IMPORTANT_LOL);

            byte[] passphraseBytes = SecretBox.Open(cipherText, nonce, privateKey);

            string passphrase = Encoding.UTF8.GetString(passphraseBytes);

            return passphrase;
        }
    }
}
