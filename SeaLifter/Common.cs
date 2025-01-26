using System.Reflection;
using UnityEngine;

class Val
{

    public enum SaveTextureFileFormat
    {
        exr,
        jpg,
        png,
        tga
    }
    public static string Cat = "≽^•⩊•^≼";

    public static BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    public static BindingFlags ValFlags = Flags | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.GetProperty;
    public static BindingFlags FunFlags = Flags;
    public static T Get<T>(object instance, string fieldName, Type type = null)
    {
        if (instance == null)
            return default;
        if (type == null)
            type = instance.GetType();
        FieldInfo field = type.GetField(fieldName, ValFlags);
        if (field != null)
            return (T)field.GetValue(instance);
        else if (type.BaseType != null)
            return Get<T>(instance, fieldName, type.BaseType);
        return default;
    }
    public static T Set<T>(object instance, string fieldName, T value) => (T)Set(instance, fieldName, value, null);
    public static object Set(object instance, string fieldName, object value = null, Type type = null)
    {
        if (type == null)
            type = instance.GetType();
        FieldInfo field = type.GetField(fieldName, ValFlags);
        if (field != null)
            field.SetValue(instance, value);
        else if (type.BaseType != null)
            Set(instance, fieldName, value, type.BaseType);
        return value;
    }

    public static T GetStatic<T>(Type type, string fieldName)
    {
        if (type == null)
            return default;

        FieldInfo field = type.GetField(fieldName, ValFlags | BindingFlags.Static);
        if (field != null)
            return (T)field.GetValue(null); // No instance, so pass null for static field.
        return default;
    }

    public static void SetStatic<T>(Type type, string fieldName, T value)
    {
        if (type == null)
            return;

        FieldInfo field = type.GetField(fieldName, ValFlags | BindingFlags.Static);
        if (field != null)
            field.SetValue(null, value); // No instance, so pass null for static field.
    }

    public static void RunFunc(object instance, string functionname, object[] paramters = null, Type type = null)
    {
        MethodInfo protectedMethod = instance.GetType().GetMethod(functionname, FunFlags);

        protectedMethod.Invoke(instance, paramters);

    }


    //    list: List<T> to resize
    //    size: desired new size
    // element: default value to insert


}

public static class CommonNonStatic
{
    public static void Resize<T>(this List<T> list, int size, T element = default(T))
    {
        int count = list.Count;

        if (size < count)
        {
            list.RemoveRange(size, count - size);
        }
        else if (size > count)
        {
            if (size > list.Capacity)   // Optimization
                list.Capacity = size;

            list.AddRange(Enumerable.Repeat(element, size - count));
        }
    }

    public static void Desize<T>(this List<T> list, int size)
    {
        int count = list.Count;

        if (size < count)
        {
            list.RemoveRange(size, count - size);
        }

    }

    public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource> source)
    {
        return source.Where(item => item != null);
    }
}