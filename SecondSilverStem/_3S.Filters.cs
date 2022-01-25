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
        public class OperandMatcherBlock
        {
            public OperandMatcherBlock(OpCode oc, string[] matchData)
            {
                _data = matchData;
            }
            public OperandMatcherBase makeMatcher(OpCode oc, string subs)
            {
                int y = 2;
                var l = y << 1;
                OperandMatcherBase res = default;
                switch (oc.OperandType)
                {
#error figure how the fuck to bind stuff compactly
                    case OperandType.InlineBrTarget:
                        break;
                    case OperandType.InlineField:
                        break;
                    case OperandType.InlineI:
                        break;
                    case OperandType.InlineI8:
                        break;
                    case OperandType.InlineMethod:
                        break;
                    case OperandType.InlineNone:
                        break;
                    case OperandType.InlinePhi:
                        break;
                    case OperandType.InlineR:
                        break;
                    case OperandType.InlineSig:
                        break;
                    case OperandType.InlineString:
                        break;
                    case OperandType.InlineSwitch:
                        break;
                    case OperandType.InlineTok:
                        break;
                    case OperandType.InlineType:
                        break;
                    case OperandType.InlineVar:
                        break;
                    case OperandType.InlineArg:
                        break;
                    case OperandType.ShortInlineBrTarget:
                        break;
                    case OperandType.ShortInlineI:
                        break;
                    case OperandType.ShortInlineR:
                        break;
                    case OperandType.ShortInlineVar:
                        break;
                    case OperandType.ShortInlineArg:
                        break;
                    default:
                        break;
                }
                return res;
            }

            internal string[] _data;
        }

        public abstract class OperandMatcherBase
        {
            public OperandMatcherBase(string data, bool lazy = false)
            {
                _data = data;
                _lazy = lazy;
            }
            internal readonly string _data;
            internal readonly bool _lazy;
            public abstract bool Match(object operand);
        }
        public abstract class OperandMatcherG<T> : OperandMatcherBase
        {
            protected OperandMatcherG(string data) : base(data)
            {

            }
            public override sealed bool Match(object operand) => operand is T t && MatchG(t);
            protected abstract bool MatchG(T operand);
        }
        public class SlowPrimitiveMatcher : OperandMatcherBase
        {
            public SlowPrimitiveMatcher(string data) : base(data)
            {
            }

            public override bool Match(object operand)
            {
                try
                {
                    return operand == AttemptParseRefl(operand.GetType(), _data);
                }
                catch { return false; }
            }
        }
        public class LabelMatcher : OperandMatcherG<ILLabel>
        {
            public LabelMatcher(string data) : base(data) { }
            protected override bool MatchG(ILLabel operand)
            {
                int.TryParse(_data, out var r);
                return operand.ToString() == _data || operand.Target.Offset == r;
            }
        }
        public class Wildcard : OperandMatcherBase
        {
            public Wildcard(string data) : base(data) { }
            public override bool Match(object operand) => true;
        }
    }
}
