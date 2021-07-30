using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedPlacedObjects;

namespace WaspPile.EDGESILK
{
    public static class EDGESILK_HOOKS
    {
        public static void Apply()
        {
            On.SaveState.SaveToString += PERFORM_SHELTERWARPS;
            On.ShelterDoor.Close += ShelterDoor_Close;
        }

        private static void ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            orig(self);
            for (int i = 0; i < self.room.updateList.Count; i++)
            {
                if (self.room.updateList[i] is CRES_SIMPLESHELTERWARP ssw) { QUEUED_WARP = ssw.dest; break; }
            }
        }

        internal static string QUEUED_WARP;
        internal static string PERFORM_SHELTERWARPS(On.SaveState.orig_SaveToString orig, SaveState self)
        {
            if (QUEUED_WARP != null)
            {
                var oldDen = self.denPosition;
                self.denPosition = QUEUED_WARP;
            }
            return orig(self);
        }

    }
}
