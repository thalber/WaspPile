using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedPlacedObjects;

namespace WaspPile.EDGESILK
{
    public class CRES_SIMPLESHELTERWARP : UpdatableAndDeletable //, ShelterBehaviors.IReactToShelterEvents
    {
        public CRES_SIMPLESHELTERWARP(Room instance, PlacedObject pobj)
        {
            po = pobj;
        }
        PlacedObject po;
        PlacedObjectsManager.ManagedData mdata => po.data as PlacedObjectsManager.ManagedData;
        public string dest { get { try { return mdata?.GetValue<string>("WARP_DESTINATION"); } catch { return null; } } }
        //public void ShelterEvent(float newFactor, float closeSpeed)
        //{
        //    try
        //    {
        //        if (newFactor > 0 && closeSpeed > 0) EDGESILK_HOOKS.QUEUED_WARP = mdata.GetValue<string>("WARP_DESTINATION");
        //    }
        //    catch (Exception e)
        //    {
        //        EDGESILK_PLUGIN.clog.Log(BepInEx.Logging.LogLevel.Debug, $"Couldn't get destination data: {e}");
        //        EDGESILK_HOOKS.QUEUED_WARP = null;
        //    }
            
        //}
    }
}
