using Juno.Model;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Juno.Interfaces
{
    public interface IProfilesRepository
    {
        Task<string> GetCurrentProfileIdByAuth0Id(string auth0Id);
        Task<Profile> GetDestinataryProfileByAuth0Id(string auth0Id);
        Task<IEnumerable<Profile>> GetChatMemberslist(string auth0Id);
        Task SaveMessage(MessageModel message);
        Task NotifyNewChatMember(string currentUserProfileId, string destinataryProfileId);
        Task<IEnumerable<MessageModel>> GetMessages(string currentUserAuth0Id, string auth0Id);
        Task<DeleteResult> DeleteOldMessages();
    }
}
