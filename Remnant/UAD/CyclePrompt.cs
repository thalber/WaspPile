using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaspPile.Remnant.UAD
{
    internal class CyclePrompt : UpdatableAndDeletable
    {
        public override void Update(bool eu)
        {
            base.Update(eu);
            if (room?.game?.IsArenaSession ?? true) { this.Destroy(); return; }
            string message = $"Remaining cycles: {RemnantConfig.martyrCycles.Value - room.game?.rainWorld.progression.currentSaveState.cycleNumber}";
            room.game?.cameras[0].hud.textPrompt.AddMessage(message, 15, 400, false, false);
            Destroy();
        }
    }
}
