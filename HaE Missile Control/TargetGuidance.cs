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
        public class TargetGuidance
        {
            private const float N = 3f;
            private const float NT = 1f;

            int _ticksFromLastFind = 1;
            int TicksFromLastFind { get { return (_ticksFromLastFind >= 1) ? TicksFromLastFind : 1; } }

            private float PGAIN { get { return N; } }
            private float TargetAccel { get {
                                                float RelativeSpeedF = (float)Math.Abs(RelativeSpeedDelta.Length());
                                                return (RelativeSpeedF > N) ? RelativeSpeedF / TicksFromLastFind * NT : 0;
                                            } }

            private MyDetectedEntityInfo targetInfo;
            private IMyShipController rc;


            private Vector3D MissileVelocityVec { get { return rc.GetShipVelocities().LinearVelocity; } }
            private Vector3D OldMyVelocityVec;
            private Vector3D NewMyVelocityVec;
            private Vector3D MyPositionVec { get { return rc.GetPosition(); } }

            private Vector3D TargetVelocityVec { get { return targetInfo.Velocity; } }
            private Vector3D TargetPositionVec { get { return targetInfo.Position; } }
            private Vector3D RangeVec { get { return targetInfo.Position - MyPositionVec; } }
            private Vector3D RelativeVelocityVec { get { return TargetVelocityVec - MissileVelocityVec; } }

            private Vector3D OldLos;
            private Vector3D NewLos;
            private Vector3D LosDelta { get { return NewLos - OldLos; } }
            private double LOSRate { get { return Math.Atan(LosDelta.Length() / OldLos.Length()) * (Math.PI / 180); } }

            private Vector3D OldTargetSpeed;
            private Vector3D NewTargetSpeed;
            private Vector3D RelativeSpeedDelta { get { return (NewTargetSpeed - OldTargetSpeed) - (NewMyVelocityVec - OldMyVelocityVec); } }




            public TargetGuidance(IMyShipController rc)
            {
                this.rc = rc;
            }

            public Vector3D CalculateAccel(MyDetectedEntityInfo info, int ticksFromLastFind)
            {
                UpdateTargetInfo(info, ticksFromLastFind);

                return APN();
            }

            private Vector3D PPN()
            {
                double mRelativeVelocity = RelativeVelocityVec.Length();

                Vector3D accelerationNormal;
                accelerationNormal = PGAIN * RelativeVelocityVec.Cross(CalculateRotVec());      //PPN term
                accelerationNormal += NewLos;                                                   //LosBias term
                accelerationNormal -= rc.GetNaturalGravity();                                   //Gravity term

                return accelerationNormal;
            }

            private Vector3D APN()
            {
                double mRelativeVelocity = RelativeVelocityVec.Length();
                Vector3D nRelativeVelocityVec = Vector3D.Normalize(RelativeVelocityVec);

                Vector3D accelerationNormal;
                accelerationNormal = PGAIN * RelativeVelocityVec.Cross(CalculateRotVec());      //PPN term
                accelerationNormal += PGAIN * TargetAccel / 2;                                  //APN term
                accelerationNormal += NewLos;                                                   //LosBias term
                accelerationNormal -= rc.GetNaturalGravity();                                   //Gravity term

                return accelerationNormal;
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
                this._ticksFromLastFind = ticksFromLastFind;

                OldLos = NewLos;
                NewLos = Vector3D.Normalize(RangeVec);

                OldTargetSpeed = NewTargetSpeed;
                NewTargetSpeed = info.Velocity;

                OldMyVelocityVec = NewMyVelocityVec;
                NewMyVelocityVec = MissileVelocityVec;
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
