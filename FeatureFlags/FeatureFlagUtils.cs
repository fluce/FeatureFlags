using System;
using System.Linq;
using System.Reflection;
using FeatureFlags.Rules;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FeatureFlags
{
    public static class FeatureFlagUtils
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
                        var rules=JsonConvert.DeserializeObject<FeatureRulesDefinition>(v);

                        return new DynamicFeatureFlagStateEvaluator(rules);
                    }
                    catch (JsonReaderException)
                    {
                        state = false;
                    }

                }
            }
            return new ConstantFeatureFlagStateEvaluator(state ? FeatureFlagState.Active : FeatureFlagState.Inactive);
        }
    }


    public class RuleConverter : JsonCreationConverter<Rule>
    {
        protected override Rule Create(Type objectType, JObject jObject)
        {
            var name = jObject["rule"].Value<string>();

            return (Rule)Activator.CreateInstance(Assembly.GetCallingAssembly()
                .GetType($"FeatureFlags.Rules.{name}Rule", true, false));
        }

        private bool FieldExists(string fieldName, JObject jObject)
        {
            return jObject[fieldName] != null;
        }
    }

    public abstract class JsonCreationConverter<T> : JsonConverter
    {
        /// <summary>
        /// Create an instance of objectType, based properties in the JSON object
        /// </summary>
        /// <param name="objectType">type of object expected</param>
        /// <param name="jObject">
        /// contents of JSON object that will be deserialized
        /// </param>
        /// <returns></returns>
        protected abstract T Create(Type objectType, JObject jObject);

        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader,
                                        Type objectType,
                                         object existingValue,
                                         JsonSerializer serializer)
        {
            // Load JObject from stream
            JObject jObject = JObject.Load(reader);

            // Create target object based on JObject
            T target = Create(objectType, jObject);

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), target);

            return target;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class FeatureRulesDefinition
    {
        [JsonProperty("contextualRules")]
        public Rule[] ContextualRules { get; set; }
    }

    namespace Rules
    {

        [JsonConverter(typeof (RuleConverter))]
        public abstract class Rule
        {
            [JsonProperty("rule")]
            public string RuleType
            {
                get
                {
                    var name = GetType().Name;
                    if (name.Length > 4 && name.EndsWith("Rule",StringComparison.Ordinal))
                        return name.Substring(0, name.Length - 4);
                    return name;
                }
            }

            public abstract FeatureFlagState Evaluate(FeatureContext context);
        }

        public class ConstantRule : Rule
        {
            public FeatureFlagState Value { get; set; }

            public override FeatureFlagState Evaluate(FeatureContext context)
            {
                return Value;
            }
        }

        public class ActiveRule : Rule
        {
            public override FeatureFlagState Evaluate(FeatureContext context)
            {
                return FeatureFlagState.Active;
            }
        }

        public class InactiveRule : Rule
        {
            public override FeatureFlagState Evaluate(FeatureContext context)
            {
                return FeatureFlagState.Inactive;
            }
        }

        public class OrRule : Rule
        {
            [JsonProperty("rules")]
            public Rule[] Rules { get; set; }

            public override FeatureFlagState Evaluate(FeatureContext context)
            {
                return Rules.Any(x => x.Evaluate(context) == FeatureFlagState.Active)
                    ? FeatureFlagState.Active
                    : FeatureFlagState.Inactive;
            }
        }

        public class AndRule : Rule
        {
            [JsonProperty("rules")]
            public Rule[] Rules { get; set; }

            public override FeatureFlagState Evaluate(FeatureContext context)
            {
                return Rules.All(x => x.Evaluate(context) == FeatureFlagState.Active)
                    ? FeatureFlagState.Active
                    : FeatureFlagState.Inactive;
            }
        }

        public class ScheduleRule : Rule
        {
            [JsonProperty("from")]
            public DateTime From { get; set; }

            [JsonProperty("to")]
            public DateTime To { get; set; }

            public override FeatureFlagState Evaluate(FeatureContext context)
            {
                return context.DateTime >= From && context.DateTime < To
                    ? FeatureFlagState.Active
                    : FeatureFlagState.Inactive;
            }

        }

        public class UserSelectionRule : Rule
        {
            [JsonProperty("uid")]
            public Guid? Uid { get; set; }

            [JsonProperty("ratio")]
            public decimal? Ratio { get; set; }

            public override FeatureFlagState Evaluate(FeatureContext context)
            {
                if (Uid.HasValue)
                    return Uid == context.Uid
                        ? FeatureFlagState.Active
                        : FeatureFlagState.Inactive;

                if (Ratio.HasValue)
                {
                    var guid = context.Uid.ToByteArray();
                    int r = guid[14]*256 + guid[15];

                    return (r < Ratio*65536)
                        ? FeatureFlagState.Active
                        : FeatureFlagState.Inactive;
                }
                return FeatureFlagState.Inactive;
            }
        }
    }

    public abstract class FeatureFlagStateEvaluator
    {
        public abstract FeatureFlagState Evaluate(FeatureContext context);
    }

    public class ConstantFeatureFlagStateEvaluator : FeatureFlagStateEvaluator
    {
        private FeatureFlagState State { get; }

        public ConstantFeatureFlagStateEvaluator(FeatureFlagState state)
        {
            State = state;
        }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return State;
        }
    }


    public class DynamicFeatureFlagStateEvaluator : FeatureFlagStateEvaluator
    {
        public FeatureRulesDefinition Rules { get; set; }

        public DynamicFeatureFlagStateEvaluator(FeatureRulesDefinition rules)
        {
            Rules = rules;
        }

        public override FeatureFlagState Evaluate(FeatureContext context)
        {
            return Rules.ContextualRules.All(x => x.Evaluate(context)==FeatureFlagState.Active) 
                ? FeatureFlagState.Active 
                : FeatureFlagState.Inactive;
        }
    }

}