using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaspPile.EyeIntheSky.Rulesets;

namespace WaspPile.EyeIntheSky
{
    public static class BrotherBigEyes
    {
        public static Dictionary<Type, GeneralRuleset> AllMyRules;
        public static bool TryGetRules(Type t, out GeneralRuleset rules) { rules = (AllMyRules.ContainsKey(t)) ? AllMyRules[t] : null; return (rules != null); }

    }
}
