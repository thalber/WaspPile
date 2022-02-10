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
using Mono.Cecil;
using static UnityEngine.Debug;
using static WaspPile.SecondSilverStem._3SUTL;
using static WaspPile.SecondSilverStem.StemTestPlugin;

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
                _parser.switchMode(ILPatternParser.ILParseMode.NONE, null);
                _parser = null;
            }
            internal readonly string[] Data;
            internal readonly Dictionary<string, ILPattern> _children = new();
            //defs
            internal readonly Dictionary<string, Type> _typedefs = new();
            public void BindType(string key, Type type) => _typedefs.SetKey(key, type);
            public void BindTypes(params (string, Type)[] pairs)
            {
                foreach (var p in pairs) BindType(p.Item1, p.Item2);
            }
            internal readonly Dictionary<string, MethodBase> _procdefs = new();
            public void BindProc(string key, MethodBase proc) => _procdefs.SetKey(key, proc);
            public void BindProcs(params (string, MethodBase)[] pairs)
            {
                foreach (var p in pairs) BindProc(p.Item1, p.Item2);
            }
            internal readonly Dictionary<string, FieldInfo> _fielddefs = new();
            public void BindFld(string key, FieldInfo field) => _fielddefs.SetKey(key, field);
            public void BindFlds(params (string, FieldInfo)[] pairs)
            {
                foreach (var p in pairs) BindFld(p.Item1, p.Item2);
            }
            public void ResetBinds()
            {
                foreach (var k in _typedefs.Keys) _typedefs.SetKey(k, null);
                foreach (var k in _procdefs.Keys) _procdefs.SetKey(k, null);
                foreach (var k in _fielddefs.Keys) _fielddefs.SetKey(k, null);
            }
            //!defs
            public IEnumerator<ILPattern> GetEnumerator() => _children.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _children.Values.GetEnumerator();
            internal readonly ILPatternParser _parser;
        }
        public class ILPattern
        {
            internal List<InstrMatchBlock> _preds = new();
            internal ILPatternCollection _pcollection;
            public IEnumerable<Func<Instruction, bool>> ReturnPredicates()
            {
                for (int i = 0; i < _preds.Count; i++) yield return (Func<Instruction, bool>)_preds[i];
            }
        }

        public class ILPatternParser
        {
            public ILPatternParser(ILPatternCollection ow)
            {
                _owner = ow;
                stlog.LogWarning($"Booting up ILPP: length {data.Length}");
            }
            public readonly ILPatternCollection _owner;
            public string[] data => _owner.Data;

            public void Advance()
            {
                stlog.LogWarning("ILPP step " + cIndex);
                stlog.LogWarning(cLine);
                if (cLine.StartsWith("//") || cLine.Length == 0) goto wrap;
                var clnSplit = cLine.Split(' ');
                if (clnSplit[0] == "BEGIN")
                {
                    if (clnSplit.Length < 2) goto wrap;
                    var newBlockName = (clnSplit.Length > 2) ? clnSplit[2] : nextDefaultName;
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
                if (Done) switchMode(ILParseMode.NONE, null);
            }
            public bool Done => cIndex >= data.Length;
            public string cLine => data[cIndex];

            internal void switchMode(ILParseMode newMode, string newBlockName)
            {
                if (newMode == cMode) return;
                stlog.LogWarning($"!!! switching mode: {newMode}, {newBlockName}");
                if (cPattern is not null)
                {
                    stlog.LogWarning("!!! pattern finalized");
                    _owner._children.Add(cBlockName, cPattern);
                    cPattern = null;
                }
                cMode = newMode;
                cBlockName = newBlockName;
                if (cMode == ILParseMode.PATTERN)
                {
                    stlog.LogWarning($"Creating new pattern");
                    cPattern = new() { _pcollection = _owner };
                }
            }
            internal InstrMatchBlock makeMatchBlock(string[] line)
            {
                var ocns = line.FirstOrDefault() ?? "nop";
                OpCode oc = Nop;
                foreach (var sub in ocns.Split('?')) if (OpCodesByName.TryGetValue(sub.ToLower(), out oc)) { break; }
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
            internal string nextDefaultName => "group" + gnc++;
            internal int gnc = 0;
        }


        public static readonly Dictionary<string, OpCode> OpCodesByName = new();
        static _3S()
        {
            foreach (var fld in typeof(OpCodes).GetFields(allContextsStatic)) try
                {
                    if (fld.FieldType == typeof(OpCode)) OpCodesByName.Add(fld.Name.ToLower(), (OpCode)fld.GetValue(null));
                    stlog.LogWarning($"{fld.Name} oc registered");
                } catch { }
        }
    }
}

