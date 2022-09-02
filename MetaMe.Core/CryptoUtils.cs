using Bitcoin.BIP39;
using Sodium;
using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace MetaMe.Core
{
    class CryptoUtils
    {
        //returns 32 byte symmetric key from mneumonic and passphrase
        public static byte[] GenerateSymmetricKey(string mneumonic, string passphrase)
        {
            BIP39 bip = new BIP39(mneumonic, passphrase, BIP39.Language.English);

            //Libsodium only taks 32 byte for keypair generation
            //use PBKDF2 to get 64byte seed to 32 bytes
            PBKDF2 func = new PBKDF2(bip.SeedBytes, bip.SeedBytes, 4096, "HMACSHA512");

            byte[] privateKey = func.GetBytes(32);
            return privateKey;
        }

        //returns 32 byte symmetric key from passphrase
        public static byte[] GenerateSymmetricKey(string passphrase)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(passphrase);

            //passphrase secret may be too short, so do a SHA256, then use the hash as a salt
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(passwordBytes);

            PBKDF2 func = new PBKDF2(passwordBytes, hash, 4096, "HMACSHA512");

            byte[] privateKey = func.GetBytes(32);
            return privateKey;
        }

        public static CryptoKeyPair GenerateEd25519KeyPair(string mneumonic, string passphrase)
        {
            BIP39 bip = new BIP39(mneumonic, passphrase, BIP39.Language.English);

            //Libsodium only taks 32 byte for keypair generation
            //use PBKDF2 to get 64byte seed to 32 bytes
            PBKDF2 func = new PBKDF2(bip.SeedBytes, bip.SeedBytes, 4096, "HMACSHA512");

            byte[] privateKey = func.GetBytes(32);

            var keyPair = PublicKeyAuth.GenerateKeyPair(privateKey); //64 byte private key | 32 byte public key

            return new CryptoKeyPair
            {
                PublicKey = keyPair.PublicKey,
                PrivateKey = keyPair.PrivateKey
            };
        }

        public static bool TryGetPrivateKey(out byte[] privateKey, bool devMode)
        {
            if (!Passphrase.TryGetPassphrase(out string passphrase, devMode)
                || !Mneumonic.TryGetMneumonic(passphrase, out string mneumonic, devMode))
            {
                privateKey = null;
                return false;
            }
            try
            {
                privateKey = GenerateSymmetricKey(mneumonic, passphrase);
                return true;
            }
            catch (System.Exception)
            {
                privateKey = null;
                return false;
            }
        }

        public static byte[] GetPrivateKey(bool devMode)
        {
            string passphrase = Passphrase.GetPassphrase(devMode);
            if (Mneumonic.TryGetMneumonic(passphrase, out string mneumonic, devMode))
            {
                byte[] privateKey = GenerateSymmetricKey(mneumonic, passphrase);
                return privateKey;
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
    }
}
