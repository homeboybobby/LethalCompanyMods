using System;
using System.Reflection;

namespace SuperSpeed.Patches
{
    public class Reflection
    {
        public static T GetFieldValue<T>(object data, string fieldName)
        {
            Type type = data.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return (T) field.GetValue(data);
        }
    }
}