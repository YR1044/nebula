﻿using NebulaAPI;
using NebulaModel.Logger;
using NebulaModel.Networking;
using NebulaModel.Packets;
using NebulaModel.Packets.Logistics;
using NebulaWorld;

/*
 * When the client opens the UI of a station (ILS/PLS/Collector) the contents gets updated and shown to
 * the player once this packet is received. He will see a loading text before that.
 */
namespace NebulaNetwork.PacketProcessors.Logistics
{
    [RegisterPacketProcessor]
    public class StationUIInitialSyncProcessor : PacketProcessor<StationUIInitialSync>
    {
        public override void ProcessPacket(StationUIInitialSync packet, NebulaConnection conn)
        {
            StationComponent stationComponent = null;
            StationComponent[] gStationPool = GameMain.data.galacticTransport.stationPool;
            StationComponent[] stationPool = GameMain.data.galaxy.PlanetById(packet.PlanetId).factory.transport.stationPool;

            stationComponent = packet.StationGId > 0 ? gStationPool[packet.StationGId] : stationPool?[packet.StationId];

            if (stationComponent == null)
            {
                Log.Error($"StationUIInitialSyncProcessor: Unable to find requested station on planet {packet.PlanetId} with id {packet.StationId} and gid of {packet.StationGId}");
                return;
            }

            stationComponent.tripRangeDrones = packet.TripRangeDrones;
            stationComponent.tripRangeShips = packet.TripRangeShips;
            stationComponent.deliveryDrones = packet.DeliveryDrones;
            stationComponent.deliveryShips = packet.DeliveryShips;
            stationComponent.warpEnableDist = packet.WarperEnableDistance;
            stationComponent.warperNecessary = packet.WarperNecessary;
            stationComponent.includeOrbitCollector = packet.IncludeOrbitCollector;
            stationComponent.energy = packet.Energy;
            stationComponent.energyPerTick = packet.EnergyPerTick;

            for (int i = 0; i < packet.ItemId.Length; i++)
            {
                if (stationComponent.storage == null)
                {
                    stationComponent.storage = new StationStore[packet.ItemId.Length];
                }

                stationComponent.storage[i].itemId = packet.ItemId[i];
                stationComponent.storage[i].max = packet.ItemCountMax[i];
                stationComponent.storage[i].count = packet.ItemCount[i];
                stationComponent.storage[i].remoteOrder = packet.RemoteOrder[i];
                stationComponent.storage[i].localLogic = (ELogisticStorage)packet.LocalLogic[i];
                stationComponent.storage[i].remoteLogic = (ELogisticStorage)packet.RemoteLogic[i];
            }

            UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;
            if (stationWindow.active && Multiplayer.Session.StationsUI.UIIsSyncedStage == 1)
            {
                //Trigger OnStationIdChange() to refresh window
                stationWindow.OnStationIdChange();
            }
        }
    }
}
