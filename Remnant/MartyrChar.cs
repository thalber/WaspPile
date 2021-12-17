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
        public const string ALLEVKEY = "REMEDY";
        public const string STARTROOM = "HI_C04";
        public static readonly Color baseBodyCol = HSL2RGB(0.583f, 0.3583f, 0.225f);//HSL2RGB(0.5835f, 0.15f, 0.45f + 0.15f);
        public static readonly Color deplBodyCol = HSL2RGB(0.5835f, 0.15f, 0.6f);
        public static readonly Color baseEyeCol = Color.yellow;
        public static readonly Color deplEyeCol = new Color(0.7f, 0f, 0f);

        public MartyrChar() : base(CHARNAME, FormatVersion.V1, 2) {
            instance = this;

        }
        public static MartyrChar instance;

        public override string Description => "REMNANT OF A MIND IS MATERIALIZED\nWEAKNESS IS BRIDGE TO STRENGTH\nINSERTION IS VIOLATION";
        //proper colors
        public override Color? SlugcatColor() => baseBodyCol;
        public override Color? SlugcatEyeColor() => baseEyeCol;
        public override bool HasGuideOverseer => false;

        public override void GetFoodMeter(out int maxFood, out int foodToSleep)
        {
            maxFood = 9; 
            var data = GetSaveSummary(CRW);
            foodToSleep = data?.CustomData?.ContainsKey(ALLEVKEY) ?? false ? 7 : 9;
        }
        protected override void GetStats(SlugcatStats stats)
        {
            base.GetStats(stats);
        }
        public override bool CanEatMeat(Player player, Creature creature) => true;

        //TODO: start room, karma cap, starvation
        //public override string StartRoom => STARTROOM;
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
        public override void StartNewGame(Room room)
        {
            var meta = CurrentMiscSaveData(CHARNAME);
            meta.TryRemoveKey(PERMADEATHKEY);
            meta.TryRemoveKey(ALLEVKEY);
            base.StartNewGame(room);
        }

        public override CustomScene BuildScene(string sceneName)
        {
            if (sceneName == "SelectMenu" && MartyrIsDead(CRW.options.saveSlot)) sceneName = "SelectMenuDisrupt";
            return base.BuildScene(sceneName);
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
        public override SelectMenuAccessibility GetSelectMenuState(SlugcatSelectMenu menu)
        {
            var meta = CurrentMiscSaveData(CHARNAME);
            if (meta.TryGetValue(PERMADEATHKEY, out var reason))
            {
                return SelectMenuAccessibility.MustRestart;
            }
            return SelectMenuAccessibility.Available;
        }

        public static bool MartyrIsDead(int saveslot)
        {
            try
            {
                var meta = CurrentMiscSaveData(CHARNAME);
                return meta.ContainsKey(PERMADEATHKEY);
            }
            catch { return false; }
        }
        public static bool RemedySaved => (CurrentSaveSummary(CHARNAME)?.CustomData as MartyrSave)?.RemedyCache ?? false;
        public static void ApplyRemedy(string source = "UNSPECIFIED")
        { CurrentSaveSummary(CHARNAME)?.CustomData.SetKey(ALLEVKEY, source); 
            Console.WriteLine($"THE SLOG DIMINISHES; SOURCE: {source}"); }
        public static void RemoveRemedy()
        { CurrentSaveSummary(CHARNAME).CustomData.TryRemoveKey(ALLEVKEY);
            Console.WriteLine("NO CURE IS FOREVER"); }

        public override CustomSaveState CreateNewSave(PlayerProgression progression)
        {
            var res = new MartyrSave(progression, this);
            res.deathPersistentSaveData.karmaCap = 8;
            res.deathPersistentSaveData.karma = 8;
            return res;
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
                    var meta = CurrentMiscSaveData(CHARNAME);
                    var deathmark = "VESSEL EXPIRATION";
                    meta.SetKey(PERMADEATHKEY, deathmark);
                    CRW.processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                    Debug.Log($"REMNANT DISRUPTED: {deathmark}");
                }
            }
            public override void SavePermanent(Dictionary<string, string> data, bool asDeath, bool asQuit)
            {
                MartyrHooks.FieldCleanup();
                if (RemnantConfig.noQuits.Value && asQuit && cycleNumber != 0)
                {
                    var meta = CurrentMiscSaveData(CHARNAME);
                    var deathmark = "ACTOR DESYNC";
                    meta.SetKey(PERMADEATHKEY, deathmark);
                    UnityEngine.Object.FindObjectOfType<RainWorld>().processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                    Debug.Log($"REMNANT DISRUPTED: {deathmark}");
                }
                if (asDeath) { data.TryRemoveKey(ALLEVKEY); }
                base.SavePermanent(data, asDeath, asQuit);
            }
            public override void LoadPermanent(Dictionary<string, string> data)
            {
                MartyrHooks.FieldCleanup();
                RemedyCache = data.TryGetValue(ALLEVKEY, out var r);
                base.LoadPermanent(data);
            }
            internal bool RemedyCache;
        }
    }
}
