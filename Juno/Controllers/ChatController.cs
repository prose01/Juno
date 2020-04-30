using Juno.Infrastructure;
using Juno.Interfaces;
using Juno.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                Id = "1",
                DisplayName = "Arya Stark",
                Avatar = "https://66.media.tumblr.com/avatar_9dd9bb497b75_128.pnj",
                Status = 0
            });

            chatParticipants.Add(new ChatParticipantViewModel()
            {
                ParticipantType = ChatParticipantTypeEnum.User,
                Id = "1",
                DisplayName = "Cersei Lannister",
                Avatar = "https://thumbnail.myheritageimages.com/502/323/78502323/000/000114_884889c3n33qfe004v5024_C_64x64C.jpg",
                Status = 1
            });

            chatParticipants.Add(new ChatParticipantViewModel()
            {
                ParticipantType = ChatParticipantTypeEnum.User,
                Id = "1",
                DisplayName = "Daenerys Targaryen",
                Avatar = "https://68.media.tumblr.com/avatar_d28d7149f567_128.png",
                Status = 2
            });

            return chatParticipants;
        }

        [NoCache]
        [HttpPost("~/ParticipantResponses")]
        public async Task<IEnumerable<ParticipantResponseViewModel>> ParticipantResponsesAsync()
        {

            List<ParticipantResponseViewModel> participantResponses = new List<ParticipantResponseViewModel> { };

            var currentUser = await _helper.GetCurrentUserProfile(User);

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
    }
}