using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedPlacedObjects;

namespace WaspPile.EDGESILK
{
    [BepInPlugin("thalber.EDGESILK", "EDGESILK", "0.0.1")]
    public class EDGESILK_PLUGIN : BaseUnityPlugin
    {
        public EDGESILK_PLUGIN()
        {
            instance = this;
        }
        public static BepInEx.Logging.ManualLogSource clog => instance.Logger;
        public static EDGESILK_PLUGIN instance;
        public void Awake()
        {
            Logger.Log(BepInEx.Logging.LogLevel.Debug, "Applying hooks on awake...");
            EDGESILK_HOOKS.Apply();

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.StringField("WARP_DESTINATION", "SU_S01", displayName:"Destination")
            }, 
            typeof(CRES_SIMPLESHELTERWARP));
        }

    }
}
