using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FeatureFlags;
using FeatureFlags.Stores.ZooKeeper.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FeatureFlags.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TestWebApp.Data;
using Microsoft.EntityFrameworkCore;
using TestWebApp.Models;

namespace TestWebApp
{
    public interface IMyFeatures
    {
        bool UserFeature { get; }

        bool FeatureA { get; }

        bool FeatureB { get; }

        bool FeatureC { get; }
    }


    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddMvc();

            services.AddSingleton<IFeatureContextProvider, ApplicationUserContextProvider>();
            services.AddFeatureFlags(x =>
            {
                x.UseZooKeeperFeatureStore("localhost:2181/TestWebApp");
                x.BootstrapFromConfig(Configuration.GetSection("FeatureFlags"));
                x.AddAccessor<IMyFeatures>();
            });
        }

        public class ApplicationUserContextProvider : AspNetCoreFeatureContextProvider<ApplicationUser>
        {
            public ApplicationUserContextProvider(IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager,
                SignInManager<ApplicationUser> signInManager) : base(httpContextAccessor, userManager, signInManager)
            {
                
            }

            public override void FillContext(FeatureContext context, ApplicationUser user)
            {
                context.Email = user.Email;
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseIdentity();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

        }
    }
}
