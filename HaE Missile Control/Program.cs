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

        const float DETONATIONDIST = 15f;

        const bool DEBUGMODE = true;
        const bool SPECIFICDEBUGMODE = true;
        const bool PROFILING = false;       //Will only work if debugmode is enabled
        const MissileManagementClient.MissileType missileType = MissileManagementClient.MissileType.SRInterceptor;

        const double MAXCASTDIST = 10000;

        private static Queue<string> _messages;

        LongRangeDetection longRangeDetection;
        ACPWrapper antennaComms;
        FlightControl flightControl;
        TargetGuidance guidance;
        MissileManagementClient clientSystem;
        TurretMonitor turretMonitor;
        ProximityFuse proximityFuse;
        IngameTime ingameTime;
        EntityTracking_Module entityTracking;
        Autopilot_Module autopilot;

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
            if (DEBUGMODE)
                _messages = new Queue<string>();

            AddToLog("", true);
            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update10 | UpdateFrequency.Update100;
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
            TargetingSystem();
            flightControl?.Main();
        }

        void EveryTenTick()
        {
            proximityFuse?.DetectSensor();
        }

        void EveryHundredTick()
        {

        }

        void TargetingSystem()
        {
            MyDetectedEntityInfo? turretTarget = null;
            double turretDist = 0;

            if (turretMonitor != null && useTurretLockon && turretMonitor.Turretcount > 0)
            {
                turretTarget = turretMonitor.Scan();
                if (turretTarget.HasValue)
                    turretDist = Vector3D.DistanceSquared(rc.GetPosition(), turretTarget.Value.Position);
            }

            if (useTurretLockon && turretDist > 750*750)
            {
                if (longRangeDetection == null && turretTarget.HasValue)
                {
                    NewLongRangeDetection(turretTarget.Value.Position);
                }
            }

            if (turretTarget == null || turretDist > 750 * 750)
                longRangeDetection?.DoDetect();
        }

        /*==========| Event callbacks |==========*/
        void OnTargetDetected(MyDetectedEntityInfo target, int ticksFromLastFind)
        {
            DebugEcho($"Target detected\n@{target.Position}");
            if (targetGuidance)
            {
                var desiredAccel = guidance.CalculateAccel(target, ticksFromLastFind);

                DebugEcho($"desiredAccel:\n{desiredAccel}");
                flightControl.DirectControl(desiredAccel);
            }

            proximityFuse?.OnTargetDetected(target, ticksFromLastFind);
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

            if (command.StartsWith("TargetLoc"))
            {
                string[] split = command.Split('|');

                Vector3D location;
                if (Vector3D.TryParse(split[1], out location))
                    NewLongRangeDetection(location);

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
            GridTerminalSystem.GetBlocksOfType(cameras, x => x.CubeGrid == Me.CubeGrid);
            foreach (var cam in cameras)
                cam.EnableRaycast = true;
            yield return true;

            gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType(gyros, x => x.CubeGrid == Me.CubeGrid);
            yield return true;

            antennaComms = new ACPWrapper(this, x => x.CubeGrid == Me.CubeGrid);
            yield return true;

            thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thrusters, x => x.CubeGrid == Me.CubeGrid);
            yield return true;

            rc = GetBlockWithNameOnGrid(RCNAME) as IMyRemoteControl;
            yield return true;

            mainCam = GetBlockWithNameOnGrid(MAINCAM) as IMyCameraBlock;
            yield return true;

            var GTSUtils = new GridTerminalSystemUtils(Me, GridTerminalSystem);
            ingameTime = new IngameTime();
            yield return true;

            entityTracking = new EntityTracking_Module(GTSUtils, rc, mainCam);
            yield return true;

            var pid = new PID_Controller.PIDSettings
            {
                PGain = 1
            };
            autopilot = new Autopilot_Module(GTSUtils, rc, ingameTime, pid, pid, entityTracking);
            yield return true;

            flightControl = new FlightControl(rc, gyros, thrusters, autopilot);
            yield return true;

            guidance = new TargetGuidance(rc);
            yield return true;

            clientSystem = new MissileManagementClient(antennaComms, rc, Me.EntityId, missileType);
            yield return true;

            turretMonitor = new TurretMonitor(this);
            turretMonitor.OnTargetDetected += OnTargetDetected;
            yield return true;

            proximityFuse = new ProximityFuse(rc, DETONATIONDIST, this);
            proximityFuse.OnEnemyInRange += proximityFuse.Detonate;

            DebugEcho("Initialized!");
        }

        public IMyTerminalBlock GetBlockWithNameOnGrid(string name)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(name, blocks, x => x.CubeGrid == Me.CubeGrid);

            return (blocks[0]);
        }

        void EchoDebugQueue()
        {
            if (!DEBUGMODE)
                return;

                while (_messages.Count > 0)
                Echo(_messages.Dequeue());
        }

        public static void DebugEcho(string s)
        {
            if (DEBUGMODE)
                _messages.Enqueue(s);
        }

        public static void SpecificDebugEcho(string s)
        {
            if (SPECIFICDEBUGMODE && DEBUGMODE)
                _messages.Enqueue(s);
        }

        public void AddToLog(string s, bool clear = false)
        {
            if (clear)
                Me.CustomData = "";

            Me.CustomData += s;
        }

        void Main(string argument, UpdateType uType)
        { //By inflex
            if (DEBUGMODE || SPECIFICDEBUGMODE)
            {
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

                    DebugEcho(exceptionDump);

                    //Optionally rethrow
                    //throw;
                }
                if (PROFILING)
                {
                    DebugEcho($"Performance info:\nPrev Runtime: {Runtime.LastRunTimeMs}\n Current Instruction Count: {Runtime.CurrentInstructionCount}");
                    AddToLog($"{Runtime.LastRunTimeMs};");
                }
                EchoDebugQueue();
            } else
            {
                SubMain(argument, uType);
            }
        }
    }
}
