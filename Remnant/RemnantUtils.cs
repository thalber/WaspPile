using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaspPile.Remnant
{
    internal static class RemnantUtils
    {
        internal static void SetKey<tKey, tValue> (this Dictionary<tKey, tValue> dict, tKey key, tValue val){
            if (dict == null) throw new ArgumentNullException();
            if (!dict.ContainsKey(key)) dict.Add(key, val);
            else dict[key] = val;
        }
    }
}
