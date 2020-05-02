﻿using Juno.Interfaces;
using Juno.Model;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Juno.Helpers
{
    public class HelperMethods : IHelperMethods
    {
        private readonly ICurrentUserRepository _profileRepository;
        private readonly string _nameidentifier;

        public HelperMethods(IOptions<Settings> settings, ICurrentUserRepository profileRepository)
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

            return await _profileRepository.GetCurrentProfileByAuth0Id(auth0Id) ?? new CurrentUser(); // Burde smide en fejl hvis bruger ikke findes.
        }
    }
}
