using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RWCustom;
using UnityEngine;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using BepInEx;

using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;
using static WaspPile.SecondSilverStem._3SUTL;

using URand = UnityEngine.Random;

namespace WaspPile.SecondSilverStem
{
    [BepInPlugin("thalber.stp", "StemTestPlugin", "0.0.1")]
    public class StemTestPlugin : BaseUnityPlugin
    {
        public StemTestPlugin()
        {
            _me = new(this);
        }

        public void OnEnable()
        {
            IL.Room.Loaded += rlTestPatch;
        }

        private void rlTestPatch(ILContext il)
        {
            ILCursor c = new(il);
            try
            {
                foreach (var kvp in samplePatterns.samplePatterns.sampleCollections)
                {
                    Logger.LogWarning($"Attempting to apply pattern {kvp.Key}...");
                    var mc = kvp.Value;
                    mc.BindTypes(
                    ("room", typeof(Room)),
                    ("absroom", typeof(AbstractRoom)),
                    ("uad", typeof(UpdatableAndDeletable)),
                    ("gate", typeof(RegionGate)),
                    ("elecgate", typeof(ElectricGate))
                    );
                    mc.BindProcs(
                        ("rm_addobj", methodof<Room>("AddObject")),
                        ("rm_ctor", ctorof<Room>(typeof(RainWorldGame), typeof(World), typeof(AbstractRoom))),
                        ("elecgate_ctor", ctorof<ElectricGate>(typeof(Room)))
                        );
                    mc.BindFlds(
                        ("rm_gate", fieldof<Room>("regionGate"))
                        );
                    foreach (var child in mc._children.Values)
                    {
                        LogWarning(child);
                    }
                    foreach (var str in mc.Data) LogWarning(str);
                    Logger.LogWarning(c.TryGotoNext(MoveType.After, mc._children.First().Value.ReturnPredicates().ToArray()) ? "SUCCESS" : "FAILURE TO MATCH");
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"ERROR APPLYING PATTERN: {e}");
            }
            
        }
        private static WeakReference _me;
        public static StemTestPlugin ME => _me?.Target as StemTestPlugin;
        public static BepInEx.Logging.ManualLogSource stlog => ME?.Logger;

        public void OnDisable()
        {
            IL.Room.Loaded -= rlTestPatch;
        }
    }
}
