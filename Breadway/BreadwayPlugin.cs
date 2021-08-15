using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaspPile.Breadway
{
    [BepInPlugin("thalber.Breadway", "Breadway", "0.0.2")]
    public class BreadwayPlugin : BaseUnityPlugin
    {
        

        public void OnEnable()
        {
            BreadwayHooks.Register();
        }

        public void OnDisable()
        {
            BreadwayHooks.Undo();
        }
    }
}
