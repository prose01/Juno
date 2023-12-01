using Juno.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Juno.Data
{
    public class ProfileContext
    {
        private readonly IMongoDatabase _database = null;

        public ProfileContext(IConfiguration config)
        {
            var client = new MongoClient(config.GetValue<string>("Mongo_ConnectionString"));
            if (client != null)
                _database = client.GetDatabase(config.GetValue<string>("Mongo_Database"));
        }

        public IMongoCollection<CurrentUser> CurrentUser => _database.GetCollection<CurrentUser>("Profile");
        public IMongoCollection<Profile> Profiles => _database.GetCollection<Profile>("Profile");
        public IMongoCollection<MessageModel> Messages => _database.GetCollection<MessageModel>("Message");
        public IMongoCollection<GroupModel> Groups => _database.GetCollection<GroupModel>("ChatGroups");
    }
}
