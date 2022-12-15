using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Juno.Model
{
    [BsonKnownTypes(typeof(CurrentUser), typeof(Profile))]
    public abstract class AbstractProfile
    {
        public abstract string ProfileId { get; set; }

        [StringLength(50, ErrorMessage = "Name length cannot be more than 50.")]
        public abstract string Name { get; set; }

        public abstract bool Contactable { get; set; }
    }
}
