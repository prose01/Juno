namespace Juno.Model
{
    public class ChatParticipantViewModel
    {
        public ChatParticipantTypeEnum ParticipantType { get; set; }
        public string Id { get; set; }
        public int Status { get; set; }
        public string Avatar { get; set; }
        public string DisplayName { get; set; }
        //public string Avatar
        //{
        //    get
        //    {
        //        return "https://www.w3schools.com/images/w3schools_green.jpg";
        //    }
        //    set
        //    {
        //    }
        //}
    }
}
