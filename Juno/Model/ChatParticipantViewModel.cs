namespace Juno.Model
{
    public class ChatParticipantViewModel
    {
        public ChatParticipantTypeEnum ParticipantType { get; set; }
        public string Id { get; set; }
        public string Auth0Id { get; set; }
        public int Status { get; set; }
        public string Avatar { get; set; }
        public string DisplayName { get; set; }
    }
}
