namespace JsonHelper
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ReflectionHelper
    {
        public delegate object Creator();
        public delegate void Setter(ref object target, object value);
        // public delegate object Getter(object target);

        private static Type creatorType = typeof(Creator);
        private static Type setterType = typeof(Setter);
        private static Type objectType = typeof(object);
        private static Type objectRefType = Type.GetType("System.Object&");

        private static ConcurrentDictionary<Type, Creator> creators = new ConcurrentDictionary<Type, Creator>();
        private static ConcurrentDictionary<PropertyInfo, Setter> propSetters = new ConcurrentDictionary<PropertyInfo, Setter>();
        private static ConcurrentDictionary<FieldInfo, Setter> fieldSetters = new ConcurrentDictionary<FieldInfo, Setter>();

        public static object CreateInstance(this Type type)
        {
            return GetCreator(type)();
        }

        public static Creator GetCreator(this Type type)
        {
            Creator creator = creators.GetOrAdd(type, (_type) =>
            {
                if (type.IsClass)
                {
                    var method = new DynamicMethod(type.Name, type, null);
                    var il = method.GetILGenerator();
                    il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                    il.Emit(OpCodes.Ret);
                    return (Creator)method.CreateDelegate(creatorType);
                }
                else
                {
                    // 结构体的初始化是不一样的！
                    var method = new DynamicMethod(type.Name, objectType, null);
                    var il = method.GetILGenerator();
                    var local = il.DeclareLocal(type);
                    il.Emit(OpCodes.Ldloca_S, local);     // 加载局部变量
                    il.Emit(OpCodes.Initobj, type);       // 初始化
                    il.Emit(OpCodes.Ldloc_0);             // 压栈
                    il.Emit(OpCodes.Box, type);           // 装箱
                    il.Emit(OpCodes.Ret);
                    return (Creator)method.CreateDelegate(creatorType);
                }
            });

            return creator;
        }

        public static Setter GetSetter(this Type type, PropertyInfo prop)
        {
            Setter setter = propSetters.GetOrAdd(prop, (_prop) =>
            {
                var setMethod = prop.GetSetMethod();
                if (setMethod == null)
                    return null;

                var method = new DynamicMethod(prop.Name, null, new Type[] { objectRefType, objectType });
                var il = method.GetILGenerator();

                if (type.IsClass)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldind_Ref);
                    il.Emit(OpCodes.Castclass, type);
                    il.Emit(OpCodes.Ldarg_1);
                    if (!prop.PropertyType.IsClass)
                        il.Emit(OpCodes.Unbox_Any, prop.PropertyType);
                    else
                        il.Emit(OpCodes.Castclass, prop.PropertyType);
                    il.Emit(OpCodes.Callvirt, setMethod);
                }
                else
                {
                    // 结构体
                    var local = il.DeclareLocal(type);  // 声明变量
                    il.Emit(OpCodes.Ldarg_0);           // 压栈
                    il.Emit(OpCodes.Ldind_Ref);         // ref
                    il.Emit(OpCodes.Unbox_Any, type);   // 拆箱
                    il.Emit(OpCodes.Stloc_0);           // 保存到局部变量
                    il.Emit(OpCodes.Ldloca_S, local);
                    il.Emit(OpCodes.Ldarg_1);
                    if (!prop.PropertyType.IsClass)
                        il.Emit(OpCodes.Unbox_Any, prop.PropertyType);
                    else
                        il.Emit(OpCodes.Castclass, prop.PropertyType);
                    il.Emit(OpCodes.Call, setMethod);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Box, type);
                    il.Emit(OpCodes.Stind_Ref);
                }

                il.Emit(OpCodes.Ret);

                return (Setter)method.CreateDelegate(setterType);
            });

            return setter;
        }

        public static Setter GetSetter(this Type type, FieldInfo field)
        {
            Setter setter = fieldSetters.GetOrAdd(field, (_field) =>
            {
                var method = new DynamicMethod(field.Name, null, new Type[] { objectRefType, objectType });
                var il = method.GetILGenerator();

                if (type.IsClass)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldind_Ref);
                    il.Emit(OpCodes.Castclass, type);
                    il.Emit(OpCodes.Ldarg_1);
                    if (!field.FieldType.IsClass)
                        il.Emit(OpCodes.Unbox_Any, field.FieldType);
                    else
                        il.Emit(OpCodes.Castclass, field.FieldType);
                    il.Emit(OpCodes.Stfld, field);
                }
                else
                {
                    // 结构体
                    var local = il.DeclareLocal(type);  // 声明变量
                    il.Emit(OpCodes.Ldarg_0);           // 压栈
                    il.Emit(OpCodes.Ldind_Ref);         // ref
                    il.Emit(OpCodes.Unbox_Any, type);   // 拆箱
                    il.Emit(OpCodes.Stloc_0);           // 保存到局部变量
                    il.Emit(OpCodes.Ldloca_S, local);
                    il.Emit(OpCodes.Ldarg_1);
                    if (!field.FieldType.IsClass)
                        il.Emit(OpCodes.Unbox_Any, field.FieldType);
                    else
                        il.Emit(OpCodes.Castclass, field.FieldType);
                    il.Emit(OpCodes.Stfld, field);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Box, type);
                    il.Emit(OpCodes.Stind_Ref);
                }

                il.Emit(OpCodes.Ret);

                return (Setter)method.CreateDelegate(setterType);
            });
            
            return setter;
        }

        public static bool IsPrimitiveEx(this Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(decimal);
        }
    }
}
