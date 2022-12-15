using System;
using System.ComponentModel.DataAnnotations;

namespace Juno.Model
{
    public class ChatFilter
    {
        [DataType(DataType.DateTime)]
        public DateTime? DateSentStart { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DateSentEnd { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DateSeenStart { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DateSeenEnd { get; set; }

        public string FromId { get; set; }

        public string FromName { get; set; }

        public string ToId { get; set; }

        public string ToName { get; set; }

        [StringLength(2000, ErrorMessage = "Message length cannot be more than 2000 characters long.")]
        public string Message { get; set; }

        public string DoNotDelete { get; set; }
    }
}
