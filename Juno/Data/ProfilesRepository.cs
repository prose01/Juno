﻿using Juno.Interfaces;
using Juno.Model;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Juno.Data
{
    public class ProfilesRepository : IProfilesRepository
    {
        private readonly ProfileContext _context = null;

        public ProfilesRepository(IOptions<Settings> settings)
        {
            _context = new ProfileContext(settings);
        }

        /// <summary>Gets the currentUser profileId by auth0Id.</summary>
        /// <param name="auth0Id">The Auth0Id.</param>
        /// <returns></returns>
        public async Task<string> GetCurrentProfileIdByAuth0Id(string auth0Id)
        {
            try
            {
                var query = from c in _context.CurrentUser.AsQueryable()
                            where c.Auth0Id == auth0Id
                            select c.ProfileId;

                return await Task.FromResult(query.FirstOrDefault());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Gets the currentUser by auth0Id.</summary>
        /// <param name="auth0Id">The Auth0Id.</param>
        /// <returns></returns>
        public async Task<CurrentUser> GetCurrentUserByAuth0Id(string auth0Id)
        {
            var filter = Builders<CurrentUser>.Filter.Eq("Auth0Id", auth0Id);

            try
            {
                return await _context.CurrentUser
                    .Find(filter)
                    .Project<CurrentUser>(this.GetCurrentUserProjection())
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Gets the current profile by profileId.</summary>
        /// <param name="profileId">The ProfileId.</param>
        /// <returns></returns>
        public async Task<Profile> GetDestinataryProfileByProfileId(string profileId)
        {
            var filter = Builders<Profile>.Filter.Eq("ProfileId", profileId);

            try
            {
                return await _context.Profiles
                    .Find(filter)
                    .Project<Profile>(this.GetProjection())
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task SaveMessage(MessageModel message)
        {
            try
            {
                await _context.Messages.InsertOneAsync(message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task NotifyNewChatMember(CurrentUser currentUser, Profile destinataryProfile)
        {
            try
            {                
                if (!destinataryProfile.ChatMemberslist.Any(m => m.ProfileId == currentUser.ProfileId))
                {
                    var filter = Builders<Profile>
                                .Filter.Eq(e => e.ProfileId, destinataryProfile.ProfileId);

                    var update = Builders<Profile>
                                    .Update.Push(p => p.ChatMemberslist, new ChatMember() { ProfileId = currentUser.ProfileId, Name = currentUser.Name, Blocked = false });

                    await _context.Profiles.FindOneAndUpdateAsync(filter, update);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IEnumerable<MessageModel>> GetMessages(string currentUserProfileId, string profileId)
        {
            try
            {
                var query = from m in _context.Messages.AsQueryable()
                            where (m.FromId == currentUserProfileId && m.ToId == profileId) || (m.FromId == profileId && m.ToId == currentUserProfileId)
                            orderby m.DateSent ascending
                            select m;

                return await Task.FromResult(query.ToList());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task MessagesSeen(ObjectId messagesId)
        {
            try
            {
                var filter = Builders<MessageModel>
                               .Filter.Eq(m => m._id, messagesId);

                var update = Builders<MessageModel>
                            .Update.Set(m => m.DateSeen, DateTime.Now);

                await _context.Messages.FindOneAndUpdateAsync(filter, update);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int TotalUnreadMessages(string chatMemberId, string profileId)
        {
            try
            {
                List<FilterDefinition<MessageModel>> filters = new List<FilterDefinition<MessageModel>>();

                filters.Add(Builders<MessageModel>.Filter.Eq(m => m.FromId, chatMemberId));

                filters.Add(Builders<MessageModel>.Filter.Eq(m => m.ToId, profileId));

                filters.Add(Builders<MessageModel>.Filter.Eq(m => m.DateSeen, null));

                var combineFilters = Builders<MessageModel>.Filter.And(filters);

                return (int)_context.Messages
                            .Find(combineFilters).CountDocuments();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        #region Maintenance

        /// <summary>Deletes Messages that are more than 30 days old.</summary>
        /// <returns></returns>
        public async Task<DeleteResult> DeleteOldMessages()
        {
            try
            {
                var filter = Builders<MessageModel>
                            .Filter.Gt(m => m.DateSent, DateTime.Now.AddDays(-30));

                return await _context.Messages.DeleteManyAsync(filter);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        private ProjectionDefinition<Profile> GetProjection()
        {
            ProjectionDefinition<Profile> projection = "{ " +
                "Auth0Id: 0, " +
                "SexualOrientation: 0, " +
                "Gender: 0, " +
                "Languagecode: 0, " +
                "Bookmarks: 0, " +
                "ProfileFilter: 0, " +
                "Visited: 0, " +
                "IsBookmarked: 0, " +
                "Likes: 0, " +
                "_id: 0, " +
                "Admin:0, " +
                "CreatedOn: 0, " +
                "UpdatedOn: 0, " +
                "LastActive: 0, " +
                "Countrycode: 0, " +
                "Age: 0, " +
                "Height: 0, " +
                "Description: 0, " +
                "Images: 0, " +
                "Tags: 0, " +
                "Body: 0, " +
                "SmokingHabits: 0, " +
                "HasChildren: 0, " +
                "WantChildren: 0, " +
                "HasPets: 0, " +
                "LivesIn: 0, " +
                "Education: 0, " +
                "EducationStatus: 0, " +
                "EmploymentStatus: 0, " +
                "SportsActivity: 0, " +
                "EatingHabits: 0, " +
                "ClotheStyle: 0, " +
                "BodyArt: 0, " +
                "}";

            return projection;
        }

        private ProjectionDefinition<CurrentUser> GetCurrentUserProjection()
        {
            ProjectionDefinition<CurrentUser> projection = "{ " +
                "Auth0Id: 0, " +
                "SexualOrientation: 0, " +
                "Gender: 0, " +
                "Languagecode: 0, " +
                "Bookmarks: 0, " +
                "ProfileFilter: 0, " +
                "Visited: 0, " +
                "IsBookmarked: 0, " +
                "Likes: 0, " +
                "_id: 0, " +
                "CreatedOn: 0, " +
                "UpdatedOn: 0, " +
                "LastActive: 0, " +
                "Countrycode: 0, " +
                "Age: 0, " +
                "Height: 0, " +
                "Description: 0, " +
                "Images: 0, " +
                "Tags: 0, " +
                "Body: 0, " +
                "SmokingHabits: 0, " +
                "HasChildren: 0, " +
                "WantChildren: 0, " +
                "HasPets: 0, " +
                "LivesIn: 0, " +
                "Education: 0, " +
                "EducationStatus: 0, " +
                "EmploymentStatus: 0, " +
                "SportsActivity: 0, " +
                "EatingHabits: 0, " +
                "ClotheStyle: 0, " +
                "BodyArt: 0, " +
                "}";

            return projection;
        }
    }
}
