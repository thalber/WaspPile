using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static RWCustom.Custom;
using static UnityEngine.Mathf;

namespace WaspPile.Remnant
{
    public static class OutlawHooks
    {
        //todo:
        //-wall crawl (rip off casheww)
        //-no weapon
        //-nyoom
        //-grabbing alive creatures
        //-damage by eating
        public static void Enable() {
            On.Player.Grabability += SwitchGrabability;
            On.Player.EatMeatUpdate += Crunch;
            On.Player.CanEatMeat += WidenDiet;
        }

        private static bool WidenDiet(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
        {
            return true;
        }

        private static void Crunch(On.Player.orig_EatMeatUpdate orig, Player self)
        {
            if (self.eatMeat > 40 && self.eatMeat % 15 == 3 && self.grasps[0].grabbed is Creature crit)
            {

                crit.Violence(self.mainBodyChunk, default, self.grasps[0].grabbedChunk, null, Creature.DamageType.Bite, Lerp(1.3f, 1.5f, Pow(UnityEngine.Random.value, 3f)), UnityEngine.Random.Range(20f, 30f));
            }
            orig(self);
        }

        private static int SwitchGrabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (obj is Weapon) return (int)Player.ObjectGrabability.CantGrab;
            if (obj is Creature && obj != self) return (int)Player.ObjectGrabability.Drag;
            else return orig(self, obj);
            //throw new NotImplementedException();
        }

        public static void Disable() {
            On.Player.Grabability -= SwitchGrabability;
            On.Player.EatMeatUpdate -= Crunch;
        }
    }
}
