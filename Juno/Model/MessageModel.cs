using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Juno.Model
{
    public class MessageModel
    {
        [BsonId]
        public ObjectId _id { get; set; }

        public string FromId { get; set; }

        public string FromName { get; set; }

        public string ToId { get; set; }

        public string ToName { get; set; }

        [StringLength(2000, ErrorMessage = "Message length cannot be more than 2000 characters long.")]
        public string Message { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DateSent { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DateSeen { get; set; }

        public bool DoNotDelete { get; set; }

        [BsonIgnore]
        public string ParticipantType { get; set; }
}
}
