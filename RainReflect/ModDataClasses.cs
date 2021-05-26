using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using UnityEngine;
using System.IO;

namespace WaspPile.RR
{
    public class ModRelay
    {
        public ModRelay(string path)
        {
            try
            {
                using (ModuleDefinition md = ModuleDefinition.ReadModule(path))
                {
                    TypeDefinition[] tds = md.GetTypes().ToArray();
                    ModInfoCarrier gmic =  new ModInfoCarrier();
                    foreach (TypeDefinition td in tds)
                    {
                        ModInfoCarrier cmic;
                        CheckThisType(td, out cmic);
                        gmic += cmic;
                    }
                    switch (gmic.resultingkind)
                    {
                        case kind.bepplugin:
                            this.myModData = new BepPluginData(path);
                            break;
                        case kind.partmod:
                        case kind.mixed:
                            this.myModData = new PartModData(path);
                            break;
                        case kind.patch:
                        case kind.invalid:
                            this.myModData = new PatchModData(path);
                            break;


                    }
                }
            }
            catch (IOException ioe)
            {
                Debug.Log($"RAINREFLECT: ERROR READING MOD FILE {new FileInfo(path).Name}");
                Debug.Log(ioe);
            }
        }
        private static void CheckThisType (TypeDefinition td, out ModInfoCarrier mic)
        {
            mic.isbepplugin = false;
            mic.ismmpt = false;
            mic.ispm = false;
            if (td.BaseType != null && td.BaseType.Name == "PartialityMod") mic.ispm = true;
            if (td.HasCustomAttributes)
            {
                foreach (CustomAttribute ca in td.CustomAttributes)
                {
                    if (ca.AttributeType.Namespace == nameof(BepInEx))
                    {
                        mic.isbepplugin = true;
                    }
                    if (ca.AttributeType.Name == "MonoModPatch") mic.ismmpt = true;
                }
            }
            if (td.HasNestedTypes)
            {
                foreach (TypeDefinition ntd in td.NestedTypes)
                {
                    ModInfoCarrier nmic;
                    CheckThisType(ntd, out nmic);
                    mic += nmic;
                }
            }
            
        }
        protected struct ModInfoCarrier
        {
            public bool ismmpt;
            public bool ispm;
            public bool isbepplugin;
            public static ModInfoCarrier operator +(ModInfoCarrier a, ModInfoCarrier b)
            {
                if (b.ispm) a.ispm = true;
                if (b.isbepplugin) a.isbepplugin = true;
                if (b.ismmpt) a.ismmpt = true;
                return a;
            }
            public kind resultingkind
            {
                get
                {
                    if (this.ismmpt)
                    {
                        if (this.ispm || this.isbepplugin) return kind.invalid;
                        else return kind.patch;
                    }
                    else
                    {
                        if (this.isbepplugin && this.ispm) return kind.mixed;
                        if (this.isbepplugin) return kind.bepplugin;
                        if (this.ispm) return kind.partmod;
                        return kind.none;
                    }
                }
            }
        }
        public enum kind
        {
            bepplugin,
            partmod,
            invalid,
            patch,
            mixed,
            none
        }
        public static string ConvertMMPName(string partname)
        {
            if (partname.StartsWith("Assembly-CSharp.") && partname.EndsWith(".mm.dll"))
            {
                return (partname.Replace("Assembly-CSharp.", string.Empty).Replace(".mm.dll", ".dll"));
            }
            else
            {
                return "Assembly-CSharp." + partname.Replace(".dll", ".mm.dll");
            }
        }
        public override string ToString()
        {
            try
            {
                return this.myModData.ToString();
            }
            catch (NullReferenceException)
            {
                return "ANGRY BZZ";
            }
        }
        public string name
        {
            get
            {
                try
                {
                    return new FileInfo(this.myModData.path).Name;
                }
                catch (NullReferenceException)
                {
                    return string.Empty;
                }
            }
        }
        private ModData myModData;
        protected class ModData
        {
            public ModData(string storagepath)
            {
                path = storagepath;
            }
            public string path;
            protected virtual string TarName => new FileInfo(path).Name;
            protected virtual string TargetPath => Path.Combine(BepInEx.Paths.PluginPath, TarName);
            public override string ToString()
            {
                return TarName + " : NONE";
            }
            public virtual kind MyKind => kind.none;
        }
        protected class PartModData : ModData
        {
            public PartModData(string storagepath) : base(storagepath)
            {
                
            }
            public override string ToString()
            {
                return TarName + " : PARTMOD";
            }
            public override kind MyKind => kind.partmod;

        }
        protected class BepPluginData : ModData
        {
            public BepPluginData(string storagepath) : base (storagepath)
            {

            }
            public override string ToString()
            {
                return TarName + " : BEPPLUGIN";
            }
            public override kind MyKind => kind.bepplugin;
        }
        protected class PatchModData : ModData
        {
            public PatchModData(string storagepath) : base(storagepath)
            {

            }
            protected override string TarName => ConvertMMPName(base.TarName);
            protected override string TargetPath => Path.Combine(BepInEx.Paths.BepInExRootPath, "monomod");
            public override string ToString()
            {
                return base.TarName + " : MMPATCH";
            }
            public override kind MyKind => kind.patch;
        }
        protected class SideDllData : ModData
        {
            public SideDllData(string storagepath) : base(storagepath)
            {

            }
            public override kind MyKind => kind.none;
        }
    }
}
