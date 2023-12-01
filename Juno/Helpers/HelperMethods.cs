using Juno.Interfaces;
using Juno.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Juno.Helpers
{
    public class HelperMethods : IHelperMethods
    {
        private readonly IProfilesRepository _profileRepository;
        private readonly string _nameidentifier;

        public HelperMethods(IConfiguration config, IProfilesRepository profileRepository)
        {
            _nameidentifier = config.GetValue<string>("Auth0_Claims_nameidentifier");
            _profileRepository = profileRepository;
        }

        /// <summary>Gets the current user profile.</summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<string> GetCurrentUserProfileId(ClaimsPrincipal user)
        {
            var auth0Id = user.Claims.FirstOrDefault(c => c.Type == _nameidentifier)?.Value;

            return await _profileRepository.GetCurrentProfileIdByAuth0Id(auth0Id) ?? throw new ArgumentException($"User unkown.", nameof(user));
        }

        /// <summary>Gets the current user profile.</summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<CurrentUser> GetCurrentUserByAuth0Id(ClaimsPrincipal user)
        {
            try
            {
                var auth0Id = user.Claims.FirstOrDefault(c => c.Type == _nameidentifier)?.Value;

                return await _profileRepository.GetCurrentUserByAuth0Id(auth0Id) ?? throw new ArgumentException($"User unkown.", nameof(user));
            }
            catch
            {
                throw;
            }
        }
    }
}
