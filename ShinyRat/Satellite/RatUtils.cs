using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.IO;
using System.Threading;
using RWCustom;

using static UnityEngine.Mathf;
using static RWCustom.Custom;

using URand = UnityEngine.Random;

namespace WaspPile.ShinyRat.Satellite
{
    internal static class RatUtils
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
        //bruh it literally dies on unimono
        //internal static MethodInfo methodof_c(string mname, BindingFlags context = allContextsStatic)
        //{
        //    //go crazy go stupid
        //    var c = new System.Diagnostics.StackTrace(Thread.CurrentThread, false);
        //    var caller = c.GetFrames()[1];
        //    return caller.GetMethod().DeclaringType.GetMethod(mname, context);
        //}
        internal static MethodInfo methodofdel<Tm>(Tm m) where Tm : Delegate => m.Method;
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

        internal static void dump(this ILContext il, string rf, string nameOverride = default)
        {
            var oname = il.Method.FullName.SkipWhile(c => Path.GetInvalidPathChars().Contains(c));
            var sb = new StringBuilder();
            foreach (var c in oname) sb.Append(c);
            File.WriteAllText(Path.Combine(rf, nameOverride ?? sb.ToString()), il.ToString());
        }
        internal static Instruction CurrentInstruction(this ILCursor c) => c.Instrs[c.Index];
        internal static FieldInfo fieldof<T>(string name, BindingFlags context = allContextsInstance)
            => typeof(T).GetField(name, context);
        internal static void CleanUp(this Type t)
        {
            foreach (var fld in t.GetFields(allContextsStatic))
            {
                try
                {
                    if (!fld.FieldType.IsValueType) fld.SetValue(null, default);
                }
                catch { }
            }
        }
        #endregion
        #region collection extensions
        internal static void SetKey<tKey, tValue>(this Dictionary<tKey, tValue> dict, tKey key, tValue val)
        {
            if (dict == null) throw new ArgumentNullException();
            if (!dict.ContainsKey(key)) dict.Add(key, val);
            else dict[key] = val;
        }
        internal static void TryRemoveKey<tKey, tVal>(this Dictionary<tKey, tVal> dict, tKey key)
        {
            if (dict.ContainsKey(key)) dict.Remove(key);
        }
        internal static bool IndexInRange(this object[] arr, int index) => index > -1 && index < arr.Length;
        internal static T RandomOrDefault<T>(this T[] arr)
        {
            var res = default(T);
            if (arr.Length > 0) return arr[URand.Range(0, arr.Length)];
            return res;
        }
        #endregion
        #region randomization extensions
        internal static float RandSign() => URand.value > 0.5f ? -1f : 1f;
        internal static Vector2 V2RandLerp(Vector2 a, Vector2 b) => Vector2.Lerp(a, b, URand.value);
        internal static float NextFloat01(this System.Random r) => (float)(r.NextDouble() / double.MaxValue);
        internal static Color Clamped(this Color bcol) => new(Clamp01(bcol.r), Clamp01(bcol.g), Clamp01(bcol.b));
        internal static Color RandDev(this Color bcol, Color dbound, bool clamped = true)
        {
            Color res = default;
            for (int i = 0; i < 3; i++) res[i] = bcol[i] + dbound[i] * URand.Range(-1f, 1f);
            return clamped ? res.Clamped() : res;
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
        internal static Vector2 MiddleOfRoom(this Room rm) => new((float)rm.PixelWidth * 0.5f, (float)rm.PixelHeight * 0.5f);
        internal static bool TryParseEnum<T>(this string s, out T res) where T : Enum
        {
            res = default;
            var mt = typeof(T);
            if (Enum.GetNames(mt).Contains(s))
            {
                res = (T)Enum.Parse(mt, s);
                return true;
            }
            return false;
        }
        internal static readonly Color echoGold = HSL2RGB(0.13f, 1, 0.63f);
        internal static void Deconstruct<T1, T2, T3>(this (T1, T2, T3) tp, out T1 o1, out T2 o2, out T3 o3)
        {
            o1 = tp.Item1; o2 = tp.Item2; o3 = tp.Item3;
        }
        internal static TOut TryGetAndParse<TOut>(this Dictionary<string, string> dict, string key, TOut defval = default)
        {
            //var parseMethod = ;
            Type mt = typeof(TOut);
            MethodInfo parseMethod = mt switch
            {
                _ when mt == typeof(Color) => methodof<OptionalUI.OpColorPicker>("HexToColor", allContextsStatic),
                _ when mt == typeof(string) => methodof(typeof(RatUtils), nameof(stringretself)),
                _ => typeof(TOut).GetMethod("parse", allContextsStatic, null, new[] { typeof(string) }, null)
            };
            if (!dict.TryGetValue(key, out var rawval)) return defval;
            try
            {
                return (TOut)parseMethod.Invoke(null, new[] { rawval });
            }
            catch
            {

            }
            if (ShinyRatPlugin.DebugMode) Debug.LogWarning($"Failed parsing value for {typeof(TOut)}");
            return defval;
        }
        internal static string stringretself(string x) => x;
        #endregion
    }
}

