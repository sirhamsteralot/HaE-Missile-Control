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
    partial class Program
    {
        public class ProximityFuse
        {
            public Action OnDetonation;
            public Action OnEnemyInRange;

            List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            List<IMyWarhead> warheads = new List<IMyWarhead>();

            IMyShipController rc;
            double detonationDist;

            public ProximityFuse(IMyShipController rc, double detonationDist, Program P)
            {
                this.rc = rc;
                this.detonationDist = detonationDist;

                P.GridTerminalSystem.GetBlocksOfType(sensors);
                P.GridTerminalSystem.GetBlocksOfType(warheads);
            }

            public void DetectSensor()
            {
                bool enemyDetected = false;

                foreach (IMySensorBlock sensor in sensors)
                {
                    if (IsEnemyInRange(sensor.LastDetectedEntity))
                        enemyDetected = true;
                }

                if (enemyDetected)
                    OnEnemyInRange?.Invoke();
            }

            public void Detonate()
            {
                OnDetonation?.Invoke();

                foreach (IMyWarhead warhead in warheads)
                {
                    warhead.IsArmed = true;
                    warhead.Detonate();
                }
            }

            public void OnTargetDetected(MyDetectedEntityInfo target, int ticksFromLastFind)
            {
                if (IsEnemyInRange(target))
                    OnEnemyInRange?.Invoke();
            }

            private bool IsEnemyInRange(MyDetectedEntityInfo info)
            {
                if (info.IsEmpty())
                    return false;

                if (info.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies && info.Relationship != MyRelationsBetweenPlayerAndBlock.Neutral)
                    return false;

                return Vector3D.DistanceSquared(info.Position, rc.GetPosition()) < detonationDist;
            }
        }
    }
}
