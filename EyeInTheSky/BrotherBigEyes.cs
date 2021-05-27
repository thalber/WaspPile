using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaspPile.EyeIntheSky.Rulesets;

namespace WaspPile.EyeIntheSky
{
    public static class BrotherBigEyes
    {
        public static Dictionary<Type, GeneralRuleset> AllMyRules { get { _amr = _amr ?? new Dictionary<Type, GeneralRuleset>(); return _amr; } set { _amr = value; } }
        private static Dictionary<Type, GeneralRuleset> _amr;
        public static bool TryGetRules(Type t, out GeneralRuleset rules) { rules = TryReturnRules(t); return (rules != null); }
        public static GeneralRuleset TryReturnRules(Type t) { return (AllMyRules.ContainsKey(t)) ? AllMyRules[t] : null; }
        public static void AddOrUpdate (in Type t, GeneralRuleset rules)
        {
            if (AllMyRules.ContainsKey(t)) AllMyRules[t] = rules;
            else AllMyRules.Add(t, rules);
        }

    }
}
