using System;
using System.Collections.Generic;

namespace Juno.Model
{
    public class ChatMember
    {
        public string ProfileId { get; set; }

        public string Name { get; set; }
        public AvatarModel Avatar { get; set; }
        public DateTime? LastMessagesSeen { get; set; }

        public bool Blocked { get; set; }
    }
}
