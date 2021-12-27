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
//using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;
using static WaspPile.Remnant.MartyrHooks;
using static WaspPile.Remnant.CommonHooks;

using MoonConvo = SLOracleBehaviorHasMark.MoonConversation;
using RocksConvo = SSOracleBehavior.PebblesConversation;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant.Satellite
{
    public class MeetMartyrSubroutine : SSOracleBehavior.ConversationBehavior
    {
        public MeetMartyrSubroutine(SSOracleBehavior owner) : base(owner,
            EnumExt_Remnant.SSOB_Subr_MeetMartyr,
            Conversation.ID.Pebbles_Red_Green_Neuron)
        {
            owner.SetNewDestination(owner.oracle.room.MiddleOfRoom() + RNV() * 50f);
            //id will differ see if it's an issue
        }
        
        internal DataPearl.AbstractDataPearl abs_message;
        internal DataPearl message => abs_message?.realizedObject as DataPearl;
        internal Player guest;
        internal bool convoStarted = false;
        internal readonly List<Player> searchedPlayers = new();

        internal Oracle pebbles => this.owner.oracle;

        public override void Update()
        {
            var croom = owner.oracle.room;
            var roomCenter = croom.MiddleOfRoom();
            //LogBunch(__arglist(croom, roomCenter));
            base.Update();
            if (windup-- > 0)
            {
                return;
            }
            try
            {
                if (searchForMessageCounter > 0)
                {
                    searchForMessageCounter--;
                    if (abs_message != null) goto regdone;
                    guest ??= (Player)croom.updateList.FirstOrDefault(uad => (uad is Player p) && !searchedPlayers.Contains(p));
                    foreach (var po in croom.updateList) if (po is DataPearl dp && dp.IsEchoPearl()) abs_message ??= dp.AbstractPearl;
                    if (guest != null)
                    {
                        if (guest.objectInStomach is DataPearl.AbstractDataPearl abp && abp.IsEchoPearl())
                        {
                            guest.Regurgitate();
                        }
                    }
                regdone:
                    //LogWarning("MARK1");
                    if (message != null) searchForMessageCounter = 0;
                }
                else
                {
                    //LogWarning("MARK2");
                    convoID = (message == null) ? Conversation.ID.Pebbles_Red_No_Neuron : Conversation.ID.Pebbles_Red_Green_Neuron;
                    if (!convoStarted)
                    {
                        owner.InitateConversation(convoID, this);
                        owner.SetNewDestination(owner.oracle.room.MiddleOfRoom() + RNV() * 100f);
                    }
                    convoStarted = true;
                    //LogWarning("MARK3");
                    if (message != null)
                    {
                        message.AllGraspsLetGoOfThisObject(true);
                        message.gravity = 0f;
                        var mch = message.firstChunk;
                        for (int i = 0; i < 2; i++)
                        {
                            mch.vel[i] = LerpAndTick(mch.vel[i], 0f, 0.1f, 0.05f);
                        }
                        if (!DistLess(mch.pos, roomCenter, 45f)) mch.vel += (roomCenter - mch.pos).normalized * 3.4f;
                        mch.vel = Vector2.ClampMagnitude(mch.vel, 21f);
                    }
                    //LogWarning("MARK5");
                    if (owner.conversation?.id != this.convoID)
                    {
                        //convo over
                        if (message != null)
                        {
                            message.gravity = 1f;
                            abs_message = null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError("ERROR in martyr SSAI subroutine update: " + e);
            }
        }
        internal int windup = 200;
        internal int searchForMessageCounter = 80;
        public override Vector2? LookPoint => (windup > 0) 
            ? owner.oracle.room.MiddleOfRoom() 
            : abs_message?.realizedObject?.firstChunk.pos ?? guest?.firstChunk.pos;
    }
}
