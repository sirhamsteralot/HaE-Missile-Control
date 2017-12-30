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
        const string RCNAME = "RC";
        const string MAINCAM = "TargetingCamera";
        const MissileManagementClient.MissileType missileType = MissileManagementClient.MissileType.SRInterceptor;

        const double MAXCASTDIST = 10000;

        LongRangeDetection longRangeDetection;
        ACPWrapper antennaComms;
        FlightControl flightControl;
        TargetGuidance guidance;
        MissileManagementClient clientSystem;

        List<IMyCameraBlock> cameras;
        List<IMyGyro> gyros;
        List<IMyThrust> thrusters;
        IMyShipController rc;
        IMyCameraBlock mainCam;

        IEnumerator<bool> initializer;
        IEnumerator<bool> stateMachine;


        bool goToOrig = false;
        bool targetGuidance = false;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            initializer = Initialize();
        }

        void SubMain(string args, UpdateType uType)
        {
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

            EveryTick();
        }

        bool ParseCommands(string command)
        {
            switch (command)
            {
                case "Target":
                    NewLongRangeDetection();
                    return true;
                case "Attack":
                    targetGuidance = true;
                    return true;
            }

            return false;
        }

        void ParseMessages(string[] messages, long senderId)
        {
            // Parse message header
            switch (messages[0])
            {
                case "MissilePing":
                    clientSystem.ReturnMissileInfo(senderId);
                    break;
            }
        }

        void EveryTick()
        {
            longRangeDetection?.DoDetect();
            flightControl?.Main();
        }

        /*==========| Event callbacks |==========*/
        void OnTargetDetected(MyDetectedEntityInfo target, int ticksFromLastFind)
        {
            Echo($"Target detected\n@{target.Position}");

            if (targetGuidance)
            {
                var desiredAccel = guidance.CalculateAccel(target, ticksFromLastFind);

                Echo($"desiredAccel:\n{desiredAccel}");
                flightControl.DirectControl(desiredAccel);
            }
        }

        /*=========| Helper Functions |=========*/
        bool NewLongRangeDetection()
        {
            if (!mainCam.CanScan(MAXCASTDIST))
                return false;

            var target = mainCam.Raycast(MAXCASTDIST);
            if (target.IsEmpty())
                return false;

            longRangeDetection = new LongRangeDetection(target.Position, cameras, rc.GetPosition());
            longRangeDetection.OnTargetFound += OnTargetDetected;
            return true;
        }

        IEnumerator<bool> Initialize()
        {
            cameras = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType(cameras);
            foreach (var cam in cameras)
                cam.EnableRaycast = true;
            yield return true;

            gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(gyros);
            yield return true;

            antennaComms = new ACPWrapper(this);
            yield return true;

            thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thrusters);
            yield return true;

            rc = GridTerminalSystem.GetBlockWithName(RCNAME) as IMyRemoteControl;
            yield return true;

            mainCam = GridTerminalSystem.GetBlockWithName(MAINCAM) as IMyCameraBlock;
            yield return true;

            flightControl = new FlightControl(rc, gyros, thrusters);
            yield return true;

            guidance = new TargetGuidance(rc);
            yield return true;

            clientSystem = new MissileManagementClient(antennaComms, rc, Me.EntityId, missileType);

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
