using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FeatureFlags.AspNetCore
{
    public class FeatureFlagsConfig
    {
        public IConfigurationSection BootstrapConfigurationSection { get; set; }

        public IFeatureStore Store { get; set; }

        public Dictionary<Type,Type> AdditionnalAccessors { get; } = new Dictionary<Type, Type>();

    }

    public static class FeatureFlagsConfigExtension
    {
        public static FeatureFlagsConfig BootstrapFromConfig(this FeatureFlagsConfig config, IConfigurationSection section)
        {
            config.BootstrapConfigurationSection = section;
            return config;
        }

        public static FeatureFlagsConfig AddAccessor<T>(this FeatureFlagsConfig config) where T:class
        {
            config.AdditionnalAccessors.Add(typeof (T), FeatureFlagAccessor.Build<T>());
            return config;
        }
    }

    public static class FeatureFlagsExtension
    {
        public static void AddFeatureFlags(this IServiceCollection services, Action<FeatureFlagsConfig> config)
        {
            var theConfig = new FeatureFlagsConfig();
            config(theConfig);

            if (theConfig.BootstrapConfigurationSection != null)
            {
                var dstore = (IDynamicFeatureStore)theConfig.Store;
                dstore.BootstrapFromConfig(theConfig.BootstrapConfigurationSection);
            }

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IFeatureStore>(theConfig.Store);
            services.AddSingleton<IFeatures,Features>();

            foreach (var additionnalAccessor in theConfig.AdditionnalAccessors)
            {
                services.AddSingleton(additionnalAccessor.Key, additionnalAccessor.Value);
            }
        }

        public static IDynamicFeatureStore BootstrapFromConfig(this IDynamicFeatureStore store, IConfigurationSection config)
        {
            foreach (var c in config.GetChildren())
            {
                if (store.GetFeatureFlagDefinition(c.Key) == null)
                    store.SetFeatureFlagDefinition(new FeatureFlagDefinition() { Name=c.Key, Definition = c.Value} );
            }
            return store;
        }


    }

    public class AspNetCoreFeatureContextProvider<T> : IFeatureContextProvider where T:class
    {
        public IHttpContextAccessor HttpContextAccessor { get; set; }
        public UserManager<T> UserManager { get; set; }
        public SignInManager<T> SignInManager { get; set; }

        public AspNetCoreFeatureContextProvider(IHttpContextAccessor httpContextAccessor, UserManager<T> userManager, SignInManager<T> signInManager )
        {
            HttpContextAccessor = httpContextAccessor;
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public virtual void FillContext(FeatureContext context, T user)
        {
            
        }


        public FeatureContext GetContext()
        {
            var ctx=new FeatureContext()
            {
                DateTime = DateTime.Now,
            };
            var user = HttpContextAccessor.HttpContext.User;
            if (SignInManager.IsSignedIn(user))
            {
                var usr = AsyncHelper.RunSync(()=>UserManager.GetUserAsync(user));
                ctx.Uid = Guid.Parse(UserManager.GetUserId(user));
                FillContext(ctx,usr);
            }
            return ctx;
        }
    }

    internal static class AsyncHelper
    {
        private static readonly TaskFactory _myTaskFactory = new
          TaskFactory(CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return AsyncHelper._myTaskFactory
              .StartNew<Task<TResult>>(func)
              .Unwrap<TResult>()
              .GetAwaiter()
              .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            AsyncHelper._myTaskFactory
              .StartNew<Task>(func)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }
    }
}
