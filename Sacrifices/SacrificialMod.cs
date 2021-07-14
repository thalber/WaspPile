using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaspPile.Sacrifices
{
    public class SacrificialMod : Partiality.Modloader.PartialityMod
    {
        // ------------------------------------------------
        // AU
        // ------------------------------------------------
        public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/5/1";
        public int version = 0;
        public string keyE = "AQAB";
        public string keyN = "uwptqosDNjimqNbRwCtJIKBXFsvYZN+b7yl668ggY46j+2Zlm/+L9TpypF6Bhu85CKnkY7ffFCQixTSzumdXrz1WVD0PTvoKDAp33U/loKHoAe/rs3HwdaOAdpug//rIGDmtwx56DC05NiLYKVRf4pS3yM1xN39Rr2at/RmAxdamKLUnoJtHRwx2eGsoKq5dmPZ7BKTmF/49N6eFUvUXEF9evPRfAdPH9bYAMNx0QS3G6SYC0IQj5zWm4FnY1C57lmvZxQgqEZDCVgadphJAjsdVAk+ZruD0O8X/dqXiIBSdEjZsvs4VDsjEF8ekHoon2UZnMEd6XocIK4CBqJ9HCMGaGZusnwhtVsGyMur1Go4w0CXDH3L5mKhcEm/V7Ik2RV5/Z2Kz8555fO7/9UiDC9vh5kgk2Mc04iJa9rcWSMfrwzrnvzHZzKnMxpmc4XoSqiExVEVJszNMKqgPiQGprkfqCgyK4+vbeBSXx3Ftalncv9acU95qxrnbrTqnyPWAYw3BKxtsY4fYrXjsR98VclsZUFuB/COPTI/afbecDHy2SmxI05ZlKIIFE/+yKJrY0T/5cT/d8JEzHvTNLOtPvC5Ls1nFsBqWwKcLHQa9xSYSrWk8aetdkWrVy6LQOq5dTSD4/53Tu0ZFIvlmPpBXrgX8KJN5LqNMmml5ab/W7wE=";
        // ------------------------------------------------
        
        public SacrificialMod()
        {
            this.author = "thalber";
            this.ModID = "Sacrifices";
            this.Version = "01";
        }

        public static void LoadedSacrifice (On.Room.orig_Loaded orig, Room instance)
        {
            orig(instance);
            if (instance.game == null) return;
            for (int i = 0; i < instance.updateList.Count; i++)
            {
                if (UEDForbidden(instance.updateList[i])) instance.RemoveObject(instance.updateList[i]);
            }
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
        }
        public static bool UEDForbidden(object ued) => ued is BackgroundScene;
    }
}
