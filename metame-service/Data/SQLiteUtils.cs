using System;
using System.Data.SQLite;
using System.Data.Common;

namespace MetaMe.WindowsClient.Data
{
    class SQLiteUtils
    {
        public static void CreateDatabase(string path)
        {
            SQLiteConnection.CreateFile(path);
        }

        public static string CreateConnectionString(string path)
        {
            string connectionString = string.Format("Data Source={0};Version=3;", path);
            return connectionString;
        }

        [Obsolete("Transition away from encrypted db")]
        public static string CreateConnectionString(string password, string path)
        {
            string connectionString = string.Format("Data Source={0};Version=3;Password={1};", path, password);
            return connectionString;
        }

        public static int ExecuteNonQuery(string connectionString, string query)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        public static void Encrypt(string password, string path)
        {
            var connectionString = CreateConnectionString(path);
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                connection.ChangePassword(password);
            }
        }

        public static void Decrypt(string password, string path)
        {
            string connectionString = string.Format("Data Source={0};Version=3;Password={1};", path, password);

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                connection.ChangePassword(string.Empty);
            }
        }

        public static bool IsEncrypted(string path)
        {
            var connectionSring = CreateConnectionString(path);
            var query = "pragma schema_version;";

            try
            {
                var result = ExecuteScalar(connectionSring, query);
                return false;
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("file is not a database"))
                {
                    return true;
                }
                throw ex;
            }

        }

        public static object ExecuteScalar(string connectionString, string query)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        public static void ExecuteReader(string query, string connectionString, Action<DbDataReader> action)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                connection.Open();
                var dbReader = command.ExecuteReader();
                action(dbReader);
            }
        }

        public static bool TableExists(string tableName, string connectionString)
        {
            string query = string.Format(
@"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{0}'",
tableName);

            var result = ExecuteScalar(connectionString, query);
            return Convert.ToInt32(result) > 0;
        }

    }
}
