﻿using System;
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
using WaspPile.Remnant.Martyr;
using SlugBase;

using static RWCustom.Custom;
using static WaspPile.Remnant.Satellite.RemnantUtils;
using static WaspPile.Remnant.Satellite.ConvoHelper;
//using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;
using static WaspPile.Remnant.Martyr.MartyrHooks;
using static WaspPile.Remnant.CommonHooks;

using MoonConvo = SLOracleBehaviorHasMark.MoonConversation;
using RocksConvo = SSOracleBehavior.PebblesConversation;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant.Satellite
{
    //add animation for extracting the pearl
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
        //internal Player guest => owner.player;
        internal bool convoStarted = false;
        internal readonly List<Player> searchedPlayers = new();

        internal Oracle pebbles => owner.oracle;

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

                if (searchForGuestCounter > 0)
                {
                    searchForGuestCounter--;
                    owner.player ??= (Player)croom.updateList.FirstOrDefault(uad => (uad is Player p) && !searchedPlayers.Contains(p));
                    owner.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                }
                //else if (pullOut > 0)
                //{
                //    pullOut--;
                //    if (pullOut % 10 == 0 && RemnantPlugin.DebugMode) LogWarning($"searching for message... {pullOut}");
                //    if (guest is not null)
                //    {
                //        pullOutHold ??= new Vector2(roomCenter.x + URand.Range(-22f, 22f), roomCenter.y - URand.Range(75, 85));
                //        //immobilise
                //        guest.Stun(5);
                //        //pull
                //        if (pullOut < 70)
                //        {
                //            var tarvel = Vector2.ClampMagnitude((Vector2)pullOutHold - guest.firstChunk.pos, 8f);
                //            guest.firstChunk.vel = Vector2.Lerp(guest.firstChunk.vel, tarvel, 0.09f);
                //        }
                //        //regurgitate
                //        if (pullOut < 10)
                //        {
                //            if (guest.objectInStomach is DataPearl.AbstractDataPearl abp && abp.IsEchoPearl())
                //            {
                //                guest.Regurgitate();
                //            }
                //            //foreach (var g in guest.grasps) if (g is not null && g.grabbed is DataPearl dp && dp.IsEchoPearl()) abs_message ??= dp.AbstractPearl;
                //        }
                //    }
                //    else pullOut = 0;
                //}
                else if (searchForMessageCounter > 0)
                {
                    searchForMessageCounter--;
                    //if (abs_message != null) goto regdone;
                    //#warning only works when object is in stomach? see and fix asap
                    //Warp issue
                    foreach (var po in croom.updateList) if (po is DataPearl dp && dp.IsEchoPearl()) abs_message ??= dp.AbstractPearl;
                    if (player is not null)
                    {
                        if (player.objectInStomach is DataPearl.AbstractDataPearl abp && abp.IsEchoPearl())
                        {
                            player.Regurgitate();
                        }
                        foreach (var g in player.grasps) if (g is not null && g.grabbed is DataPearl dp && dp.IsEchoPearl()) abs_message ??= dp.AbstractPearl;
                    }
                    owner.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
                    //regdone:
                    //LogWarning("MARK1");
                    //if (message != null) searchForMessageCounter = 0;
                }
                else
                {
                    //LogWarning("MARK2");
                    if (!convoStarted)
                    {
                        convoID = (message == null) ? Conversation.ID.Pebbles_Red_No_Neuron : Conversation.ID.Pebbles_Red_Green_Neuron;
                        owner.afterGiveMarkAction = this.action;
                        owner.InitateConversation(convoID, this);
                        owner.SetNewDestination(owner.oracle.room.MiddleOfRoom() + RNV() * 100f);
                        convo = owner.conversation;
                        convoStarted = true;
                    }
                    owner.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                    if (message != null)
                    {
                        message.AllGraspsLetGoOfThisObject(true);
                        message.gravity = 0f;
                        var mch = message.firstChunk;
                        for (int i = 0; i < 2; i++)
                        {
                            mch.vel[i] = LerpAndTick(mch.vel[i], 0f, 0.04f, 0.03f);
                        }
                        if (!DistLess(mch.pos, roomCenter, 15f)) mch.vel += (roomCenter - mch.pos).normalized * 3.4f;
                        mch.vel = Vector2.ClampMagnitude(mch.vel, 21f);
                    }
                    //LogWarning("MARK5");
                    if (owner.action is SSOracleBehavior.Action.General_GiveMark) return;
                    var cev = convo.events.FirstOrDefault();
                    if (convo.events.Count > 0)
                    {
                        //if (cev is Conversation.SpecialEvent spec && spec.eventName == "karma")
                        //{
                        //    convo.events.RemoveAt(0);
                        //    convo.paused = true;
                        //    //owner.
                        //}
                    }
                    else
                    {
                        //convo over
                        LogWarning("MARTYR MEETING OVER");
                        if (message != null)
                        {
                            message.gravity = 1f;
                            abs_message = null;
                        }
                        owner.NewAction(convoID == Conversation.ID.Pebbles_Red_Green_Neuron ? SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut : SSOracleBehavior.Action.ThrowOut_ThrowOut);
                    }
                }
                uerrc = 0;
                owner.restartConversationAfterCurrentDialoge = false;
                
            }
            catch (Exception e)
            {
                LogError("ERROR in martyr SSAI subroutine update: " + e);
                if (uerrc > 40)
                {
                    LogWarning("Martyr SSAI subroutine irrecoverable; aborting. Sorry!");
                    owner.NewAction(SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut);
                }
            }
        }
        public override void Deactivate()
        {
            base.Deactivate();
        }
        public override void Activate(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
        {
            base.Activate(oldAction, newAction);
            owner.restartConversationAfterCurrentDialoge = false;
        }

        internal int uerrc;
        internal byte windup = 15;
        internal byte searchForGuestCounter = 13;
        internal byte searchForMessageCounter = 6;
        internal byte pullOut = 88;
        internal RocksConvo convo;
        public override Vector2? LookPoint => (windup > 0) 
            ? owner.oracle.room.MiddleOfRoom() 
            : abs_message?.realizedObject?.firstChunk.pos ?? player?.firstChunk.pos;
        internal Vector2? pullOutHold;
    }
}
