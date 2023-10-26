using Juno.Model;
using MongoDB.Bson;
using System;
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
        Task<IEnumerable<MessageModel>> GetGroupMessages(string groupId);
        Task<GroupModel> GetGroup(string groupId);
        Task<IEnumerable<GroupModel>> GetGroups(string[] groupIds);
        Task SaveLastMessagesSeen(CurrentUser currentUser, string groupId, DateTime? lastDateSeen);
        Task SaveLastGroupMessagesSeen(CurrentUser currentUser, string groupId, DateTime? lastDateSeen);
        int TotalUnreadMessages(bool groupMessage, string fromId, string toId, DateTime? lastDateSeen);
        Task<IEnumerable<MessageModel>> GetProfileMessages(string profileId, int skip, int limit);
        Task<IEnumerable<MessageModel>> GetChatsByFilter(ChatFilter chatFilter, int skip, int limit);
        Task DoNotDelete(MessageModel[] messages, bool doNotDelete);
    }
}
