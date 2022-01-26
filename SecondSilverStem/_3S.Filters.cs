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
            public InstrMatchBlock(OpCode oc, string[] matchData)
            {
                _data = matchData;
            }
            public OperandMatcherBase makeMatcher(OpCode oc)
            {
                int y = 2;
                var l = y << 1;
                OperandMatcherBase res = default;
                res = oc.OperandType switch
                {
                    OperandType.InlineBrTarget => throw new NotImplementedException(),
                    OperandType.InlineField => throw new NotImplementedException(),
                    OperandType.InlineI => throw new NotImplementedException(),
                    OperandType.InlineI8 => throw new NotImplementedException(),
                    OperandType.InlineMethod => throw new NotImplementedException(),
                    OperandType.InlineNone => throw new NotImplementedException(),
                    OperandType.InlinePhi => throw new NotImplementedException(),
                    OperandType.InlineR => throw new NotImplementedException(),
                    OperandType.InlineSig => throw new NotImplementedException(),
                    OperandType.InlineString => throw new NotImplementedException(),
                    OperandType.InlineSwitch => throw new NotImplementedException(),
                    OperandType.InlineTok => throw new NotImplementedException(),
                    OperandType.InlineType => throw new NotImplementedException(),
                    OperandType.InlineVar => throw new NotImplementedException(),
                    OperandType.InlineArg => throw new NotImplementedException(),
                    OperandType.ShortInlineBrTarget => throw new NotImplementedException(),
                    OperandType.ShortInlineI => throw new NotImplementedException(),
                    OperandType.ShortInlineR => throw new NotImplementedException(),
                    OperandType.ShortInlineVar => throw new NotImplementedException(),
                    OperandType.ShortInlineArg => throw new NotImplementedException(),
                    _ => throw new NotImplementedException(),
                };
                return res;
            }
            
            public bool Match(Instruction instr) 
                => rootMatcher.Match(instr.Operand);
            internal readonly string[] _data;
            internal readonly OperandMatcherBase rootMatcher;
        }

        public abstract class OperandMatcherBase
        {
            public OperandMatcherBase(string[] data, bool lazy = false)
            {
                _data = data;
                _lazy = lazy;
            }
            internal readonly string[] _data;
            internal readonly bool _lazy;
            internal InstrMatchBlock _owner;
            public abstract bool Match(object operand);
        }
        public abstract class OperandMatcherG<T> : OperandMatcherBase
        {
            protected OperandMatcherG(string[] data) : base(data)
            {

            }
            public override sealed bool Match(object operand) => operand is T t && MatchG(t);
            protected abstract bool MatchG(T operand);
        }
        public class SlowPrimitiveMatcher : OperandMatcherBase
        {
            public SlowPrimitiveMatcher(string[] data) : base(data)
            {
            }

            public override bool Match(object operand)
            {
                try
                {
                    return operand == AttemptParseRefl(operand.GetType(), _data[0]);
                }
                catch { return false; }
            }
        }
        public class LabelMatcher : OperandMatcherG<ILLabel>
        {
            public LabelMatcher(string[] data) : base(data) { }
            protected override bool MatchG(ILLabel operand)
            {
                int.TryParse(_data[0], out var r);
                return operand.ToString() == _data[0] || operand.Target.Offset == r;
            }
        }
        public class Wildcard : OperandMatcherBase
        {
            public Wildcard(string[] data) : base(data) { }
            public override bool Match(object operand) => true;
        }
        public class MethodMatcher : OperandMatcherG<MethodInfo>
        {
            public MethodMatcher(string[] data) : base(data)
            {
            }

            protected override bool MatchG(MethodInfo operand)
            {

            }
        }
    }
}
