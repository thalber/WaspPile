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
            public void BindType(string key, Type type) => _typedefs.SetKey(key, type);
            internal readonly Dictionary<string, MethodBase> _procdefs = new();
            public void BindProc(string key, MethodBase proc) => _procdefs.SetKey(key, proc);
            internal readonly Dictionary<string, FieldInfo> _fielddefs = new();
            public void BindFld(string key, FieldInfo field) => _fielddefs.SetKey(key, field);

            public IEnumerator<ILPattern> GetEnumerator() => _children.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _children.Values.GetEnumerator();

            internal readonly ILPatternParser _parser;
        }
        public class ILPattern
        {
            internal List<InstrMatchBlock> _preds = new();
            internal ILPatternCollection _pcollection;
            //public IEnumerable<Func<Instruction, bool>> ReturnPredicates() => _preds.AsEnumerable();
        }

        public class ILPatternParser
        {
            public ILPatternParser(ILPatternCollection ow)
            {
                _owner = ow;
            }
            public readonly ILPatternCollection _owner;
            public string[] data => _owner.Data;

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
#warning put all def actions in here
                        int ac = clnSplit.FirstOrDefault() switch { "TYPE" => 0, "PROC" => 1, "FLD" => 2, _ => -1 };
                        if (ac is -1) goto wrap;
                        for (int i = 1; i < clnSplit.Length; i++)
                        {
                            var css = clnSplit[i];
                            switch (ac) {
                                case 0: _owner.BindType(css, null); break;
                                case 1: _owner.BindProc(css, null); break;
                                case 2: _owner.BindFld(css, null); break;
                            }
                        }
                        break;
                    case ILParseMode.PATTERN:
                        try
                        {
                            cPattern._preds.Add(makeMatchBlock(clnSplit));
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
                    _owner._children.Add(cBlockName, cPattern);
                    cPattern = null;
                }
                cMode = newMode;
                cBlockName = newBlockName;
                if (cMode == ILParseMode.PATTERN)
                {
                    cPattern = new() { _pcollection = _owner };
                }
            }
            internal InstrMatchBlock makeMatchBlock(string[] line)
            {
                OpCodesByName.TryGetValue(line[0], out var oc);
                return new InstrMatchBlock(oc, line, cPattern);
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

        public static readonly Dictionary<string, OpCode> OpCodesByName = new(StringComparer.InvariantCultureIgnoreCase);
        static _3S()
        {
            foreach (var fld in typeof(OpCodes).GetFields(allContextsStatic)) try
                {
                    if (fld.FieldType == typeof(OpCode)) OpCodesByName.Add(fld.Name, (OpCode)fld.GetValue(null));
                } catch { }
        }
    }
}

