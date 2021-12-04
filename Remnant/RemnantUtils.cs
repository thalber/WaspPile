using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant
{
    internal static class RemnantUtils
    {
        internal const BindingFlags allContextsInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        internal const BindingFlags allContextsStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        internal const BindingFlags allContextsCtor = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

        internal static void SetKey<tKey, tValue> (this Dictionary<tKey, tValue> dict, tKey key, tValue val){
            if (dict == null) throw new ArgumentNullException();
            if (!dict.ContainsKey(key)) dict.Add(key, val);
            else dict[key] = val;
        }
        internal static void TryRemoveKey<tKey, tVal> (this Dictionary<tKey, tVal> dict, tKey key)
        {
            if (dict.ContainsKey(key)) dict.Remove(key);
        }
        internal static float RandSign() => URand.value > 0.5f ? -1f : 1f;
        internal static Vector2 V2RandLerp(Vector2 a, Vector2 b) => Vector2.Lerp(a, b, URand.value);

        internal static RainWorld CRW => UnityEngine.Object.FindObjectOfType<RainWorld>();
    }
}
