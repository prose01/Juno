using Juno.Model;
using System.Collections.Generic;
using System.Security.Claims;

namespace Juno.Interfaces
{
    public interface IHelperMethods
    {
        string GetCurrentUserProfile(ClaimsPrincipal user);
        List<MessageViewModel> mockMessageHistorylist { get; set; }
    }
}
