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
        Task<CurrentUser> GetCurrentUserByAuth0Id(string auth0Id);
        Task<Profile> GetDestinataryProfileByProfileId(string profileId);
        Task SaveMessage(MessageModel message);
        Task NotifyNewChatMember(CurrentUser currentUser, Profile destinataryProfile);
        Task<IEnumerable<MessageModel>> GetMessages(string currentUserProfileId, string profileId);
        Task MessagesSeen(ObjectId messagesId);
        int TotalUnreadMessages(string chatMemberId, string profileId);
        Task<IEnumerable<MessageModel>> GetProfileMessages(string profileId);
        Task<IEnumerable<MessageModel>> GetChatsByFilter(ChatFilter chatFilter, int skip, int limit);
        Task DoNotDelete(MessageModel[] messages, bool doNotDelete);
        Task<DeleteResult> DeleteOldMessages();
    }
}
