using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using CompletelyOptional;
using OptionalUI;
using UnityEngine;
using System.IO;
using System.Reflection;

using static WaspPile.ShinyRat.ShinyConfig;
using static WaspPile.ShinyRat.Satellite.RatUtils;
using static RWCustom.Custom;

namespace WaspPile.ShinyRat
{
    internal class ShinyOI : OptionInterface
    {
        public ShinyOI(BaseUnityPlugin plugin) : base(plugin)
        {
            FetchOID();
        }
        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[profiles.Length];
            for (int i = 0; i < profiles.Length; i++)
            {
                Tabs[i] = new($"Player {i + 1}");
                var ct = Tabs[i];
#error pain
            }
        }

        private static Dictionary<string, Vector2[]> OIData = new();
        private static void FetchOID()
        {
            OIData.Clear();
            string txp = combinePath(RootFolderDirectory(), "ratOiCoords.txt");
            Stream ers = Assembly.GetExecutingAssembly().GetManifestResourceStream("WaspPile.ShinyRat.ratOICoords.txt");
            string[] tx = new string[0];
            if (File.Exists(txp)) tx = File.ReadAllLines(txp);
            if (ers is not null) tx = Regex.Split(Encoding.UTF8.GetString(new BinaryReader(ers).ReadBytes((int)ers.Length)), Environment.NewLine);
            foreach (string l in tx)
            {
                var split = Regex.Split(l, ", ");
            }
        }
        static ShinyOI()
        {
            
        }
    }
}
