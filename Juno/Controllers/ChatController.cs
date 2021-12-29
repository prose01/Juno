using Juno.Chat;
using Juno.Infrastructure;
using Juno.Interfaces;
using Juno.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public async Task<IEnumerable<ParticipantResponseViewModel>> ParticipantResponsesAsync([FromBody] CurrentUser item)
        {
            try
            {
                List<ChatParticipantViewModel> chatParticipants = new List<ChatParticipantViewModel> { };

                if (item.ChatMemberslist.Count > 0)
                {
                    foreach (var profile in item.ChatMemberslist)
                    {
                        var oldConnectedParticipants = ChatHub.AllConnectedParticipants.Where(x => x.Participant.Id == profile.ProfileId);

                        chatParticipants.Add(new ChatParticipantViewModel()
                        {
                            ParticipantType = ChatParticipantTypeEnum.User,
                            Id = profile.ProfileId,
                            DisplayName = profile.Name,
                            Avatar = "",
                            Status = oldConnectedParticipants.Any() ? 0 : 3
                        });
                    }
                }

                List<ParticipantResponseViewModel> participantResponses = new List<ParticipantResponseViewModel> { };

                foreach (var friend in chatParticipants)
                {
                    participantResponses.Add(new ParticipantResponseViewModel()
                    {
                        Participant = friend,
                        Metadata = new ParticipantMetadataViewModel { TotalUnreadMessages = _profileRepository.TotalUnreadMessages(friend.Id, item.ProfileId) }
                    });
                }

                return participantResponses;
            }
            catch (Exception ex)
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

                var destinataryProfile = await _profileRepository.GetDestinataryProfileByProfileId(destinataryId);

                var messages = await _profileRepository.GetMessages(profileId, destinataryProfile.ProfileId);

                foreach (var message in messages)
                {
                    message.Message = _cryptography.Decrypt(message.Message);

                    if (message.ToId == profileId && message.DateSeen == null)
                    {
                        await _profileRepository.MessagesSeen(message._id);
                    }
                }

                return messages;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [NoCache]
        [HttpGet("~/ProfileMessages/{profileId}")]
        public async Task<IEnumerable<MessageModel>> ProfileMessages(string profileId)
        {
            try
            {
                var currentUser = await _helper.GetCurrentUserByAuth0Id(User);

                if (currentUser == null || !currentUser.Admin)
                {
                    throw new ArgumentException($"Current user is null or does not have admin status.");
                }

                var messages = await _profileRepository.GetProfileMessages(profileId);

                foreach (var message in messages)
                {
                    message.Message = _cryptography.Decrypt(message.Message);

                    if (message.ToId == profileId && message.DateSeen == null)
                    {
                        await _profileRepository.MessagesSeen(message._id);
                    }
                }

                return messages;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Gets the specified chats based on a filter. Eg. { Message: 'something' }
        /// </summary>
        /// <param name="parameterFilter"></param>
        /// <exception cref="ArgumentException">ProfileFilter is null. {requestBody.ChatFilter}</exception>
        /// <exception cref="ArgumentException">Cannot find any matching chats. {requestBody.ProfileFilter}</exception>
        /// <returns></returns>
        [NoCache]
        [HttpPost("~/GetChatsByFilter")]
        public async Task<IEnumerable<MessageModel>> GetChatsByFilter([FromBody] RequestBody requestBody, [FromQuery] ParameterFilter parameterFilter)
        {
            try
            {
                var currentUser = await _helper.GetCurrentUserByAuth0Id(User);

                if (currentUser == null || !currentUser.Admin)
                {
                    throw new ArgumentException($"Current user is null or does not have admin status.");
                }

                if (requestBody.ChatFilter == null) throw new ArgumentException($"ChatFilter is null.", nameof(requestBody.ChatFilter));

                var skip = parameterFilter.PageIndex == 0 ? parameterFilter.PageIndex : parameterFilter.PageIndex * parameterFilter.PageSize;

                return await _profileRepository.GetChatsByFilter(requestBody.ChatFilter, skip, parameterFilter.PageSize) ?? throw new ArgumentException($"Cannot find any matching chats.", nameof(requestBody.ChatFilter));

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
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> DeleteOldMessages()
        {
            try
            {
                //var oldMessages = await _profileRepository.DeleteOldMessages(); // TODO: Needs to be testet.

                return NoContent();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}