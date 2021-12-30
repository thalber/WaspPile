using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using RWCustom;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using WaspPile.Remnant.UAD;

using static RWCustom.Custom;
using static WaspPile.Remnant.Satellite.RemnantUtils;
using static WaspPile.Remnant.Satellite.ConvoHelper;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;

using MoonConvo = SLOracleBehaviorHasMark.MoonConversation;
using RocksConvo = SSOracleBehavior.PebblesConversation;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant
{
    static partial class MartyrHooks
    {
        private static bool ProcessDialogue(this Conversation convo)
        {
            var clang = CRW.inGameTranslator.currentLanguage;
            //var ovres = ConvoOverrideExists(convo.id, clang);
            Log("MARTYR COMMS: trying to override " + convo.id.ToString());
            if (convo.id.ToString().Contains("Moon_Pearl_") && convo is MoonConvo mc) mc.PearlIntro();
            return convo.TryEnqueuePatchedEvents(clang);
        }

        public static void CONVO_Enable()
        {
            IL.SLOracleBehaviorHasMark.MoonConversation.AddEvents += IL_SLOB_OverrideConvos;
            IL.GhostConversation.AddEvents += IL_Echo_OverrideConvos;
            IL.SSOracleBehavior.PebblesConversation.AddEvents += IL_SSOB_OverrideConvos;
            IL.SSOracleBehavior.NewAction += insertPebblesSequence;
            //On.Conversation.SpecialEvent.Activate += speceventNotify5p;
        }

        private static void speceventNotify5p(On.Conversation.SpecialEvent.orig_Activate orig, Conversation.SpecialEvent self)
        {
            if (self.owner.interfaceOwner is SSOracleBehavior) self.eventName = "karma";
            orig(self);
        }

        private static void insertPebblesSequence(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(MoveType.Before, xx=> xx.MatchNewobj<SSOracleBehavior.SSOracleMeetWhite>());
            c.Remove();
            c.Emit(Newobj, ctorof<Satellite.MeetMartyrSubroutine>(typeof(SSOracleBehavior)));
            //il.dump(RootFolderDirectory(), "ssob_newentry");
        }

        //TODO: candidate for moving into commonHooks
        private static void IL_SSOB_OverrideConvos(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.Before, xx => xx.MatchBr(out _));
            var rb = c.CurrentInstruction();
            c.Index = 0;
            c.Emit(Ldarg_0);
            c.EmitDelegate<Func<Conversation, bool>>(ProcessDialogue);
            c.Emit(Brtrue, rb);
            LogWarning("MARTYR COMMS: SSOracle conversation defiled");
        }

        private static void IL_Echo_OverrideConvos(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.Before, xx => xx.MatchBr(out _));
            var rb = c.CurrentInstruction();
            c.Index = 0;
            c.Emit(Ldarg_0);
            c.EmitDelegate<Func<Conversation, bool>>(ProcessDialogue);
            c.Emit(Brtrue, rb);
            LogWarning("MARTYR COMMS: Echo conversation defiled");
        }

        private static void IL_SLOB_OverrideConvos(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.Before,
                xx => xx.MatchBr(out var whatever),
                xx => xx.Match(Ldarg_0),
                xx => xx.MatchCallOrCallvirt<MoonConvo>("get_State"));
            var exit = c.CurrentInstruction();
            c.Index = 0;
            c.GotoNext(MoveType.After,
                xx => xx.MatchBox<int>(),
                xx => xx.MatchCallOrCallvirt<string>("Concat"),
                xx => xx.MatchCallOrCallvirt<Debug>("Log"));
            c.Emit(Ldarg_0);
            c.EmitDelegate<Func<Conversation, bool>>(ProcessDialogue);
            c.Emit(Brtrue, exit);
            LogWarning("MARTYR COMMS: SSOracle conversation defiled");
        }

        public static void CONVO_Disable()
        {
            IL.SLOracleBehaviorHasMark.MoonConversation.AddEvents -= IL_SLOB_OverrideConvos;
            IL.GhostConversation.AddEvents -= IL_Echo_OverrideConvos;
            IL.SSOracleBehavior.PebblesConversation.AddEvents -= IL_SSOB_OverrideConvos;
            IL.SSOracleBehavior.NewAction -= insertPebblesSequence;
            //On.Conversation.SpecialEvent.Activate -= speceventNotify5p;
        }
    }
}
