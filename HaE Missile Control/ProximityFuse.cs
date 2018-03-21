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

            List<IMySensorBlock> sensors;
            List<IMyWarhead> warheads;

            IMyShipController rc;
            double detonationDist;

            public ProximityFuse(IMyShipController rc, double detonationDist)
            {
                this.rc = rc;
                this.detonationDist = detonationDist;
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

            private bool IsEnemyInRange(MyDetectedEntityInfo info)
            {
                if (info.Relationship != MyRelationsBetweenPlayerAndBlock.Enemies && info.Relationship != MyRelationsBetweenPlayerAndBlock.Neutral)
                    return false;

                return Vector3D.DistanceSquared(info.Position, rc.GetPosition()) < detonationDist;
            }
        }
    }
}
