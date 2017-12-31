using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        ACPWrapper antennaComms;
        MissileManagement missileManagement;
        MissileCoordination missileCoordination;
        TurretMonitor turretMonitor;

        enum CurrentSystemMode { Command, Automatic}
        CurrentSystemMode currentMode = CurrentSystemMode.Command;

        IEnumerator<bool> initializer;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update10 | UpdateFrequency.Update100; 
            initializer = Initialize();
        }

        public void SubMain(string args, UpdateType uType)
        {
            //Initialize
            if (!initializer.MoveNext())
                initializer.Dispose();
            else
                return;

            //Parse regular commands
            if (!ParseCommands(args))
            {
                //Parse Messages
                long senderId;
                string[] messages = antennaComms.Main(args, out senderId);
                if (messages != null)
                {
                    ParseMessages(messages, senderId);
                }
            }


            if (currentMode == CurrentSystemMode.Automatic)
            {
                turretMonitor.SlowScan();
                missileCoordination.Main(uType);
            }
            
            if ((uType & UpdateType.Update10) != 0)
                EveryTenTick();
            if ((uType & UpdateType.Update100) != 0)
                EveryHundredTick();
        }

        void EveryTenTick()
        {

        }

        void EveryHundredTick()
        {
            
        }


        /*==========| Event callbacks |==========*/
        void OnTargetDetected(MyDetectedEntityInfo target)
        {
            Echo("Target detected!");
        }

        void OnMissileAdded(MissileManagement.MissileInfo info)
        {
            Echo($"Missile with ID {info.id} added.");
            Me.CustomData = missileManagement.TotalMissileCount.ToString();
        }

        void OnMissileRemoved(MissileManagement.MissileInfo info)
        {
            Echo($"Missile with ID {info.id} removed.");
            Me.CustomData = missileManagement.TotalMissileCount.ToString();
        }

        /*=========| Helper Functions |=========*/

        bool ParseCommands(string command)
        {
            switch (command)
            {
                case "RefreshMissiles":
                    missileManagement.RefreshMissileList();
                    return true;
                case "TargetMissile":
                    MissileManagement.MissileInfo missile = missileManagement.GetMissileCloseTo(Me.GetPosition(), MissileManagement.MissileType.SRInterceptor, false);
                    missileManagement.SendCommand(missile, "Target");
                    return true;

                case "FireMissile":
                    MissileManagement.MissileInfo fire = missileManagement.GetMissileCloseTo(Me.GetPosition(), MissileManagement.MissileType.SRInterceptor, true);
                    missileManagement.SendCommand(fire, "Attack");
                    return true;

                case "ModeAutomatic":
                    currentMode = CurrentSystemMode.Automatic;
                    return true;

                case "ModeManual":
                    currentMode = CurrentSystemMode.Command;
                    return true;
            }

            return false;
        }

        void ParseMessages(string[] messages, long senderId)
        {
            // Parse message header
            switch (messages[0])
            {
                case "missileEntry":
                    missileManagement.ParseMissileEntry(messages);
                    break;
            }
        }

        IEnumerator<bool> Initialize()
        {
            antennaComms = new ACPWrapper(this);
            yield return true;

            missileManagement = new MissileManagement(antennaComms);
            missileManagement.OnMissileAdded += OnMissileAdded;
            missileManagement.OnMissileRemoved += OnMissileRemoved;
            yield return true;

            missileCoordination = new MissileCoordination(missileManagement, Me, antennaComms);
            yield return true;

            turretMonitor = new TurretMonitor(this);
            turretMonitor.OnTargetDetected += OnTargetDetected;
            turretMonitor.OnTargetDetected += missileCoordination.OnTargetDetected;
            yield return true;

            Echo("Initialized!");
        }

        void Main(string argument, UpdateType uType)
        { //By inflex
            try
            {
                SubMain(argument, uType);
            }
            catch (Exception e)
            {
                var sb = new StringBuilder();

                sb.AppendLine("Exception Message:");
                sb.AppendLine($"   {e.Message}");
                sb.AppendLine();

                sb.AppendLine("Stack trace:");
                sb.AppendLine(e.StackTrace);
                sb.AppendLine();

                var exceptionDump = sb.ToString();

                Echo(exceptionDump);

                //Optionally rethrow
                throw;
            }
        }
    }
}