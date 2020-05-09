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
        private readonly IHelperMethods _helper;

        public ChatController(IHelperMethods helperMethods)
        {
            _helper = helperMethods;
        }

        [NoCache]
        [HttpPost("~/ListFriends")]
        public async Task<IEnumerable<ChatParticipantViewModel>> ListFriends()
        {
            List<ChatParticipantViewModel> chatParticipants = new List<ChatParticipantViewModel> { };

            chatParticipants.Add(new ChatParticipantViewModel()
            {
                ParticipantType = ChatParticipantTypeEnum.User,
                Id = "5FfGkM_QBhkO8jGRHddP5g",
                Auth0Id = "auth0|5dcbcd3c1e0b6c0e8b05a5e2",
                DisplayName = "Peter",
                Avatar = "",
                Status = 0
            });

            chatParticipants.Add(new ChatParticipantViewModel()
            {
                ParticipantType = ChatParticipantTypeEnum.User,
                Id = "Lm7l7IsFvIACg6RRqRZKXg",
                Auth0Id = "auth0|5c62f8a596979e1735449127",
                DisplayName = "Kurt Hansen",
                Avatar = "",
                Status = 1
            });

            chatParticipants.Add(new ChatParticipantViewModel()
            {
                ParticipantType = ChatParticipantTypeEnum.User,
                Id = "E7oelfGokauTlMWiw9RFSA",
                Auth0Id = "auth0|5dcbu65c1e0b6c0e8bste5e2",
                DisplayName = "Daenerys Targaryen",
                Avatar = "",
                Status = 2
            });

            return chatParticipants;
        }

        [NoCache]
        [HttpPost("~/ParticipantResponses")]
        public async Task<IEnumerable<ParticipantResponseViewModel>> ParticipantResponsesAsync()
        {

            List<ParticipantResponseViewModel> participantResponses = new List<ParticipantResponseViewModel> { };

            //var currentUser = await _helper.GetCurrentUserProfile(User);

            var friends = await this.ListFriends();

            foreach (var friend in friends)
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