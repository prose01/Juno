using Juno.Interfaces;
using Juno.Model;
using Microsoft.Extensions.Configuration;
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
        private readonly int _maxMessages;

        public ProfilesRepository(IOptions<Settings> settings, IConfiguration config)
        {
            _context = new ProfileContext(settings);
            _maxMessages = config.GetValue<int>("DeleteMaxMessageNumber");
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
            catch
            {
                throw;
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
            catch
            {
                throw;
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
            catch
            {
                throw;
            }
        }

        public async Task SaveMessage(MessageModel message)
        {
            try
            {
                List<FilterDefinition<MessageModel>> filters = new List<FilterDefinition<MessageModel>>();

                filters.Add(Builders<MessageModel>.Filter.Eq(m => m.ToId, message.ToId));

                filters.Add(Builders<MessageModel>.Filter.Ne(m => m.DoNotDelete, true));

                // We allow privateMessges to have maximum number of messeges for two profiles combined.
                if (message.MessageType == MessageType.PrivateMessage)
                {
                    filters.Add(Builders<MessageModel>.Filter.Eq(m => m.FromId, message.FromId));
                }

                var combineFilters = Builders<MessageModel>.Filter.And(filters);

                var count = _context.Messages.CountDocuments(combineFilters);

                // Check if Profile has reached max number of message.
                if (count >= _maxMessages)
                {
                    SortDefinition<MessageModel> sortDefinition = Builders<MessageModel>.Sort.Ascending(m => m.DateSent);

                    List<MessageModel> messageToBeDeleted = await _context.Messages.Find(combineFilters).Sort(sortDefinition).Limit(1).ToListAsync();

                    // If we can delete a message, then delete it and insert new message otherwise ignore new message. 
                    if (messageToBeDeleted != null && messageToBeDeleted.Count > 0)
                    {
                        await _context.Messages.DeleteOneAsync(m => m._id == messageToBeDeleted.First()._id);

                        await _context.Messages.InsertOneAsync(message);
                    }
                }
                else
                {
                    await _context.Messages.InsertOneAsync(message);
                }
            }
            catch
            {
                throw;
            }
        }

        //public async Task NotifyNewChatMember(CurrentUser currentUser, Profile destinataryProfile)      // TODO: Check if this is still relevent or correct!
        //{
        //    try
        //    {
        //        if (!destinataryProfile.Bookmarks.Any(m => m.ProfileId == currentUser.ProfileId))
        //        {
        //            var filter = Builders<Profile>
        //                        .Filter.Eq(e => e.ProfileId, destinataryProfile.ProfileId);

        //            var update = Builders<Profile>
        //                            .Update.Push(p => p.Bookmarks, new Bookmark() { ProfileId = currentUser.ProfileId, Name = currentUser.Name, Blocked = false, IsBookmarked = false });

        //            await _context.Profiles.UpdateOneAsync(filter, update);
        //        }
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

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
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<MessageModel>> GetGroupMessages(string groupId)
        {
            try
            {
                if (string.IsNullOrEmpty(groupId))
                    return null;

                var query = from m in _context.Messages.AsQueryable()
                            where m.ToId == groupId
                            orderby m.DateSent ascending
                            select m;

                return await Task.FromResult(query.ToList());
            }
            catch
            {
                throw;
            }
        }

        public async Task<GroupModel> GetGroup(string groupId)
        {
            try
            {
                if(string.IsNullOrEmpty(groupId))
                    return null;

                var filter = Builders<GroupModel>
                                .Filter.Eq(g => g.GroupId, groupId);

                return await _context.Groups
                    .Find(filter)
                    .FirstOrDefaultAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<GroupModel>> GetGroups(string[] groupIds)
        {
            try
            {
                if(groupIds == null || groupIds.Length == 0) 
                    return null;

                var filter = Builders<GroupModel>
                                .Filter.In(g => g.GroupId, groupIds);

                return await _context.Groups
                    .Find(filter)
                    .ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task SaveLastMessagesSeen(CurrentUser currentUser, string profileId, DateTime? lastDateSeen)
        {
            try
            {
                if (lastDateSeen != null)
                {
                    var filter = Builders<CurrentUser>
                                .Filter.Eq(c => c.ProfileId, currentUser.ProfileId);


                    List<Bookmark> updateBookmarks = new List<Bookmark>();

                    foreach (var member in currentUser.Bookmarks)
                    {
                        if (member.ProfileId == profileId)
                        {
                            member.LastMessagesSeen = lastDateSeen;
                        }

                        updateBookmarks.Add(member);
                    }

                    var update = Builders<CurrentUser>
                                    .Update.Set(c => c.Bookmarks, updateBookmarks);

                    await _context.CurrentUser.UpdateOneAsync(filter, update);
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task SaveLastGroupMessagesSeen(CurrentUser currentUser, string groupId, DateTime? lastDateSeen)
        {
            try
            {
                if (lastDateSeen != null)
                {
                    if (currentUser.Groups.ContainsKey(groupId))
                    {
                        currentUser.Groups[groupId] = lastDateSeen;

                        var filter = Builders<CurrentUser>
                                        .Filter.Eq(c => c.ProfileId, currentUser.ProfileId);

                        var update = Builders<CurrentUser>
                                    .Update.Set(c => c.Groups, currentUser.Groups);

                        await _context.CurrentUser.UpdateOneAsync(filter, update);
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public int TotalUnreadMessages(bool groupMessage, string fromId, string toId, DateTime? lastDateSeen)
        {
            try
            {
                if (lastDateSeen != null)
                {
                    List<FilterDefinition<MessageModel>> filters = new List<FilterDefinition<MessageModel>>();

                    // Normal chat between profiles have both toId and fromId, whereas for groups we don't want message user has written themselves.
                    if (groupMessage)
                    {
                        filters.Add(Builders<MessageModel>.Filter.Ne(m => m.FromId, fromId));
                    }
                    else
                    {
                        filters.Add(Builders<MessageModel>.Filter.Eq(m => m.FromId, fromId));
                    }

                    filters.Add(Builders<MessageModel>.Filter.Eq(m => m.ToId, toId));

                    filters.Add(Builders<MessageModel>.Filter.Gt(m => m.DateSent, lastDateSeen));

                    var combineFilters = Builders<MessageModel>.Filter.And(filters);

                    return (int)_context.Messages
                                .Find(combineFilters).CountDocuments();
                }

                return 0;
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<MessageModel>> GetProfileMessages(string profileId, int skip, int limit)
        {
            try
            {
                List<FilterDefinition<MessageModel>> filters = new List<FilterDefinition<MessageModel>>();

                filters.Add(Builders<MessageModel>.Filter.Eq(m => m.FromId, profileId));

                filters.Add(Builders<MessageModel>.Filter.Eq(m => m.ToId, profileId));

                var combineFilters = Builders<MessageModel>.Filter.Or(filters);

                SortDefinition<MessageModel> sortDefinition = Builders<MessageModel>.Sort.Descending(m => m.DateSent);

                return await _context.Messages
                            .Find(combineFilters).Sort(sortDefinition).Skip(skip).Limit(limit).ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<MessageModel>> GetChatsByFilter(ChatFilter chatFilter, int skip, int limit)
        {
            try
            {
                List<FilterDefinition<MessageModel>> filters = new List<FilterDefinition<MessageModel>>();

                if (chatFilter.MessageType != MessageType.NotChosen)
                    filters.Add(Builders<MessageModel>.Filter.Eq(m => m.MessageType, chatFilter.MessageType));

                if (chatFilter.FromId != null)
                    filters.Add(Builders<MessageModel>.Filter.Eq(m => m.FromId, chatFilter.FromId));

                if (chatFilter.FromName != null)
                    filters.Add(Builders<MessageModel>.Filter.Regex(m => m.FromName, new BsonRegularExpression(chatFilter.FromName, "i")));

                if (chatFilter.ToId != null)
                    filters.Add(Builders<MessageModel>.Filter.Eq(m => m.ToId, chatFilter.ToId));

                if (chatFilter.ToName != null)
                    filters.Add(Builders<MessageModel>.Filter.Regex(m => m.ToName, new BsonRegularExpression(chatFilter.ToName, "i")));

                // For now we cannot filter on message as it is stored encrypted in the database.
                //if (chatFilter.Message != null)
                //    filters.Add(Builders<MessageModel>.Filter.Regex(m => m.Message, new BsonRegularExpression(chatFilter.Message, "i")));

                if (chatFilter.DateSentStart != null)
                    filters.Add(Builders<MessageModel>.Filter.Gte(m => m.DateSent, chatFilter.DateSentStart));

                if (chatFilter.DateSentEnd != null)
                    filters.Add(Builders<MessageModel>.Filter.Lte(m => m.DateSent, chatFilter.DateSentEnd));

                if (chatFilter.DateSeenStart != null)
                    filters.Add(Builders<MessageModel>.Filter.Gte(m => m.DateSent, chatFilter.DateSeenStart));

                if (chatFilter.DateSeenEnd != null)
                    filters.Add(Builders<MessageModel>.Filter.Lte(m => m.DateSeen, chatFilter.DateSeenEnd));

                if (chatFilter.DoNotDelete != null)
                {
                    bool.TryParse(chatFilter.DoNotDelete, out bool parsedValue);
                    filters.Add(Builders<MessageModel>.Filter.Eq(m => m.DoNotDelete, parsedValue));
                }

                // Safety break for searches with no filters.
                if (filters.Count == 0)
                    return new List<MessageModel>();

                var combineFilters = Builders<MessageModel>.Filter.And(filters);

                SortDefinition<MessageModel> sortDefinition = Builders<MessageModel>.Sort.Descending(m => m.DateSent);

                return await _context.Messages
                            .Find(combineFilters).Sort(sortDefinition).Skip(skip).Limit(limit).ToListAsync();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>Set messages as Do Not Delete</summary>
        /// <param name="messages"></param>
        public async Task DoNotDelete(MessageModel[] messages, bool doNotDelete)
        {
            try
            {
                foreach (var message in messages)
                {
                    List<FilterDefinition<MessageModel>> filters = new List<FilterDefinition<MessageModel>>();

                    filters.Add(Builders<MessageModel>.Filter.Eq(m => m.FromId, message.FromId));

                    filters.Add(Builders<MessageModel>.Filter.Eq(m => m.ToId, message.ToId));

                    filters.Add(Builders<MessageModel>.Filter.Eq(m => m.DateSent, message.DateSent));

                    var combineFilters = Builders<MessageModel>.Filter.And(filters);

                    var update = Builders<MessageModel>
                                .Update.Set(m => m.DoNotDelete, doNotDelete);

                    await _context.Messages.UpdateOneAsync(combineFilters, update);
                }
            }
            catch
            {
                throw;
            }
        }

        private ProjectionDefinition<Profile> GetProjection()
        {
            ProjectionDefinition<Profile> projection = "{ " +
                "_id: 0, " +
                "Auth0Id: 0, " +
                "Seeking: 0, " +
                "Gender: 0, " +
                "Languagecode: 0, " +
                "ProfileFilter: 0, " +
                "Visited: 0, " +
                "IsBookmarked: 0, " +
                "Likes: 0, " +
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
                "_id: 0, " +
                "Auth0Id: 0, " +
                "Seeking: 0, " +
                "Gender: 0, " +
                "Languagecode: 0, " +
                "ProfileFilter: 0, " +
                "Visited: 0, " +
                "IsBookmarked: 0, " +
                "Likes: 0, " +
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
                "Complains: 0 " +
                "}";

            return projection;
        }
    }
}
