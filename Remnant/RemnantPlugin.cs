using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;

namespace WaspPile.Remnant
{
    [BepInPlugin("EchoWorld.Martyrdom", "Martyrdom", "0.0.1")]
    public class RemnantPlugin : BaseUnityPlugin
    {
        public void OnEnable()
        {
            SlugBase.PlayerManager.RegisterCharacter(new MartyrChar());
        }
    }


}
