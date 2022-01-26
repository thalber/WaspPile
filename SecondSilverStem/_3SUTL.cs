using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RWCustom;
using UnityEngine;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;

using URand = UnityEngine.Random;
using System.Collections;

namespace WaspPile.SecondSilverStem
{
    public static partial class _3SUTL
    {
        //reflection flag templates
        public const BindingFlags allContextsInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        public const BindingFlags allContextsStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        public const BindingFlags allContextsCtor = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

        #region refl/cil helpers
        public static string pbfiname(string propname) => $"<{propname}>k__BackingField";
        /// <summary>
        /// takes methodinfo from T, defaults to <see cref="allContextsInstance"/>
        /// </summary>
        /// <typeparam name="T">target type</typeparam>
        /// <param name="mname">methodname</param>
        /// <param name="context">binding flags, default private+public+instance</param>
        /// <returns></returns>
        public static MethodInfo methodof<T>(string mname, BindingFlags context = allContextsInstance)
            => typeof(T).GetMethod(mname, context);
        /// <summary>
        /// takes methodinfo from t, defaults to <see cref="allContextsStatic"/>
        /// </summary>
        /// <param name="t"></param>
        /// <param name="mname"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static MethodInfo methodof(Type t, string mname, BindingFlags context = allContextsStatic)
            => t.GetMethod(mname, context);
        //bruh it literally dies on unimono
        //public static MethodInfo methodof_c(string mname, BindingFlags context = allContextsStatic)
        //{
        //    //go crazy go stupid
        //    var c = new System.Diagnostics.StackTrace(Thread.CurrentThread, false);
        //    var caller = c.GetFrames()[1];
        //    return caller.GetMethod().DeclaringType.GetMethod(mname, context);
        //}
        public static MethodInfo methodofdel<Tm>(Tm m) where Tm : Delegate => m.Method;
        /// <summary>
        /// gets constructorinfo from T. no static ctors by default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="pms"></param>
        /// <returns></returns>
        public static ConstructorInfo ctorof<T>(BindingFlags context = allContextsCtor, params Type[] pms)
            => typeof(T).GetConstructor(context, null, pms, null);
        public static ConstructorInfo ctorof<T>(params Type[] pms)
            => typeof(T).GetConstructor(pms);

        public static void dump(this ILContext il, string rf, string nameOverride = default)
        {
            var oname = il.Method.FullName.SkipWhile(c => Path.GetInvalidPathChars().Contains(c));
            var sb = new StringBuilder();
            foreach (var c in oname) sb.Append(c);
            File.WriteAllText(Path.Combine(rf, nameOverride ?? sb.ToString()), il.ToString());
        }
        public static Instruction CurrentInstruction(this ILCursor c) => c.Instrs[c.Index];
        public static FieldInfo fieldof<T>(string name, BindingFlags context = allContextsInstance)
            => typeof(T).GetField(name, context);
        public static void CleanUp(this Type t)
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
        public static void SetKey<tKey, tValue>(this Dictionary<tKey, tValue> dict, tKey key, tValue val)
        {
            if (dict == null) throw new ArgumentNullException();
            if (!dict.ContainsKey(key)) dict.Add(key, val);
            else dict[key] = val;
        }
        public static void TryRemoveKey<tKey, tVal>(this Dictionary<tKey, tVal> dict, tKey key)
        {
            if (dict.ContainsKey(key)) dict.Remove(key);
        }
        public static bool IndexInRange(this object[] arr, int index) => index > -1 && index < arr.Length;
        public static T RandomOrDefault<T>(this T[] arr)
        {
            var res = default(T);
            if (arr.Length > 0) return arr[URand.Range(0, arr.Length)];
            return res;
        }
        #endregion
        #region randomization extensions
        public static float RandSign() => URand.value > 0.5f ? -1f : 1f;
        public static Vector2 V2RandLerp(Vector2 a, Vector2 b) => Vector2.Lerp(a, b, URand.value);
        public static float NextFloat01(this System.Random r) => (float)(r.NextDouble() / double.MaxValue);
        public static Color Clamped(this Color bcol) => new(Clamp01(bcol.r), Clamp01(bcol.g), Clamp01(bcol.b));
        public static Color RandDev(this Color bcol, Color dbound, bool clamped = true)
        {
            Color res = default;
            for (int i = 0; i < 3; i++) res[i] = bcol[i] + dbound[i] * URand.Range(-1f, 1f);
            return clamped ? res.Clamped() : res;
        }
        #endregion
        #region misc bs
        public static void LogBunch(__arglist)
        {
            var ai = new ArgIterator(__arglist);
            while (ai.GetRemainingCount() != 0)
            {
                UnityEngine.Debug.Log(TypedReference.ToObject(ai.GetNextArg()));
            }
        }
        public static string combinePath(params string[] parts) => parts.Aggregate(Path.Combine);
        public static RainWorld CRW => UnityEngine.Object.FindObjectOfType<RainWorld>();
        public static CreatureTemplate GetCreatureTemplate(CreatureTemplate.Type t) => StaticWorld.creatureTemplates[(int)t];
        public static Vector2 MiddleOfRoom(this Room rm) => new((float)rm.PixelWidth * 0.5f, (float)rm.PixelHeight * 0.5f);
        public static bool TryParseEnum<T>(this string s, out T res) where T : Enum
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
        public static readonly Color echoGold = HSL2RGB(0.13f, 1, 0.63f);

        public static void Deconstruct<T1, T2>(this (T1, T2) tp, out T1 o1, out T2 o2) { o1 = tp.Item1; o2 = tp.Item2; }
        public static void Deconstruct<T1, T2, T3>(this (T1, T2, T3) tp, out T1 o1, out T2 o2, out T3 o3) { o1 = tp.Item1; o2 = tp.Item2; o3 = tp.Item3; }

        public static TD MakeDel<TD>(MethodInfo m) where TD : Delegate => (TD)Delegate.CreateDelegate(typeof(TD), m);
        public static object AttemptParseRefl(Type mt, string rawval)
        {
            
            MethodInfo parseMethod = mt switch
            {
                _ when mt == typeof(Color) => methodof<OptionalUI.OpColorPicker>("HexToColor", allContextsStatic),
                _ when mt == typeof(string) => methodof(typeof(_3SUTL), nameof(stringretself)),
                _ => mt.GetMethod("Parse", allContextsStatic, null, new[] { typeof(string) }, null)
            };
            return parseMethod.Invoke(null, new[] { rawval });
        }
        public static TOut TryGetAndParse<TKey, TOut>(this Dictionary<TKey, string> dict, TKey key, TOut defval = default)
        {
            if (!dict.TryGetValue(key, out var rawval)) return defval;
            var mt = typeof(TOut);
            try
            {
                return (TOut)AttemptParseRefl(mt, rawval);
            }
            catch (Exception e)
            {
                if (_3S.DebugMode) LogWarning("Parse invocation fail!\n" + e);
            }
            if (_3S.DebugMode) LogWarning($"_3S: Could not parse value for {typeof(TOut)}");
            return defval;
        }
        public static string stringretself(string x) => x;
        #endregion
    }
}

#region nfvcr
namespace System
{
    public struct ValueTuple<T1, T2> : IEquatable<ValueTuple<T1, T2>>, IComparable, IComparable<ValueTuple<T1, T2>>
    {
        /// <summary>
        /// The current <see cref="ValueTuple{T1, T2}"/> instance's first component.
        /// </summary>
        public T1 Item1;

        /// <summary>
        /// The current <see cref="ValueTuple{T1, T2}"/> instance's first component.
        /// </summary>
        public T2 Item2;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTuple{T1, T2}"/> value type.
        /// </summary>
        /// <param name="item1">The value of the tuple's first component.</param>
        /// <param name="item2">The value of the tuple's second component.</param>
        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        /// <summary>
        /// Returns a value that indicates whether the current <see cref="ValueTuple{T1, T2}"/> instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/> if the current instance is equal to the specified object; otherwise, <see langword="false"/>.</returns>
        ///
        /// <remarks>
        /// The <paramref name="obj"/> parameter is considered to be equal to the current instance under the following conditions:
        /// <list type="bullet">
        ///     <item><description>It is a <see cref="ValueTuple{T1, T2}"/> value type.</description></item>
        ///     <item><description>Its components are of the same types as those of the current instance.</description></item>
        ///     <item><description>Its components are equal to those of the current instance. Equality is determined by the default object equality comparer for each component.</description></item>
        /// </list>
        /// </remarks>
        public override bool Equals(object obj)
        {
            return obj is ValueTuple<T1, T2> tuple && Equals(tuple);
        }

        /// <summary>
        /// Returns a value that indicates whether the current <see cref="ValueTuple{T1, T2}"/> instance is equal to a specified <see cref="ValueTuple{T1, T2}"/>.
        /// </summary>
        /// <param name="other">The tuple to compare with this instance.</param>
        /// <returns><see langword="true"/> if the current instance is equal to the specified tuple; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The <paramref name="other"/> parameter is considered to be equal to the current instance if each of its fields
        /// are equal to that of the current instance, using the default comparer for that field's type.
        /// </remarks>
        public bool Equals(ValueTuple<T1, T2> other)
        {
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);
        }

        int IComparable.CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }

            if (other is not ValueTuple<T1, T2>)
            {
                throw new ArgumentException();
            }

            return CompareTo((ValueTuple<T1, T2>)other);
        }

        /// <summary>Compares this instance to a specified instance and returns an indication of their relative values.</summary>
        /// <param name="other">An instance to compare.</param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and <paramref name="other"/>.
        /// Returns less than zero if this instance is less than <paramref name="other"/>, zero if this
        /// instance is equal to <paramref name="other"/>, and greater than zero if this instance is greater 
        /// than <paramref name="other"/>.
        /// </returns>
        public int CompareTo(ValueTuple<T1, T2> other)
        {
            int c = Comparer<T1>.Default.Compare(Item1, other.Item1);
            if (c != 0)
            {
                return c;
            }

            return Comparer<T2>.Default.Compare(Item2, other.Item2);
        }

        /// <summary>
        /// Returns the hash code for the current <see cref="ValueTuple{T1, T2}"/> instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Item1?.GetHashCode() ?? 0;
                hash = hash * 31 + Item2?.GetHashCode() ?? 0;
                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the value of this <see cref="ValueTuple{T1, T2}"/> instance.
        /// </summary>
        /// <returns>The string representation of this <see cref="ValueTuple{T1, T2}"/> instance.</returns>
        /// <remarks>
        /// The string returned by this method takes the form <c>(Item1, Item2)</c>,
        /// where <c>Item1</c> and <c>Item2</c> represent the values of the <see cref="Item1"/>
        /// and <see cref="Item2"/> fields. If either field value is <see langword="null"/>,
        /// it is represented as <see cref="String.Empty"/>.
        /// </remarks>
        public override string ToString()
        {
            return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ")";
        }
    }
    public struct ValueTuple<T1, T2, T3>
        : IEquatable<ValueTuple<T1, T2, T3>>, IComparable, IComparable<ValueTuple<T1, T2, T3>>
    {
        /// <summary>
        /// The current <see cref="ValueTuple{T1, T2, T3}"/> instance's first component.
        /// </summary>
        public T1 Item1;
        /// <summary>
        /// The current <see cref="ValueTuple{T1, T2, T3}"/> instance's second component.
        /// </summary>
        public T2 Item2;
        /// <summary>
        /// The current <see cref="ValueTuple{T1, T2, T3}"/> instance's third component.
        /// </summary>
        public T3 Item3;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTuple{T1, T2, T3}"/> value type.
        /// </summary>
        /// <param name="item1">The value of the tuple's first component.</param>
        /// <param name="item2">The value of the tuple's second component.</param>
        /// <param name="item3">The value of the tuple's third component.</param>
        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        /// <summary>
        /// Returns a value that indicates whether the current <see cref="ValueTuple{T1, T2, T3}"/> instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/> if the current instance is equal to the specified object; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The <paramref name="obj"/> parameter is considered to be equal to the current instance under the following conditions:
        /// <list type="bullet">
        ///     <item><description>It is a <see cref="ValueTuple{T1, T2, T3}"/> value type.</description></item>
        ///     <item><description>Its components are of the same types as those of the current instance.</description></item>
        ///     <item><description>Its components are equal to those of the current instance. Equality is determined by the default object equality comparer for each component.</description></item>
        /// </list>
        /// </remarks>
        public override bool Equals(object? obj)
        {
            return obj is ValueTuple<T1, T2, T3> tuple && Equals(tuple);
        }

        /// <summary>
        /// Returns a value that indicates whether the current <see cref="ValueTuple{T1, T2, T3}"/>
        /// instance is equal to a specified <see cref="ValueTuple{T1, T2, T3}"/>.
        /// </summary>
        /// <param name="other">The tuple to compare with this instance.</param>
        /// <returns><see langword="true"/> if the current instance is equal to the specified tuple; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The <paramref name="other"/> parameter is considered to be equal to the current instance if each of its fields
        /// are equal to that of the current instance, using the default comparer for that field's type.
        /// </remarks>
        public bool Equals(ValueTuple<T1, T2, T3> other)
        {
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2)
                && EqualityComparer<T3>.Default.Equals(Item3, other.Item3);
        }

        int IComparable.CompareTo(object? other)
        {
            if (other is not null)
            {
                if (other is ValueTuple<T1, T2, T3> objTuple)
                {
                    return CompareTo(objTuple);
                }
                throw new ArgumentException("incorrect tuple types");
                //ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
            }

            return 1;
        }

        /// <summary>Compares this instance to a specified instance and returns an indication of their relative values.</summary>
        /// <param name="other">An instance to compare.</param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and <paramref name="other"/>.
        /// Returns less than zero if this instance is less than <paramref name="other"/>, zero if this
        /// instance is equal to <paramref name="other"/>, and greater than zero if this instance is greater
        /// than <paramref name="other"/>.
        /// </returns>
        public int CompareTo(ValueTuple<T1, T2, T3> other)
        {
            int c = Comparer<T1>.Default.Compare(Item1, other.Item1);
            if (c != 0) return c;

            c = Comparer<T2>.Default.Compare(Item2, other.Item2);
            if (c != 0) return c;

            return Comparer<T3>.Default.Compare(Item3, other.Item3);
        }

        /// <summary>
        /// Returns the hash code for the current <see cref="ValueTuple{T1, T2, T3}"/> instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Item1?.GetHashCode() ?? 0 + Item2?.GetHashCode() ?? 0 + Item3?.GetHashCode() ?? 0;
        }
        /// <summary>
        /// Returns a string that represents the value of this <see cref="ValueTuple{T1, T2, T3}"/> instance.
        /// </summary>
        /// <returns>The string representation of this <see cref="ValueTuple{T1, T2, T3}"/> instance.</returns>
        /// <remarks>
        /// The string returned by this method takes the form <c>(Item1, Item2, Item3)</c>.
        /// If any field value is <see langword="null"/>, it is represented as <see cref="string.Empty"/>.
        /// </remarks>
        public override string ToString()
        {
            return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ")";
        }
    }
    /// <summary>Represent a type can be used to index a collection either from the start or the end.</summary>
    /// <remarks>
    /// Index is used by the C# compiler to support the new index syntax
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 } ;
    /// int lastElement = someArray[^1]; // lastElement = 5
    /// </code>
    /// </remarks>
    public readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        /// <summary>Construct an Index using a value and indicating if the index is from the start or from the end.</summary>
        /// <param name="value">The index value. it has to be zero or positive number.</param>
        /// <param name="fromEnd">Indicating if the index is from the start or from the end.</param>
        /// <remarks>
        /// If the Index constructed from the end, index value 1 means pointing at the last element and index value 0 means pointing at beyond last element.
        /// </remarks>
        public Index(int value, bool fromEnd = false)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Non-negative number required.");
            }

            if (fromEnd)
            {
                _value = ~value;
            }
            else
            {
                _value = value;
            }
        }

        // The following private constructors mainly created for perf reason to avoid the checks
        private Index(int value)
        {
            _value = value;
        }

        /// <summary>Create an Index pointing at first element.</summary>
        public static Index Start => new(0);

        /// <summary>Create an Index pointing at beyond last element.</summary>
        public static Index End => new(~0);

        /// <summary>Create an Index from the start at the position indicated by the value.</summary>
        /// <param name="value">The index value from the start.</param>
        public static Index FromStart(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Non-negative number required.");
            }

            return new Index(value);
        }

        /// <summary>Create an Index from the end at the position indicated by the value.</summary>
        /// <param name="value">The index value from the end.</param>
        public static Index FromEnd(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Non-negative number required.");
            }

            return new Index(~value);
        }

        /// <summary>Returns the index value.</summary>
        public int Value
        {
            get
            {
                if (_value < 0)
                {
                    return ~_value;
                }
                else
                {
                    return _value;
                }
            }
        }

        /// <summary>Indicates whether the index is from the start or the end.</summary>
        public bool IsFromEnd => _value < 0;

        /// <summary>Calculate the offset from the start using the giving collection length.</summary>
        /// <param name="length">The length of the collection that the Index will be used with. length has to be a positive value</param>
        /// <remarks>
        /// For performance reason, we don't validate the input length parameter and the returned offset value against negative values.
        /// we don't validate either the returned offset is greater than the input length.
        /// It is expected Index will be used with collections which always have non negative length/count. If the returned offset is negative and
        /// then used to index a collection will get out of range exception which will be same affect as the validation.
        /// </remarks>
        public int GetOffset(int length)
        {
            int offset = _value;
            if (IsFromEnd)
            {
                // offset = length - (~value)
                // offset = length + (~(~value) + 1)
                // offset = length + value + 1

                offset += length + 1;
            }
            return offset;
        }

        /// <summary>Indicates whether the current Index object is equal to another object of the same type.</summary>
        /// <param name="value">An object to compare with this object</param>
        public override bool Equals(object? value) => value is Index index && _value == index._value;

        /// <summary>Indicates whether the current Index object is equal to another Index object.</summary>
        /// <param name="other">An object to compare with this object</param>
        public bool Equals(Index other) => _value == other._value;

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode() => _value;

        /// <summary>Converts integer number to an Index.</summary>
        public static implicit operator Index(int value) => FromStart(value);

        /// <summary>Converts the value of the current Index object to its equivalent string representation.</summary>
        public override string ToString()
        {
            if (IsFromEnd)
            {
                return $"^{(uint)Value}";
            }

            return ((uint)Value).ToString();
        }
    }

    /// <summary>Represent a range has start and end indexes.</summary>
    /// <remarks>
    /// Range is used by the C# compiler to support the range syntax.
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 };
    /// int[] subArray1 = someArray[0..2]; // { 1, 2 }
    /// int[] subArray2 = someArray[1..^0]; // { 2, 3, 4, 5 }
    /// </code>
    /// </remarks>
    public readonly struct Range : IEquatable<Range>
    {
        /// <summary>Represent the inclusive start index of the Range.</summary>
        public Index Start { get; }

        /// <summary>Represent the exclusive end index of the Range.</summary>
        public Index End { get; }

        /// <summary>Construct a Range object using the start and end indexes.</summary>
        /// <param name="start">Represent the inclusive start index of the range.</param>
        /// <param name="end">Represent the exclusive end index of the range.</param>
        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        /// <summary>Indicates whether the current Range object is equal to another object of the same type.</summary>
        /// <param name="value">An object to compare with this object</param>
        public override bool Equals(object? value) =>
            value is Range r &&
            r.Start.Equals(Start) &&
            r.End.Equals(End);

        /// <summary>Indicates whether the current Range object is equal to another Range object.</summary>
        /// <param name="other">An object to compare with this object</param>
        public bool Equals(Range other) => other.Start.Equals(Start) && other.End.Equals(End);

        /// <summary>Returns the hash code for this instance.</summary>
        //public override int GetHashCode()
        //{
        //    return HashHelpers.Combine(Start.GetHashCode(), End.GetHashCode());
        //}

        /// <summary>Converts the value of the current Range object to its equivalent string representation.</summary>
        public override string ToString()
        {
            return Start.ToString() + ".." + End.ToString();
        }

        /// <summary>Create a Range object starting from start index to the end of the collection.</summary>
        public static Range StartAt(Index start) => new Range(start, Index.End);

        /// <summary>Create a Range object starting from first element in the collection to the end Index.</summary>
        public static Range EndAt(Index end) => new Range(Index.Start, end);

        /// <summary>Create a Range object starting from first element to the end.</summary>
        public static Range All => new Range(Index.Start, Index.End);

        /// <summary>Calculate the start offset and length of range object using a collection length.</summary>
        /// <param name="length">The length of the collection that the range will be used with. length has to be a positive value.</param>
        /// <remarks>
        /// For performance reason, we don't validate the input length parameter against negative values.
        /// It is expected Range will be used with collections which always have non negative length/count.
        /// We validate the range is inside the length scope though.
        /// </remarks>
        public (int, int) GetOffsetAndLength(int length)
        {
            int start;
            Index startIndex = Start;
            if (startIndex.IsFromEnd)
                start = length - startIndex.Value;
            else
                start = startIndex.Value;

            int end;
            Index endIndex = End;
            if (endIndex.IsFromEnd)
                end = length - endIndex.Value;
            else
                end = endIndex.Value;

            if ((uint)end > (uint)length || (uint)start > (uint)end)
            {
                throw new ArgumentException("length");
            }

            return (start, end - start);
        }
    }
}
#endregion
