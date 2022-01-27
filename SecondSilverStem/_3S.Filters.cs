using System;
using System.Collections;
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

using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;
using static WaspPile.SecondSilverStem._3SUTL;

using URand = UnityEngine.Random;

namespace WaspPile.SecondSilverStem
{
    public static partial class _3S
    {
        public class InstrMatchBlock
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="oc">opcode</param>
            /// <param name="matchData">string array pattern for matching operands. 0th elm is still opcode because there's no using system.range in nf3.5</param>
            public InstrMatchBlock(OpCode oc, string[] matchData, ILPattern ow)
            {
                _data = matchData;
                _op = oc;
                pattern = ow;
                var ndt = new string[Math.Max(0, matchData.Length - 1)];
                Array.Copy(matchData, 1, ndt, 0, ndt.Length);
                _rootMatcher = oc.OperandType switch
                {
                    OperandType.InlineNone => new Wildcard(this, new string[0]),
                    //primitives
                    OperandType.InlineString
                    or OperandType.ShortInlineR
                    or OperandType.InlineR
                    or OperandType.InlineI8
                    or OperandType.ShortInlineI
                    or OperandType.InlineI 
                    or OperandType.InlineI8 => new SlowPrimitiveMatcher(this, ndt),
                    //types, fields, methods, locvars
                    OperandType.InlineField => new FieldMatcher(this, ndt),
                    OperandType.InlineMethod => new MethodMatcher(this, ndt),
                    OperandType.InlineType => new TypeMatcher(this, ndt),
                    OperandType.ShortInlineVar 
                    or OperandType.InlineVar => new LocVarMatcher(this, ndt),
                    //jumps
                    OperandType.InlineSwitch => new LabelArrayMatcher(this, ndt),
                    OperandType.ShortInlineBrTarget 
                    or OperandType.InlineBrTarget => new LabelMatcher(this, ndt),
                    //others
                    //OperandType.InlineTok => throw new NotImplementedException(),
                    //OperandType.InlineArg => throw new NotImplementedException(),
                    //OperandType.ShortInlineArg => throw new NotImplementedException(),
                    //OperandType.InlinePhi => throw new NotImplementedException(),
                    //OperandType.InlineSig => throw new NotImplementedException(),
                    _ => null,
                };
                if (DebugMode) LogWarning($"Root matcher created: {(object)_rootMatcher ?? ("null")}");
            }

            public bool Match(Instruction instr) 
                => instr.OpCode == _op && (_rootMatcher?.Match(instr.Operand) ?? true);
            internal readonly string[] _data;
            internal readonly OpCode _op;
            internal readonly OperandMatcherBase _rootMatcher;
            internal readonly ILPattern pattern;
            /// <summary>
            /// converts a <see cref="InstrMatchBlock"/> into a predicate for <see cref="ILCursor.GotoNext(Func{Instruction, bool}[])"/>-likes.
            /// </summary>
            /// <param name="imb"></param>
            public static explicit operator Func<Instruction, bool> (InstrMatchBlock imb)
                => imb.Match;
        }
        /// <summary>
        /// base class for creating operand comparers
        /// </summary>
        public abstract class OperandMatcherBase
        {
            public OperandMatcherBase(InstrMatchBlock imb, params string[] data) { _data = data; }
            internal readonly string[] _data;
            internal string _data0 => _data.FirstOrDefault();
            /// <summary>
            /// sets whether a matcher should return true when a comparison result is inconclusive.
            /// Example: <see cref="MethodMatcher"/> encountered a method code that was not bound to anything on <see cref="ILPatternCollection"/> level.
            /// </summary>
            public bool Lazy;

            internal InstrMatchBlock _matchblock;
            public ILPattern Pattern => _matchblock?.pattern;
            public ILPatternCollection Collection => _matchblock?.pattern?._pcollection;

            public abstract bool Match(object operand);
        }
        /// <summary>
        /// base for type specific matchers
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract class OperandMatcherG<T> : OperandMatcherBase
        {
            protected OperandMatcherG(InstrMatchBlock imb, params string[] data) : base(imb, data)
            {

            }
            public override sealed bool Match(object operand) => operand is T t && MatchG(t);
            public abstract bool MatchG(T operand);
        }
        /// <summary>
        /// Covers most runtime primitive types. Works through reflection, which is slow!
        /// </summary>
        public class SlowPrimitiveMatcher : OperandMatcherBase
        {
            public SlowPrimitiveMatcher(InstrMatchBlock imb, params string[] data) : base(imb, data) { }
            public override bool Match(object operand)
            {
                try
                {
                    var mt = operand.GetType();
                    valcache.TryGetValue(mt, out var cachedRes);
                    cachedRes ??= AttemptParseRefl(mt, _data0);
                    valcache.SetKey(mt, cachedRes);
                    return operand == cachedRes;
                }
                catch { return Lazy; }
            }
            private Dictionary<Type, object> valcache = new();
        }
        public class Wildcard : OperandMatcherBase
        {
            public Wildcard(InstrMatchBlock imb, params string[] data) : base(imb, data) { Lazy = true; }
            public override bool Match(object operand) => true;
        }
        public class LocVarMatcher : OperandMatcherG<VariableDefinition>
        {
            public LocVarMatcher(InstrMatchBlock imb, params string[] data) : base(imb, data) { }
            public override bool MatchG(VariableDefinition operand)
            {
                if (int.TryParse(_data0, out var vin)) return operand.Index == vin;
                else return Lazy;
            }
        }
        public class MethodMatcher : OperandMatcherG<MethodBase>
        {
            /// <param name="data">0: method bind</param>
            public MethodMatcher(InstrMatchBlock imb, params string[] data) : base(imb, data) { }
            public override bool MatchG(MethodBase operand)
            {
                Collection._procdefs.TryGetValue(_data0, out var exM);
                return exM?.Equals(operand) ?? Lazy;
            }
        }
        public class FieldMatcher : OperandMatcherG<FieldInfo>
        {
            public FieldMatcher(InstrMatchBlock imb, params string[] data) : base(imb, data) { }
            public override bool MatchG(FieldInfo operand)
            {
                Collection._fielddefs.TryGetValue(_data0, out var fld);
                return fld?.Equals(operand) ?? Lazy;
            }
        }
        public class TypeMatcher : OperandMatcherG<Type>
        {
            public TypeMatcher(InstrMatchBlock imb, params string[] data) : base(imb, data) { }

            public override bool MatchG(Type operand)
            {
                if (Collection._typedefs.TryGetValue(_data0, out var mt)) return mt == operand;
                else return Lazy;
            }
        }
        public class LabelMatcher : OperandMatcherG<ILLabel>
        {
            public LabelMatcher(InstrMatchBlock imb, params string[] data) : base(imb, data) { }
            public override bool MatchG(ILLabel operand)
            {
                return Lazy ? operand.ToString().Contains(_data0) : operand.ToString() == _data0;
            }
        }
        public class LabelArrayMatcher : OperandMatcherG<ILLabel[]>
        {
            public LabelArrayMatcher(InstrMatchBlock imb, params string[] data) : base(imb, data)
            {
                OpenEnded = data.LastOrDefault() == "???";
                for (int i = 0; i < data.Length - 1; i++)
                {
                    OperandMatcherBase newChild = default;
                    var css = data[i + 1];
                    if (css == "??")
                    {
                        newChild = new Wildcard(_matchblock, css) { };
                        goto cycleEnd;
                    }
                    bool clazy = false;
                    
                    if (css.StartsWith("?"))
                    {
                        clazy = true;
                        css = css[1..];
                    }
                    newChild = new LabelMatcher(_matchblock, css) { Lazy = clazy};

                    cycleEnd:
                    if (newChild is not null) _children.Add(i, newChild);
                }
            }
            public readonly bool OpenEnded;
            internal readonly Dictionary<int, OperandMatcherBase> _children = new();
            public override bool MatchG(ILLabel[] operand)
            {
                bool stillSuccess = true;
                for (int i = 0; i < operand.Length; i++)
                {
                    if (_children.TryGetValue(i, out var cmat)) 
                    {
                        stillSuccess &= cmat.Match(operand[i]);
                    }
                    else
                    {
                        stillSuccess &= OpenEnded;
                        break;
                    }
                }
                return stillSuccess;
            }
        }
    }
}
