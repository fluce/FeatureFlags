using System.Threading;

namespace FeatureFlags
{
    public class AmbiantContextProvider : IFeatureContextProvider
    {
        private static readonly AsyncLocal<FeatureContext> AsyncLocalFeatureContext = new AsyncLocal<FeatureContext>();

        public FeatureContext GetContext()
        {
            var a = AsyncLocalFeatureContext.Value;
            if (a != null)
            {
                return a;
            }
            a=new FeatureContext();
            AsyncLocalFeatureContext.Value = a;
            return a;
        }

        public void SetContext(FeatureContext context)
        {
            AsyncLocalFeatureContext.Value = context;
        }
    }
}