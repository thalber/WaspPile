using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;

namespace WaspPile.Remnant
{
    internal static class RemnantConfig
    {
        internal static ConfigEntry<bool> noQuits;
        internal static ConfigEntry<int> martyrCycles;
    }
}
