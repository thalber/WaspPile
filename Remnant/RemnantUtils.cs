using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;

using static UnityEngine.Mathf;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant
{
    internal static class RemnantUtils
    {
        //reflection flag templates
        internal const BindingFlags allContextsInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        internal const BindingFlags allContextsStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        internal const BindingFlags allContextsCtor = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;
        //reflection helpers
        internal static string pbfiname(string propname) => $"<{propname}>k__BackingField";

        internal static MethodBase rsh_getm<T>(string mname, BindingFlags context = allContextsInstance) 
            => typeof(T).GetMethod(mname, context);
        internal static MethodBase rsh_getctor<T>(BindingFlags context = allContextsCtor, params Type[] pms) 
            => typeof(T).GetConstructor(context, null, pms, null);
        internal static MethodBase rsh_getctor<T>(params Type[] pms) 
            => typeof(T).GetConstructor(pms);

        //ien helpers
        internal static void SetKey<tKey, tValue>(this Dictionary<tKey, tValue> dict, tKey key, tValue val) {
            if (dict == null) throw new ArgumentNullException();
            if (!dict.ContainsKey(key)) dict.Add(key, val);
            else dict[key] = val;
        }
        internal static void TryRemoveKey<tKey, tVal>(this Dictionary<tKey, tVal> dict, tKey key)
        {
            if (dict.ContainsKey(key)) dict.Remove(key);
        }
        //randomization extensions
        internal static float RandSign() => URand.value > 0.5f ? -1f : 1f;
        internal static Vector2 V2RandLerp(Vector2 a, Vector2 b) => Vector2.Lerp(a, b, URand.value);
        internal static float NextFloat01(this System.Random r) => (float)(r.NextDouble() / double.MaxValue);
        internal static Color Clamped(this Color bcol) => new Color(Clamp01(bcol.r), Clamp01(bcol.g), Clamp01(bcol.b));
        internal static Color RandDev(this Color bcol, Color dbound, bool clamped = true)
        {
            Color res = default;
            for (int i = 0; i < 3; i++) res[i] = bcol[i] + dbound[i] * URand.Range(-1f, 1f);
            return clamped? res.Clamped() : res;
        }

        //monomod.cil
        internal static Instruction CurrentInstruction(this ILCursor c) => c.Instrs[c.Index];

        //misc bs
        internal static void LogBunch(__arglist)
        {
            var ai = new ArgIterator(__arglist);
            while (ai.GetRemainingCount() != 0)
            {
                Debug.Log(TypedReference.ToObject(ai.GetNextArg()));
            }
        }

        internal static RainWorld CRW => UnityEngine.Object.FindObjectOfType<RainWorld>();
        internal static CreatureTemplate GetCreatureTemplate(CreatureTemplate.Type t) => StaticWorld.creatureTemplates[(int)t];
    }
}
