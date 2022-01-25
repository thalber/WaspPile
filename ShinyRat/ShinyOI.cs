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
        //todo: highlight broken elements
        //todo: find a fix for intersecting scrollboxes
        public ShinyOI(BaseUnityPlugin plugin) : base(plugin)
        {

        }
        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[profiles.Length];
            ratPages = new RatPage[profiles.Length];
            for (int i = 0; i < profiles.Length; i++)
            {
                Tabs[i] = new($"Player {i + 1}");
                ratPages[i] = new(this);
                var cTab = Tabs[i];
                var cProf = profiles[i];
                var cPage = ratPages[i];
                string keyprefix = $"ShinyRat.p{i}.";
                //header
                OpRect headerBack = new(new(15, 495), new(530, 90));
                OpLabel header0 = new(30f, 520, "ShinyRat", true);
                OpLabel header1 = new(new(240, 500), new(280, 80), 
                    $"Profile settings for {cTab.name}\nSelect sprite replacements and colors below", 
                    FLabelAlignment.Right);
                cPage.op_enabled = new(new(20, 450), keyprefix + $"enabled", true) { description = cProf.enabled.Description.Description };
                cPage.op_yield = new(new(325, 450), keyprefix + $"ctyield") { description = cProf.yieldToCT.Description.Description };
                OpLabel lb_enabled = new(new Vector2(20, 445), new(175, 32), "Profile enabled");
                OpLabel lb_yield = new(new Vector2(329, 440), new(175, 35), "Prioritize CustomTail");
                cTab.AddItems(headerBack, header0, header1, cPage.op_enabled, lb_enabled, cPage.op_yield, lb_yield);
                //bp
                OpLabel lb_bp = new(new Vector2(15, 375), new Vector2(175, 35), "Bodyparts", FLabelAlignment.Left, true) { color = echoGold };
                OpScrollBox bpHolder = new(new(15, 15), new(270, 360), cProf.BodyPartSettings.Count * 220 + 5);
                cTab.AddItems(lb_bp, bpHolder);
                int cind = 0;
                foreach(KeyValuePair<BP, SpriteGroupInfo> settingPair in cProf.BodyPartSettings)
                {
                    int y_off = cind * 220;
                    OpRect box = new(new(5, y_off + 5), new(245, 214));
                    OpLabel lb_groupTitle = new(new Vector2(10,  y_off + 175), new(245, 32), settingPair.Key.ToString().ToUpper(), bigText:true);
                    OpTextBox op_elmname = new(new(10,  y_off + 151), 225, keyprefix + $"{settingPair.Key}_baseElm", DefaultElmBaseNames[settingPair.Key]);
                    OpLabel lb_elmname = new(10,  y_off + 128, "Element name");
                    OpUpdown op_scX = new(new(10,  y_off + 95), 100, keyprefix + $"{settingPair.Key}_scaleX", 1f, 2);
                    OpLabel lb_scX = new(10,  y_off + 72, "scale X");
                    OpUpdown op_scY = new(new(10,  y_off + 38), 100, keyprefix + $"{settingPair.Key}_scaleY", 1f, 2);
                    OpLabel lb_scY = new(10,  y_off + 15, "scale Y");
                    cPage.op_spriteGroups.Add(settingPair.Key, (op_elmname, op_scX, op_scY));
                    bpHolder.AddItems(box, lb_groupTitle, op_elmname, lb_elmname, op_scX, lb_scX, op_scY, lb_scY);
                    cind++;
                }
                //colors
                OpLabel lb_cl = new(new Vector2(350, 245), new(235, 32), "Colors", FLabelAlignment.Right, true) { color = echoGold };
                OpScrollBox colorHolder = new(new(315, 15), new(270, 228), 520, true);
                cTab.AddItems(lb_cl, colorHolder);
                cind = 0;
                for (int j = 0; j < 3; j++)
                {
                    int x_off = cind * 170 + 5;
                    string cgName = j switch {
                        0 => "body",
                        1 => "eyes",
                        2 => "terrainHand",
                        _ => throw new IndexOutOfRangeException()
                    };
                    OpRect box = new(new(x_off, 25), new(160, 200));
                    OpColorPicker op_ccol = new(new(x_off + 5, 35), 
                        keyprefix + $"{cgName}_col", 
                        j switch { 0 => "FFFFFF", 1 => "303030", 2 => "FFFFFF", _ => "FFFFFF" });
                    OpLabel lb_ccol = new(x_off + 15, 190, cgName, true);
                    colorHolder.AddItems(box, op_ccol, lb_ccol);
                    cPage.op_colorGroups[j] = op_ccol;
                    cind++;
                }
                cPage.ratLogo = new(new(170, 535));
                cTab.AddItems(cPage.ratLogo);
                cPage.mutexScrol[0] = bpHolder;
                cPage.mutexScrol[1] = colorHolder;
            }
            PERPETUALTORMENT = cycleBpForAllPlayers();
        }
        public override void Update(float dt)
        {
            base.Update(dt);
            foreach (var page in ratPages)
            {
                //prevent intersections
                var acBox = page.mutexScrol.FirstOrDefault(xx => xx.MouseOver);
                if (acBox is not null) foreach (var other in page.mutexScrol) if (other != acBox) other.ScrollToTop();
                //spin the rat
                //todo: more rats?
                if (page.ratLogo.container.GetChildCount() is not 0)
                {
                    page.ratLogo.container.GetChildAt(0).rotation += 13f * dt;
                }
                else if (ShinyRatPlugin.ME.atlasDone)
                {
                    FSprite logoSprite = new(Futile.atlasManager.GetElementWithName("ShinyRat"));
                    logoSprite.scale = 0.5f;
                    page.ratLogo.container.AddChild(logoSprite);
                }
            }
            if (PERPETUALTORMENT.MoveNext())
            {
                evalSpriteGroup(PERPETUALTORMENT.Current);
            }
        }
        public override void ConfigOnChange()
        {
            base.ConfigOnChange();
            applyCfg();
        }
        private void applyCfg()
        {
            if (ShinyRatPlugin.DebugMode) Debug.LogWarning("saving rat cfg");
            for (int i = 0; i < ratPages.Length; i++) ratPages[i].writeToProfile(profiles[i]);
            ShinyRatPlugin.ME.savecfg();
        }
        internal Dictionary<string, string> p_config => config;

        internal RatPage[] ratPages;
        internal sealed class RatPage
        {
            private ShinyOI owner;
            internal RatPage(ShinyOI ow)
            {
                owner = ow;
            }
            internal OpCheckBox op_enabled;
            internal OpCheckBox op_yield;
            internal Dictionary<BP, (OpTextBox, OpUpdown, OpUpdown)> op_spriteGroups = new();
            /// <summary>
            /// 0:body; 1:eyes; 2:TTH
            /// </summary>
            internal OpColorPicker[] op_colorGroups = new OpColorPicker[3];
            internal OpContainer ratLogo;
            internal OpScrollBox[] mutexScrol = new OpScrollBox[2];
            internal void readFromProfile(RatProfile prof)
            {
                op_enabled.valueBool = prof.enabled.Value;
                op_yield.valueBool = prof.yieldToCT.Value;
                var cfg = owner.p_config;
                foreach (KeyValuePair<BP, SpriteGroupInfo> kvp in prof.BodyPartSettings)
                {
                    if (!op_spriteGroups.TryGetValue(kvp.Key, out var tri)) continue;
                    tri.Deconstruct(out var elm, out var scx, out var scy);
                    cfg.SetKey(elm.key, kvp.Value.baseElm.Value);
                    cfg.SetKey(scx.key, kvp.Value.scaleX.Value.ToString());
                    cfg.SetKey(scy.key, kvp.Value.scaleY.Value.ToString());
                }
                cfg.SetKey(op_colorGroups[0].key, OpColorPicker.ColorToHex(prof.bodyCol));
                cfg.SetKey(op_colorGroups[1].key, OpColorPicker.ColorToHex(prof.faceCol));
                cfg.SetKey(op_colorGroups[2].key, OpColorPicker.ColorToHex(prof.TTHCol));
            }
            internal void writeToProfile(RatProfile prof)
            {
                prof.enabled.Value = op_enabled.valueBool;
                prof.yieldToCT.Value = op_yield.valueBool;
                var cfg = owner.p_config;
                foreach (KeyValuePair<BP, (OpTextBox, OpUpdown, OpUpdown)> kvp in op_spriteGroups)
                {
                    if (!prof.BodyPartSettings.TryGetValue(kvp.Key, out var sgi)) continue;
                    kvp.Value.Deconstruct(out var elm, out var scx, out var scy);
                    sgi.baseElm.Value = cfg.TryGetAndParse(elm.key, sgi.baseElm.Value);
                    sgi.scaleX.Value = cfg.TryGetAndParse(scx.key, sgi.scaleX.Value);
                    sgi.scaleY.Value = cfg.TryGetAndParse(scy.key, sgi.scaleY.Value);
                }
                prof.bodyCol = cfg.TryGetAndParse(op_colorGroups[0].key, prof.bodyCol);
                prof.faceCol = cfg.TryGetAndParse(op_colorGroups[1].key, prof.bodyCol);
                prof.TTHCol = cfg.TryGetAndParse(op_colorGroups[2].key, prof.bodyCol);
            }
        }

        private object freedom;
        internal IEnumerator<(BP, OpTextBox)> PERPETUALTORMENT;
        internal IEnumerator<(BP, OpTextBox)> cycleBpForAllPlayers()
        {
            int cpi = default;
            while (freedom == null)
            {
                cpi++;
                if (cpi >= ratPages.Length) cpi = 0;
                var cpage = ratPages[cpi];
                foreach (var kvp in cpage.op_spriteGroups) yield return (kvp.Key, kvp.Value.Item1);
            }
        }
        internal void evalSpriteGroup((BP, OpTextBox) sg)
        {
            static IEnumerable<string> appendall(string b, IEnumerable<object> postf)
            {
                foreach (var p in postf) yield return b + postf;
            }
            static IEnumerable<int> allbetween(int from, int to)
            {
                int lowb = Mathf.Min(from, to), higb = Mathf.Max(from, to);
                while (lowb <= higb) { yield return lowb++; }
            }

            Color res = Color.grey;
            if (Futile.atlasManager is null) goto skipAll;
            string[] requiredElms = sg.Item1 switch
            {
                BP.arm => appendall(sg.Item2.value, allbetween(0, 12).Cast<object>()).ToArray(),
                BP.head => appendall(sg.Item2.value, allbetween(0, 17).Cast<object>()).ToArray(),
                BP.face => appendall(sg.Item2.value, allbetween(0, 8).Cast<object>()).ToArray(),
                _ => new[] {sg.Item2.value}
            };
            int errc = 0;
            foreach (var relm in requiredElms.SkipWhile(Futile.atlasManager.DoesContainElementWithName)) errc++;
            if (errc <= 0) res = Color.cyan;
            else if (errc < requiredElms.Length) res = Color.yellow;
            else res = Color.red;
            skipAll:
            sg.Item2.colorText = res;
        }

        ~ShinyOI()
        {
            PERPETUALTORMENT?.Dispose();
        }
        static ShinyOI()
        {
            
        }
    }
}
