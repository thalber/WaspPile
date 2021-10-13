using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlugBase;

namespace WaspPile.Remnant
{
    public class OutlawChar : SlugBaseCharacter
    {
        public OutlawChar() : base("Outlaw", FormatVersion.V1, 0)
        {
            instance = this;
        }
        public static OutlawChar instance;

        public override string Description => "GNAWING, ACIDIC SIMPLICITY\nTHE VERY WALLS KNEEL\nCOGNITION IS BURDEN";
        protected override void GetStats(SlugcatStats stats)
        {
#warning finish outlawyer stats
            base.GetStats(stats);
            stats.bodyWeightFac = 1.21f;
            stats.runspeedFac = 1.3f;
            stats.poleClimbSpeedFac = 1.32f;
            stats.corridorClimbSpeedFac = 1.25f;
            stats.throwingSkill = 0;
            stats.generalVisibilityBonus = 1.2f;
            stats.visualStealthInSneakMode = 0.7f;
        }
        
        protected override void Disable()
        {
            OutlawHooks.Disable();
            CommonHooks.Disable();
        }
        protected override void Enable()
        {
            OutlawHooks.Enable();
            CommonHooks.Enable();
        }
    }
}
