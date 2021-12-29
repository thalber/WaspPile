using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using RWCustom;
using UnityEngine;

using static RWCustom.Custom;
using static WaspPile.Remnant.Satellite.RemnantUtils;
using static UnityEngine.Debug;

using MoonConvo = SLOracleBehaviorHasMark.MoonConversation;
using RocksConvo = SSOracleBehavior.PebblesConversation;

namespace WaspPile.Remnant.Satellite
{
//#warning karma specevent does not work with fp
//fixed
    internal static class ConvoHelper
    {
        internal static bool TryEnqueuePatchedEvents(this Conversation c, InGameTranslator.LanguageID lang)
        {
            List<Conversation.DialogueEvent> ins = new(ModifiedConvo(c, lang));
            foreach (var cev in ins) if (cev is null)
                {
                    LogWarning("MARTYR COMMS: null element in conversation list; convo presumed MIA");
                    return false;
                }
            c.events.AddRange(ins);
            return true;
        }
        internal static IEnumerable<Conversation.DialogueEvent> ModifiedConvo(Conversation con,
            InGameTranslator.LanguageID lang)
        {
            string subf = default, entryOverride = default;
            bool pickrandom = false;
            switch (con.id)
            {
                case Conversation.ID.Moon_Misc_Item:
                    subf = "miscitems";
                    entryOverride = (con as MoonConvo)?.describeItem.ToString();
                    break;
                case Conversation.ID.Moon_Pearl_Misc:
                case Conversation.ID.Moon_Pearl_Misc2:
                    subf = "miscpearls";
                    entryOverride = (con.id == Conversation.ID.Moon_Pearl_Misc) ? "p1" : "p2";
                    pickrandom = true;
                    break;
                case Conversation.ID.Moon_Pebbles_Pearl:
                    subf = "pebblespearls";
                    entryOverride = "quale";
                    pickrandom = true;
                    break;
            }
            string[] covres = default;
            Log($"Getting convo override files...");
            LogBunch(__arglist(con.id, lang, subf, entryOverride));
            covres = GetConvoOverrideResource(con.id, lang, subf, entryOverride);
            if (covres == null)
            {
                LogWarning($"No resource override found! using vanilla method");
                yield return null;
                yield break;
            }
            if (pickrandom)
            {
                Log("Random line picked");
                yield return new Conversation.TextEvent(con, 0, covres.RandomOrDefault(), 0);
                yield break;
            }
            foreach (var l in covres)
            {
                if (l.Length == 0) continue;
                var spl = Regex.Split(l, " : ");
                switch (spl.Length)
                {
                    case 3:
                        int inw, linger;
                        int.TryParse(spl[0], out inw);
                        int.TryParse(spl[1], out linger);
                        yield return new Conversation.TextEvent(con, inw, spl[2], linger);
                        break;
                    case 2:
                        switch (spl[0])
                        {
                            case "SPECEVENT":
                                yield return new Conversation.SpecialEvent(con, 0, spl[1]);
                                break;
                            case "PEBBLESWAIT":
                                int.TryParse(spl[1], out var wait);
                                yield return new RocksConvo.PauseAndWaitForStillEvent(con, null, wait);
                                break;
                        }
                        break;
                    case 1:
                        yield return new Conversation.TextEvent(con, 0, l, 0);
                        break;
                }

            }
        }
        internal static string[] GetConvoOverrideResource(Conversation.ID id,
            InGameTranslator.LanguageID lang, string subfolder = default, string entryOverride = default)
        {
            var rs = MartyrChar.GetRes(
                "text", lang.ToString(), subfolder ?? "dialogue", entryOverride ?? id.ToString() + ".txt"
                );
            if (rs != null)
            {
                BinaryReader br = new(rs);
                return Regex.Split(Encoding.UTF8.GetString(br.ReadBytes((int)rs.Length)), Environment.NewLine);
            }
            LogWarning("requested resource is null!");
            return null;
        }
        internal static bool ConvoOverrideExists(Conversation.ID id, InGameTranslator.LanguageID lang)
            => GetConvoOverrideResource(id, lang) != null;
    }
}
