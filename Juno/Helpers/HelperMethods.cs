using Juno.Interfaces;
using Juno.Model;
using Microsoft.Extensions.Options;
using System;
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
            mockMessageHistorylist = mockMessageHistory();
        }

        /// <summary>Gets the current user profile.</summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<CurrentUser> GetCurrentUserProfile(ClaimsPrincipal user)
        {
            var auth0Id = user.Claims.FirstOrDefault(c => c.Type == _nameidentifier)?.Value;

            return await _profileRepository.GetCurrentProfileByAuth0Id(auth0Id) ?? new CurrentUser(); // Burde smide en fejl hvis bruger ikke findes.
        }

        private List<MessageViewModel> mockMessageHistory()
        {
            List<MessageViewModel> messageHistory = new List<MessageViewModel> { };

            messageHistory.Add(new MessageViewModel()
            {
                FromId = "auth0|5dcbcd3c1e0b6c0e8b05a5e2",
                ToId = "auth0|5c62f8a596979e1735449127",
                Message = "Hej med dig",
                DateSent = DateTime.Now
            });

            messageHistory.Add(new MessageViewModel()
            {
                FromId = "auth0|5dcbcd3c1e0b6c0e8b05a5e2",
                ToId = "auth0|5c62f8a596979e1735449127",
                Message = "Hvordan går det",
                DateSent = DateTime.Now
            });

            messageHistory.Add(new MessageViewModel()
            {
                FromId = "auth0|5dcbcd3c1e0b6c0e8b05a5e2",
                ToId = "auth0|5c62f8a596979e1735449127",
                Message = "Ses snart",
                DateSent = DateTime.Now
            });

            return messageHistory;
        }
    }
}
