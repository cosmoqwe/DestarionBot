using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DestarionBot
{
    internal static class SQLiteDatabase
    {
        private static readonly string DatabaseFilePath = Path.Combine(AppContext.BaseDirectory, "data", "localdatabase.db");
        private static readonly string ConnectionString = $"Data Source={DatabaseFilePath};Pooling=True;";
        static SQLiteDatabase()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "data")))
                {
                    Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "data"));
                }
                if (!File.Exists(DatabaseFilePath))
                {
                    using (var connection = new SqliteConnection(ConnectionString))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        ChatId INTEGER PRIMARY KEY,
                        Server TEXT,
                        Username TEXT,
                        State TEXT,
                        Language TEXT,
                        LastRequestTime TEXT,
                        RequestCount INTEGER
                    );";
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Log("Error in static constructor SQLiteDatabase: " + ex.Message + " Stack trace: " + ex.StackTrace, Logger.LogLevel.Error);
            }
        }
        public static async Task<object> Get(string query, object[] args)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                object? result;
                try
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        await ReplaceParameters(query, args, command);
                        result = await command.ExecuteScalarAsync();
                    }
                    return result ?? default(object);
                }
                catch (Exception ex)
                {
                    await Logger.LogAsync("Error on getting result from SQLite database!" +
                        " Query: " + query + " connection string: " + ConnectionString + " Exception: " + ex.Message + " Stack trace: " + ex.StackTrace, Logger.LogLevel.Error);
                }
            }
            return default(object);
        }
        public static async Task<T> Get<T>(string query, object[] args, Func<SqliteDataReader, T> mapFunc)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        await ReplaceParameters(query, args, command);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if(await reader.ReadAsync())
                            {
                                return mapFunc(reader);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Logger.LogAsync("Error on getting result from SQLite database!" +
                        " Query: " + query + " connection string: " + ConnectionString + " Exception: " + ex.Message + " Stack trace: " + ex.StackTrace, Logger.LogLevel.Error);
                }
            }
            return default(T);
        }
        public static async Task<bool> Execute(string query, object[] args)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        await ReplaceParameters(query, args, command);
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    await Logger.LogAsync("Error on executing command in SQLite database! Query: " + query + " Exception:" + ex.Message + " Stack trace:" + ex.StackTrace, Logger.LogLevel.Error);
                    return false;
                }
            }
        }
        public static async Task ReplaceParameters(string query, object[] args, SqliteCommand command)
        {
            if (args is not null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string paramName = "@param" + i;
                    query = query.Replace("{" + i + "}", paramName);
                    command.Parameters.AddWithValue(paramName, args[i]);
                }
                command.CommandText = query;
            }
        }
    }
}
