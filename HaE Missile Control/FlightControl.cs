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
            

            private Vector3D speedTarget;

            public FlightControl(IMyShipController control, List<IMyGyro> gyros, List<IMyThrust> thrusters)
            {
                this.control = control;
                this.gyros = gyros;
                this.thrusters = thrusters;
            }

            public void Main()
            {
                if (speedTarget == null || speedTarget == Vector3D.Zero)
                    return;


                Vector3D normalizedTarget = -control.GetNaturalGravity();

                var dotProd = Vector3D.Dot(control.GetShipVelocities().LinearVelocity, speedTarget);
                if (control.GetShipVelocities().LinearVelocity.LengthSquared() < 10)
                {
                    normalizedTarget += speedTarget;
                }  
                else
                {
                    if (dotProd > 0)
                        normalizedTarget += Reflect(control.GetShipVelocities().LinearVelocity, speedTarget, 5);
                    else
                        normalizedTarget += - control.GetShipVelocities().LinearVelocity;
                }
                    

                normalizedTarget.Normalize();

                GyroUtils.PointInDirection(gyros, control, normalizedTarget, 2);

                
                if (Vector3D.DistanceSquared(speedTarget, control.GetShipVelocities().LinearVelocity) > 0.25)
                {
                    var forwardDot = Vector3D.Dot(control.WorldMatrix.Forward, normalizedTarget);

                    if (forwardDot > 0.707) //Only fire thrusters when within 45 degrees
                    {
                        ThrustUtils.SetThrust(thrusters, control.WorldMatrix.Forward, forwardDot * speedTarget.Length());
                    }
                    else
                    {
                        ThrustUtils.SetThrust(thrusters, control.WorldMatrix.Forward, 0);
                    }
                }
                else
                {
                    OnTargetSpeed();
                }
            }

            public void DirectControl(Vector3D direction)
            {
                direction.Normalize();

                GyroUtils.PointInDirection(gyros, control, direction, 2);
                ThrustUtils.SetThrustBasedDot(thrusters, direction);
            }

            public void Accelerate(Vector3D acceleration)
            {
                speedTarget = Vector3D.ClampToSphere(control.GetShipVelocities().LinearVelocity + acceleration, speedLimit);
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
