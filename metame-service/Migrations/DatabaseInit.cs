using MetaMe.Core;
using MetaMe.WindowsClient.Data;
using System.IO;

namespace MetaMe.WindowsClient.Migrations
{
    class DatabaseInit
    {
        public static bool RequiresMigration()
        {
            var dbPath = PathUtils.GetSQLiteDatabasePath(ClientApplication.Instance.DevMode);
            return !File.Exists(dbPath);
        }

        public static void Migrate()
        {
            var dbPath = PathUtils.GetSQLiteDatabasePath(ClientApplication.Instance.DevMode);
            SQLiteUtils.CreateDatabase(dbPath);

            string connectionString = SQLiteUtils.CreateConnectionString(dbPath);
            string query =
@"CREATE TABLE IF NOT EXISTS IdleActivity 
(Id INTEGER PRIMARY KEY AUTOINCREMENT, 
Start TEXT, 
Stop TEXT,
Type TEXT)";
            SQLiteUtils.ExecuteNonQuery(connectionString, query);

            string query1 =
@"CREATE TABLE IF NOT EXISTS AppActivity 
(Id INTEGER PRIMARY KEY AUTOINCREMENT, 
AppId INTEGER,
Start TEXT, 
Stop TEXT)";
            SQLiteUtils.ExecuteNonQuery(connectionString, query1);

            string query2 =
@"CREATE TABLE IF NOT EXISTS App 
(Id INTEGER PRIMARY KEY AUTOINCREMENT, 
Name TEXT,
IsWebsite INTEGER)";
            SQLiteUtils.ExecuteNonQuery(connectionString, query2);
        }

    }
}
