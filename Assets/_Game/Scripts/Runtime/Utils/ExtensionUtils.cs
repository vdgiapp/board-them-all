using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using URandom = UnityEngine.Random;

namespace Game
{
    public static class ExtensionUtils
    {
#region Float - Double
        /// <summary>
        /// Remap a value from one range to another.
        /// </summary>
        /// <param name="value">Source value.</param>
        /// <param name="min1">Source min.</param>
        /// <param name="max1">Source max.</param>
        /// <param name="min2">Target min.</param>
        /// <param name="max2">Target max</param>
        /// <returns>Remap[ed value.</returns>
        public static float Remap(this float value, float min1, float max1, float min2, float max2)
        {
            return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
        }

        /// <summary>
        /// Checks <paramref name="value"/> for approximate equality with <paramref name="other"/>.
        /// </summary>
        /// <param name="value">First operand.</param>
        /// <param name="other">Second operand.</param>
        /// <returns><see langword="true"/> if the variables are the same.</returns>
        public static bool Approx(this float value, float other) => Mathf.Approximately(value, other);
                
        /// <summary>
        /// Randomly changes the sign (+ or -) of <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Target value.</param>
        /// <returns>Value with randomized sign.</returns>
        public static float WithRandomSign(this float value) => value * (URandom.Range(0, 2) * 2 - 1);

        /// <summary>
        /// Remap a value from one range to another.
        /// </summary>
        /// <param name="value">Source value.</param>
        /// <param name="min1">Source min.</param>
        /// <param name="max1">Source max.</param>
        /// <param name="min2">Target min.</param>
        /// <param name="max2">Target max</param>
        /// <returns>Remapped value.</returns>
        public static double Remap(this double value, double min1, double max1, double min2, double max2)
        {
            return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
        }

        /// <summary>
        /// Checks <paramref name="value"/> for approximate equality with <paramref name="other"/> and <paramref name="minDifference"/>.
        /// </summary>
        /// <param name="value">First operand.</param>
        /// <param name="other">Second operand.</param>
        /// <param name="minDifference">If difference between <paramref name="value"/> and <paramref name="other"/> is smaller than this, then result is true.</param>
        /// <returns><see langword="true"/> if the variables are the same.</returns>
        public static bool Approx(this double value, double other, double minDifference = 1E-17) => Math.Abs(value - other) <= minDifference;

        /// <summary>
        /// Randomly changes the sign (+ or -) of <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Target value.</param>
        /// <returns>Value with randomized sign.</returns>
        public static double WithRandomSign(this double value) => value * (URandom.Range(0, 2) * 2 - 1);
        
        public static float LinearToDecibel(float linear)
        {
            linear = Mathf.Clamp01(linear);
            if (linear <= 0.0001f) return -80f;
            return 20f * Mathf.Log10(linear);
        }
        
        public static float DecibelToLinear(float dB)
        {
            if (dB <= -80f) return 0f;
            return Mathf.Pow(10f, dB / 20f);
        }
#endregion
        
#region Collections
        public static void RemoveWhere<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, Predicate<KeyValuePair<TKey, TValue>> predicate)
        {
            List<TKey> keysToRemove = new List<TKey>();
            foreach (var kvp in dictionary)
            {
                if (predicate(kvp)) keysToRemove.Add(kvp.Key);
            }
            foreach (var key in keysToRemove) dictionary.Remove(key);
        }

        public static T GetOrDefault<T>(this IList<T> list, int index, T defaultValue = default)
        {
            if (index < 0 || index >= list.Count) return defaultValue;
            return list[index];
        }
        
        public static bool TryGetValue<T>(this IList<T> list, int index, out T value)
        {
            if (index < 0 || index >= list.Count)
            {
                value = default;
                return false;
            }
            value = list[index];
            return true;
        }
        
        public static T GetRandom<T>(this IList<T> list, T defaultValue = default)
        {
            return list.Count == 0 ? defaultValue : list[URandom.Range(0, list.Count)];
        }
        
        public static bool ContainsAll<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (T item in items) if (!list.Contains(item)) return false;
            return true;
        }

        public static bool ContainsAll<T>(this IList<T> list, params T[] items)
        {
            foreach (T item in items) if (!list.Contains(item)) return false;
            return true;
        }

        public static void MoveToFront<T>(this IList<T> list, T item)
        {
            int index = list.IndexOf(item);
            if (index < 0)
            {
                Debug.LogError($"List does not contain item: {item}");
                return;
            }
            list.RemoveAt(index);
            list.Insert(0, item);
        }
#endregion

#region Vector2 - Vector3
        public static Vector3 GetCenter(this IList<Vector3> positions)
        {
            if (positions.Count == 0) return Vector3.zero;
            if (positions.Count == 1) return positions[0];

            float sumX = 0, sumY = 0, sumZ = 0;
            foreach (Vector3 pos in positions)
            {
                sumX += pos.x;
                sumY += pos.y;
                sumZ += pos.z;
            }
            return new Vector3(sumX / positions.Count, sumY / positions.Count, sumZ / positions.Count);
        }
        
        public static Vector3 GetCenter(this IList<Vector3Int> positions)
        {
            if (positions.Count == 0) return Vector3.zero;
            if (positions.Count == 1) return positions[0];

            int sumX = 0, sumY = 0, sumZ = 0;
            foreach (Vector3Int pos in positions)
            {
                sumX += pos.x;
                sumY += pos.y;
                sumZ += pos.z;
            }
            return new Vector3((float)sumX / positions.Count, (float)sumY / positions.Count, (float)sumZ / positions.Count);
        }
        
        public static Vector2 GetCenter(this IList<Vector2> positions)
        {
            if (positions.Count == 0) return Vector2.zero;
            if (positions.Count == 1) return positions[0];

            float sumX = 0, sumY = 0;
            foreach (Vector2 pos in positions)
            {
                sumX += pos.x;
                sumY += pos.y;
            }
            return new Vector2(sumX / positions.Count, sumY / positions.Count);
        }
        
        public static Vector2 GetCenter(this IList<Vector2Int> positions)
        {
            if (positions.Count == 0) return Vector2.zero;
            if (positions.Count == 1) return positions[0];

            int sumX = 0, sumY = 0;
            foreach (Vector2Int pos in positions)
            {
                sumX += pos.x;
                sumY += pos.y;
            }
            return new Vector2((float)sumX / positions.Count, (float)sumY / positions.Count);
        }

        public static Vector2 WithX(this Vector2 vec2, float x)
        {
            Vector2 newOne = vec2;
            newOne.x = x;
            return newOne;
        }
        
        public static Vector2 WithY(this Vector2 vec2, float y)
        {
            Vector2 newOne = vec2;
            newOne.y = y;
            return newOne;
        }
#endregion
        
#region Color
        /// <summary>
        /// Set value to color's channel.
        /// </summary>
        /// <param name="color">Target color.</param>
        /// <param name="channel">Channel index of the color.</param>
        /// <param name="value">Value to set.</param>
        /// <returns>Changed copy of the color.</returns>
        public static Color With(this Color color, int channel, float value)
        {
            color[channel] = value;
            return color;
        }

        public static Color WithR(this Color color, float r) => With(color, 0, r);
        public static Color WithG(this Color color, float g) => With(color, 1, g);
        public static Color WithB(this Color color, float b) => With(color, 2, b);
        public static Color WithA(this Color color, float a) => With(color, 3, a);

        /// <summary>
        /// Set values to color's channels.
        /// </summary>
        /// <param name="color">Target color.</param>
        /// <param name="channel1">First channel index of the color.</param>
        /// <param name="value1">First channel value.</param>
        /// <param name="channel2">Second channel index of the color.</param>
        /// <param name="value2">Second channel value.</param>
        /// <returns>Changed copy of the color.</returns>
        public static Color With(this Color color, int channel1, float value1, int channel2, float value2)
        {
            color[channel1] = value1;
            color[channel2] = value2;

            return color;
        }

        public static Color WithRG(this Color color, float r, float g) => With(color, 0, r, 1, g);
        public static Color WithRB(this Color color, float r, float b) => With(color, 0, r, 2, b);
        public static Color WithRA(this Color color, float r, float a) => With(color, 0, r, 3, a);
        public static Color WithGB(this Color color, float g, float b) => With(color, 1, g, 2, b);
        public static Color WithGA(this Color color, float g, float a) => With(color, 1, g, 3, a);
        public static Color WithBA(this Color color, float b, float a) => With(color, 2, b, 3, a);

        /// <summary>
        /// Set values to color's channels.
        /// </summary>
        /// <param name="color">Target color.</param>
        /// <param name="channel1">First channel index of the color.</param>
        /// <param name="value1">First channel value.</param>
        /// <param name="channel2">Second channel index of the color.</param>
        /// <param name="value2">Second channel value.</param>
        /// <param name="channel3">Third channel index of the color.</param>
        /// <param name="value3">Third channel value.</param>
        /// <returns>Changed copy of the color.</returns>
        public static Color With(this Color color, int channel1, float value1, int channel2, float value2, int channel3, float value3)
        {
            color[channel1] = value1;
            color[channel2] = value2;
            color[channel3] = value3;

            return color;
        }

        public static Color WithRGB(this Color color, float r, float g, float b) => With(color, 0, r, 1, g, 2, b);
        public static Color WithRGA(this Color color, float r, float g, float a) => With(color, 0, r, 1, g, 3, a);
        public static Color WithRBA(this Color color, float r, float b, float a) => With(color, 0, r, 2, b, 3, a);
        public static Color WithGBA(this Color color, float g, float b, float a) => With(color, 1, g, 2, b, 3, a);
        
        /// <summary>
        /// Set value to color's channel.
        /// </summary>
        /// <param name="color">Target color.</param>
        /// <param name="channel">Channel index of the color.</param>
        /// <param name="value">Value to set.</param>
        /// <returns>Changed copy of the color.</returns>
        public static Color32 With(this Color32 color, int channel, byte value)
        {
            color[channel] = value;
            return color;
        }

        public static Color32 WithR(this Color32 color, byte r) => With(color, 0, r);
        public static Color32 WithG(this Color32 color, byte g) => With(color, 1, g);
        public static Color32 WithB(this Color32 color, byte b) => With(color, 2, b);
        public static Color32 WithA(this Color32 color, byte a) => With(color, 3, a);

        /// <summary>
        /// Set values to color's channels.
        /// </summary>
        /// <param name="color">Target color.</param>
        /// <param name="channel1">First channel index of the color.</param>
        /// <param name="value1">First channel value.</param>
        /// <param name="channel2">Second channel index of the color.</param>
        /// <param name="value2">Second channel value.</param>
        /// <returns>Changed copy of the color.</returns>
        public static Color32 With(this Color32 color, int channel1, byte value1, int channel2, byte value2)
        {
            color[channel1] = value1;
            color[channel2] = value2;

            return color;
        }

        public static Color32 WithRG(this Color32 color, byte r, byte g) => With(color, 0, r, 1, g);
        public static Color32 WithRB(this Color32 color, byte r, byte b) => With(color, 0, r, 2, b);
        public static Color32 WithRA(this Color32 color, byte r, byte a) => With(color, 0, r, 3, a);
        public static Color32 WithGB(this Color32 color, byte g, byte b) => With(color, 1, g, 2, b);
        public static Color32 WithGA(this Color32 color, byte g, byte a) => With(color, 1, g, 3, a);
        public static Color32 WithBA(this Color32 color, byte b, byte a) => With(color, 2, b, 3, a);

        /// <summary>
        /// Set values to color's channels.
        /// </summary>
        /// <param name="color">Target color.</param>
        /// <param name="channel1">First channel index of the color.</param>
        /// <param name="value1">First channel value.</param>
        /// <param name="channel2">Second channel index of the color.</param>
        /// <param name="value2">Second channel value.</param>
        /// <param name="channel3">Third channel index of the color.</param>
        /// <param name="value3">Third channel value.</param>
        /// <returns>Changed copy of the color.</returns>
        public static Color32 With(this Color32 color, int channel1, byte value1, int channel2, byte value2, int channel3, byte value3)
        {
            color[channel1] = value1;
            color[channel2] = value2;
            color[channel3] = value3;

            return color;
        }

        public static Color32 WithRGB(this Color32 color, byte r, byte g, byte b) => With(color, 0, r, 1, g, 2, b);
        public static Color32 WithRGA(this Color32 color, byte r, byte g, byte a) => With(color, 0, r, 1, g, 3, a);
        public static Color32 WithRBA(this Color32 color, byte r, byte b, byte a) => With(color, 0, r, 2, b, 3, a);
        public static Color32 WithGBA(this Color32 color, byte g, byte b, byte a) => With(color, 1, g, 2, b, 3, a);
#endregion

#region Camera - Canvas Scaler
        public static Vector3 WorldToScreenPoint(this Camera camera, Vector3 position, CanvasScaler canvasScaler)
        {
            var screenPosition = camera.WorldToScreenPoint(position);

            var multiplier = canvasScaler.GetScaleFactor();
            screenPosition.x /= multiplier;
            screenPosition.y /= multiplier;

            return screenPosition;
        }
        
        public static float GetScaleFactor(this CanvasScaler scaler)
        {
            return Mathf.Lerp(Screen.width / scaler.referenceResolution.x, Screen.height / scaler.referenceResolution.y, scaler.matchWidthOrHeight);
        }
#endregion

#region TextMeshPro - String Tags
        public static string WithColor(this string str, Color color, bool withAlpha = true)
        {
            if (withAlpha) return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{str}</color>";
        }
#endregion
    }
}