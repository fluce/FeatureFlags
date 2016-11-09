using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeatureFlags
{
    public interface IWatchdog
    {
        Task<List<string>> GetActiveNodes();
    }
}