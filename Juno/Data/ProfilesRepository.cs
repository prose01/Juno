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

        /// <summary>Gets the current profile by auth0Id.</summary>
        /// <param name="auth0Id">The Auth0Id.</param>
        /// <returns></returns>
        public async Task<CurrentUser> GetCurrentProfileByAuth0Id(string auth0Id)
        {
            var filter = Builders<CurrentUser>.Filter.Eq("Auth0Id", auth0Id);

            try
            {
                return await _context.CurrentUser
                    .Find(filter)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>Gets the bookmarked profiles.</summary>
        /// <param name="currentUser">The current user.</param>
        /// <returns></returns>
        public async Task<IEnumerable<Profile>> GetBookmarkedProfiles(CurrentUser currentUser)
        {
            try
            {
                // TODO: Select just what's needed not entire Profile.
                var query = _context.Profiles.Find(p => currentUser.Bookmarks.Contains(p.ProfileId));

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
    }
}