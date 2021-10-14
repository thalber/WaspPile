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
        public const string STARTROOM = "HI_C04";
        public MartyrChar() : base("Martyr", FormatVersion.V1, 0) { instance = this; }
        public static MartyrChar instance;
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

        public class MartyrSave : CustomSaveState
        {
            public MartyrSave(PlayerProgression prog, SlugBaseCharacter schar) : base(prog, schar)
            {
            }

            public override void SavePermanent(Dictionary<string, string> data, bool asDeath, bool asQuit)
            {
                if (RemnantConfig.MartyrLimited && asQuit && !data.ContainsKey("DISRUPT")) data.Add("DISRUPT", "YES");
                base.SavePermanent(data, asDeath, asQuit);
            }
        }
    }
}
