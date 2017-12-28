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
        public class ThrustUtils
        {
            public static void SetThrust(List<IMyThrust> thrusters, Vector3D direction, double percent)
            {
                foreach (var thrust in thrusters)
                {
                    if (Vector3D.Dot(thrust.WorldMatrix.Backward, direction) > 0.9)
                    {
                        thrust.ThrustOverridePercentage = (float)percent;
                    }
                }
            }
        }
    }
}
