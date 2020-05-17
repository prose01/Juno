﻿using Juno.Interfaces;
using Juno.Model;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Juno.Helpers
{
    public class HelperMethods : IHelperMethods
    {
        private readonly IProfilesRepository _profileRepository;
        private readonly string _nameidentifier;
        public List<MessageViewModel> mockMessageHistorylist { get; set; }

        public HelperMethods(IProfilesRepository profileRepository, IOptions<Settings> settings)
        {
            _profileRepository = profileRepository;
            _nameidentifier = settings.Value.auth0Id;
        }

        /// <summary>Gets the current user profile.</summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<CurrentUser> GetCurrentUserProfile(ClaimsPrincipal user)
        {
            var auth0Id = user.Claims.FirstOrDefault(c => c.Type == _nameidentifier)?.Value;

            // TODO: Select just what's needed not entire Profile.
            return await _profileRepository.GetCurrentProfileByAuth0Id(auth0Id) ?? new CurrentUser(); // TODO: Burde smide en fejl hvis bruger ikke findes.
        }
    }
}
