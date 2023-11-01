using Juno.Interfaces;
using Juno.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Juno.Chat
{
    [Authorize]
    public class GroupChatHub : Hub
    {
        private readonly IProfilesRepository _profileRepository;
        public static List<ParticipantResponseViewModel> AllConnectedParticipants { get; set; } = new List<ParticipantResponseViewModel>();
        private static List<ParticipantResponseViewModel> DisconnectedParticipants { get; set; } = new List<ParticipantResponseViewModel>();
        private static List<GroupChatParticipantViewModel> AllGroupParticipants { get; set; } = new List<GroupChatParticipantViewModel>();
        private object ParticipantsConnectionLock = new object();

        private readonly ICryptography _cryptography;

        public GroupChatHub(IProfilesRepository profileRepository, ICryptography cryptography)
        {
            _profileRepository = profileRepository;
            _cryptography = cryptography;
        }

        private static IEnumerable<ParticipantResponseViewModel> FilteredGroupParticipants(string currentUserId)
        {
            return AllConnectedParticipants
                .Where(p => p.Participant.ParticipantType == ChatParticipantTypeEnum.User
                       || AllGroupParticipants.Any(g => g.Id == p.Participant.Id && g.ChattingTo.Any(u => u.Id == currentUserId))
                );
        }

        public static IEnumerable<ParticipantResponseViewModel> ConnectedParticipants(string currentUserId)
        {
            return FilteredGroupParticipants(currentUserId).Where(x => x.Participant.Id != currentUserId);
        }

        public void Join()
        {
            try
            {
                lock (ParticipantsConnectionLock)
                {
                    var currentUser = _profileRepository.GetCurrentUserByAuth0Id(Context.UserIdentifier).Result;

                    var oldConnectedParticipants = AllConnectedParticipants.Where(x => x.Participant.Id == currentUser.ProfileId);

                    if (!oldConnectedParticipants.Any())
                    {
                        AllConnectedParticipants.Add(new ParticipantResponseViewModel()
                        {
                            Metadata = new ParticipantMetadataViewModel()
                            {
                                TotalUnreadMessages = 0
                            },
                            Participant = new ChatParticipantViewModel()
                            {
                                ParticipantType = ChatParticipantTypeEnum.User,
                                Id = currentUser.ProfileId,
                                DisplayName = currentUser.Name,
                                Initials = currentUser.Avatar.Initials,
                                InitialsColour = currentUser.Avatar.InitialsColour,
                                CircleColour = currentUser.Avatar.CircleColour,
                                Status = oldConnectedParticipants.Any() ? 0 : 3
                            }
                        });
                    }

                    var groups = _profileRepository.GetGroups(currentUser.Groups?.Keys.ToArray()).Result;

                    if (groups != null)
                    {
                        foreach (var group in groups)
                        {
                            var groupParticipant = AllGroupParticipants.Find(x => x.Id == group.GroupId);

                            if (groupParticipant == null)
                            {
                                groupParticipant = new GroupChatParticipantViewModel()
                                {
                                    ParticipantType = ChatParticipantTypeEnum.Group,
                                    Id = group.GroupId,
                                    DisplayName = group.Name,
                                    Initials = group.Avatar.Initials,
                                    InitialsColour = group.Avatar.InitialsColour,
                                    CircleColour = group.Avatar.CircleColour,
                                    Status = 3, // TODO: Look in oldConnectedParticipants if any group members are there
                                    ChattingTo = new List<ChatParticipantViewModel>()
                                };

                                AllGroupParticipants.Add(groupParticipant);

                                groupParticipant.ChattingTo.Add(new ChatParticipantViewModel()
                                {
                                    Id = currentUser.ProfileId,   // Use the same Participant as earlier line 61. The user should be added to the groups participants list (AllGroupParticipants) and later the groups should be added to the over participant list (AllConnectedParticipants)
                                    Status = 0
                                });

                                AllConnectedParticipants.Add(new ParticipantResponseViewModel()
                                {
                                    Metadata = new ParticipantMetadataViewModel()
                                    {
                                        TotalUnreadMessages = 0
                                    },
                                    Participant = groupParticipant
                                });
                            }
                            else
                            {
                                groupParticipant.ChattingTo.Add(new ChatParticipantViewModel()
                                {
                                    Id = currentUser.ProfileId,   // Use the same Participant as earlier line 61. The user should be added to the groups participants list (AllGroupParticipants) and later the groups should be added to the over participant list (AllConnectedParticipants)
                                    Status = 0
                                });
                            }
                        }
                    }                    

                    // This will be used as the user's unique ID to be used on ng-chat as the connected user.
                    // You should most likely use another ID on your application
                    Clients.Caller.SendAsync("generatedUserId", currentUser.ProfileId);
                }
            }
            catch
            {
                throw;
            }
        }

        public async Task SendMessage(MessageModel message)
        {
            try
            {
                // Look into using enums instead of string. This is ugly :)
                if(message.ParticipantType.ToLower() == "group") 
                {
                    var sender = AllConnectedParticipants.Find(x => x.Participant.Id == message.FromId);

                    var destinataryGroup = await _profileRepository.GetGroup(message.ToId);
                    var currentUser = await _profileRepository.GetCurrentUserByAuth0Id(Context.UserIdentifier);

                    // If currentUser is blocked on Grouplist then do not go any further.
                    if (!destinataryGroup.GroupMemberslist.Any(m => m.ProfileId == currentUser.ProfileId && m.Blocked) || currentUser.Admin)     // TODO: This needs to check if currentUser is blocked from group
                    {
                        message.MessageType = MessageType.Group;
                        message.ToId = destinataryGroup.GroupId;
                        message.ToName = destinataryGroup.Name;
                        message.FromName = currentUser.Name;
                        message.DoNotDelete = false;
                        message.ParticipantType = ChatParticipantTypeEnum.Group.ToString();

                        if (sender != null)
                        {
                            var groupDestinatary = AllGroupParticipants.Where(x => x.Id == message.ToId).FirstOrDefault();

                            if (groupDestinatary != null)
                            {
                                // Notify all users in the group except the sender
                                var usersInGroupToNotify = AllConnectedParticipants
                                                           .Where(p => p.Participant.Id != sender.Participant.Id
                                                                  && groupDestinatary.ChattingTo.Any(g => g.Id == p.Participant.Id)
                                                           )
                                                           .Select(g => g.Participant.Id);

                                await Clients.Groups(usersInGroupToNotify.ToList()).SendAsync("messageReceived", groupDestinatary, message);
                            }
                        }

                        // Messages should be encrypted before storing to database.
                        var encryptedMessage = _cryptography.Encrypt(message.Message);
                        message.Message = encryptedMessage;

                        await _profileRepository.SaveMessage(message);
                    }
                }
                else
                {
                    var sender = AllConnectedParticipants.Find(x => x.Participant.Id == message.FromId);

                    var destinataryProfile = await _profileRepository.GetDestinataryProfileByProfileId(message.ToId);
                    var currentUser = await _profileRepository.GetCurrentUserByAuth0Id(Context.UserIdentifier);

                    // If currentUser is blocked on Grouplist then do not go any further.
                    if (!destinataryProfile.ChatMemberslist.Any(m => m.ProfileId == currentUser.ProfileId && m.Blocked) || currentUser.Admin)     // TODO: This needs to check if currentUser is blocked from group
                    {
                        message.MessageType = MessageType.PrivateMessage;
                        message.ToId = destinataryProfile.ProfileId;
                        message.ToName = destinataryProfile.Name;
                        message.FromName = currentUser.Name;
                        message.DoNotDelete = false;

                        if (sender != null)
                        {
                            await Clients.Group(message.ToId).SendAsync("updateCurrentUserSubject");
                            await Clients.Group(message.ToId).SendAsync("messageReceived", sender.Participant, message);
                        }

                        // Messages should be encrypted before storing to database.
                        var encryptedMessage = _cryptography.Encrypt(message.Message);
                        message.Message = encryptedMessage;

                        await _profileRepository.SaveMessage(message);
                        await _profileRepository.NotifyNewChatMember(currentUser, destinataryProfile); 
                    }
                }                
            }
            catch
            {
                throw;
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                lock (ParticipantsConnectionLock)
                {
                    var currentUserProfileId = _profileRepository.GetCurrentProfileIdByAuth0Id(Context.UserIdentifier).Result;

                    var connectionIndex = AllConnectedParticipants.FindIndex(x => x.Participant.Id == currentUserProfileId);

                    if (connectionIndex >= 0)
                    {
                        var participant = AllConnectedParticipants.ElementAt(connectionIndex);

                        var groupsParticipantIsIn = AllGroupParticipants.Where(x => x.ChattingTo.Any(u => u.Id == participant.Participant.Id));

                        AllConnectedParticipants.RemoveAll(x => groupsParticipantIsIn.Any(g => g.Id == x.Participant.Id));
                        AllGroupParticipants.RemoveAll(x => groupsParticipantIsIn.Any(g => g.Id == x.Id));

                        AllConnectedParticipants.Remove(participant);
                        DisconnectedParticipants.Add(participant);

                        Clients.All.SendAsync("friendsListChanged", AllConnectedParticipants);
                    }

                    Clients.All.SendAsync("UserIsOffline", currentUserProfileId);

                    return base.OnDisconnectedAsync(exception);
                }
            }
            catch
            {
                throw;
            }
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                lock (ParticipantsConnectionLock)
                {
                    var currentUserProfileId = _profileRepository.GetCurrentProfileIdByAuth0Id(Context.UserIdentifier).Result;

                    Groups.AddToGroupAsync(Context.ConnectionId, currentUserProfileId);

                    var connectionIndex = DisconnectedParticipants.FindIndex(x => x.Participant.Id == currentUserProfileId);

                    if (connectionIndex >= 0)
                    {
                        var participant = DisconnectedParticipants.ElementAt(connectionIndex);

                        DisconnectedParticipants.Remove(participant);
                        AllConnectedParticipants.Add(participant);

                        Clients.All.SendAsync("friendsListChanged", AllConnectedParticipants);
                    }

                    Clients.All.SendAsync("UserIsOnline", currentUserProfileId);

                    return base.OnConnectedAsync();
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
