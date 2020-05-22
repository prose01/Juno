using Juno.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Juno.Interfaces
{
    public interface IProfilesRepository
    {
        Task<CurrentUser> GetCurrentProfileByAuth0Id(string auth0Id);
        Task<Profile> GetDestinataryProfileByAuth0Id(string auth0Id);
        Task<IEnumerable<Profile>> GetChatMemberslist(CurrentUser profile);
        Task SaveMessage(MessageViewModel message);
        Task NotifyNewChatMember(string currentUserProfileId, string destinataryProfileId);
        Task<IEnumerable<MessageViewModel>> GetMessages(string currentUserAuth0Id, string auth0Id);
    }
}
