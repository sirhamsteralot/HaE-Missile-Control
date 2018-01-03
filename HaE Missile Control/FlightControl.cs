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
        public class FlightControl
        {
            public Action OnTargetSpeed;

            private double speedLimit = 100;

            private IMyShipController control;
            private List<IMyGyro> gyros;
            private List<IMyThrust> thrusters;

            bool accelerateInDirection = false;
            

            private Vector3D accelerateTarget;

            public FlightControl(IMyShipController control, List<IMyGyro> gyros, List<IMyThrust> thrusters)
            {
                this.control = control;
                this.gyros = gyros;
                this.thrusters = thrusters;
            }

            public void Main()
            {
                if (accelerateTarget == null || accelerateTarget == Vector3D.Zero || accelerateTarget == Vector3D.PositiveInfinity)
                    return;

                GyroUtils.PointInDirection(gyros, control, accelerateTarget, 1);
                ThrustUtils.SetThrustBasedDot(thrusters, accelerateTarget, 4);
                //ThrustUtils.SetMinimumThrust(thrusters, control.WorldMatrix.Forward, 0.25);

                if (accelerateInDirection && control.GetShipSpeed() >= 99.99)
                {
                    accelerateTarget = Vector3D.Zero;
                    BoostForward(0);
                    accelerateInDirection = false;
                }
            }

            public void AccelerateInDirection(Vector3D direction)
            {
                direction.Normalize();
                accelerateTarget = direction;

                if (direction != Vector3D.Zero)
                    accelerateInDirection = true;
                else
                    accelerateInDirection = false;
            }

            public void DirectControl(Vector3D direction)
            {
                direction.Normalize();
                accelerateTarget = direction;
            }

            public void BoostForward(float amount)
            {
                ThrustUtils.SetThrust(thrusters, control.WorldMatrix.Forward, amount);
            }

            public void Accelerate(Vector3D acceleration)
            {
                accelerateTarget = Vector3D.ClampToSphere(control.GetShipVelocities().LinearVelocity + acceleration, speedLimit);
            }

            private static Vector3D Project(Vector3D one, Vector3D two) //project a on b
            {
                Vector3D projection = one.Dot(two) / two.LengthSquared() * two;
                return projection;
            }

            private static Vector3D Reflect(Vector3D a, Vector3D b, double rejectionFactor = 1) //mirror a over b
            {
                Vector3D project_a = Project(a, b);
                Vector3D reject_a = a - project_a;
                Vector3D reflect_a = project_a - reject_a * rejectionFactor;
                return reflect_a;
            }
        }
    }
}
