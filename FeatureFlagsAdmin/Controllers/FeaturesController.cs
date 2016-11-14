using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Antlr4.Runtime;
using FeatureFlags;
using FeatureFlags.Evaluator;
using FeatureFlags.FeatureFlag;
using FeatureFlags.Grammar;
using FeatureFlagsAdmin.Models.FeaturesViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FeatureFlagsAdmin.Controllers
{
    public class FeaturesController : Controller
    {
        public IDynamicFeatureStore DynamicFeatureStore { get; set; }
        public IFeatureStore FeatureStore { get; set; }

        public FeaturesController(IDynamicFeatureStore featureStore)
        {
            FeatureStore = featureStore as IFeatureStore;
            DynamicFeatureStore = featureStore;
        }

        [Route("/")]
        public async Task<IActionResult> Index()
        {
            var model = new IndexViewModel
            {
                Features = FeatureStore.GetAllFeatures()
                                       .Select(
                                            x=>
                                            {
                                                var definition = DynamicFeatureStore.GetFeatureFlagDefinition(x.Name);
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
                ActiveNodes = await (FeatureStore as IWatchdog)?.GetActiveNodes()
            };

            return View(model);
        }


        [HttpPost]
        [Route("/features/{key}/activate")]
        public void Activate(string key)
        {
            var def= DynamicFeatureStore.GetFeatureFlagDefinition(key);
            var p = FeatureFlagEvaluatorUtils.Parse(key,def.Definition);
            var dynamicEvaluator = p as DynamicFeatureFlagStateEvaluator;
            if (dynamicEvaluator != null)
            {
                dynamicEvaluator.Rules.OverrideValue = null;
                DynamicFeatureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = FeatureFlagEvaluatorUtils.SerializeRules(dynamicEvaluator.Rules) });
            }
            else
                DynamicFeatureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = "true" });

        }

        [HttpPost]
        [Route("/features/{key}/deactivate")]
        public void Dectivate(string key)
        {
            var def = DynamicFeatureStore.GetFeatureFlagDefinition(key);
            var p = FeatureFlagEvaluatorUtils.Parse(key,def.Definition);
            var dynamicEvaluator = p as DynamicFeatureFlagStateEvaluator;
            if (dynamicEvaluator != null)
            {
                dynamicEvaluator.Rules.OverrideValue = false;
                DynamicFeatureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = FeatureFlagEvaluatorUtils.SerializeRules(dynamicEvaluator.Rules) });
            }
            else
                DynamicFeatureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = "false" });
        }


        [HttpPost]
        [Route("/features/{key}")]
        public void SetRule(string key, string rule)
        {
            var def = DynamicFeatureStore.GetFeatureFlagDefinition(key);
            var p = FeatureFlagEvaluatorUtils.Parse(key,def.Definition);
            var dynamicEvaluator = p as DynamicFeatureFlagStateEvaluator ?? new DynamicFeatureFlagStateEvaluator(key, new FeatureRulesDefinition());
            dynamicEvaluator.Rules.ActiveExpression = rule;
            DynamicFeatureStore.SetFeatureFlagDefinition(new FeatureFlagDefinition { Name = key, Definition = FeatureFlagEvaluatorUtils.SerializeRules(dynamicEvaluator.Rules) });
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
