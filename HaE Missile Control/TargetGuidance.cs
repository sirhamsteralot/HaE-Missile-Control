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
            private const float N = 3.5f;
            private const float NT = 0.5f;

            private const float MAXN = 20f;
            private const float MINN = -20f;

            int _ticksFromLastFind = 1;
            int TicksFromLastFind { get { return (_ticksFromLastFind >= 1) ? _ticksFromLastFind : 1; } }

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

                return HPN();
                //return APN();
                //return PPN();
                //return ExperimentalGuidance();
            }

            private Vector3D PPN()
            {
                Vector3D accelerationNormal = PGAIN * RelativeVelocityVec.Cross(CalculateRotVec());      //PPN term

                return accelerationNormal;
            }

            private Vector3D ExperimentalGuidance()
            {
                Vector3D nRelativeVelocityVec = RelativeVelocityVec;
                double mRelativeVelocity = nRelativeVelocityVec.Normalize();

                Vector3D accelerationNormal;
                accelerationNormal = PGAIN * LosDelta;                                          //HPN term
                accelerationNormal += NewLos;                                                   //LosBias term

                return accelerationNormal;
            }

            private Vector3D HPN()
            {
                double lambda = LOSRate;
                double gamma = (PGAIN * LOSRate);
                float cos = MyMath.FastCos((float)gamma - (float)lambda);
                double IPNGain = (RelativeVelocityVec.Length() * PGAIN) / (MissileVelocityVec.Length() * cos);

                IPNGain = MathHelperD.Clamp(IPNGain, MINN, MAXN);
                IPNGain = (IPNGain != double.NaN) ? IPNGain : PGAIN;

                DebugEcho($"IPNGain: {IPNGain:#.###}");

                Vector3D accelerationNormal;
                accelerationNormal = IPNGain * RelativeVelocityVec.Cross(CalculateRotVec());        //PPN term
                accelerationNormal += IPNGain * TargetAccel / 2;                                    //APN term
                accelerationNormal += IPNGain * LosDelta;                                           //HPN term
                accelerationNormal += -rc.GetNaturalGravity();                                      //Gravity term
                accelerationNormal += NewLos;                                                       //LosBias term

                return accelerationNormal;
            }

            private Vector3D APN()
            {
                Vector3D nRelativeVelocityVec = RelativeVelocityVec;
                double mRelativeVelocity = nRelativeVelocityVec.Normalize();

                Vector3D accelerationNormal;
                accelerationNormal = PGAIN * RelativeVelocityVec.Cross(CalculateRotVec());      //PPN term
                accelerationNormal += PGAIN * TargetAccel / 2;                                  //APN term
                accelerationNormal += NewLos;                                                   //LosBias term

                return accelerationNormal;
            }

            private Vector3D CalculateRotVec()
            {
                Vector3D RxV = Vector3D.Cross(RangeVec, RelativeVelocityVec);
                //Vector3D RdR = RangeVec * RangeVec;
                double RdR = RangeVec.LengthSquared();

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
                if (one == Vector3D.Zero)
                    return two;

                return one - Project(one, two);
            }
        }
    }
}
