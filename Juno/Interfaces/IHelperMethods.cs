using Juno.Model;
using System.Collections.Generic;
using System.Security.Claims;

namespace Juno.Interfaces
{
    public interface IHelperMethods
    {
        string GetCurrentUserProfile(ClaimsPrincipal user);
        List<MessageModel> mockMessageHistorylist { get; set; }
    }
}
