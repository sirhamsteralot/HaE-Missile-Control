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
            private double negligible = 1;

            private IMyShipController control;
            private List<IMyGyro> gyros;
            private List<IMyThrust> thrusters;

            private bool accelerateInDirection = false;

            private PID_Controller PID_Controller = new PID_Controller(0.6, 3, 8);
            private Autopilot_Module autopilotModule;

            private Vector3D accelerateTarget;

            public FlightControl(IMyShipController control, List<IMyGyro> gyros, List<IMyThrust> thrusters, Autopilot_Module autopilotModule)
            {
                this.autopilotModule = autopilotModule;
                this.control = control;
                this.gyros = gyros;
                this.thrusters = thrusters;
            }

            public void Main()
            {
                if (accelerateTarget == null || accelerateTarget == Vector3D.Zero || accelerateTarget == Vector3D.PositiveInfinity || PID_Controller == null || !accelerateTarget.IsValid())
                    return;

                Vector3D accelerateTargetNormalized = accelerateTarget;
                double accelerateTargetLength = accelerateTargetNormalized.Normalize();
                double error = Vector3D.Dot(control.WorldMatrix.Forward, accelerateTarget) + accelerateTargetLength;
                double multiplier = Math.Abs(PID_Controller.NextValue(error, 0.016));

                if (accelerateTargetLength < negligible)
                    accelerateTargetNormalized = Vector3D.Normalize(control.GetShipVelocities().LinearVelocity);

                GyroUtils.PointInDirection(gyros, control, accelerateTargetNormalized, multiplier);
                //ThrustUtils.SetThrustBasedDot(thrusters, accelerateTargetNormalized, multiplier);
                autopilotModule.ThrustToVelocity(control.GetShipVelocities().LinearVelocity + accelerateTarget);
                

                if (accelerateInDirection && control.GetShipSpeed() >= (speedLimit - 0.01))
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
