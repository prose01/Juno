using Juno.Model;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Juno.Interfaces
{
    public interface IHelperMethods
    {
        Task<CurrentUser> GetCurrentUserProfile(ClaimsPrincipal user);

        string GetCurrentUserAuth0Id(ClaimsPrincipal user);
    }
}
