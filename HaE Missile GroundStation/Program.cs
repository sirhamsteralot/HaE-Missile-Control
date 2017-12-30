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
        MissileManagement missileManagement;

        IEnumerator<bool> initializer;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            initializer = Initialize();
        }

        public void Main(string args, UpdateType uType)
        {
            //Initialize
            if (!initializer.MoveNext())
                initializer.Dispose();
            else
                return;

            //Parse regular commands
            if (!ParseCommands(args))
            {
                //Parse Messages
                long senderId;
                string[] messages = antennaComms.Main(args, out senderId);
                if (messages != null)
                {
                    ParseMessages(messages, senderId);
                }
            }
        }

        bool ParseCommands(string command)
        {
            switch(command)
            {
                case "RefreshMissiles":
                    missileManagement.RefreshMissileList();
                    return true;
            }

            return false;
        }

        void ParseMessages(string[] messages, long senderId)
        {
            // Parse message header
            switch (messages[0])
            {
                case "missileEntry":
                    missileManagement.ParseMissileEntry(messages);
                    break;
            }
        }

        /*==========| Event callbacks |==========*/
        void OnMissileAdded(MissileManagement.MissileInfo info)
        {
            Echo($"Missile with ID {info.id} added.");
        }

        IEnumerator<bool> Initialize()
        {
            antennaComms = new ACPWrapper(this);
            yield return true;

            missileManagement = new MissileManagement(antennaComms);
            missileManagement.OnMissileAdded += OnMissileAdded;
            yield return true;

            Echo("Initialized!");
        }
    }
}