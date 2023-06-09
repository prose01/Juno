using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Juno.Model
{
    public class GroupModel
    {
        [BsonId]
        public ObjectId _id { get; set; }

        public string GroupId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public string Name { get; set; }

        public string Description { get; set; }

        public AvatarModel Avatar { get; set; }

        public string Countrycode { get; set; }

        public List<GroupMember> GroupMemberslist { internal get; set; }
    }
}
