// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultRegistry.cs" company="Web Advanced">
// Copyright 2012 Web Advanced (www.webadvanced.com)
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Web;
using FeatureFlags;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using TestWebAppOWIN.Models;

namespace TestWebAppOWIN.DependencyResolution {
    using FeatureFlags.Stores.ZooKeeper;
    using StructureMap;
    using StructureMap.Configuration.DSL;
    using StructureMap.Graph;

    public class DefaultRegistry : Registry {
        #region Constructors and Destructors

        public DefaultRegistry() {
            Scan(
                scan => {
                    scan.TheCallingAssembly();
                    scan.WithDefaultConventions();
					scan.With(new ControllerConvention());
                });
            //For<IExample>().Use<Example>();

            For<IFeatureContextProvider>().Use<AspNetCoreFeatureContextProvider>();
            For<IFeatureStore>().Use(new ZooKeeperFeatureStore("localhost:2181/TestWebApp"));
            For<IFeatures>().Use<Features>();

        }

        #endregion
    }

    public class AspNetCoreFeatureContextProvider : IFeatureContextProvider
    {
        public IContainer Container { get; set; }

        public AspNetCoreFeatureContextProvider(IContainer container)
        {
            Container = container;
        }

        public virtual void FillContext(FeatureContext context, ApplicationUser user)
        {

        }

        private HttpContextBase HttpContext
        {
            get
            {
                var ctx = Container.TryGetInstance<HttpContextBase>();
                return ctx ?? new HttpContextWrapper(System.Web.HttpContext.Current);
            }
        }


        public FeatureContext GetContext()
        {
            var ctx = new FeatureContext()
            {
                DateTime = DateTime.Now,
            };
            var hctx = HttpContext;
            var usrmanager = hctx.GetOwinContext().Get<ApplicationUserManager>();
            var user = hctx.User;
            if ( hctx.Request.IsAuthenticated)
            {
                var id = user.Identity.GetUserName();
                var usr = AsyncHelper.RunSync(()=> usrmanager.FindByNameAsync(id));
                ctx.Uid = Guid.Parse(usr.Id);
                ctx.Email = usr.Email;
                FillContext(ctx, usr);
            }
            return ctx;
        }
    }

}