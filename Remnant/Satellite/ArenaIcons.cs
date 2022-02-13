using System;
using System.Collections.Generic;
using SlugBase;
using Menu;
using UnityEngine;
using WaspPile.Remnant.Martyr;

namespace WaspPile.Remnant.Satellite
{
    internal static class ArenaIcons
    {
        public static void Apply()
        {
            On.Menu.PlayerResultBox.ctor += PlayerResultBox_ctor;
        }

        private static void PlayerResultBox_ctor(On.Menu.PlayerResultBox.orig_ctor orig, PlayerResultBox self, Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, ArenaSitting.ArenaPlayer player, int index)
        {
            orig(self, menu, owner, pos, size, player, index);
            var pd = ArenaAdditions.GetSelectedArenaCharacter(menu.manager.arenaSetup, player.playerNumber);
            if (pd.player is MartyrChar mc) //&& pd.type == ArenaAdditions.PlayerDescriptor.Type.SlugBase
            {
                var oldp = self.portrait;
                self.RemoveSubObject(oldp);
                self.portrait = new MenuIllustration(
                    menu,
                    self,
                    string.Empty,
                    RemnantPlugin.martyrFaceName,
                    oldp.pos,
                    oldp.crispPixels,
                    oldp.anchorCenter);
                self.subObjects.Add(self.portrait);
                oldp.RemoveSprites();
            }
        }

        public static void Undo()
        {
        }
    }
}