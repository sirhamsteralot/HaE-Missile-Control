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
        public class MissileManagement
        {
            private HashSet<MissileInfo> missileList;

            public MissileManagement()
            {
                missileList = new HashSet<MissileInfo>();
            }

            public void AddMissileEntry(long id, Vector3D location, Vector3D pointingDirection, MissileType missileType)
            {
                var info = new MissileInfo(id, location, pointingDirection, missileType);

                missileList.Add(info);
            }

            public MissileInfo GetMissileEntry(long missileId, bool delete)
            {
                MissileInfo tempInfo = default(MissileInfo);

                foreach(var info in missileList)
                {
                    tempInfo = (info.id == missileId) ? info : default(MissileInfo);
                }

                if (delete && tempInfo != default(MissileInfo))
                    missileList.Remove(tempInfo);

                return tempInfo;
            }

            public struct MissileInfo
            {
                public MissileInfo (long id, Vector3D location, Vector3D direction, MissileType missileType)
                {
                    this.id = id;
                    this.location = location;
                    this.direction = direction;
                    this.missileType = missileType;
                }

                public override bool Equals(Object obj)
                {
                    return obj is MissileInfo && this == (MissileInfo)obj;
                }

                public override int GetHashCode()
                {
                    return id.GetHashCode();
                }

                public static bool operator ==(MissileInfo x, MissileInfo y)
                {
                    return x.id == y.id;
                }

                public static bool operator !=(MissileInfo x, MissileInfo y)
                {
                    return !(x == y);
                }

                public long id;
                public Vector3D location;
                public Vector3D direction;
                public MissileType missileType;
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
