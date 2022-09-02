//using Bitcoin.BIP39;
//using MetaMe.Core;
//using Newtonsoft.Json;
//using Sodium;
//using System;
//using System.Collections.Immutable;
//using System.IO;
//using System.Security.Cryptography;
//using System.Text;

//namespace MetaMe.WindowsClient
//{
//    class Test
//    {
//        public static void Test02()
//        {
//            string outputFolder = PathUtils.GetApplicationDeviceFolder();

//            string hourLevelFactsDataPath = PathUtils.GetDeviceDataPath<HourLevelFact>();

//            byte[] privateKey = CryptoUtils.GetPrivateKey();
//            ImmutableArray<HourLevelFact> hourLevelFacts = RepositoryUtils.DecryptFromFile<ImmutableArray<HourLevelFact>>(hourLevelFactsDataPath, privateKey);
//            ImmutableArray<AppActivityInfo> activities = RepositoryUtils.GetArrayDataFromRollingBackupFiles<AppActivityInfo>();
//            ImmutableArray<IdleActivityInfo> idleActivities = RepositoryUtils.GetArrayDataFromRollingBackupFiles<IdleActivityInfo>();

//            File.WriteAllText(Path.Combine(outputFolder, "HourLevelFact.json"), JsonConvert.SerializeObject(hourLevelFacts, Formatting.Indented));
//            File.WriteAllText(Path.Combine(outputFolder, "AppActivityInfo.json"), JsonConvert.SerializeObject(activities, Formatting.Indented));
//            File.WriteAllText(Path.Combine(outputFolder, "IdleActivityInfo.json"), JsonConvert.SerializeObject(idleActivities, Formatting.Indented));
//        }

//        public static void Test01()
//        {
//            //restore seed phrase from password
//            string mneumonicSentence = "noble sustain owner patrol finish exclude question universe put ginger length devote";
//            string passphrase = "y\"38pu#EK&Zqi=Na";

//            Mneumonic.CreateMneumonicKeyFile(mneumonicSentence, passphrase);
//            Mneumonic.TryGetMneumonic(passphrase, out string mneumonic);
//            Mneumonic.TryGetMneumonic("ABC123asdfasdf", out mneumonic);

//            Passphrase.CreatePassphraseKeyFile(passphrase);
//            string result3 = Passphrase.GetPassphrase();

//            BIP39 bip = new BIP39(mneumonicSentence, passphrase, BIP39.Language.English);
//            BIP39 bip2 = new BIP39(bip.EntropyBytes, passphrase, BIP39.Language.English);

//            //Libsodium only taks 32 byte for keypair generation
//            //use PBKDF2 to get 64byte seed to 32 bytes
//            PBKDF2 func = new PBKDF2(bip.SeedBytes, bip.SeedBytes, 4096, "HMACSHA512");
//            byte[] privateKey = CryptoUtils.GenerateSymmetricKey(passphrase);

//            var messageText = String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
//            byte[] messageBytes = Encoding.UTF8.GetBytes(messageText);

//            byte[] nonce = SecretBox.GenerateNonce();
//            //Symmetric encryption test
//            byte[] cipherText1 = SecretBox.Create(messageBytes, nonce, privateKey);

//            byte[] decryptedBytes1 = SecretBox.Open(cipherText1, nonce, privateKey);
//            var decryptedMessage1 = Encoding.UTF8.GetString(decryptedBytes1);


//            //PublicKeyAuth uses Ed25519
//            var keyPair2 = PublicKeyAuth.GenerateKeyPair(privateKey); //64 byte private key | 32 byte public key

//            //Convert it
//            var curve25519publicKey = PublicKeyAuth.ConvertEd25519PublicKeyToCurve25519PublicKey(keyPair2.PublicKey);
//            var curve25519privateKey = PublicKeyAuth.ConvertEd25519SecretKeyToCurve25519SecretKey(keyPair2.PrivateKey);

//            //Test Encryption. (NOT NEEDED YET)
//            byte[] ciphertext2 = PublicKeyBox.Create(messageBytes, nonce, curve25519privateKey, curve25519publicKey);

//            var decryptedBytes2 = PublicKeyBox.Open(ciphertext2, nonce, curve25519privateKey, curve25519publicKey);
//            string decryptedMessage2 = Encoding.UTF8.GetString(decryptedBytes2);


//            //Test Signature and Verify
//            var signature = PublicKeyAuth.Sign(messageBytes, keyPair2.PrivateKey);
//            var verificationBytes = PublicKeyAuth.Verify(signature, keyPair2.PublicKey);
//            string verificationString = Encoding.UTF8.GetString(verificationBytes);

//            //mneumonic phrase storage

//        }

//    }
//}
