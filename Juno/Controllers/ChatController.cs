using Juno.Infrastructure;
using Juno.Interfaces;
using Juno.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Juno.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    //[Authorize]
    [ApiController]
    public class ChatController : Controller
    {
        private readonly IProfilesRepository _profileRepository;
        private readonly IHelperMethods _helper;

        public ChatController(IProfilesRepository profileRepository, IHelperMethods helperMethods)
        {
            _profileRepository = profileRepository;
            _helper = helperMethods;
        }

        [NoCache]
        [HttpPost("~/ParticipantResponses")]
        public async Task<IEnumerable<ParticipantResponseViewModel>> ParticipantResponsesAsync()
        {
            var currentUser = await _helper.GetCurrentUserProfile(User);

            var bookmarkedProfiles = _profileRepository.GetBookmarkedProfiles(currentUser);

            List<ChatParticipantViewModel> chatParticipants = new List<ChatParticipantViewModel> { };

            if (bookmarkedProfiles.Result != null)
            {
                foreach (var profile in bookmarkedProfiles.Result)
                {
                    chatParticipants.Add(new ChatParticipantViewModel()
                    {
                        ParticipantType = ChatParticipantTypeEnum.User,
                        Id = profile.Auth0Id,
                        DisplayName = profile.Name,
                        Avatar = ""
                    });
                }
            }

            List<ParticipantResponseViewModel> participantResponses = new List<ParticipantResponseViewModel> { };

            foreach (var friend in chatParticipants)
            {
                participantResponses.Add(new ParticipantResponseViewModel()
                {
                    Participant = friend,
                    Metadata = new ParticipantMetadataViewModel { TotalUnreadMessages = 123 }
                });

            }

            return participantResponses;
        }

        [NoCache]
        [HttpPost("~/MessageHistory")]
        public async Task<IEnumerable<MessageViewModel>> MessageHistory()
        {
            //var currentUser = await _helper.GetCurrentUserProfile(User);

            // Get from database
            //List<MessageViewModel> messageHistory = this.mockMessageHistory();

            return _helper.mockMessageHistorylist;
        }
    }
}