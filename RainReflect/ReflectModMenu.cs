using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Menu;
using UnityEngine;
using AssemblyCSharp;

namespace WaspPile.RR
{
    class ReflectModMenu : Menu.Menu
    {
        public ReflectModMenu (ProcessManager manager, MainMenu mm) : base (manager, ProcessManager.ProcessID.OptionsMenu)
        {
            this.pages.Add(new Page(this, null, "MM_MAINPAGE", 0));
            this.pages[0].subObjects.Add(new SimpleButton(this, this.pages[0], "BACK", "GOBACK", new Vector2(100f, 100f), new UnityEngine.Vector2(100f, 100f)));
            this.pages[0].subObjects.Add(new Menu.HorizontalSlider(this, this.pages[0], "", new Vector2(500f, 110f), new Vector2(100f, 10f), Slider.SliderID.MusicVol, false));
            mm.ShutDownProcess();
            this.scene = new InteractiveMenuScene(this, this.pages[0], MenuScene.SceneID.Dream_Pebbles);
            this.scene.BuildScene();
            w = new Watcher();
            GenerateButtons();
        }
        public void GenerateButtons()
        {
            Vector2 cpos = new Vector2(300f, 100f);
            foreach (ModRelay mr in w.mrs)
            {
                this.pages[0].subObjects.Add(new SimpleButton(this, this.pages[0], mr.ToString(), mr.name, cpos, new Vector2(300f, 35f)));
                cpos.y += 35f;
                Debug.Log(mr);
            }
        }
        Watcher w;

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "GOBACK")
            {
                this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                this.ShutDownProcess();
            }
        }
    }
}
