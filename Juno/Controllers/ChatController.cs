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
            try
            {
                var profileId = await _helper.GetCurrentUserProfileId(User);

                var chatMember = _profileRepository.GetChatMemberslist(profileId);

                List<ChatParticipantViewModel> chatParticipants = new List<ChatParticipantViewModel> { };

                if (chatMember.Result != null)
                {
                    foreach (var profile in chatMember.Result)
                    {
                        chatParticipants.Add(new ChatParticipantViewModel()
                        {
                            ParticipantType = ChatParticipantTypeEnum.User,
                            Id = profile.ProfileId,
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
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [NoCache]
        [HttpPost("~/MessageHistory")]
        public async Task<IEnumerable<MessageModel>> MessageHistory([FromBody] string destinataryId)
        {
            try
            {
                var profileId = await _helper.GetCurrentUserProfileId(User);

                var destinataryProfile = await _profileRepository.GetDestinataryProfileByProfileId(destinataryId); // TODO: Change destinataryId to ProfileId in paramenter

                var messages = await _profileRepository.GetMessages(profileId, destinataryProfile.ProfileId);

                foreach (var message in messages)
                {
                    message.Message = _cryptography.Decrypt(message.Message);
                }

                return messages;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Maintenance

        /// <summary>Deletes Message that are more than 30 days old.</summary>
        /// <returns></returns>
        [NoCache]
        [HttpDelete("~/DeleteOldMessages")]
        public async Task<IActionResult> DeleteOldMessages()
        {
            try
            {
                //var oldMessages = await _profileRepository.DeleteOldMessages(); // TODO: Needs to be testet.

                return Ok();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}