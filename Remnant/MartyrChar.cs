using Menu;
using SlugBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;


using static RWCustom.Custom;
using static WaspPile.Remnant.RemnantUtils;

namespace WaspPile.Remnant
{
    public class MartyrChar : SlugBaseCharacter
    {
        public const string CHARNAME = "Martyr";
        public const string PERMADEATHKEY = "DISRUPT";
        public const string STARTROOM = "HI_C04";
        public static readonly Color baseBodyCol = HSL2RGB(0.583f, 0.3583f, 0.225f);//HSL2RGB(0.5835f, 0.15f, 0.45f + 0.15f);
        public static readonly Color deplBodyCol = HSL2RGB(0.5835f, 0.15f, 0.6f);
        public static readonly Color baseEyeCol = Color.yellow;
        public static readonly Color deplEyeCol = RainWorld.GoldRGB;

        public MartyrChar() : base(CHARNAME, FormatVersion.V1, 2) { 
            instance = this;
            
        }
        public static MartyrChar instance;

        public override string Description => "REMNANT OF A MIND IS MATERIALIZED\nWEAKNESS IS BRIDGE TO STRENGTH\nINSERTION IS VIOLATION";
        //proper colors
        public override Color? SlugcatColor() => baseBodyCol;
        public override Color? SlugcatEyeColor() => baseEyeCol;
        public override bool HasGuideOverseer => false;
        //public override string StartRoom => STARTROOM;
        public override CustomSaveState CreateNewSave(PlayerProgression progression)
        {
            return new MartyrSave(progression, this);
        }
        protected override void Disable()
        {
            MartyrHooks.Disable();
            CommonHooks.Disable();
        }
        protected override void Enable()
        {
            
            MartyrHooks.Enable();
            CommonHooks.Enable();
        }
        public override SelectMenuAccessibility GetSelectMenuState(SlugcatSelectMenu menu)
        {
            var meta = SaveManager.GetCharacterData(CHARNAME, CRW.options.saveSlot);
            if (meta.TryGetValue(PERMADEATHKEY, out var reason))
            {
                return SelectMenuAccessibility.MustRestart;
            }
            return SelectMenuAccessibility.Available;
        }
        public override CustomScene BuildScene(string sceneName)
        {
            if (sceneName == "SelectMenu" && MartyrIsDead(CRW.options.saveSlot)) sceneName = "SelectMenuDisrupt";
            return base.BuildScene(sceneName);
        }
        public override void StartNewGame(Room room)
        {
            SaveManager.GetCharacterData(CHARNAME, CRW.options.saveSlot).TryRemoveKey(PERMADEATHKEY);
            base.StartNewGame(room);
        }
        public override Stream GetResource(params string[] path)
        {
            var patchedPath = new string[path.Length];
            for (int i = path.Length - 1; i > -1; i--) patchedPath[i] = path[i];
            //kinda janky for having 2 overlapping scenes but whatevs
            if (path[path.Length - 2] == "SelectMenuDisrupt" && path.Last() != "scene.json") 
                patchedPath[path.Length - 2] = "SelectMenu";
            string oresname = "WaspPile.Remnant.assets." + string.Join(".", patchedPath);

            var tryret = Assembly.GetExecutingAssembly().GetManifestResourceStream(oresname);
            if (tryret != null) Console.WriteLine($"BUILDING SCENE FROM ER: {oresname}");
            return tryret ?? base.GetResource(path);
        }

        public static bool MartyrIsDead(int saveslot)
        {
            try
            {
                var meta = SaveManager.GetCharacterData("Martyr", saveslot);
                return meta.ContainsKey(PERMADEATHKEY);
            }
            catch { return false; }
        }

        public class MartyrSave : CustomSaveState
        {
            public MartyrSave(PlayerProgression prog, SlugBaseCharacter schar) : base(prog, schar)
            {
            }

            public override void Save(Dictionary<string, string> data)
            {
                base.Save(data);
                if (cycleNumber >= RemnantConfig.martyrCycles.Value)
                {
                    var meta = SaveManager.GetCharacterData(CHARNAME, CRW.options.saveSlot);
                    var deathmark = "VESSEL EXPIRATION";
                    meta.SetKey(PERMADEATHKEY, deathmark);
                    CRW.processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                    Debug.Log($"REMNANT DISRUPTED: {deathmark}");
                }
            }

            public override void SavePermanent(Dictionary<string, string> data, bool asDeath, bool asQuit)
            {
                if (RemnantConfig.noQuits.Value && asQuit)
                {
                    var meta = SaveManager.GetCharacterData(CHARNAME, CRW.options.saveSlot);
                    var deathmark = "ACTOR DESYNC";
                    meta.SetKey(PERMADEATHKEY, deathmark);
                    UnityEngine.Object.FindObjectOfType<RainWorld>().processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                    Debug.Log($"REMNANT DISRUPTED: {deathmark}");
                }
                base.SavePermanent(data, asDeath, asQuit);
            }
        }
    }
}
