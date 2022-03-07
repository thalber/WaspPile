using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace WaspPile.Remnant
{
    internal static class RemnantConfig
    {
        /// <summary>
        /// player bubble keybinds
        /// </summary>
        internal static ConfigEntry<KeyCode>[] abilityBinds = new ConfigEntry<KeyCode>[4];
        internal static KeyCode GetKeyForPlayer(int player)
        {
            if (player <= -1 || player >= abilityBinds.Length) return abilityBinds[0].Value;
            return abilityBinds[player].Value;
        }
        internal static ConfigEntry<bool> noQuits;
        internal static ConfigEntry<int> martyrCycles;
        internal static ConfigEntry<int> martyrCure;

        internal static ConfigEntry<bool> forceDebugMode;
    }
}
