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
            PlayerManager.RegisterCharacter(new defaultRodentChar());
            On.Menu.MainMenu.ctor += overrideMenuScene;
            //SlugBase.SlugBaseCharacter
        }

        private void ReadConfig()
        {
            try
            {
                slideshows.Clear();
                var lines = File.ReadAllLines(Path.Combine(RootFolderDirectory(), "rodentSetup.txt"));
                foreach (var l in lines)
                {
                    var spl = Split(l, " : ");
                    if (spl.Length == 2)
                    {
                        switch (spl[0])
                        {
                            case "description": desc = spl[1]; break;
                            case "name": cname = spl[1]; break;
                            case "displayname": dname = spl[1]; break;
                            case "spawns": int.TryParse(spl[1], out useSpawns); break;
                            case "start": startRoom = spl[1]; break;
                            case "mainmenu": mmov = spl[1]; break;
                        }
                    }
                    else if (spl.Length > 2)
                    {
                        switch (spl[0])
                        {
                            case "slideshow":
                                slideshows.Add(spl[1], spl.SkipWhile(xx => spl.IndexOf(xx) > 1).ToArray());
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Could not read rodent config: " + e);
            }
        }

        private void overrideMenuScene(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            ReadConfig();
            orig(self, manager, showRegionSpecificBkg);
            if (mmov != null) inst.overrideNext(mmov);
        }
        static readonly Dictionary<string, string[]> slideshows = new();
        static string mmov;
        static string desc = "nothing interesting";
        static string cname = "default_rodent";
        static string dname = "El Raton";
        static int useSpawns = 0;
        static string startRoom = "SU_C04";
        static defaultRodentChar inst;

        class defaultRodentChar : SlugBaseCharacter
        {
            public override string DisplayName => dname;
            public override string StartRoom => startRoom;
            public defaultRodentChar() : base(cname, FormatVersion.V1, useSpawns)
            {
                inst = this;
            }
            public void overrideNext(string n) => OverrideNextScene(n, null);
            public override bool HasSlideshow(string slideshowName) => slideshows.ContainsKey(slideshowName);
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
