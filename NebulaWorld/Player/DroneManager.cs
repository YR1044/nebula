﻿using NebulaAPI;
using NebulaModel.Packets.Players;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaWorld.Player
{
    public class DroneManager : IDisposable
    {
        private static int[] DronePriorities = new int[255];
        private static System.Random rnd = new System.Random();
        private static Dictionary<ushort, List<int>> PlayerDroneBuildingPlans = new Dictionary<ushort, List<int>>();
        private static List<int> PendingBuildRequests = new List<int>();
        private static List<Vector3> CachedPositions = new List<Vector3>();

        public DroneManager()
        {
            DronePriorities = new int[255];
            PlayerDroneBuildingPlans = new Dictionary<ushort, List<int>>();
            PendingBuildRequests = new List<int>();
            CachedPositions = new List<Vector3>();
        }

        public void Dispose()
        {
            DronePriorities = null;
            PlayerDroneBuildingPlans = null;
            PendingBuildRequests = null;
            CachedPositions = null;
        }


        public void BroadcastDroneOrder(int droneId, int entityId, int stage)
        {
            if (!Multiplayer.IsActive)
            {
                return;
            }

            int priority = 0;
            if (stage == 1 || stage == 2)
            {
                priority = rnd.Next();
                DronePriorities[droneId] = priority;
            }
            else
            {
                GameMain.mainPlayer.mecha.droneLogic.serving.Remove(entityId);
            }
            Multiplayer.Session.Network.SendPacketToLocalPlanet(new NewDroneOrderPacket(GameMain.mainPlayer.planetId, droneId, entityId, Multiplayer.Session.LocalPlayer.Id, stage, priority, GameMain.localPlanet.factory.prebuildPool[-entityId].pos));
        }

        public void AddPlayerDronePlan(ushort playerId, int entityId)
        {
            if (!PlayerDroneBuildingPlans.ContainsKey(playerId))
            {
                PlayerDroneBuildingPlans.Add(playerId, new List<int>());
            }
            PlayerDroneBuildingPlans[playerId].Add(entityId);
        }

        public void RemovePlayerDronePlan(ushort playerId, int entityId)
        {
            if (PlayerDroneBuildingPlans.ContainsKey(playerId))
            {
                PlayerDroneBuildingPlans[playerId].Remove(entityId);
            }
        }

        public int[] GetPlayerDronePlans(ushort playerId)
        {
            if (PlayerDroneBuildingPlans.ContainsKey(playerId))
            {
                return PlayerDroneBuildingPlans[playerId].ToArray();
            }
            return null;
        }

        public void AddBuildRequestSent(int entityId)
        {
            PendingBuildRequests.Add(entityId);
        }

        public bool IsPendingBuildRequest(int entityId)
        {
            return PendingBuildRequests.Contains(entityId);
        }

        public bool RemoveBuildRequest(int entityId)
        {
            return PendingBuildRequests.Remove(entityId);
        }

        public void ClearCachedPositions()
        {
            CachedPositions.Clear();

            using (Multiplayer.Session.World.GetRemotePlayersModels(out Dictionary<ushort, RemotePlayerModel> remotePlayersModels))
            {
                foreach (RemotePlayerModel model in remotePlayersModels.Values)
                {
                    //Check only players on the same planet
                    if (model.Movement.GetLastPosition().LocalPlanetId != GameMain.mainPlayer.planetId)
                    {
                        continue;
                    }
                    //Cache players positions for this looking for traget session
                    CachedPositions.Add(model.Movement.GetLastPosition().LocalPlanetPosition.ToVector3());
                }
            }
        }

        public bool IsLocalPlayerClosestTo(ref Vector3 entityPos)
        {
            if (!Multiplayer.IsActive)
            {
                return true;
            }

            float myDistance = (GameMain.mainPlayer.position - entityPos).sqrMagnitude;

            foreach (Vector3 playerPostion in CachedPositions)
            {
                //If remote player is closer, ignore the entity
                if (myDistance > (playerPostion - entityPos).sqrMagnitude)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
