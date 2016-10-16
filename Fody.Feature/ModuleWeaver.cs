using System;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;

public class Feature
{
    // Will log an informational message to MSBuild
    public Action<string> LogInfo { get; set; }

    // An instance of Mono.Cecil.ModuleDefinition for processing
    public ModuleDefinition ModuleDefinition { get; set; }

    TypeSystem typeSystem;

    // Init logging delegates to make testing easier
    public Feature()
    {
        LogInfo = m => { };
    }

    public void Execute()
    {
        typeSystem = ModuleDefinition.TypeSystem;

        var types =
            ModuleDefinition.Types.Where(
                x => x.Properties.Any(y => y.CustomAttributes.Any(z => z.AttributeType.FullName == "FeatureFlags.FeatureFlagAttribute")));

        var properties = 
            ModuleDefinition.Types.SelectMany(x=>x.Properties).Where(y => y.CustomAttributes.Any(z => z.AttributeType.FullName == "FeatureFlags.FeatureFlagAttribute"));


        var isActiveMethod=ModuleDefinition.Types.First(x => x.FullName == "FeatureFlags.Features").Methods.FirstOrDefault(x=>x.FullName== "System.Boolean FeatureFlags.Features::IsActive(System.String)");

        foreach (var property in properties)
        {
            var args=property.CustomAttributes.First(x => x.AttributeType.FullName == "FeatureFlags.FeatureFlagAttribute")
                .ConstructorArguments;

            var body = property.GetMethod.Body;
            var processor = body.GetILProcessor();
            var l=body.Instructions.ToList();
            foreach (var i in l) processor.Remove(i);

            processor.Append(Instruction.Create(OpCodes.Ldstr, args.Count==0?property.Name:(string)args[0].Value));
            processor.Append(Instruction.Create(OpCodes.Call, isActiveMethod));
            processor.Append(Instruction.Create(OpCodes.Ret));
            var backingFieldName = $"<{property.Name}>k__BackingField";
            var backingField=property.DeclaringType.Fields.FirstOrDefault(x => x.Name == backingFieldName);
            property.DeclaringType.Fields.Remove(backingField);
        }

    }

}