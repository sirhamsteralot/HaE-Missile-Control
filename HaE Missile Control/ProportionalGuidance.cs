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
        public class ProportionalGuidance
        {
            private const float N = 11.21f;
            private const float NT = 0.1635f;

            private int lastTargetTime = 1;
            private float PGAIN { get { return N * lastTargetTime; } }
            private float TargetAccel { get { return (float)SpeedDelta.Length() / lastTargetTime; } }

            private MyDetectedEntityInfo targetInfo;
            private IMyShipController rc;


            private Vector3D MyVelocityVec { get { return rc.GetShipVelocities().LinearVelocity; } }
            private Vector3D MyPositionVec { get { return rc.GetPosition(); } }

            private Vector3D TargetVelocityVec { get { return targetInfo.Velocity; } }
            private Vector3D TargetPositionVec { get { return targetInfo.Position; } }
            private Vector3D RangeVec { get { return targetInfo.Position - MyPositionVec; } }
            private Vector3D RelativeVelocityVec { get { return TargetVelocityVec - MyVelocityVec; } }

            private Vector3D OldLos;
            private Vector3D NewLos;
            private Vector3D LosDelta { get { return NewLos - OldLos; } }
            private double LOSRate { get { return LosDelta.Length(); } }

            private Vector3D OldTargetSpeed;
            private Vector3D NewTargetSpeed;
            private Vector3D SpeedDelta { get { return NewTargetSpeed - OldTargetSpeed; } }




            public ProportionalGuidance(IMyShipController rc)
            {
                this.rc = rc;
            }

            public Vector3D CalculateAPNAccel(MyDetectedEntityInfo info, int ticksFromLastFind)
            {
                UpdateTargetInfo(info, ticksFromLastFind);

                Vector3D omega = CalculateRotVec();
                Vector3D nRange = Vector3D.Normalize(RangeVec);
                Vector3D nVelocity = Vector3D.Normalize(RelativeVelocityVec);
                Vector3D nMissileVelocity = Vector3D.Normalize(MyVelocityVec);
                double mRelativeVelocity = RelativeVelocityVec.Length();


                //Vector3D accelerationNormal = Vector3D.Cross(PGAIN * RelativeVelocityVec, omega);
                //Vector3D accelerationNormal = -Vector3D.Cross(PGAIN * mRelativeVelocity * nRange, omega);
                //Vector3D accelerationNormal = -Vector3D.Cross(PGAIN * mRelativeVelocity * nMissileVelocity, omega);


                //MODDB GAMEDEV THINGY

                //Vector3D accelerationNormal = NewLos * PGAIN * RelativeVelocityVec.Length() * LOSRate;
                Vector3D accelerationNormal = NewLos * PGAIN * RelativeVelocityVec.Length() * LOSRate + LosDelta * TargetAccel * (0.5 * PGAIN);

                return Reject(accelerationNormal, RelativeVelocityVec);
                //return Vector3D.Reject(accelerationNormal, nRange);
                //return accelerationNormal;
            }

            private Vector3D CalculateRotVec()
            {
                Vector3D RxV = Vector3D.Cross(RangeVec, RelativeVelocityVec);
                Vector3D RdR = RangeVec * RangeVec;

                return RxV / RdR;
            }

            private void UpdateTargetInfo(MyDetectedEntityInfo info, int ticksFromLastFind)
            {
                targetInfo = info;
                lastTargetTime = ticksFromLastFind;

                OldLos = NewLos;
                NewLos = Vector3D.Normalize(RangeVec);

                OldTargetSpeed = NewTargetSpeed;
                NewTargetSpeed = info.Velocity;
            }

            private static Vector3D Project(Vector3D one, Vector3D two) //project a on b
            {
                Vector3D projection = one.Dot(two) / two.LengthSquared() * two;
                return projection;
            }

            private static Vector3D Reject(Vector3D one, Vector3D two)
            {
                if (two == Vector3D.Zero)
                    return one;

                return one - Project(one, two);
            }
        }
    }
}
