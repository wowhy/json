using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonHelper
{
    public static class JSON
    {
        [ThreadStatic]
        private static JsonParser parser = new JsonParser();

        public static T ToObject<T>(string json)
        {            
            return (T)ToObject(json, typeof(T));
        }

        public static object ToObject(string json, Type type)
        {
            object data = parser.Parse(json);
            if (data == null)
                return null;

            if (type == null)
                return data;
            
            if (data is ArrayList)
                return ConvertToArray((ArrayList)data, type);

            return ConvertToObject((Dictionary<string, object>)data, type);
        }

        private static object ConvertToArray(ArrayList array, Type type)
        {
            // 数组类型
            if (type.IsArray)
            {
                return CreateArray(array, type.GetElementType());
            }

            // List<> 类型
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return CreateList(array, type.GetGenericArguments()[0]);
            }

            // ArrayList
            if (type == typeof(ArrayList))
                return array;

            // TODO: 扩展更多转换类型

            throw new ArgumentException("不支持的数据类型");
        }

        private static object ConvertToObject(Dictionary<string, object> data, Type type)
        {
            var result = ReflectionHelper.CreateInstance(type);
            var obj = default(object);

            var fields = type.GetFields();
            foreach (var field in fields)
            {
                if (data.TryGetValue(field.Name, out obj))
                {
                    type.GetSetter(field)(ref result, ConvertToValue(obj, field.FieldType));
                }
            }

            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (data.TryGetValue(prop.Name, out obj))
                {
                    type.GetSetter(prop)(ref result, ConvertToValue(obj, prop.PropertyType));
                }
            }

            return result;
        }

        private static object ConvertToValue(object data, Type type)
        {
            if (type.IsPrimitiveEx())
                return Convert.ChangeType(data, type);

            if (data is ArrayList)
                return ConvertToArray((ArrayList)data, type);

            return ConvertToObject((Dictionary<string, object>)data, type);
        }

        private static object CreateArray(ArrayList array, Type type)
        {
            var result = Array.CreateInstance(type, array.Count);
            for (int i = 0; i < array.Count; i++)
                result.SetValue(ConvertToValue(array[i], type), i);
            return result;
        }

        private static object CreateList(ArrayList array, Type type)
        {
            var result = (IList)ReflectionHelper.CreateInstance(typeof(List<>).MakeGenericType(type));
            for (int i = 0; i < array.Count; i++)
                result.Add(ConvertToValue(array[i], type));
            return result;
        }
    }
}
