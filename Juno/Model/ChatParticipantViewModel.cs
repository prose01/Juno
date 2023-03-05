namespace Juno.Model
{
    public class ChatParticipantViewModel
    {
        public ChatParticipantTypeEnum ParticipantType { get; set; }

        public string Id { get; set; }

        public int Status { get; set; }

        public string DisplayName { get; set; }

        public string Initials { get; set; }

        public string CircleColor { get; set; }
    }
}
