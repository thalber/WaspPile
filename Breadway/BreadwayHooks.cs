using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MonoMod.Cil;
using System.IO;
using System.Text.RegularExpressions;
//using System.Collections.Concurrent;

namespace WaspPile.Breadway
{
    internal static class BreadwayHooks
    {
        internal static void Register()
        {   
            On.WorldLoader.LoadAbstractRoom += WL_LAbsRoomHk;
            ThreadPool.GetMaxThreads(out int oldwt, out int oldSecThr);
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, oldSecThr);
            Console.WriteLine($"Thread limit: {Environment.ProcessorCount}");
        }

        private static void WL_LAbsRoomHk(On.WorldLoader.orig_LoadAbstractRoom orig, World world, string roomName, AbstractRoom room, RainWorldGame.SetupValues setupValues)
        {
            var tarFile = WorldLoader.FindRoomFileDirectory(roomName, false) + ".txt";
            var levelLines = File.ReadAllLines(tarFile);
            bool NeedBake = RoomPreprocessor.VersionFix(ref levelLines) || (int.Parse(levelLines[9].Split(new char[] { '|' })[0]) < world.preProcessingGeneration);
            var origDB = setupValues.dontBake;
            setupValues.dontBake = true;
            orig(world, roomName, room, setupValues);
            //setupValues.dontBake = true;
            if (NeedBake) ThreadPool.QueueUserWorkItem((z) => { QueueRoomBake(room, levelLines, world, setupValues, world.preProcessingGeneration, tarFile); });
        }

        //private static void ConstructAIDP(On.AIdataPreprocessor.orig_ctor orig, object self, AImap aiMap, bool falseBake)
        //{
        //    //throw new NotImplementedException();
        //    var instance = self as AIdataPreprocessor;
        //    orig(self, aiMap, falseBake);
        //    var allSR = new List<AIdataPreprocessor.SubRoutine>();

        //}



        internal static void Undo()
        {
            On.WorldLoader.LoadAbstractRoom -= WL_LAbsRoomHk;
        }

        internal static void QueueRoomBake(AbstractRoom rm, string[] leveltext, World world, RainWorldGame.SetupValues sval, int ppg, string tarFile)
        {
            try
            {
                lock (RoomLocks)
                {
                    if (RoomLocks.Contains(rm.name)) return;
                    Console.WriteLine($"{DateTime.Now} : Queued baking of room {rm.name}");
                    Console.WriteLine($"Current thread: {Thread.CurrentThread.ManagedThreadId}");
                    RoomLocks.Add(rm.name);
                }
                sval.dontBake = false;
                var res = RoomPreprocessor.PreprocessRoom(rm, leveltext, world, sval, ppg);
                File.WriteAllLines(tarFile, res);
                lock (RoomLocks) RoomLocks.Remove(rm.name);
                Console.WriteLine($"{DateTime.Now} : Baking of {rm.name} finished, result saved:\n{tarFile}");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while baking " + rm.name);
                Console.WriteLine(e);
            }
            
        }

        internal static HashSet<string> RoomLocks = new HashSet<string>();
        //internal static Queue<Exception> _encEx = new Queue<Exception>();
    }
}
