using MySqlConnector;

namespace DestarionBot
{
    
    public static class Queries
    {
        public enum QueryType
        {
            IsCharacterLinked,
            UnlinkCharacter,
            GetCharacterObjId,
            GetVerifiesCount,
            RegisterUserAndLinkCharacter,
            TakeMessages,
            DeleteMessages
        }

        private static readonly Dictionary<QueryType, string> _queryMap;
        static Queries()
        {
            _queryMap = new Dictionary<QueryType, string>
            {
                {QueryType.GetCharacterObjId, "SELECT obj_id FROM character_variables WHERE name = 'telegram_random' AND value = {0}"},
                {QueryType.IsCharacterLinked, "SELECT COUNT(*) FROM telegram_verifies WHERE object_id = {0}" },
                {QueryType.UnlinkCharacter,   "DELETE FROM telegram_verifies WHERE object_id = {0}" },
                {QueryType.GetVerifiesCount,  "SELECT COUNT(*) FROM telegram_verifies WHERE chat_id = {0}" },
                {QueryType.RegisterUserAndLinkCharacter, "INSERT INTO telegram_verifies (chat_id, telegram_id, object_id) VALUES ({0}, {1}, {2})" },
            };
        }
        public static async Task<object> Get(QueryType queryType, object[] args , string selectedServer)
        {
            return await Database.Get(await GetQuery(queryType), args, selectedServer);
        }
        public static async Task<ICollection<T>> Get<T>(QueryType queryType, object[] args, string selectedServer, ICollection<T> collection, Func<MySqlDataReader, T> mapFunc)
        {
            return await Database.Get<T>(await GetQuery(queryType), args, selectedServer, collection, mapFunc);
        }
        public static async Task<bool> Execute(QueryType queryType, object[] args, string selectedServer)
        {
            return await Database.Execute(await GetQuery(queryType), args, selectedServer);
        }
        public static async Task<string> GetQuery(QueryType queryType)
        {
            if (!_queryMap.TryGetValue(queryType, out var query))
            {
                throw new KeyNotFoundException("Key not found in Queries._queryMap! QueryType: " + queryType);
            }
            return query;
        }
    }
}
