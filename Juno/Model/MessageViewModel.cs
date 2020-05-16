using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Juno.Model
{
    public class MessageViewModel
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public int Type { get; set; }
        public string FromId { get; set; }
        public string ToId { get; set; }
        public string Message { get; set; }
        public DateTime? DateSent { get; set; }
        public DateTime? DateSeen { get; set; }
        public string DownloadUrl { get; set; }
        public int? FileSizeInBytes { get; set; }
    }
}
