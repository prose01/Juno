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
    [Authorize]
    [ApiController]
    public class ChatController : Controller
    {
        private readonly IProfilesRepository _profileRepository;
        private readonly IHelperMethods _helper;
        private readonly ICryptography _cryptography;

        public ChatController(IProfilesRepository profileRepository, IHelperMethods helperMethods, ICryptography cryptography)
        {
            _profileRepository = profileRepository;
            _helper = helperMethods;
            _cryptography = cryptography;
        }

        [NoCache]
        [HttpPost("~/ParticipantResponses")]
        public async Task<IEnumerable<ParticipantResponseViewModel>> ParticipantResponsesAsync()
        {
            var auth0Id = _helper.GetCurrentUserProfile(User);

            var chatMember = _profileRepository.GetChatMemberslist(auth0Id);

            List<ChatParticipantViewModel> chatParticipants = new List<ChatParticipantViewModel> { };

            if (chatMember.Result != null)
            {
                foreach (var profile in chatMember.Result)
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
                    Metadata = new ParticipantMetadataViewModel { TotalUnreadMessages = 123 }       // TODO: set number of unread messages.
                });

            }

            return participantResponses;
        }

        [NoCache]
        [HttpPost("~/MessageHistory")]
        public async Task<IEnumerable<MessageViewModel>> MessageHistory([FromBody] string destinataryId)
        {
            var auth0Id = _helper.GetCurrentUserProfile(User);

            var messages = await _profileRepository.GetMessages(auth0Id, destinataryId);

            foreach(var message in messages)
            {
                message.Message = _cryptography.Decrypt(message.Message);
            }

            return messages;
        }
    }
}