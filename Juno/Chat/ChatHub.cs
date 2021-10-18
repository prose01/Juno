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
        private static List<ParticipantResponseViewModel> AllConnectedParticipants { get; set; } = new List<ParticipantResponseViewModel>();
        private static List<ParticipantResponseViewModel> DisconnectedParticipants { get; set; } = new List<ParticipantResponseViewModel>();
        private object ParticipantsConnectionLock = new object();

        private readonly IHelperMethods _helper;
        private readonly ICryptography _cryptography;

        public ChatHub(IProfilesRepository profileRepository, IHelperMethods helperMethod, ICryptography cryptography)
        {
            _profileRepository = profileRepository;
            _helper = helperMethod;
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
                    var oldConnectedParticipants = AllConnectedParticipants.Where(x => x.Participant.Id == Context.UserIdentifier);

                    if (oldConnectedParticipants.Count() == 0)
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
                                Id = Context.UserIdentifier
                            }
                        });
                    }

                    // This will be used as the user's unique ID to be used on ng-chat as the connected user.
                    // You should most likely use another ID on your application
                    Clients.Caller.SendAsync("generatedUserId", Context.UserIdentifier);

                    Clients.All.SendAsync("friendsListChanged", AllConnectedParticipants);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task SendMessage(MessageModel message)
        {
            try
            {
                var sender = AllConnectedParticipants.Find(x => x.Participant.Id == message.FromId);

                if (sender != null)
                {
                    var destinataryProfile = await _profileRepository.GetDestinataryProfileByAuth0Id(message.ToId);
                    var currentUserProfileId = await _profileRepository.GetCurrentProfileIdByAuth0Id(Context.UserIdentifier);

                    // If currentUser is on the destinataryProfile's ChatMemberslist AND is blocked then do not go any further. // TODO: If currenUser is Admin, send message anyway.
                    if (!destinataryProfile.ChatMemberslist.Any(m => m.ProfileId == currentUserProfileId && m.Blocked))
                    {
                        var encryptedMessage = _cryptography.Encrypt(message.Message);
                        message.Message = encryptedMessage;

                        await _profileRepository.SaveMessage(message);
                        await _profileRepository.NotifyNewChatMember(Context.UserIdentifier, destinataryProfile.Auth0Id);

                        await Clients.Group(message.ToId).SendAsync("messageReceived", sender.Participant, message);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                lock (ParticipantsConnectionLock)
                {
                    var connectionIndex = AllConnectedParticipants.FindIndex(x => x.Participant.Id == Context.UserIdentifier);

                    if (connectionIndex >= 0)
                    {
                        var participant = AllConnectedParticipants.ElementAt(connectionIndex);

                        AllConnectedParticipants.Remove(participant);
                        DisconnectedParticipants.Add(participant);

                        Clients.All.SendAsync("friendsListChanged", AllConnectedParticipants);
                    }

                    return base.OnDisconnectedAsync(exception);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override Task OnConnectedAsync()
        {
            try
            {
                lock (ParticipantsConnectionLock)
                {
                    Groups.AddToGroupAsync(Context.ConnectionId, Context.UserIdentifier);

                    var connectionIndex = DisconnectedParticipants.FindIndex(x => x.Participant.Id == Context.UserIdentifier);

                    if (connectionIndex >= 0)
                    {
                        var participant = DisconnectedParticipants.ElementAt(connectionIndex);

                        DisconnectedParticipants.Remove(participant);
                        AllConnectedParticipants.Add(participant);

                        Clients.All.SendAsync("friendsListChanged", AllConnectedParticipants);
                    }

                    return base.OnConnectedAsync();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
