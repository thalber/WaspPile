using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WaspPile.TongueGrinder
{
    public class LPrimitive
    {
        public LPrimitive(string raw)
        {
            var sp = Regex.Split(raw, "[#:]");
            if (sp.Length == 2)
            {
                hasName = true;
                PropName = sp[0];
                Contents = sp[1];
            }
            else { hasName = false; PropName = null; Contents = raw; }
        }
        public string Contents;
        public string PropName;
        public bool hasName;
    }
}
