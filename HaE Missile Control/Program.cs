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

        static Action<string> GlobalEcho;

        LongRangeDetection longRangeDetection;
        ACPWrapper antennaComms;
        FlightControl flightControl;
        TargetGuidance guidance;
        MissileManagementClient clientSystem;
        TurretMonitor turretMonitor;

        List<IMyCameraBlock> cameras;
        List<IMyGyro> gyros;
        List<IMyThrust> thrusters;
        IMyShipController rc;
        IMyCameraBlock mainCam;

        IEnumerator<bool> initializer;
        IEnumerator<bool> stateMachine;

        //Messages
        string[] messages;
        long senderId;

        bool targetGuidance = false;
        bool useTurretLockon = false;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update10 | UpdateFrequency.Update100;
            GlobalEcho = Echo;
            initializer = Initialize();
        }

        void SubMain(string args, UpdateType uType)
        {
            if (!initializer.MoveNext())
                initializer.Dispose();
            else
                return;

            //Parse messages
            if (messages != null)
            {
                ParseMessages(messages, senderId);
            }

            //Parse commands
            if (!ParseCommands(args))
            {
                //Get Messages
                messages = antennaComms.Main(args, out senderId);
            }



            if ((uType & UpdateType.Update1) != 0)
                EveryTick();
            if ((uType & UpdateType.Update10) != 0)
                EveryTenTick();
            if ((uType & UpdateType.Update100) != 0)
                EveryHundredTick();
        }

        void EveryTick()
        {
            if (turretMonitor != null && useTurretLockon && turretMonitor.Turretcount > 0)
            {
                turretMonitor.Scan();
            } else
            {
                longRangeDetection?.DoDetect();
            }
            flightControl?.Main();
        }

        void EveryTenTick()
        {

        }

        void EveryHundredTick()
        {

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
                case "TurretControll":
                    useTurretLockon = true;
                    targetGuidance = true;
                    break;
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
                case "Target":
                    NewLongRangeDetection();
                    break;
                case "TargetLoc":
                    Vector3D location;
                    if (Vector3D.TryParse(messages[1], out location))
                        NewLongRangeDetection(location);
                    break;
                case "AttackLoc":
                    Vector3D locationT;
                    if (Vector3D.TryParse(messages[1], out locationT))
                        NewLongRangeDetection(locationT);
                    targetGuidance = true;
                    break;
                case "Attack":
                    targetGuidance = true;
                    break;
                case "LaunchOut":
                    flightControl.BoostForward(100);
                    break;
                case "UseTurretLockon":
                    useTurretLockon = true;
                    break;
                case "LaunchInDirection":
                    Vector3D direction;
                    if (Vector3D.TryParse(messages[1], out direction))
                        flightControl.AccelerateInDirection(direction);
                    break;
                case "FullTurretGuidance":
                    useTurretLockon = true;
                    targetGuidance = true;
                    break;
            }
        }

        bool NewLongRangeDetection()
        {
            if (!mainCam.CanScan(MAXCASTDIST))
                return false;

            var target = mainCam.Raycast(MAXCASTDIST);
            if (target.IsEmpty())
                return false;

            NewLongRangeDetection(target.Position);
            return true;
        }

        void NewLongRangeDetection(Vector3D position)
        {
            longRangeDetection = new LongRangeDetection(position, cameras, rc.GetPosition());
            longRangeDetection.OnTargetFound += OnTargetDetected;
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
            yield return true;

            turretMonitor = new TurretMonitor(this);
            turretMonitor.OnTargetDetected += OnTargetDetected;
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
