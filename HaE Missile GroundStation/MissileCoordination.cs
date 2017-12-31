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
        public class MissileCoordination
        {
            public Action OnSystemOverwhelmed;
            public Action<MissileManagement.MissileInfo, MyDetectedEntityInfo> OnTargetFiredAt;
            public Action<MyDetectedEntityInfo> OnCantFireAtTarget;

            private MissileManagement management;
            private IMyTerminalBlock reference;
            private ACPWrapper antennas;

            private Dictionary<long, MyDetectedEntityInfo> targets;
            private Dictionary<long, MissileManagement.MissileInfo> firedMissiles;

            private List<IEnumerator<bool>> missileStaging;

            public MissileCoordination(MissileManagement management, IMyTerminalBlock rc, ACPWrapper antennas)
            {
                this.management = management;
                this.reference = rc;
                this.antennas = antennas;

                targets = new Dictionary<long, MyDetectedEntityInfo>();
                missileStaging = new List<IEnumerator<bool>>();
            }

            public void Main(UpdateType uType)
            {
                if (targets.Count == 0)
                    return;
                else if (targets.Count > management.SRIMissileCount)
                    OnSystemOverwhelmed?.Invoke();

                if ((uType & UpdateType.Update100) != 0)
                    OnUpdate100();
                if ((uType & UpdateType.Update10) != 0)
                    OnUpdate10();
            }

            private void OnUpdate10()
            {
                IssueMissileCommands();
            }

            private void OnUpdate100()
            {
                MoveNextState();
            }

            private void IssueMissileCommands()
            {
                foreach (var targetPair in targets)
                {
                    MyDetectedEntityInfo target = targetPair.Value;
                    Vector3D targetDirection = target.Position - reference.GetPosition();
                    double distance = targetDirection.Length();
                    targetDirection.Normalize();

                    MissileManagement.MissileInfo missile;
                    missile = management.GetMissileCloseToAndInDirection(reference.GetPosition(), targetDirection, MissileManagement.MissileType.SRInterceptor, distance / 4, 0, false);

                    if (missile == default(MissileManagement.MissileInfo))
                    {
                        OnCantFireAtTarget?.Invoke(target);
                        continue;
                    }

                    LaunchNewMissile(missile, targetPair.Key);
                }
            }

            private void LaunchNewMissile(MissileManagement.MissileInfo missile, long targetId)
            {
                IEnumerator<bool> tempSM = MissileSM(missile, targetId);
                tempSM.MoveNext();
            }

            private IEnumerator<bool> MissileSM(MissileManagement.MissileInfo missile, long targetId)
            {
                management.SendCommand(missile, "LaunchOut");
                firedMissiles[missile.id] = missile;
                management.RemoveMissile(missile);
                yield return true;

                string[] command = {
                    "TargetLoc",
                    targets[targetId].Position.ToString()
                };

                management.SendCommand(missile, command);

                OnTargetFiredAt?.Invoke(missile, targets[targetId]);
            }

            private void MoveNextState()
            {
                for (int i = missileStaging.Count -1; i >=0; i--)
                {
                    if (!missileStaging[i].MoveNext())
                    {
                        missileStaging[i].Dispose();
                        missileStaging.RemoveAt(i);
                    }    
                }
            }

            /*==========| Event callbacks |==========*/
            public void OnTargetDetected(MyDetectedEntityInfo target)
            {
                if (target.IsEmpty())
                    return;

                targets[target.EntityId] = target;
            }
        }
    }
}
