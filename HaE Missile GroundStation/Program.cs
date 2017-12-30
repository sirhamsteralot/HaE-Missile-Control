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
    partial class Program : MyGridProgram
    {
        ACPWrapper antennaComms;

        IEnumerator<bool> initializer;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            initializer = Initialize();
        }

        public void Main(string args, UpdateType uType)
        {
            if (!initializer.MoveNext())
                initializer.Dispose();
            else
                return;

            long senderId;
            string[] messages = antennaComms.Main(args, out senderId);
            if (messages != null)
            {
                foreach (var message in messages)
                {
                    ParseMessage(message);
                }
            }

            if (args == "")
                return;

            string[] msg = { args };
            if (antennaComms.PrepareMSG(msg, "HaE Missile Server"))
                Echo("Added message to queue");
        }

        void ParseMessage(string message)
        {
            Echo(message);
        }

        IEnumerator<bool> Initialize()
        {
            antennaComms = new ACPWrapper(this);
            yield return true;

            Echo("Initialized!");
        }
    }
}