using System;
using System.Collections.Generic;
using URandom = UnityEngine.Random;

namespace Game
{
    public static class EnumUtils
    {
        public static T GetRandomEnumValue<T>() where T : Enum
        {
            Array values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(URandom.Range(0, values.Length));
        }
        
        public static IEnumerable<T> GetRandomEnumValues<T>(int count) where T : Enum
        {
            IList<T> list = GetListEnumValue<T>();
            if (count <= 0) return Array.Empty<T>();
            if (count >= list.Count) return list;

            int removeCount = list.Count - count;
            for (int i = 0; i < removeCount; i++)
            {
                int index = URandom.Range(0, list.Count);
                list.RemoveAt(index);
            }
            return list;
        }
        
        public static IList<T> GetListEnumValue<T>() where T : Enum
        {
            IList<T> list = new List<T>();
            foreach (T t in Enum.GetValues(typeof(T))) list.Add(t);
            return list;
        }
        
        public static int GetEnumLength<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Length;
        }
        
        public static T ToEnum<T>(this string value) where T : struct, Enum
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static bool TryToEnum<T>(this string value, out T type) where T : struct, Enum
        {
            return Enum.TryParse(value, out type);
        }
    }
}