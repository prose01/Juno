using Juno.Model;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Juno.Interfaces
{
    public interface ICurrentUserRepository
    {
        Task<CurrentUser> GetCurrentProfileByAuth0Id(string auth0Id);
    }
}
