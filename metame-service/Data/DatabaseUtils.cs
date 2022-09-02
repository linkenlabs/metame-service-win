using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppRow = MetaMe.WindowsClient.Data.App;
using AppActivityRow = MetaMe.WindowsClient.Data.AppActivity;
using System.Data.SQLite;

namespace MetaMe.WindowsClient.Data
{
    class DatabaseUtils
    {
        public static ImmutableArray<IdleActivity> GetIdleActivities(string connectionString)
        {
            if (!SQLiteUtils.TableExists("IdleActivity", connectionString))
            {
                return ImmutableArray.Create<IdleActivity>();
            }

            List<IdleActivity> list = new List<IdleActivity>();
            //select all from db to get the ids
            string query = "SELECT * FROM IdleActivity";

            SQLiteUtils.ExecuteReader(query, connectionString, (reader) =>
            {
                while (reader.Read())
                {
                    var row = new IdleActivity
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Type = Convert.ToString(reader["Type"]),
                        Start = Convert.ToString(reader["Start"]),
                        Stop = Convert.ToString(reader["Stop"])
                    };
                    list.Add(row);
                }
            });

            return list.ToImmutableArray();
        }
        public static ImmutableArray<AppActivityRow> GetAppActivities(string connectionString)
        {
            if (!SQLiteUtils.TableExists("AppActivity", connectionString))
            {
                return ImmutableArray.Create<AppActivityRow>();
            }

            List<AppActivityRow> list = new List<AppActivityRow>();
            //select all from db to get the ids
            string query = "SELECT * FROM AppActivity";

            SQLiteUtils.ExecuteReader(query, connectionString, (reader) =>
            {
                while (reader.Read())
                {
                    var row = new AppActivityRow
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        AppId = Convert.ToInt32(reader["AppId"]),
                        Start = Convert.ToString(reader["Start"]),
                        Stop = Convert.ToString(reader["Stop"])
                    };
                    list.Add(row);
                }
            });

            return list.ToImmutableArray();

        }

        public static AppRow GetAppByName(string name, string connectionString)
        {
            if (!SQLiteUtils.TableExists("App", connectionString))
            {
                return null;
            }

            List<AppRow> list = new List<AppRow>();
            //select all from db to get the ids
            string query = "SELECT * FROM App WHERE Name = @name";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                command.Parameters.Add(new SQLiteParameter("@name", name));

                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var row = new AppRow
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = Convert.ToString(reader["Name"]),
                        IsWebsite = Convert.ToInt32(reader["IsWebsite"])
                    };
                    list.Add(row);
                }
            }
            return list.FirstOrDefault();
        }
        public static ImmutableArray<AppRow> GetApps(string connectionString)
        {
            if (!SQLiteUtils.TableExists("App", connectionString))
            {
                return ImmutableArray.Create<AppRow>();
            }

            List<AppRow> list = new List<AppRow>();
            //select all from db to get the ids
            string query = "SELECT * FROM App";

            SQLiteUtils.ExecuteReader(query, connectionString, (reader) =>
            {
                while (reader.Read())
                {
                    var row = new AppRow
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = Convert.ToString(reader["Name"]),
                        IsWebsite = Convert.ToInt32(reader["IsWebsite"])
                    };
                    list.Add(row);
                }
            });

            return list.ToImmutableArray();

        }

        public static void BulkInsertAppActivityRow(ImmutableArray<AppActivityRow> rows, string connectionString)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "INSERT INTO AppActivity(AppId, Start, Stop) " +
                        "VALUES($appId, $start, $stop);";

                    var appIdParameter = command.CreateParameter();
                    appIdParameter.ParameterName = "$appId";
                    command.Parameters.Add(appIdParameter);

                    var startParameter = command.CreateParameter();
                    startParameter.ParameterName = "$start";
                    command.Parameters.Add(startParameter);

                    var stopParameter = command.CreateParameter();
                    stopParameter.ParameterName = "$stop";
                    command.Parameters.Add(stopParameter);

                    foreach (var item in rows)
                    {
                        appIdParameter.Value = item.AppId;
                        startParameter.Value = item.Start;
                        stopParameter.Value = item.Stop;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        public static void BulkInsertIdleActivity(ImmutableArray<IdleActivity> rows, string connectionString)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "INSERT INTO IdleActivity(Type, Start, Stop) " +
                        "VALUES($type, $start, $stop);";

                    var appIdParameter = command.CreateParameter();
                    appIdParameter.ParameterName = "$type";
                    command.Parameters.Add(appIdParameter);

                    var startParameter = command.CreateParameter();
                    startParameter.ParameterName = "$start";
                    command.Parameters.Add(startParameter);

                    var stopParameter = command.CreateParameter();
                    stopParameter.ParameterName = "$stop";
                    command.Parameters.Add(stopParameter);

                    foreach (var item in rows)
                    {
                        appIdParameter.Value = item.Type;
                        startParameter.Value = item.Start;
                        stopParameter.Value = item.Stop;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        public static void BulkInsertAppRows(ImmutableArray<AppRow> appRows, string connectionString)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "INSERT INTO App(Name, IsWebsite) " +
                        "VALUES($name, $isWebsite);";

                    var nameParameter = command.CreateParameter();
                    nameParameter.ParameterName = "$name";
                    command.Parameters.Add(nameParameter);

                    var isWebsiteParameter = command.CreateParameter();
                    isWebsiteParameter.ParameterName = "$isWebsite";
                    command.Parameters.Add(isWebsiteParameter);

                    foreach (var item in appRows)
                    {
                        nameParameter.Value = item.Name;
                        isWebsiteParameter.Value = item.IsWebsite;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }
    }
}
