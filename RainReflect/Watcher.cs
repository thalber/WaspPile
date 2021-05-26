using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace WaspPile.RR
{
    class Watcher
    {
        public Watcher()
        {
            mrs = new List<ModRelay>();
            string[] plugins = Directory.GetFiles(Path.Combine(BepInEx.Paths.GameRootPath, "Mods"));
            
            foreach (string pl in plugins)
            {
                var fi = new FileInfo(pl);
                if (fi.Extension == ".dll")
                {
                    mrs.Add(new ModRelay(pl));
                }
                
            }
            Debug.Log("Watcher set up!");
        }
        public List<ModRelay> mrs;
    }
}
