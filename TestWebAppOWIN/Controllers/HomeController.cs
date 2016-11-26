using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FeatureFlags;
using TestWebAppOWIN.DependencyResolution;

namespace TestWebAppOWIN.Controllers
{
    public class HomeController : Controller
    {
        public IMyFeatures MyFeatures { get; set; }
        public IFeatures Features { get; set; }

        public HomeController(IFeatures features, IMyFeatures myFeatures)
        {
//            IFeatures features = DependencyResolver.Current.GetService<IFeatures>();
            Features = features;
            MyFeatures = myFeatures;
        }

        public ActionResult Index()
        {
            /*var def =
                ((IFeatureStore) (((Features) Features).FeatureStore)).GetAllFeatures().ToList();
            foreach (var featureFlag in def)
            {
                Debug.WriteLine(featureFlag.Name);
            }*/
            if (MyFeatures.UserFeature)
                ViewData["Message"] = "UserFeature is activated";
            else
                ViewData["Message"] = "UserFeature is NOT activated";

            ViewData["Container"] = TestWebAppOWIN.App_Start.StructuremapMvc.container.WhatDoIHave();

            ViewBag.Features = Features;

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}