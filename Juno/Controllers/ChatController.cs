using Juno.Chat;
using Juno.Infrastructure;
using Juno.Interfaces;
using Juno.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver.Linq;
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
                string[] profileIds = item.ChatMemberslist.Select(p => p.ProfileId).ToArray();

                List<ParticipantResponseViewModel> participantResponses = new List<ParticipantResponseViewModel> { };

                if (item.ChatMemberslist.Count > 0)
                {
                    List<ChatParticipantViewModel> chatParticipants = new List<ChatParticipantViewModel> { };

                    foreach (var profile in item.ChatMemberslist)
                    {
                        var oldConnectedParticipants = GroupChatHub.AllConnectedParticipants.Where(x => x.Participant.Id == profile.ProfileId);

                        chatParticipants.Add(new ChatParticipantViewModel()
                        {
                            ParticipantType = ChatParticipantTypeEnum.User,
                            Id = profile.ProfileId,
                            DisplayName = profile.Name,
                            Initials = profile.Avatar.Initials, 
                            InitialsColour = profile.Avatar.InitialsColour,
                            CircleColour = profile.Avatar.CircleColour,
                            Status = oldConnectedParticipants.Any() ? 0 : 3
                        });
                    }

                    foreach (var friend in chatParticipants)
                    {
                        // TODO: This is called all the time from Front private fetchFriendsList(isBootstrapping: boolean) Move this out in its own call and set a pollingIntervalWindowInstance on it like for fetchFriendsList!!!
                        participantResponses.Add(new ParticipantResponseViewModel()
                        {
                            Participant = friend,
                            Metadata = new ParticipantMetadataViewModel { TotalUnreadMessages = _profileRepository.TotalUnreadMessages(friend.Id, item.ProfileId) }
                        });
                    }
                }

                var groups = await _profileRepository.GetGroups(item.Groups?.ToArray());

                if(groups != null)
                {
                    List<ChatParticipantViewModel> participantGroup = new List<ChatParticipantViewModel> { };

                    foreach (var group in groups)
                    {
                        if (group.GroupMemberslist.Where(x => x.ProfileId == item.ProfileId && x.Blocked == true).Count() > 0)
                            continue;

                        var set1 = new HashSet<string>(GroupChatHub.AllConnectedParticipants.Where(x => x.Participant.Id != item.ProfileId).Select(x => x.Participant.Id));
                        var set2 = new HashSet<string>(group.GroupMemberslist.Where(x => x.Blocked == false).Select(x => x.ProfileId));
                        set1.IntersectWith(set2);

                        //var list1 = GroupChatHub.AllConnectedParticipants.Where(x => x.Participant.Id != item.ProfileId).Select(x => x.Participant.Id).Where(i => group.ChatMemberslist.Where(x => x.Blocked == false).Select(x => x.ProfileId).Contains(i)).ToList();


                        participantGroup.Add(new ChatParticipantViewModel()
                        {
                            ParticipantType = ChatParticipantTypeEnum.Group,
                            Id = group.GroupId,
                            DisplayName = group.Name,
                            Initials = group.Avatar.Initials,
                            InitialsColour = group.Avatar.InitialsColour,
                            CircleColour = group.Avatar.CircleColour,
                            Status = set1.Any() ? 0 : 3
                        });
                    }

                    foreach (var group in participantGroup)
                    {
                        // TODO: This is called all the time from Front private fetchFriendsList(isBootstrapping: boolean) Move this out in its own call and set a pollingIntervalWindowInstance on it like for fetchFriendsList!!!
                        participantResponses.Add(new ParticipantResponseViewModel()
                        {
                            Participant = group,
                            Metadata = new ParticipantMetadataViewModel { TotalUnreadMessages = 0 }
                            //Metadata = new ParticipantMetadataViewModel { TotalUnreadMessages = _profileRepository.TotalUnreadGroupMessages(group.Id, item.ProfileId) } /// TODO: Groups don't have a DateSeen set and user/Profile doesn't know if message is seen!
                        });
                    }
                }

                return participantResponses;
            }
            catch
            {
                throw;
            }
        }

        [NoCache]
        [HttpPost("~/MessageHistory")]
        public async Task<IEnumerable<MessageModel>> MessageHistory([FromBody] ChatParticipantViewModel chatparticipant)
        {
            try
            {
                if (chatparticipant.ParticipantType == ChatParticipantTypeEnum.Group)
                {
                    var messages = await _profileRepository.GetGroupMessages(chatparticipant.Id);

                    foreach (var message in messages)
                    {
                        message.Message = _cryptography.Decrypt(message.Message);
                    }

                    var tt = messages.LastOrDefault();

                    return messages;
                }
                else
                {
                    var profileId = await _helper.GetCurrentUserProfileId(User);

                    var destinataryProfile = await _profileRepository.GetDestinataryProfileByProfileId(chatparticipant.Id);

                    if (destinataryProfile != null)
                    {
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
                }

                return null;
            }
            catch
            {
                throw;
            }
        }

        [NoCache]
        [HttpPost("~/Groups")]
        public async Task<IEnumerable<GroupChatParticipantViewModel>> Groups()
        {
            try
            {
                var currentUser = await _helper.GetCurrentUserByAuth0Id(User);
                //string[] profileIds = { currentUser.ProfileId };
                //var avatars = await _profileRepository.GetProfileAvatarrByIds(profileIds);

                if (currentUser == null)
                {
                    throw new ArgumentException($"Current user is null.");
                }

                var groups = await _profileRepository.GetGroups(currentUser.Groups.ToArray());

                List<GroupChatParticipantViewModel> allGroupParticipants = new List<GroupChatParticipantViewModel>();
                List<ChatParticipantViewModel> chatParticipants = new List<ChatParticipantViewModel> { };

                foreach (var group in groups)
                {
                    var oldConnectedParticipants = GroupChatHub.AllConnectedParticipants.Where(x => x.Participant.Id == currentUser.ProfileId);

                    //var avatarInfo = avatars.Where(p => p.ProfileId == currentUser.ProfileId).ToList();

                    chatParticipants.Add(new ChatParticipantViewModel()
                    {
                        ParticipantType = ChatParticipantTypeEnum.User,
                        Id = currentUser.ProfileId,
                        DisplayName = currentUser.Name,
                        Initials = currentUser.Avatar.Initials,
                        InitialsColour = currentUser.Avatar.InitialsColour,
                        CircleColour = currentUser.Avatar.CircleColour,
                        Status = oldConnectedParticipants.Any() ? 0 : 3
                    });

                    allGroupParticipants.Add(new GroupChatParticipantViewModel()
                    {
                        Id = group.GroupId,
                        DisplayName = group.Name,
                        ChattingTo = chatParticipants
                    });
                }

                return allGroupParticipants;
            }
            catch
            {
                throw;
            }
        }

        #region Admin stuff

        /// <summary>
        /// Gets profile messages
        /// </summary>
        /// <param name="parameterFilter"></param>
        /// <exception cref="ArgumentException">Current user is null or does not have admin status.</exception>
        /// <returns></returns>
        [NoCache]
        [HttpGet("~/ProfileMessages/{profileId}")]
        public async Task<IEnumerable<MessageModel>> ProfileMessages(string profileId, [FromQuery] ParameterFilter parameterFilter)
        {
            try
            {
                var currentUser = await _helper.GetCurrentUserByAuth0Id(User);

                if (currentUser == null || !currentUser.Admin)
                {
                    throw new ArgumentException($"Current user is null or does not have admin status.");
                }

                var skip = parameterFilter.PageIndex == 0 ? parameterFilter.PageIndex : parameterFilter.PageIndex * parameterFilter.PageSize;

                var messages = await _profileRepository.GetProfileMessages(profileId, skip, parameterFilter.PageSize);

                foreach (var message in messages)
                {
                    message.Message = _cryptography.Decrypt(message.Message);
                }

                return messages;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Gets the specified message based on a filter. Eg. { Message: 'something' }
        /// </summary>
        /// <param name="parameterFilter"></param>
        /// <exception cref="ArgumentException">Current user is null or does not have admin status.</exception>
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

                var messages = await _profileRepository.GetChatsByFilter(requestBody.ChatFilter, skip, parameterFilter.PageSize) ?? throw new ArgumentException($"Cannot find any matching chats.", nameof(requestBody.ChatFilter));

                foreach (var message in messages)
                {
                    message.Message = _cryptography.Decrypt(message.Message);
                }

                return messages;

            }
            catch
            {
                throw;
            }
        }

        /// <summary>Set messages as Do Not Delete</summary>
        /// <param name="messages"></param>
        /// <exception cref="ArgumentException">Current user is null or does not have admin status.</exception>
        /// <exception cref="ArgumentException">Messages is null {requestBody.ChatFilter}</exception>
        /// <exception cref="ArgumentException">Cannot find any matching chats. {requestBody.ProfileFilter}</exception>
        [NoCache]
        [HttpPost("~/DoNotDelete")]
        public async Task DoNotDelete([FromBody] MessageModel[] messages)
        {
            if (messages == null || messages.Length < 1) throw new ArgumentException($"Messages is null");

            try
            {
                var currentUser = await _helper.GetCurrentUserByAuth0Id(User);

                if (currentUser == null || !currentUser.Admin)
                {
                    throw new ArgumentException($"Current user is null or does not have admin status.");
                }

                await _profileRepository.DoNotDelete(messages, true);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>Allow Deleteof messages</summary>
        /// <param name="messages"></param>
        /// <exception cref="ArgumentException">Current user is null or does not have admin status.</exception>
        /// <exception cref="ArgumentException">Messages is null {requestBody.ChatFilter}</exception>
        /// <exception cref="ArgumentException">Cannot find any matching chats. {requestBody.ProfileFilter}</exception>
        [NoCache]
        [HttpPost("~/AllowDelete")]
        public async Task AllowDelete([FromBody] MessageModel[] messages)
        {
            if (messages == null || messages.Length < 1) throw new ArgumentException($"Messages is null");

            try
            {
                var currentUser = await _helper.GetCurrentUserByAuth0Id(User);

                if (currentUser == null || !currentUser.Admin)
                {
                    throw new ArgumentException($"Current user is null or does not have admin status.");
                }

                await _profileRepository.DoNotDelete(messages, false);
            }
            catch
            {
                throw;
            }
        }

        #endregion

        #region Maintenance

        ///// <summary>Deletes Message that are more than 30 days old.</summary>
        ///// <returns></returns>
        //[NoCache]
        //[HttpDelete("~/DeleteOldMessages")]
        //[ProducesResponseType((int)HttpStatusCode.NoContent)]
        //public async Task<IActionResult> DeleteOldMessages()
        //{
        //    try
        //    {
        //        //var oldMessages = await _profileRepository.DeleteOldMessages(); // TODO: Needs to be testet.

        //        return NoContent();
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //}

        #endregion
    }
}