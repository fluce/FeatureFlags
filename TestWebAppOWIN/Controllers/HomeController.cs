using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FeatureFlags;

namespace TestWebAppOWIN.Controllers
{
    public class HomeController : Controller
    {
        public IFeatures Features { get; set; }

        public HomeController(IFeatures features)
        {
//            IFeatures features = DependencyResolver.Current.GetService<IFeatures>();
            Features = features;
        }

        public ActionResult Index()
        {
            /*var def =
                ((IFeatureStore) (((Features) Features).FeatureStore)).GetAllFeatures().ToList();
            foreach (var featureFlag in def)
            {
                Debug.WriteLine(featureFlag.Name);
            }*/
            if (Features.IsActive("UserFeature"))
                ViewData["Message"] = "UserFeature is activated";
            else
                ViewData["Message"] = "UserFeature is NOT activated";

            ViewData["Container"] = TestWebAppOWIN.App_Start.StructuremapMvc.container.WhatDoIHave();
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