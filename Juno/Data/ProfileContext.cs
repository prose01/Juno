﻿using Juno.Model;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Juno.Data
{
    public class ProfileContext
    {
        private readonly IMongoDatabase _database = null;

        public ProfileContext(IOptions<Settings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            if (client != null)
                _database = client.GetDatabase(settings.Value.Database);
        }

        public IMongoCollection<CurrentUser> CurrentUser => _database.GetCollection<CurrentUser>("Profile");
        public IMongoCollection<Profile> Profiles => _database.GetCollection<Profile>("Profile");
        public IMongoCollection<MessageModel> Messages => _database.GetCollection<MessageModel>("Message");
        public IMongoCollection<GroupModel> Groups => _database.GetCollection<GroupModel>("ChatGroups");
    }
}
