using Microsoft.Data.Sqlite;

namespace DestarionBot
{
    internal static class SQLiteQueries
    {
        public enum QueryType
        {
            GetUser,
            SaveUserData,
        }

        private static readonly Dictionary<QueryType, string> _queryMap;
        static SQLiteQueries()
        {
            _queryMap = new Dictionary<QueryType, string>
            {
                {QueryType.GetUser, "SELECT * FROM Users WHERE ChatId = {0}"},
                {QueryType.SaveUserData, "INSERT OR REPLACE INTO Users (ChatId, Username, Server, State, Language, LastRequestTime, RequestCount) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})"}
            };
        }
        public static async Task<object> Get(QueryType queryType, object[] args)
        {
            return await SQLiteDatabase.Get(await GetQuery(queryType), args);
        }
        public static async Task<T> Get<T>(QueryType queryType, object[] args, Func<SqliteDataReader, T> mapFunc)
        {
            return await SQLiteDatabase.Get(await GetQuery(queryType), args, mapFunc);
        }
        public static async Task<bool> Execute(QueryType queryType, object[] args)
        {
            return await SQLiteDatabase.Execute(await GetQuery(queryType), args);
        }
        public static async Task<string> GetQuery(QueryType queryType)
        {
            if (!_queryMap.TryGetValue(queryType, out var query))
            {
                throw new KeyNotFoundException($"Not found a query of {queryType} type. Stack trace: {Environment.StackTrace}");
            }
            return query;
        }
    }
}
