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
        public const string guidanceComputerTag = "[GuidanceComputer]";
        public const string attachmentPointTag = "[Attachment]";

        public List<DeployPair> missileDeployPairs = new List<DeployPair>();
        
        public struct DeployPair
        {
            public IMyProgrammableBlock guidanceComputer;
            public Detachable attachmentPoint;

            public struct Detachable
            {
                public IMyMotorStator attachmentRotor;
                public IMyShipMergeBlock attachmentMergeBlock;

                public void Detach()
                {
                    attachmentRotor?.Detach();
                    if (attachmentMergeBlock != null)
                        attachmentMergeBlock.Enabled = false;
                }
            }
        }
        

        public Program()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument.StartsWith("DeploySingleMissile"))
            {
                    FetchBlocks();
                    DeployMissile();
            }
            else if (argument.StartsWith("DeployAtTarget"))
            {
                string[] split = argument.Split('|');

                Vector3D targetLocation;

                if (Vector3D.TryParse(split[1], out targetLocation))
                {
                    FetchBlocks();
                    DeployMissile(targetLocation);
                }
            }
            else if (argument.StartsWith("DeployMultipleAtTarget"))
            {
                string[] split = argument.Split('|');

                Vector3D targetLocation;
                int count = 0;

                if (Vector3D.TryParse(split[2], out targetLocation) && int.TryParse(split[1], out count))
                {
                    FetchBlocks();

                    for (int i = 0; i < count; i++)
                        DeployMissile(targetLocation);
                }
            }
            else if (argument.StartsWith("DeployMultiple"))
            {
                string[] split = argument.Split('|');

                int count = 0;

                if (int.TryParse(split[1], out count))
                {
                    FetchBlocks();

                    for (int i = 0; i < count; i++)
                        DeployMissile();
                }
            }


        }

        List<IMyProgrammableBlock> programmableBlocks = new List<IMyProgrammableBlock>();
        List<IMyMotorStator> attachmentRotors = new List<IMyMotorStator>();
        List<IMyShipMergeBlock> attachmentMergeBlocks = new List<IMyShipMergeBlock>();
        public void FetchBlocks()
        {
            missileDeployPairs.Clear();
            attachmentRotors.Clear();
            

            GridTerminalSystem.GetBlocksOfType(attachmentRotors, x => x.CustomName.Contains(attachmentPointTag) && x.TopGrid != null);

            foreach (var attachmentPoint in attachmentRotors)
            {
                var rotorTopGrid = attachmentPoint.TopGrid;

                programmableBlocks.Clear();
                GridTerminalSystem.GetBlocksOfType(programmableBlocks, x => x.CubeGrid == rotorTopGrid && x.CustomName.Contains(guidanceComputerTag));
                if (programmableBlocks.Count > 0)
                {
                    DeployPair deployPair = new DeployPair();
                    deployPair.guidanceComputer = programmableBlocks[0];

                    GridTerminalSystem.GetBlocksOfType(attachmentMergeBlocks, x => x.CustomName.Contains(attachmentPointTag) && x.CubeGrid == attachmentPoint.TopGrid);
                    if (attachmentMergeBlocks.Count > 0)
                        deployPair.attachmentPoint.attachmentMergeBlock = attachmentMergeBlocks[0];
                    else
                        deployPair.attachmentPoint.attachmentRotor = attachmentPoint;

                    missileDeployPairs.Add(deployPair);
                }
                
            }

            
        }

        public void DeployMissile()
        {
            int deployPairCount = missileDeployPairs.Count;

            if (deployPairCount > 0)
            {
                DeployPair pair = missileDeployPairs[deployPairCount - 1];

                if (pair.guidanceComputer.TryRun("TurretControll"))
                {
                    pair.attachmentPoint.Detach();

                    missileDeployPairs.RemoveAt(deployPairCount - 1);
                }
            }
        }

        public void DeployMissile(Vector3D targetPosition)
        {
            int deployPairCount = missileDeployPairs.Count;

            if (deployPairCount > 0)
            {
                DeployPair pair = missileDeployPairs[deployPairCount - 1];

                if (pair.guidanceComputer.TryRun($"TargetLoc|{targetPosition}"))
                {
                    pair.attachmentPoint.Detach();

                    missileDeployPairs.RemoveAt(deployPairCount - 1);
                }
            }
        }
    }
}