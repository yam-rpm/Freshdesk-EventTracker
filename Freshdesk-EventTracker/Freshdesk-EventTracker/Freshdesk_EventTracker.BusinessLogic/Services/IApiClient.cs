using System.Threading;
using System.Threading.Tasks;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public interface IApiClient
    {
        Task<T?> GetAsync<T>(
            string endpoint,
            CancellationToken ct = default);
    }
}