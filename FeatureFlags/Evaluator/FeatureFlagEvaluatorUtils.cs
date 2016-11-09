using System;
using System.Linq.Expressions;
using FeatureFlags.Grammar;
using Newtonsoft.Json;

namespace FeatureFlags.Evaluator
{
    public static class FeatureFlagEvaluatorUtils
    {
        public static FeatureFlagStateEvaluator Parse(string v)
        {
            bool state = false;
            if (v != null && !bool.TryParse(v, out state))
            {
                int stateAsInt;
                if (int.TryParse(v, out stateAsInt))
                    state = stateAsInt == 1;
                else if (string.Compare(v, "on", StringComparison.OrdinalIgnoreCase) == 0)
                    state = true;
                else if (string.Compare(v, "yes", StringComparison.OrdinalIgnoreCase) == 0)
                    state = true;
                else
                {
                    try
                    {
                        var rules = JsonConvert.DeserializeObject<FeatureRulesDefinition>(v);

                        if (!string.IsNullOrEmpty(rules.ActiveExpression))
                            rules.ActiveFunc = Compiler<Globals, bool>.Compile(rules.ActiveExpression);

                        return new DynamicFeatureFlagStateEvaluator(rules);
                    }
                    catch (JsonReaderException)
                    {
                        state = false;
                    }
                    catch (CompileException)
                    {
                        state = false;
                    }

                }
            }
            return new ConstantFeatureFlagStateEvaluator(state ? FeatureFlagState.Active : FeatureFlagState.Inactive);
        }

        public static string SerializeRules(FeatureRulesDefinition def)
        {
            return JsonConvert.SerializeObject(def);
        }

        public static bool TryCompileRuleExpression(string rule)
        {
            Expression expression;
            var r=Compiler<Globals, bool>.Compile(rule,out expression);
            var global = new Globals()
            {
                Now = DateTime.Now,
                User = new Globals.UserInfo() { Email = "test@mail.com", Uid = Guid.NewGuid()}
            };
            try
            {
                r(global);
            }
            catch (Exception ex)
            {
                throw new CompileException("Error executing rule : "+ex.Message,innerException:ex);
            }
            return true;
        }

    }


}