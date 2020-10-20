using Juno.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Juno.Helpers
{
    public class NameUserIdProvider : IUserIdProvider
    {
        private readonly string _nameidentifier;

        public NameUserIdProvider(IOptions<Settings> settings)
        {
            _nameidentifier = settings.Value.auth0Id;
        }

        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User.Claims.FirstOrDefault(c => c.Type == _nameidentifier)?.Value;
        }
    }
}
