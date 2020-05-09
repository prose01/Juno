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
        private static List<ParticipantResponseViewModel> AllConnectedParticipants { get; set; } = new List<ParticipantResponseViewModel>();
        private static List<ParticipantResponseViewModel> DisconnectedParticipants { get; set; } = new List<ParticipantResponseViewModel>();
        private object ParticipantsConnectionLock = new object();

        private readonly IHelperMethods _helper;

        public ChatHub(IHelperMethods helperMethod)
        {
            _helper = helperMethod;
        }

        public static IEnumerable<ParticipantResponseViewModel> ConnectedParticipants(string currentUserId)
        {
            return AllConnectedParticipants
                .Where(x => x.Participant.Id != currentUserId);
        }

        public void Join(string userName)
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
                            Id = Context.UserIdentifier,
                            //Auth0Id = Context.UserIdentifier
                        }
                    });
                }

                // This will be used as the user's unique ID to be used on ng-chat as the connected user.
                // You should most likely use another ID on your application
                Clients.Caller.SendAsync("generatedUserId", Context.UserIdentifier);

                Clients.All.SendAsync("friendsListChanged", AllConnectedParticipants);
            }
        }

        public void SendMessage(MessageViewModel message)
        {
            var sender = AllConnectedParticipants.Find(x => x.Participant.Id == message.FromId);
            //var receiver = AllConnectedParticipants.Find(x => x.Participant.Id == message.ToId);

            if (sender != null)
            {
                // Set From Auth0Id
                //message.FromAuth0Id = Context.UserIdentifier;

                // Set To Auth0Id
                //message.ToAuth0Id = receiver.Participant.Auth0Id;

                // Save to databaes
                _helper.mockMessageHistorylist.Add(message);
                //Clients.Client(message.ToId).SendAsync("messageReceived", sender.Participant, message);

                Clients.Group(message.ToId).SendAsync("messageReceived", sender.Participant, message);
                //Clients.All.SendAsync("messageReceived", sender.Participant, message);
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
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

        public override Task OnConnectedAsync()
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
    }
}
