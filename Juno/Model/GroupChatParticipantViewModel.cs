using System.Collections.Generic;

namespace Juno.Model
{
    public class GroupChatParticipantViewModel : ChatParticipantViewModel
    {
        public IList<ChatParticipantViewModel> ChattingTo { get; set; }
    }
}
