using Juno.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Juno.Interfaces
{
    public interface IProfilesRepository
    {
        Task<CurrentUser> GetCurrentProfileByAuth0Id(string auth0Id);
        Task<IEnumerable<Profile>> GetBookmarkedProfiles(CurrentUser profile);
    }
}
