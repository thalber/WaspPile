using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;

namespace WaspPile.Sacrifices
{
    public class SacrificialMod : Partiality.Modloader.PartialityMod
    {
        // ------------------------------------------------
        // AU
        // ------------------------------------------------
        public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/5/1";
        public int version = 3;
        public string keyE = "AQAB";
        public string keyN = "uwptqosDNjimqNbRwCtJIKBXFsvYZN+b7yl668ggY46j+2Zlm/+L9TpypF6Bhu85CKnkY7ffFCQixTSzumdXrz1WVD0PTvoKDAp33U/loKHoAe/rs3HwdaOAdpug//rIGDmtwx56DC05NiLYKVRf4pS3yM1xN39Rr2at/RmAxdamKLUnoJtHRwx2eGsoKq5dmPZ7BKTmF/49N6eFUvUXEF9evPRfAdPH9bYAMNx0QS3G6SYC0IQj5zWm4FnY1C57lmvZxQgqEZDCVgadphJAjsdVAk+ZruD0O8X/dqXiIBSdEjZsvs4VDsjEF8ekHoon2UZnMEd6XocIK4CBqJ9HCMGaGZusnwhtVsGyMur1Go4w0CXDH3L5mKhcEm/V7Ik2RV5/Z2Kz8555fO7/9UiDC9vh5kgk2Mc04iJa9rcWSMfrwzrnvzHZzKnMxpmc4XoSqiExVEVJszNMKqgPiQGprkfqCgyK4+vbeBSXx3Ftalncv9acU95qxrnbrTqnyPWAYw3BKxtsY4fYrXjsR98VclsZUFuB/COPTI/afbecDHy2SmxI05ZlKIIFE/+yKJrY0T/5cT/d8JEzHvTNLOtPvC5Ls1nFsBqWwKcLHQa9xSYSrWk8aetdkWrVy6LQOq5dTSD4/53Tu0ZFIvlmPpBXrgX8KJN5LqNMmml5ab/W7wE=";
        // ------------------------------------------------
        
        public SacrificialMod()
        {
            this.author = "thalber";
            this.ModID = "Sacrifices";
            this.Version = "01";
        }

        public static void ViewedSacrifice(On.Room.orig_NowViewed orig, Room instance)
        {
            orig(instance);
            for (int i = instance.updateList.Count - 1; i >= 0; i--)
            {
                if (NeverDrawThisUAD(instance.updateList[i]))
                {
                    instance.RemoveObject(instance.updateList[i]);
                }
            }
        }
        public static void LoadedSacrifice (On.Room.orig_Loaded orig, Room instance)
        {
            RoomSettings.RoomEffect eff = null;
            for (int i = 0; i < instance.roomSettings.effects.Count; i++) if (instance.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.AboveCloudsView || instance.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.RoofTopView)
                {
                    eff = instance.roomSettings.effects[i];
                    instance.roomSettings.effects.RemoveAt(i);
                    break;
                }
            orig(instance);
            if (eff != null) instance.roomSettings.effects.Add(eff);
            if (instance.game == null) return;
        }
        public static void CameraSacrifice (On.RoomCamera.orig_SetUpFullScreenEffect orig, RoomCamera instance, string container)
        {
            orig(instance, container);
            instance.fullScreenEffect?.RemoveFromContainer();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            On.Room.Loaded += LoadedSacrifice;
            On.RoomCamera.SetUpFullScreenEffect += CameraSacrifice;
            On.Room.NowViewed += ViewedSacrifice;
            On.RainWorld.Start += BlurSacrifice;
        }

        private void BlurSacrifice(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);
            try
            {
                string shadertext = Encoding.UTF8.GetString(Properties.Resources.SceneCheap);
                var mat = new UnityEngine.Material(shadertext);
                var newfshader = FShader.CreateShader("cheapBlur", mat.shader);
                self.Shaders["SceneBlur"] = newfshader;
                self.Shaders["SceneBlurLightEdges"] = newfshader;
                Console.WriteLine("Blur butchered successfully");
            }
            catch (Exception e) { Console.WriteLine($"error butchering scene blur: {e}"); }
        }

        public static bool NeverDrawThisUAD(UpdatableAndDeletable uad)
        {
            return (uad is MeltLights || uad is Lightning || uad is GenericZeroGSpeck || uad is SuperStructureProjector.SuperStructureProjectorPart || uad is SuperStructureProjector);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            On.Room.Loaded -= LoadedSacrifice;
            On.RoomCamera.SetUpFullScreenEffect -= CameraSacrifice;
            On.Room.NowViewed -= ViewedSacrifice;
            On.RainWorld.Start -= BlurSacrifice;
        }
    }
}
