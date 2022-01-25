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

using URand = UnityEngine.Random;

namespace WaspPile.SecondSilverStem
{
    public static partial class _3S
    {
        public static bool DebugMode => true;
        public class ILPatternCollection : IEnumerable<ILPattern>
        {
            public ILPatternCollection(string[] rawdata)
            {
                Data = rawdata;
                _parser = new(this);
                while (!_parser.Done) _parser.Advance();
            }
            internal readonly string[] Data;
            internal readonly Dictionary<string, ILPattern> _children = new();
            internal readonly Dictionary<string, Type> _typedefs = new();
            public void BindType(string k, Type t) => _typedefs.SetKey(k, t);

            public IEnumerator<ILPattern> GetEnumerator()
            {
                throw new NotImplementedException();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            internal readonly ILPatternParser _parser;
            public class ILPatternParser
            {
                public ILPatternParser(ILPatternCollection ow)
                {
                    owner = ow;
                }
                public readonly ILPatternCollection owner;
                public string[] data => owner.Data;

                public void Advance()
                {
                    if (cLine.StartsWith("//") || cLine.Length == 0) goto wrap;
                    var clnSplit = cLine.Split(' ');
                    if (clnSplit[0] == "BEGIN")
                    {
                        if (clnSplit.Length < 2) goto wrap;
                        var newBlockName = (clnSplit.Length > 2) ? clnSplit[3] : nextDefaultName;
                        switchMode(ParseEnum<ILParseMode>(clnSplit[1]), newBlockName);
                        goto wrap;
                    }
                    else if (clnSplit[0] == "END")
                    {
                        switchMode(ILParseMode.NONE, null);
                        goto wrap;
                    }
                    switch (cMode)
                    {
                        case ILParseMode.NONE:
                            break;
                        case ILParseMode.DEFS:
                            owner.BindType(clnSplit[0], null);
                            break;
                        case ILParseMode.PATTERN:
                            try
                            {
                                cPattern._preds.Add(parsePredicate(clnSplit));
                            }
                            catch { }

                            break;
                        default:
                            break;
                    }
                wrap:
                    cIndex++;
                }
                public bool Done => cIndex < data.Length;
                public string cLine => data[cIndex];

                internal void switchMode(ILParseMode newMode, string newBlockName)
                {
                    if (newMode == cMode) return;
                    if (cPattern is not null)
                    {
                        owner._children.Add(cBlockName, cPattern);
                        cPattern = null;
                    }
                    cMode = newMode;
                    cBlockName = newBlockName;
                    if (cMode == ILParseMode.PATTERN)
                    {
                        cPattern = new();
                    }
                }
                internal Func<Instruction, bool> parsePredicate (string[] line)
                {
                    throw new NotImplementedException();
                }
                internal ILPattern cPattern;
                internal int cIndex = 0;
                internal string cBlockName = default;
                internal ILParseMode cMode = ILParseMode.NONE;
                internal enum ILParseMode
                {
                    NONE,
                    DEFS,
                    PATTERN,
                }
                internal string nextDefaultName => throw new NotImplementedException();
            }
        }
        public class ILPattern
        {
            internal List<Func<Instruction, bool>> _preds = new();
            public IEnumerable<Func<Instruction, bool>> ReturnPredicates() => _preds.AsEnumerable();
        }
    }
}

