using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlugBase;
using WaspPile.Remnant.Martyr;
using static UnityEngine.Debug;

namespace WaspPile.Remnant.UAD
{
    internal class CyclePrompt : UpdatableAndDeletable
    {
        public override void Update(bool eu)
        {
            base.Update(eu);
            if (room?.game?.IsArenaSession ?? true) { this.Destroy(); return; }
            if (!room.game.TryGetSave<MartyrChar.MartyrSave>(out var ms)) goto whatever;
            string message = $"Remaining cycles: {ms.RemainingCycles}";
            room.game?.cameras[0].hud.textPrompt.AddMessage(message, 15, 400, false, false);
            if (RemnantPlugin.DebugMode) { LogWarning($"notif player: {ms.RemainingCycles} ({ms.cycleLimit}, {ms.cycleCure}, {ms.cureApplied})"); }
        whatever:
            Destroy();
        }
    }
}
