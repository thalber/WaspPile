using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WaspPile.Remnant
{
    public class MartyrChar : SlugBase.SlugBaseCharacter
    {
        public const string STARTROOM = "HI_C04";
        public MartyrChar() : base("Martyr", FormatVersion.V1, 0) { instance = this; }
        public static MartyrChar instance;
        public override string Description => "when piss gets dark you know shit's boutta go down";
        public override Color? SlugcatColor() => new Color(0.15f, 0.15f, 0.3f);
        public override Color? SlugcatEyeColor() => Color.yellow;
        public override string StartRoom => STARTROOM;
        protected override void Disable()
        {
            MartyrHooks.Disable();
        }
        protected override void Enable()
        {
            MartyrHooks.Enable();
        }
    }
}
