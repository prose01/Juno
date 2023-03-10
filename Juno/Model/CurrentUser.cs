using System.Collections.Generic;

namespace Juno.Model
{
    public class CurrentUser : AbstractProfile
    {
        #region special properties
        public List<ChatMember> ChatMemberslist { get; set; }
        #endregion

        public string Auth0Id { internal get; set; }

        public override string ProfileId { get; set; }

        public bool Admin { get; set; } = false;

        public override string Name { get; set; }

        public override bool Contactable { get; set; } = true;

        public override AvatarModel Avatar { get; set; }
    }
}
