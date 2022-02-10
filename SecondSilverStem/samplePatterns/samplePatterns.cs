using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaspPile.SecondSilverStem;
using System.Reflection;
using System.IO;

using static WaspPile.SecondSilverStem.StemTestPlugin;

namespace WaspPile.SecondSilverStem.samplePatterns
{
    public static class samplePatterns
    {
        internal static readonly Dictionary<string, _3S.ILPatternCollection> sampleCollections = new();

        static samplePatterns()
        {
            
            var mt = typeof(samplePatterns);
            var casm = mt.Assembly;
            foreach(var res in casm.GetManifestResourceNames())
            {
                var str = casm.GetManifestResourceStream(res);
                try
                {
                    var resstring = Encoding.UTF8.GetString(new BinaryReader(str).ReadBytes((int)str.Length));
                    _3S.ILPatternCollection c = new(resstring.Split('\n', '\r'));
                    sampleCollections.Add(res, c);
                    stlog.LogWarning(resstring);
                }
                catch (Exception e)
                {
                    stlog.LogWarning($"Error on inst sample collection: {e}");
                }
            }
        }
    }
}
