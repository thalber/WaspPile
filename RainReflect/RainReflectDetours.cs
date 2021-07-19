using Menu;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;

namespace WaspPile.RR
{
    internal class RainReflectDetours
    {
        public delegate void orig_MainMenu_Ctor(MainMenu self, ProcessManager manager, bool regspecbg);
        public static void MainMenu_Ctor (orig_MainMenu_Ctor orig, MainMenu self, ProcessManager manager, bool rsbg)
        {
            orig(self, manager, rsbg);
            self.pages[0].subObjects.Add(new SimpleButton(self, self.pages[0], "MODS", "MODMENU", new UnityEngine.Vector2(100f, 200f), new UnityEngine.Vector2(110f, 30f)));
        }

        public delegate void orig_MainMenu_Signal(MainMenu self, MenuObject sender, string message);
        public static void MainMenu_Signal(orig_MainMenu_Signal orig, MainMenu self, Menu.MenuObject sender, string message)
        {
            if (sender is SimpleButton && message == "MODMENU")
            {
                (sender as SimpleButton).menuLabel.text = "BZZZZ";
                //(sender as SimpleButton).
                self.PlaySound(SoundID.MENU_Checkbox_Check);
                ReflectModMenu rmm = new ReflectModMenu(self.manager, self);
                //rmm.mainmenu = self;
                self.manager.currentMainLoop = rmm;
                //self.manager.
            }
            
            orig(self, sender, message);
            
        }


        public static List<Hook> hooks = new List<Hook>();

        public static void ApplyAllDetours()
        {
            hooks.Add(new Hook(typeof(Menu.MainMenu).GetConstructors()[0], typeof(RainReflectDetours).GetMethod(nameof(MainMenu_Ctor))));
            hooks.Add(new Hook(typeof(Menu.MainMenu).GetMethod(nameof(Menu.MainMenu.Singal)), typeof(RainReflectDetours).GetMethod(nameof(MainMenu_Signal))));
            UnityEngine.Debug.Log("Rainref: stuff applied!");
        }
    }
}
