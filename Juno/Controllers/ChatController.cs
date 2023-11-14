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
                var currentUser = await _helper.GetCurrentUserByAuth0Id(User);

                if (currentUser == null)
                {
                    throw new ArgumentException($"Current user is null.");
                }

                List<ParticipantResponseViewModel> participantResponses = new List<ParticipantResponseViewModel> { };

                if (currentUser.Bookmarks.Count > 0)
                {
                    List<ChatParticipantViewModel> chatParticipants = new List<ChatParticipantViewModel> { };

                    foreach (var profile in currentUser.Bookmarks)
                    {
                        if (!profile.Blocked)
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
                    }

                    foreach (var friend in chatParticipants)
                    {
                        participantResponses.Add(new ParticipantResponseViewModel()
                        {
                            Participant = friend,
                            Metadata = new ParticipantMetadataViewModel { TotalUnreadMessages = 0 }
                        });
                    }
                }

                var groups = await _profileRepository.GetGroups(currentUser.Groups?.Keys.ToArray());

                if (groups != null)
                {
                    List<ChatParticipantViewModel> participantGroup = new List<ChatParticipantViewModel> { };

                    foreach (var group in groups)
                    {
                        if (group.GroupMemberslist.Where(x => x.ProfileId == currentUser.ProfileId && x.Blocked == true).Count() > 0)
                            continue;

                        var set1 = new HashSet<string>(GroupChatHub.AllConnectedParticipants.Where(x => x.Participant.Id != currentUser.ProfileId).Select(x => x.Participant.Id));
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
                        participantResponses.Add(new ParticipantResponseViewModel()
                        {
                            Participant = group,
                            Metadata = new ParticipantMetadataViewModel { TotalUnreadMessages = 0 }
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
                var currentUser = await _helper.GetCurrentUserByAuth0Id(User);

                if (currentUser == null)
                {
                    throw new ArgumentException($"Current user is null.");
                }

                if (chatparticipant.ParticipantType == ChatParticipantTypeEnum.Group && currentUser.Groups.ContainsKey(chatparticipant.Id))
                {
                    var messages = await _profileRepository.GetGroupMessages(chatparticipant.Id);

                    foreach (var message in messages)
                    {
                        message.Message = _cryptography.Decrypt(message.Message);
                    }

                    // Save last message seen date with user group
                    if (messages.Any())
                    {
                        _ = _profileRepository.SaveLastGroupMessagesSeen(currentUser, messages.LastOrDefault().ToId, DateTime.Now);
                    }

                    return messages;
                }
                else
                {
                    var destinataryProfile = await _profileRepository.GetDestinataryProfileByProfileId(chatparticipant.Id);

                    if (destinataryProfile != null)
                    {
                        var messages = await _profileRepository.GetMessages(currentUser.ProfileId, destinataryProfile.ProfileId);

                        foreach (var message in messages)
                        {
                            message.Message = _cryptography.Decrypt(message.Message);
                        }

                        if (messages.Any())
                        {
                            // Save last message seen date with user
                            _ = _profileRepository.SaveLastMessagesSeen(currentUser, messages.LastOrDefault().FromId, DateTime.Now);
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
        [HttpPost("~/UnreadMessages")]
        public async Task<Dictionary<string, int>> UnreadMessages()
        {
            try
            {
                var currentUser = await _helper.GetCurrentUserByAuth0Id(User);

                if (currentUser == null)
                {
                    throw new ArgumentException($"Current user is null.");
                }

                Dictionary<string, int> unreadMessages = new Dictionary<string, int>();

                foreach (var profile in currentUser.Bookmarks)
                {
                    unreadMessages.Add(profile.ProfileId, _profileRepository.TotalUnreadMessages(false, profile.ProfileId, currentUser.ProfileId, profile.LastMessagesSeen));
                }

                foreach (KeyValuePair<string, DateTime?> group in currentUser.Groups)
                {
                    unreadMessages.Add(group.Key, _profileRepository.TotalUnreadMessages(true, currentUser.ProfileId, group.Key, group.Value));
                }

                return unreadMessages;
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

                if (currentUser == null)
                {
                    throw new ArgumentException($"Current user is null.");
                }

                var groups = await _profileRepository.GetGroups(currentUser.Groups?.Keys.ToArray());

                List<GroupChatParticipantViewModel> allGroupParticipants = new List<GroupChatParticipantViewModel>();
                List<ChatParticipantViewModel> chatParticipants = new List<ChatParticipantViewModel> { };

                foreach (var group in groups)
                {
                    var oldConnectedParticipants = GroupChatHub.AllConnectedParticipants.Where(x => x.Participant.Id == currentUser.ProfileId);

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
    }
}