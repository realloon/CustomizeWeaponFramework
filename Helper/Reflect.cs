using System;
using System.Reflection;

namespace CustomizeWeapon.Helper;

public static class Reflect {
    public static T Get<T>(object instance, string fieldName) {
        var type = instance.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var fieldInfo = type.GetField(fieldName, flags);

        if (fieldInfo == null) {
            throw new MissingFieldException($"'{fieldName}' not found in '{type}'");
        }

        return (T)fieldInfo.GetValue(instance);
    }

    public static void Set<T>(T instance, string fieldName, object value) {
        var type = typeof(T);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var fieldInfo = type.GetField(fieldName, flags);

        if (fieldInfo == null) {
            throw new MissingFieldException($"'{fieldName}' not found in '{type}'");
        }

        fieldInfo.SetValue(instance, value);
    }
}