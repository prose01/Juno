using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Juno.Model
{
    [BsonIgnoreExtraElements]
    public class Profile : AbstractProfile
    {
        #region special properties
        public List<Bookmark> Bookmarks { internal get; set; }
        #endregion

        public override string ProfileId { get; set; }

        public override string Name { get; set; }

        public override bool Contactable { get; set; } = true;

        public override AvatarModel Avatar { get; set; }
    }
}
