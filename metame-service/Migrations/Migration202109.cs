using MetaMe.Core;
using MetaMe.WindowsClient.Data;
using System.IO;

namespace MetaMe.WindowsClient.Migrations
{
    // For this migration just decrypt the DB
    class Migration202109
    {
        public static bool RequiresMigration()
        {
            var dbPath = PathUtils.GetSQLiteDatabasePath(ClientApplication.Instance.DevMode);
            if (!File.Exists(dbPath))
            {
                return false;
            }
            return SQLiteUtils.IsEncrypted(dbPath);
        }
        public static void Migrate()
        {
            var dbPath = PathUtils.GetSQLiteDatabasePath(ClientApplication.Instance.DevMode);
            string dbPassword = "9L6iyu{4HjysmKrX";
            SQLiteUtils.Decrypt(dbPassword, dbPath);
        }
    }
}
