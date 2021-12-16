﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.IO;
using System.Threading;

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

        #region refl/cil helpers
        internal static string pbfiname(string propname) => $"<{propname}>k__BackingField";
        /// <summary>
        /// takes methodinfo from T, defaults to <see cref="allContextsInstance"/>
        /// </summary>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="mname">methodname</param>
        /// <param name="context">binding flags, default private+public+instance</param>
        /// <returns></returns>
        internal static MethodInfo methodof<T>(string mname, BindingFlags context = allContextsInstance) 
            => typeof(T).GetMethod(mname, context);
        /// <summary>
        /// takes methodinfo from t, defaults to <see cref="allContextsStatic"/>
        /// </summary>
        /// <param name="t"></param>
        /// <param name="mname"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static MethodInfo methodof(Type t, string mname, BindingFlags context = allContextsStatic)
            => t.GetMethod(mname, context);
        /// <summary>
        /// the forbidden methodof: checks stack trace to get calling type's method. defaults to <see cref="allContextsStatic"/>
        /// </summary>
        /// <param name="mname"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static MethodInfo methodof_c(string mname, BindingFlags context = allContextsStatic)
        {
            //go crazy go stupid
            var c = new System.Diagnostics.StackTrace(Thread.CurrentThread, false);
            var caller = c.GetFrames()[1];
            return caller.GetMethod().DeclaringType.GetMethod(mname, context);
        }
        /// <summary>
        /// gets constructorinfo from T. no static ctors by default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="pms"></param>
        /// <returns></returns>
        internal static ConstructorInfo ctorof<T>(BindingFlags context = allContextsCtor, params Type[] pms) 
            => typeof(T).GetConstructor(context, null, pms, null);
        internal static ConstructorInfo ctorof<T>(params Type[] pms) 
            => typeof(T).GetConstructor(pms);

        internal static void dump(this ILContext il, string rf)
        {
            var oname = il.Method.FullName.SkipWhile(c => Path.GetInvalidPathChars().Contains(c));
            var sb = new StringBuilder();
            foreach (var c in oname) sb.Append(c);
            File.WriteAllText(Path.Combine(rf, sb.ToString()), il.ToString());
        }
        internal static Instruction CurrentInstruction(this ILCursor c) => c.Instrs[c.Index];
        #endregion
        #region collection extensions
        internal static void SetKey<tKey, tValue>(this Dictionary<tKey, tValue> dict, tKey key, tValue val) {
            if (dict == null) throw new ArgumentNullException();
            if (!dict.ContainsKey(key)) dict.Add(key, val);
            else dict[key] = val;
        }
        internal static void TryRemoveKey<tKey, tVal>(this Dictionary<tKey, tVal> dict, tKey key)
        {
            if (dict.ContainsKey(key)) dict.Remove(key);
        }
        internal static bool IndexInRange(this object[] arr, int index) => index > -1 && index < arr.Length;
        #endregion
        #region randomization extensions
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
        #endregion
        #region misc bs
        internal static void LogBunch(__arglist)
        {
            var ai = new ArgIterator(__arglist);
            while (ai.GetRemainingCount() != 0)
            {
                Debug.Log(TypedReference.ToObject(ai.GetNextArg()));
            }
        }
        internal static string combinePath(params string[] parts) => parts.Aggregate(Path.Combine);
        internal static RainWorld CRW => UnityEngine.Object.FindObjectOfType<RainWorld>();
        internal static CreatureTemplate GetCreatureTemplate(CreatureTemplate.Type t) => StaticWorld.creatureTemplates[(int)t];
        #endregion
    }
}