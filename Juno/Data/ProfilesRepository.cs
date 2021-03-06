﻿using Juno.Interfaces;
using Juno.Model;
using Microsoft.Extensions.Options;
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
                var query = from p in _context.CurrentUser.AsQueryable()
                            where p.Auth0Id == auth0Id
                            select new { p.ProfileId };

                return await Task.FromResult(query.FirstOrDefault().ToString());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Gets the current profile by auth0Id.</summary>
        /// <param name="auth0Id">The Auth0Id.</param>
        /// <returns></returns>
        public async Task<Profile> GetDestinataryProfileByAuth0Id(string auth0Id)
        {
            var filter = Builders<Profile>.Filter.Eq("Auth0Id", auth0Id);

            try
            {
                return await _context.Profiles
                    .Find(filter)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Gets the currentUser unblocked ChatMember's profileIds by currentUser auth0Id.</summary>
        /// <param name="auth0Id">The Auth0Id.</param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetCurrentUsersChatMemberIds(string auth0Id)
        {
            try
            {
                var filter = Builders<CurrentUser>.Filter.Eq("Auth0Id", auth0Id);

                var currentUser = await _context.CurrentUser
                    .Find(filter)
                    .FirstOrDefaultAsync();

                var memberslist = (currentUser.ChatMemberslist.Where(member => !member.Blocked).Select(member => member.ProfileId)).ToList();
                
                return memberslist;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Get profiles on the ChatMemberslist.</summary>
        /// <param name="currentUser">The current user.</param>
        /// <returns></returns>
        public async Task<IEnumerable<Profile>> GetChatMemberslist(string auth0Id)
        {
            try
            {
                var memberslist = await GetCurrentUsersChatMemberIds(auth0Id);

                var query = _context.Profiles.Find(p => memberslist.Contains(p.ProfileId)).Project<Profile>("{ _id: 0 , Auth0Id: 1, Name: 1}").ToList();

                return await Task.FromResult(query.ToList());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task SaveMessage(MessageViewModel message)
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

        public async Task NotifyNewChatMember(string currentUserAuth0Id, string destinataryAuth0Id)
        {
            try
            {
                var currentUserProfileId = await GetCurrentProfileIdByAuth0Id(currentUserAuth0Id);

                var filter = Builders<Profile>
                            .Filter.Eq(e => e.Auth0Id, destinataryAuth0Id);

                var destinataryProfile = await _context.Profiles
                        .Find(filter)
                        .FirstOrDefaultAsync();
                
                if (!destinataryProfile.ChatMemberslist.Any(m => m.ProfileId == currentUserProfileId))
                {
                    var update = Builders<Profile>
                                    .Update.Push(p => p.ChatMemberslist, new ChatMember() { ProfileId = currentUserProfileId, Blocked = false });

                    var options = new FindOneAndUpdateOptions<Profile>
                    {
                        ReturnDocument = ReturnDocument.After
                    };

                    await _context.Profiles.FindOneAndUpdateAsync(filter, update, options);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IEnumerable<MessageViewModel>> GetMessages(string currentUserAuth0Id, string auth0Id)
        {
            try
            {
                var query = from m in _context.Messages.AsQueryable()
                            where (m.FromId == currentUserAuth0Id && m.ToId == auth0Id) || (m.FromId == auth0Id && m.ToId == currentUserAuth0Id)
                            orderby m.DateSent descending
                            select m;

                return await Task.FromResult(query.ToList());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        #region Maintenance

        /// <summary>Deletes Message that are more than 30 days old.</summary>
        /// <returns></returns>
        public async Task<DeleteResult> DeleteOldMessages()
        {
            try
            {
                var filter = Builders<MessageViewModel>
                            .Filter.Lt(m => m.DateSent, DateTime.Now.AddDays(-30));

                try
                {
                    return await _context.Messages.DeleteManyAsync(filter);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}
