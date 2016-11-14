using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace FeatureFlagsAdmin.Models.FeaturesViewModels
{
    public class IndexViewModel
    {
        public List<FeatureFlagsViewModel> Features { get; set; }
        public List<string> ActiveNodes { get; set; }
        public List<string> AllStores { get; set; }
    }

    public class FeatureFlagsViewModel
    {
        public string Key { get; set; }
        public bool IsActive { get; set; }
        public bool IsDynamic { get; set; }
        public string Definition { get; set; }
    }
}
