using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace FeatureFlags
{
    public interface IFeatureFlagAccessor
    {
        IFeatures Features { get; set; }
    }

    public class BaseFeatureFlagAccessor: IFeatureFlagAccessor
    {
        public bool IsActive(string feature)
        {
            return Features.IsActive(feature);
        }

        public IFeatures Features { get; set; }
    }

    public static class FeatureFlagAccessor
    {
        public static readonly ConcurrentDictionary<Type,Type> Cache=new ConcurrentDictionary<Type, Type>();
        public static Type Build<T>() where T : class
        {
            return Cache.GetOrAdd(typeof (T), BuildType);
        }

        public static T BuildAndInstanciate<T>(IFeatures features) where T : class
        {
            var concreteType = Cache.GetOrAdd(typeof(T), BuildType);

            var r=(T)Activator.CreateInstance(concreteType, features);
            return r;
        }


        private static Type BuildType(Type interfaceType) 
        {
            string name = interfaceType.FullName;
            AssemblyName asmName = new AssemblyName(name);
#if DEBUG
            AssemblyBuilder ab =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    asmName, AssemblyBuilderAccess.RunAndSave, Path.GetTempPath());
#else
            AssemblyBuilder ab =
                AppDomain.CurrentDomain.DefineDynamicAssembly(
                    asmName, AssemblyBuilderAccess.Run);
#endif
            ModuleBuilder mb = ab.DefineDynamicModule(name,name+".dll");

            TypeBuilder tb =
                mb.DefineType("FeatureFlagAccessorAutogen." + name, TypeAttributes.Public, typeof (BaseFeatureFlagAccessor));
            tb.AddInterfaceImplementation(interfaceType);

            var prefix = interfaceType.GetCustomAttribute<FeatureFlagPrefixAttribute>()?.FeatureKeyPrefix ?? "";
            if (prefix != string.Empty && !prefix.EndsWith(".", StringComparison.Ordinal)) prefix += ".";

            foreach (var p in interfaceType.GetProperties())
            {
                var attr = p.GetCustomAttribute<FeatureFlagAttribute>();
                if (attr != null)
                {
                    AddProperty(tb, p.Name, prefix + attr.FeatureKey);
                }
                else
                {
                    AddProperty(tb, p.Name, prefix + p.Name);
                }
            }

            var cb =
                tb.DefineConstructor(
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
                    MethodAttributes.RTSpecialName,
                    CallingConventions.HasThis, new[] {typeof (IFeatures)});

            ILGenerator il = cb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, typeof(BaseFeatureFlagAccessor).GetProperty("Features",typeof(IFeatures)).SetMethod);
            il.Emit(OpCodes.Ret);

            Type tc = tb.CreateType();

#if DEBUG
            ab.Save(name+".dll");
#endif
            return tc;
        }

        static readonly MethodInfo methodInfo = typeof(BaseFeatureFlagAccessor).GetMethod("IsActive", new[] { typeof(string) });

        private static void AddProperty(TypeBuilder tb, string propertyName, string featureName)
        {
            MethodBuilder mbIM = tb.DefineMethod("get_"+propertyName,
                MethodAttributes.Public | MethodAttributes.HideBySig |
                MethodAttributes.NewSlot | MethodAttributes.Virtual |
                MethodAttributes.Final | MethodAttributes.SpecialName,
                typeof (bool),
                Type.EmptyTypes);
            ILGenerator il = mbIM.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, featureName);
            il.Emit(OpCodes.Call, methodInfo);
            il.Emit(OpCodes.Ret);

            var pb = tb.DefineProperty(propertyName, PropertyAttributes.None, typeof (bool), new[] {typeof (bool)});
            pb.SetGetMethod(mbIM);
        }
    }
}
