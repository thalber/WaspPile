using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlugBase;
using System.IO;

using static RWCustom.Custom;
using static System.Text.RegularExpressions.Regex;

namespace DefaultRodent
{
    [BepInPlugin("thalber.defaultRodent", "defaultRodent", "0.0.1")]
    public class drPlugin : BaseUnityPlugin
    {
        public drPlugin()
        {
            try
            {
                var lines = File.ReadAllLines(Path.Combine(RootFolderDirectory(), "rodentSetup.txt"));
                foreach (var l in lines)
                {
                    var spl = Split(l, " : ");
                    if (spl.Length == 2) {
                        switch (spl[0])
                        {
                            case "description": desc = spl[1]; break;
                            case "name": cname = spl[1]; break;
                            case "displayname": dname = spl[1]; break;
                            case "spawns": int.TryParse(spl[1], out useSpawns); break;
                            case "start": startRoom = spl[1]; break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Could not read rodent config: " + e);
            }
            PlayerManager.RegisterCharacter(new defaultRodentChar());
            //SlugBase.SlugBaseCharacter
        }
        static string desc = "nothing interesting";
        static string cname = "default_rodent";
        static string dname = "El Raton";
        static int useSpawns = 0;
        static string startRoom = "SU_C04";

        class defaultRodentChar : SlugBaseCharacter
        {
            public override string DisplayName => dname;
            public defaultRodentChar() : base(cname, FormatVersion.V1, useSpawns)
            {

            }
            public override string Description => desc;
            protected override void Disable()
            {
            }

            protected override void Enable()
            {
            }
        }
    }
}
