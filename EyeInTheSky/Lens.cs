using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaspPile.EyeIntheSky.Rulesets;
using UnityEngine;

namespace WaspPile.EyeIntheSky
{
    public class Lens : Partiality.Modloader.PartialityMod
    {
        public Lens() : base()
        {
            this.author = "thalber";
            this.Version = "0";
            this.ModID = "EyeInTheSky";
        }
        public override void OnEnable()
        {
            base.OnEnable();
            LensHooks.ApplyToVanilla();
            On.Menu.Menu.ctor += testPoint;
        }

        public static void testPoint(On.Menu.Menu.orig_ctor orig, Menu.Menu self, ProcessManager mngr, ProcessManager.ProcessID id)
        {
            orig(self, mngr, id);
            var testSpriteReplacements = new Dictionary<int, FSprite>();
            for (int i = 0; i < 3; i++)
            {
                testSpriteReplacements.Add(i, new FSprite("BodyA", true) { scaleX = 10, scaleY = 10, alpha = 1,  });
            }

            var oninit = new SpriteArrayRuleset(typeof(DangleFruit), testSpriteReplacements);
            var grs = new GeneralRuleset();
            grs.DoOnInit = oninit;
            BrotherBigEyes.AddOrUpdate(typeof(DangleFruit), grs);
        }
    }
}
