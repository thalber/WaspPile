using Menu;
using SlugBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WaspPile.Remnant
{
    public class MartyrChar : SlugBaseCharacter
    {
        public const string CHARNAME = "Martyr";
        public const string PERMADEATHKEY = "DISRUPT";
        public const string STARTROOM = "HI_C04";
        public MartyrChar() : base(CHARNAME, FormatVersion.V1, 0) { }//instance = this; }
        //public static MartyrChar instance;
        public override string Description => "REMNANT OF A MIND IS MATERIALIZED\nWEAKNESS IS BRIDGE TO STRENGTH\nINSERTION IS VIOLATION";
        //proper colors
        public override Color? SlugcatColor() => new Color(0.15f, 0.15f, 0.3f);
        public override Color? SlugcatEyeColor() => Color.yellow;
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
            var crw = GameObject.FindObjectOfType<RainWorld>();
            var meta = SaveManager.GetCharacterData("Martyr", crw.options.saveSlot);
            if (meta.TryGetValue(PERMADEATHKEY, out var deathTime))
            {
                Debug.Log($"REMNANT HAD BEEN DISRUPTED: {deathTime}, FORCING RESTART");
                return SelectMenuAccessibility.MustRestart;
            }
            return SelectMenuAccessibility.Available;
        }
        public override CustomScene BuildScene(string sceneName)
        {
            if (sceneName == "SelectMenu") sceneName = "SleepScreen";
#warning finish scenes setup
            return base.BuildScene(sceneName);
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

            public override void LoadPermanent(Dictionary<string, string> data)
            {
            }

            public override void SavePermanent(Dictionary<string, string> data, bool asDeath, bool asQuit)
            {
                if (RemnantConfig.MartyrLimited && asQuit)
                {
                    var crw = GameObject.FindObjectOfType<RainWorld>();
                    var meta = SaveManager.GetCharacterData("Martyr", crw.options.saveSlot);
                    var deathmark = DateTime.Now.ToString();
                    meta.SetKey(PERMADEATHKEY, deathmark);
                }
                base.SavePermanent(data, asDeath, asQuit);
            }
        }
    }
}
