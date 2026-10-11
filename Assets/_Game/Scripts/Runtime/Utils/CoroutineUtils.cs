using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public static class CoroutineUtils
    {
        public static WaitForEndOfFrame EndOfFrame { get; private set; } = new();
        public static WaitForFixedUpdate FixedUpdate { get; private set; } = new();
    
        private static readonly Dictionary<float, WaitForSeconds> _wfsCache = new();
        private static readonly Dictionary<float, WaitForSecondsRealtime> _wfsRealtimeCache = new();

        public static WaitForSeconds Wait(float time)
        {
            if (_wfsCache.TryGetValue(time, out var w)) return w;
            w = new WaitForSeconds(time);
            _wfsCache[time] = w;
            return w;
        }
    
        public static WaitForSecondsRealtime WaitRealtime(float time)
        {
            if (_wfsRealtimeCache.TryGetValue(time, out var w)) return w;
            w = new WaitForSecondsRealtime(time);
            _wfsRealtimeCache[time] = w;
            return w;
        }

        public static IEnumerator WaitUntil(Func<bool> condition)
        {
            while (!condition()) yield return null;
        }

        public static IEnumerator WaitWhile(Func<bool> condition)
        {
            while (condition()) yield return null;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static IEnumerator WhenAll(params Func<bool>[] conditions)
        {
            while (!AllTrue(conditions)) yield return null;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static IEnumerator WhenAny(params Func<bool>[] conditions)
        {
            while (!AnyTrue(conditions)) yield return null;
        }

        private static bool AllTrue(Func<bool>[] conditions)
        {
            foreach (Func<bool> condition in conditions)
            {
                if (!condition()) return false;
            }
            return true;
        }

        private static bool AnyTrue(Func<bool>[] conditions)
        {
            foreach (Func<bool> condition in conditions)
            {
                if (condition()) return true;
            }
            return false;
        }
    }
}
