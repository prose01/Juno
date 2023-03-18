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
    public class ChatHub : Hub
    {
        /// TODO: Kig på https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client
        /// https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-5.0
        /// 

        private readonly IProfilesRepository _profileRepository;
        public static List<ParticipantResponseViewModel> AllConnectedParticipants { get; set; } = new List<ParticipantResponseViewModel>();
        private static List<ParticipantResponseViewModel> DisconnectedParticipants { get; set; } = new List<ParticipantResponseViewModel>();
        private object ParticipantsConnectionLock = new object();

        private readonly ICryptography _cryptography;

        public ChatHub(IProfilesRepository profileRepository, ICryptography cryptography)
        {
            _profileRepository = profileRepository;
            _cryptography = cryptography;
        }

        public static IEnumerable<ParticipantResponseViewModel> ConnectedParticipants(string currentUserId)
        {
            return AllConnectedParticipants
                .Where(x => x.Participant.Id != currentUserId);
        }

        public void Join(string userName)
        {
            try
            {
                lock (ParticipantsConnectionLock)
                {
                    var currentUserProfileId = _profileRepository.GetCurrentProfileIdByAuth0Id(Context.UserIdentifier).Result;

                    var oldConnectedParticipants = AllConnectedParticipants.Where(x => x.Participant.Id == currentUserProfileId);

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
                                DisplayName = userName,
                                Id = currentUserProfileId,
                                Status = 0
                            }
                        });
                    }

                    // This will be used as the user's unique ID to be used on ng-chat as the connected user.
                    // You should most likely use another ID on your application
                    Clients.Caller.SendAsync("generatedUserId", currentUserProfileId);

                    Clients.All.SendAsync("friendsListChanged", AllConnectedParticipants);
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
                var sender = AllConnectedParticipants.Find(x => x.Participant.Id == message.FromId);

                var destinataryProfile = await _profileRepository.GetDestinataryProfileByProfileId(message.ToId);
                var currentUser = await _profileRepository.GetCurrentUserByAuth0Id(Context.UserIdentifier);

                // If currentUser is on the destinataryProfile's ChatMemberslist AND is blocked then do not go any further.
                if (!destinataryProfile.ChatMemberslist.Any(m => m.ProfileId == currentUser.ProfileId && m.Blocked) || currentUser.Admin)
                {
                    message.ToId = destinataryProfile.ProfileId;
                    message.ToName = destinataryProfile.Name;
                    message.FromName = currentUser.Name;
                    message.DoNotDelete = false;

                    if (sender != null)
                    {
                        await Clients.Group(message.ToId).SendAsync("messageReceived", sender.Participant, message);
                    }

                    // Messages should be encrypted before storing to database.
                    var encryptedMessage = _cryptography.Encrypt(message.Message);
                    message.Message = encryptedMessage;

                    await _profileRepository.SaveMessage(message);
                    await _profileRepository.NotifyNewChatMember(currentUser, destinataryProfile);
                }

                // See https://github.com/rpaschoal/ng-chat-netcoreapp/blob/master/NgChatSignalR/ChatHub.cs7
            }
            catch
            {
                throw;
            }
        }

        public Task OnDisconnectedAsync(Exception? exception)
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
