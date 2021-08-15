using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaspPile.EnforceSolitude
{
    [BepInPlugin("thalber.EnfSolitude", "EnforceSolitude", "0.0.1")]
    public class ESol : BaseUnityPlugin
    {
        public void OnEnable()
        {
            On.Region.ctor += Region_ctor;
        }

        private void Region_ctor(On.Region.orig_ctor orig, Region self, string name, int firstRoomIndex, int regionNumber)
        {
            orig(self, name, firstRoomIndex, regionNumber);
            self.regionParams.playerGuideOverseerSpawnChance = 0f;
            //throw new NotImplementedException();
        }

        public void OnDisable()
        {
            On.Region.ctor -= Region_ctor;
        }

    }
}
