﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Juno.Model
{
    [BsonKnownTypes(typeof(CurrentUser))]
    public abstract class AbstractProfile
    {
        [BsonId]
        public abstract ObjectId _id { get; set; }
        public abstract string Auth0Id { get; set; }
        public abstract string ProfileId { get; set; }
        public abstract bool Admin { get; set; }
        public abstract string Name { get; set; }

        [DataType(DataType.DateTime)]
        public abstract DateTime CreatedOn { get; set; }

        [DataType(DataType.DateTime)]
        public abstract DateTime UpdatedOn { get; set; }

        [DataType(DataType.DateTime)]
        public abstract DateTime LastActive { get; set; }

        [Range(0, 120)]
        public abstract int? Age { get; set; }

        [Range(0, 220)]
        public abstract int? Height { get; set; }

        [Range(0, 200)]
        public abstract int? Weight { get; set; }

        [StringLength(2000, ErrorMessage = "Description length cannot be more than 2000.")]
        public abstract string Description { get; set; }
        public abstract List<ImageModel> Images { get; set; }

        //public abstract string[] Tags { get; set; }

        //public abstract string JobTitle { get; set; }

        [BsonRepresentation(BsonType.String)]
        [EnumDataType(typeof(GenderType))]
        public abstract GenderType Gender { get; set; }
        public abstract SexualOrientationType SexualOrientation { get; set; }

        [BsonRepresentation(BsonType.String)]
        public abstract BodyType Body { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract SmokingHabitsType SmokingHabits { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract AllergiesType Allergies { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract bool HasChildren { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract WantChildrenType WantChildren { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract LocationType LivesIn { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract EducationType EducationArea { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract EducationStatusType EducationStatus { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract EducationLevelType EducationLevel { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract EmploymentStatusType EmploymentStatus { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract EmploymentAreaType EmploymentArea { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract EmploymentLevelType EmploymentLevel { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract PoliticalOrientationType PoliticalOrientation { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract SportType Sport { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract EatingHabitsType EatingHabits { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract ClotheStyleType ClotheStyle { get; set; }

        //[BsonRepresentation(BsonType.String)]
        //public abstract BodyArtType BodyArt { get; set; }
    }
}
