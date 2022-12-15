using Juno.Model;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Juno.Interfaces
{
    public interface IHelperMethods
    {
        Task<string> GetCurrentUserProfileId(ClaimsPrincipal user);
        Task<CurrentUser> GetCurrentUserByAuth0Id(ClaimsPrincipal user);
        List<MessageModel> mockMessageHistorylist { get; set; }
    }
}
