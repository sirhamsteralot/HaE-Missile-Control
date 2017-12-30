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
            public Action<MissileInfo> OnMissileAdded;

            private HashSet<MissileInfo> missileList;
            private ACPWrapper antennaProtocol;


            /*=============| Initializer |=============*/
            public MissileManagement (ACPWrapper antennaProtocol)
            {
                missileList = new HashSet<MissileInfo>();
                this.antennaProtocol = antennaProtocol;
            }

            /*==========| General Commands |==========*/
            public void RefreshMissileList()
            {
                missileList.Clear();

                antennaProtocol.PrepareMSG("MissilePing", 0);
            }

            public bool ParseMissileEntry(string[] missileEntry)
            {
                if (missileEntry.Length == 5)
                {
                    long Id;
                    if (!Int64.TryParse(missileEntry[1], out Id))
                        return false;

                    Vector3D location;
                    if (!Vector3D.TryParse(missileEntry[2], out location))
                        return false;

                    Vector3D direction;
                    if (!Vector3D.TryParse(missileEntry[3], out direction))
                        return false;

                    
                    int backingNumber;
                    if (!Int32.TryParse(missileEntry[4], out backingNumber))
                        return false;
                    MissileType type = (MissileType)backingNumber;

                    if (AddMissileEntry(Id, location, direction, type))
                        return true;

                    return false;
                }
                else
                {
                    return false;
                }
            }

            private bool AddMissileEntry(long id, Vector3D location, Vector3D pointingDirection, MissileType missileType)
            {
                var info = new MissileInfo(id, location, pointingDirection, missileType);

                if (missileList.Add(info))
                {
                    OnMissileAdded?.Invoke(info);
                    return true;
                }
                    
                return false;
            }
            /*==========| Command  Manager |==========*/

            public bool SendCommand(MissileInfo missile, string[] command)
            {
                return antennaProtocol.PrepareMSG(command, missile.id);
            }

            public bool SendCommand(MissileInfo missile, string command)
            {
                return antennaProtocol.PrepareMSG(command, missile.id);
            }

            /*===============| Getters |==============*/
            public MissileInfo GetMissileCloseTo(Vector3D location, MissileType type, bool delete)
            {
                MissileInfo tempInfo = default(MissileInfo);

                foreach (var info in missileList)
                {
                    tempInfo = (Vector3D.DistanceSquared(info.location, location) < Vector3D.DistanceSquared(tempInfo.location, location)) && (type & info.missileType)!=0 ? info : tempInfo;
                }

                if (delete && tempInfo != default(MissileInfo))
                    missileList.Remove(tempInfo);

                return tempInfo;
            }

            public MissileInfo GetMissileCloseToAndInDirection(Vector3D location, Vector3D direction, MissileType type, double range, double dotRange, bool delete)
            {
                MissileInfo tempInfo = default(MissileInfo);

                foreach (var info in missileList)
                {
                    if (((info.direction.Dot(direction) < dotRange) && Vector3D.DistanceSquared(location, info.location) < (range * range)) && (type & info.missileType) != 0)
                    {
                        tempInfo = info;
                    }
                }

                if (delete && tempInfo != default(MissileInfo))
                    missileList.Remove(tempInfo);

                return tempInfo;
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

            /*============| Other  Types |============*/
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
