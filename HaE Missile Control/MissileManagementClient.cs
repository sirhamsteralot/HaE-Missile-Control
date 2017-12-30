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
        public class MissileManagementClient
        {
            private ACPWrapper antennaWrapper;
            private IMyShipController rc;

            private long id;
            private Vector3D Location { get { return rc.GetPosition(); } }
            private Vector3D Direction { get { return rc.WorldMatrix.Forward; } }
            private MissileType missileType;

            public MissileManagementClient(ACPWrapper antennaWrapper, IMyShipController rc, long id, MissileType missileType)
            {
                this.antennaWrapper = antennaWrapper;
                this.rc = rc;
                this.id = id;
                this.missileType = missileType;
            }

            public void ReturnMissileInfo(long returnAddr)
            {
                string[] missileInfo = {
                    "missileEntry",
                    id.ToString(),
                    Location.ToString(),
                    Direction.ToString(),
                    ((int)missileType).ToString()
                };

                antennaWrapper.PrepareMSG(missileInfo, returnAddr);
            }

            [Flags]
            public enum MissileType
            {
                ICBM,
                SRInterceptor,
                MRInterceptor,
                LRInterceptor,
            }
        }
    }
}
