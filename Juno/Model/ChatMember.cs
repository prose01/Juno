using System.Collections.Generic;

namespace Juno.Model
{
    public class ChatMember
    {
        public string ProfileId { get; set; }

        public string Name { get; set; }

        public bool Blocked { get; set; }
        public Dictionary<string, string> Avatar { get; set; }
    }
}
