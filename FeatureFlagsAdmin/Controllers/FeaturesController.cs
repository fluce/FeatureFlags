using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Antlr4.Runtime;
using FeatureFlags;
using FeatureFlags.Evaluator;
using FeatureFlags.FeatureFlag;
using FeatureFlags.Grammar;
using FeatureFlags.Stores.ZooKeeper;
using FeatureFlags.ZooKeeper.Stores.ZooKeeper;
using FeatureFlagsAdmin.Models.FeaturesViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FeatureFlagsAdmin.Controllers
{

    public interface IFeatureStoreFactory
    {
        IDynamicFeatureStore GetFeatureStore(string store);

        Task<List<string>> GetStores();
    }

    public class FeatureStoreFactory : IFeatureStoreFactory
    {
        readonly ConcurrentDictionary<string, IDynamicFeatureStore> cache=new ConcurrentDictionary<string, IDynamicFeatureStore>();

        public string ZooKeeperAddress { get; }

        public ZooKeeperStores ZooKeeperStores { get; }

        public FeatureStoreFactory(string zooKeeperAddress)
        {
            ZooKeeperAddress = zooKeeperAddress;
            ZooKeeperStores = new ZooKeeperStores(ZooKeeperAddress);
        }

        public IDynamicFeatureStore GetFeatureStore(string store)
        {
            return cache.GetOrAdd(store,
                x => new ZooKeeperFeatureStore(ZooKeeperAddress + "/" + x, false, false));
        }

        public Task<List<string>> GetStores()
        {
            return ZooKeeperStores.GetStores();
        }
    }

    [Route("/")]
    public class FeaturesController : Controller
    {
        public IFeatureStoreFactory FeatureStoreFactory { get; set; }

        public FeaturesController(IFeatureStoreFactory featureStoreFactory)
        {
            FeatureStoreFactory = featureStoreFactory;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            return View(await FeatureStoreFactory.GetStores());
        }

        [Route("{store}")]
        public async Task<IActionResult> StoreIndex(string store)
        {
            var featureStore = FeatureStoreFactory.GetFeatureStore(store);
            var model = new IndexViewModel
            {
                Features = featureStore.GetAllFeatures()
                                       .Select(
                                            x=>
                                            {
                                                var definition = featureStore.GetFeatureFlagDefinition(x.Name);
                                                var p = FeatureFlagEvaluatorUtils.Parse(x.Name,definition.Definition);
                                                var dynamicEvaluator = p as DynamicFeatureFlagStateEvaluator;
                                                if (dynamicEvaluator != null)
                                                {
                                                    return new FeatureFlagsViewModel
                                                    {
                                                        Key = x.Name,
                                                        IsDynamic = true,
                                                        IsActive = dynamicEvaluator.Rules.OverrideValue.GetValueOrDefault(true),
                                                        Definition = dynamicEvaluator.Rules.ActiveExpression
                                                    };
                                                }
                                                else
                                                    return new FeatureFlagsViewModel
                                                    {
                                                        Key = x.Name,
                                                        IsDynamic = false,
                                                        IsActive = x.GetState(null) == FeatureFlagState.Active
                                                    };
                                            }
                                        ).OrderBy(x=>x.Key).ToList(),
                ActiveNodes = await (featureStore as IWatchdog)?.GetActiveNodes(),
                AllStores = await FeatureStoreFactory.GetStores()
            };

            return View(model);
        }


        [HttpPost]
        [Route("{store}/features/{key}/activate")]
        public void Activate(string store, string key)
        {
            var featureStore = FeatureStoreFactory.GetFeatureStore(store);
            var def = featureStore.GetFeatureFlagDefinition(key);
            var p = FeatureFlagEvaluatorUtils.Parse(key,def.Definition);
            var dynamicEvaluator = p as DynamicFeatureFlagStateEvaluator;
            if (dynamicEvaluator != null)
            {
                dynamicEvaluator.Rules.OverrideValue = null;
                featureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = FeatureFlagEvaluatorUtils.SerializeRules(dynamicEvaluator.Rules) });
            }
            else
                featureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = "true" });

        }

        [HttpPost]
        [Route("{store}/features/{key}/deactivate")]
        public void Dectivate(string store, string key)
        {
            var featureStore = FeatureStoreFactory.GetFeatureStore(store);
            var def = featureStore.GetFeatureFlagDefinition(key);
            var p = FeatureFlagEvaluatorUtils.Parse(key,def.Definition);
            var dynamicEvaluator = p as DynamicFeatureFlagStateEvaluator;
            if (dynamicEvaluator != null)
            {
                dynamicEvaluator.Rules.OverrideValue = false;
                featureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = FeatureFlagEvaluatorUtils.SerializeRules(dynamicEvaluator.Rules) });
            }
            else
                featureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = "false" });
        }


        [HttpPost]
        [Route("{store}/features/{key}")]
        public void SetRule(string store, string key, string rule)
        {
            var featureStore = FeatureStoreFactory.GetFeatureStore(store);
            var def = featureStore.GetFeatureFlagDefinition(key);
            var p = FeatureFlagEvaluatorUtils.Parse(key,def.Definition);
            var dynamicEvaluator = p as DynamicFeatureFlagStateEvaluator ?? new DynamicFeatureFlagStateEvaluator(key, new FeatureRulesDefinition());
            dynamicEvaluator.Rules.ActiveExpression = rule;
            featureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = FeatureFlagEvaluatorUtils.SerializeRules(dynamicEvaluator.Rules) });
        }

        [HttpPost]
        [Route("/features/checksyntax")]
        public CheckResult SetRule(string rule)
        {
            try
            {
                FeatureFlagEvaluatorUtils.TryCompileRuleExpression(rule);
                return new CheckResult() {Success = true};
            }
            catch (CompileException e)
            {
                var re = e.InnerException as RecognitionException;
                return new CheckResult() {Success = false, Message = e.Message, Line=e.Line, Column = e.Column};
            }
        }

        public class CheckResult
        {
            public bool Success { get; set; } 

            public string Message { get; set; }

            public int Line { get; set; }
            public int Column { get; set; }
        }
    }
}
