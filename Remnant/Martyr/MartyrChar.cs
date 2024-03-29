﻿using Menu;
using SlugBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using WaspPile.Remnant.Satellite;

using static UnityEngine.Debug;
using static RWCustom.Custom;
using static WaspPile.Remnant.Satellite.RemnantUtils;

namespace WaspPile.Remnant.Martyr
{
    public class MartyrChar : SlugBaseCharacter
    {
        public const string CHARNAME = "Martyr";
        public const string PERMADEATHKEY = "DISRUPT";
        public const string ALLEVKEY = "REMEDY";
        public const string LIFETIMEKEY = "TOTALLT";
        public const string LTCUREKEY = "BONUSLT";
        public const string LTSWITCHKEY = "BLTACTIVE";
        public const string STARTROOM = "SB_MARTYR1";
        public static readonly Color baseBodyCol = HSL2RGB(0.583f, 0.3583f, 0.225f);
        public static readonly Color deplBodyCol = HSL2RGB(0.5835f, 0.15f, 0.6f);
        public static readonly Color baseEyeCol = HSL2RGB(0.125f, 0.979f, 0.795f);
        public static readonly Color deplEyeCol = new(0.7f, 0f, 0f);
        public static readonly Color echoGold = HSL2RGB(0.13f, 1, 0.63f);

        public MartyrChar() : base(CHARNAME, FormatVersion.V1, 2, false)
        {
            //__ME = new WeakReference(this);
            ME = new(this);
            DevMode = RemnantPlugin.DebugMode;
            //instance = this;
        }
        //public static MartyrChar instance;
        internal static TWeakReference<MartyrChar> ME;
        internal void overrideNext(string SceneImage, SceneImageFilter filter) => OverrideNextScene(SceneImage, filter);

        #region chardetails
        public override bool PlaceKarmaFlower => false;
        public override bool HasArenaPortrait(int playerNumber, bool dead)
        {
            return true;
        }
        public override string Description => RemnantPlugin.DoTrolling
            ? "REMNANT OF A MIND IS MATERIALIZED\nWEAKNESS IS BRIDGE TO STRENGTH\nINSERTION IS VIOLATION"
            : "The remnant of a mind, materialized, weakened in the physical plane but retaining\nabilities of the Void. In a state outside the Cycle itself, your journey will only last as long as you do.";
        //proper colors
        [Obsolete]
        public override Color? SlugcatColor() => baseBodyCol;
        [Obsolete]
        public override Color? SlugcatEyeColor() => baseEyeCol;
        public override bool HasGuideOverseer => false;
        public override bool HasDreams => false;
        public override bool CanUsePassages(SaveState save)
        {
            return false;
        }
        public override void GetFoodMeter(out int maxFood, out int foodToSleep)
        {
            maxFood = 9;
            foodToSleep = 9;
        }
        protected override void GetStats(SlugcatStats stats)
        {
            base.GetStats(stats);
            SlugcatStats ssn = new(2, stats.malnourished);
            CloneInstance(ssn, stats);
            stats.throwingSkill = 2;
            if (RemnantPlugin.DebugMode)
            {
                LogWarning($"GetStats run: {stats.malnourished}");
                LogWarning(Json.Serialize(InstFieldsToDict(stats)));
            }
        }
        public override bool CanEatMeat(Player player, Creature creature) => creature is Centipede || creature is not IPlayerEdible;
        public override bool QuarterFood => true;
        public override string DisplayName => RemnantPlugin.DoTrolling ? "Martyr" : "The Martyr";
        public override string StartRoom => STARTROOM;
        #endregion chardetails
        #region hooks lc
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
        #endregion hooks lc

        #region scenes and menus
        public override bool HasSlideshow(string slideshowName) => false;
        public override bool HasScene(string sceneName)
            => sceneName switch
            {
                "Red_Ascend"
                or _ when sceneName.StartsWith("Outro_") => false,
                _ => base.HasScene(sceneName)
            };
        public override CustomScene BuildScene(string sceneName)
        {
            if (sceneName == "SelectMenu" && MartyrIsDead(CRW.options.saveSlot)) sceneName = "SelectMenuDisrupt";
            return base.BuildScene(sceneName);
        }
        internal static Stream GetRes(params string[] path)
        {
            var patchedPath = new string[path.Length];
            Array.Copy(path, patchedPath, path.Length);
            //kinda janky for having 2 overlapping scenes but whatevs
            var last = path[path.Length - 1];
            if (path.Length > 2 && path[path.Length - 2] == "SelectMenuDisrupt" && path.Last() != "scene.json")
                patchedPath[path.Length - 2] = "SelectMenu";
            if (last.StartsWith("MultiplayerPortrait"))
            {
                patchedPath[path.Length - 1] = $"MultiplayerPortraitX{(last.EndsWith("1.png") ? 1 : 0)}.png";
            }

            string oresname = "WaspPile.Remnant.assets." + string.Join(".", patchedPath);
            if (RemnantPlugin.DebugMode || true)
            {
                LogWarning(oresname);

            }
            var tryret = Assembly.GetExecutingAssembly().GetManifestResourceStream(oresname);
            if (tryret != null && RemnantPlugin.DebugMode) LogWarning($"LOADING ER: {oresname}");
            return tryret;
        }
        public override Stream GetResource(params string[] path) => GetRes(path) ?? base.GetResource();
        public override SelectMenuAccessibility GetSelectMenuState(SlugcatSelectMenu menu)
        {
            var meta = CurrentMiscSaveData(CHARNAME);
            if (meta.TryGetValue(PERMADEATHKEY, out _))
            {
                return SelectMenuAccessibility.MustRestart;
            }
            return SelectMenuAccessibility.Available;
        }
        #endregion scenes and menus
        #region saves
        public override void StartNewGame(Room room)
        {
            base.StartNewGame(room);
            if (room.game.IsStorySession)
            {
                var ss = room.game.GetStorySession.saveState;
                ss.miscWorldSaveData.SLOracleState.neuronsLeft = 0;
                //??...
                ss.deathPersistentSaveData.theMark = true;
                ss.theGlow = true;
            }
            CurrentMiscSaveData(CHARNAME).TryRemoveKey(PERMADEATHKEY);

        }
        public static bool MartyrIsDead(int saveslot)
        {
            try
            {
                var meta = SaveManager.GetCharacterData(CHARNAME, saveslot);
                return meta.ContainsKey(PERMADEATHKEY) & RemnantConfig.noQuits.Value;
            }
            catch { return false; }
        }
        //public bool RemedySaved => GetSaveSummary(CRW).CustomPersistentData.ContainsKey(ALLEVKEY);
        //public void ApplyRemedy(string source = "UNSPECIFIED")
        //{
        //    GetSaveSummary(CRW).CustomPersistentData.SetKey(ALLEVKEY, source);
        //    Console.WriteLine($"THE SLOG DIMINISHES; SOURCE: {source}");
        //}
        //public void RemoveRemedy()
        //{
        //    GetSaveSummary(CRW).CustomPersistentData.TryRemoveKey(ALLEVKEY);
        //    Console.WriteLine("NO CURE IS FOREVER");
        //}
        public override CustomSaveState CreateNewSave(PlayerProgression progression)
        {
            var res = new MartyrSave(progression, this, RemnantConfig.martyrCycles.Value, RemnantConfig.martyrCure.Value);
            res.deathPersistentSaveData.karmaCap = 8;
            res.deathPersistentSaveData.karma = 8;
            return res;
        }
        public class MartyrSave : CustomSaveState
        {
            public MartyrSave(PlayerProgression prog, SlugBaseCharacter schar, 
                int clim = 10, int ccure = 3) : base(prog, schar)
            {
                this.cycleLimit = clim;
                this.cycleCure = ccure;
                ME = new(this);
            }
            internal static TWeakReference<MartyrSave> ME;

            public override void Save(Dictionary<string, string> data)
            {
                base.Save(data);
                if (RemainingCycles < 0)
                {
                    var meta = CurrentMiscSaveData(CHARNAME);
                    var deathmark = "VESSEL EXPIRATION";
                    meta.SetKey(PERMADEATHKEY, deathmark);
                    CRW.processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                    Log($"REMNANT DISRUPTED: {deathmark}");
                }
            }
            public override void SavePermanent(Dictionary<string, string> data, bool asDeath, bool asQuit)
            {
                MartyrHooks.FieldCleanup();

                cycleLimit ??= RemnantConfig.martyrCycles.Value;
                cycleCure ??= RemnantConfig.martyrCure.Value;
                if (RemnantPlugin.DebugMode)
                {
                    LogWarning($"martyr session length settings: {cycleLimit}, {cycleCure}; bonus cycles active: {cureApplied}");
                }
                if (cureApplied)
                {
                    data.SetKey(LTSWITCHKEY, "1");
                }
                data.SetKey(LIFETIMEKEY, cycleLimit.ToString());
                data.SetKey(LTCUREKEY, cycleCure.ToString());

                if (RemnantConfig.noQuits.Value && asQuit && cycleNumber != 0 || imDone)
                {
                    var meta = CurrentMiscSaveData(CHARNAME);
                    var deathmark = "ACTOR DESYNC";
                    meta.SetKey(PERMADEATHKEY, deathmark);
                    var cpm = CRW.processManager;
                    if (cpm.upcomingProcess != null) cpm.RequestMainProcessSwitch(ProcessManager.ProcessID.SlugcatSelect);
                    Log($"REMNANT DISRUPTED: {deathmark}");
                }
                if (RemedyCache && !asDeath) { data.SetKey(ALLEVKEY, "ON"); LogWarning("REMEDY RETAINED"); }
                else { data.SetKey(ALLEVKEY, "OFF"); LogWarning("SAVED AS DEATH, REMEDY REMOVED"); }
                //else if (RemedyCache) data.SetKey(ALLEVKEY, "UNSPECIFIED");
                base.SavePermanent(data, asDeath, asQuit);
            }
            public override void LoadPermanent(Dictionary<string, string> data)
            {
                MartyrHooks.FieldCleanup();
                //RemedyCache = data[ALLEVKEY] == "ON";
                if (data.TryGetValue(LIFETIMEKEY, out var rawlt) && int.TryParse(rawlt, out var ltin)) cycleLimit = ltin;
                if (data.TryGetValue(LTCUREKEY, out var rawcr) && int.TryParse(rawcr, out var crin)) cycleCure = crin;
                data.TryGetValue(ALLEVKEY, out var res);
                data.TryGetValue(LTSWITCHKEY, out var o); cureApplied = o is not null;
                RemedyCache = res == "ON";
                //if (RemnantPlugin.DebugMode) LogWarning("LOADPERM RUN");
                base.LoadPermanent(data);
            }
            private bool rc;
            //ensures disrupt on save
            internal bool imDone;
#warning number verif, check if saves correctly
            internal int? cycleLimit;
            internal int? cycleCure;
            internal bool cureApplied = false;
            internal int RemainingCycles
                => (cureApplied? ((int)cycleLimit + (int)cycleCure) : (int)cycleLimit) - cycleNumber;

            internal bool RemedyCache
            {
                get { if (RemnantPlugin.DebugMode) LogWarning("REMEDY CACHED: " + rc); return rc; }
                set { rc = value; if (RemnantPlugin.DebugMode) LogWarning("REMEDY CACHE SET TO: " + value); }
            }
        }
        #endregion saves
    }
}
