using Juno.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Juno.Interfaces
{
    public interface IProfilesRepository
    {
        Task<string> GetCurrentProfileIdByAuth0Id(string auth0Id);
        Task<Profile> GetDestinataryProfileByProfileId(string profileId);
        Task<IEnumerable<Profile>> GetChatMemberslist(string profileId);
        Task SaveMessage(MessageModel message);
        Task NotifyNewChatMember(string currentUserProfileId, Profile destinataryProfile);
        Task<IEnumerable<MessageModel>> GetMessages(string currentUserProfileId, string profileId);
        Task MessagesSeen(ObjectId messagesId);
        int TotalUnreadMessages(string profileId);
        Task<DeleteResult> DeleteOldMessages();
    }
}
