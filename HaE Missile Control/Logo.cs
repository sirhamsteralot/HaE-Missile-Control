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
        public class Logo
        {
            private int Teller = 0;
            private int WhereInTheLoop = 0;
            private int WaitFrames = 0;

            Program P;

            string TopMsg = "HaE Targeting  V0.2.3";

            string[] frames = {
                                "[=========||===========]",
                                "[===========||=========]",
                                "[=============||=======]",
                                "[===============||=====]",
                                "[=================||===]",
                                "[===================||=]",
                                "[|====================|]",
                                "[=||===================]",
                                "[===||=================]",
                                "[=====||===============]",
                                "[=======||=============]",
                              };

            public Logo(Program P, int WF = 5, int startingpoint = 0)
            {
                this.P = P;

                WhereInTheLoop = startingpoint;
                WaitFrames = WF;
            }

            public void DoTick()
            {
                if (Teller < WaitFrames)
                {
                    Teller++;
                }
                else
                {
                    Teller = 0;
                    WhereInTheLoop++;

                    if (WhereInTheLoop >= frames.Length)
                        WhereInTheLoop = 0;

                    P.Echo(TopMsg + "\n" + frames[WhereInTheLoop]);
                }
            }
        }
    }
}
