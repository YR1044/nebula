﻿using NebulaModel.Packets.Factory.Belt;
using NebulaModel.Logger;
using System;
using System.Collections.Generic;

namespace NebulaWorld.Factory
{
    public class BeltManager : IDisposable
    {
        private List<BeltUpdate> beltPickupUpdates;
        private List<BeltUpdatePutItemOnPacket> beltPutdownPackets;
        private int putDownTimer;
        const int MAX_PUTDOWN_WAIT_TICK = 30;

        public BeltManager()
        {
            beltPickupUpdates = new List<BeltUpdate>();
            beltPutdownPackets = new List<BeltUpdatePutItemOnPacket>();
            putDownTimer = 0;
        }

        public void Dispose()
        {
            beltPickupUpdates = null;
            beltPutdownPackets = null;
        }

        public void BeltPickupStarted()
        {
            beltPickupUpdates.Clear();
        }

        public void RegisterBeltPickupUpdate(int itemId, int count, int beltId)
        {
            if (Multiplayer.IsActive)
            {
                beltPickupUpdates.Add(new BeltUpdate(itemId, count, beltId));
            }
        }

        public void BeltPickupEnded()
        {
            if (GameMain.data.localPlanet != null)
            {
                Multiplayer.Session.Network.SendPacketToLocalStar(new BeltUpdatePickupItemsPacket(beltPickupUpdates.ToArray(), GameMain.data.localPlanet.id));
            }

            beltPickupUpdates.Clear();
        }

        public bool TryPutItemOnBelt(BeltUpdatePutItemOnPacket packet)
        {
            using (Multiplayer.Session.Factories.IsIncomingRequest.On())
            {
                CargoTraffic cargoTraffic = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory?.cargoTraffic;
                bool ret = false;
                if (cargoTraffic == null)
                {
                    // Ignore events on factroies not loaded yet
                    return true;
                }
                if (packet.ItemCount == 1)
                {
                    ret = cargoTraffic.PutItemOnBelt(packet.BeltId, packet.ItemId, packet.ItemInc);
                    return ret;
                }
                else
                {                    
                    if (cargoTraffic.beltPool[packet.BeltId].id != 0 && cargoTraffic.beltPool[packet.BeltId].id == packet.BeltId)
                    {
                        int index = cargoTraffic.beltPool[packet.BeltId].segIndex + cargoTraffic.beltPool[packet.BeltId].segPivotOffset;
                        ret = cargoTraffic.GetCargoPath(cargoTraffic.beltPool[packet.BeltId].segPathId).TryInsertItem(index, packet.ItemId, packet.ItemCount, packet.ItemInc);
                    }
                    return ret;
                }
            }
        }

        public void RegiserbeltPutdownPacket(BeltUpdatePutItemOnPacket packet)
        {
            beltPutdownPackets.Add(packet);
        }

        public void GameTick()
        {
            if (beltPutdownPackets.Count <= 0)
            {
                return;
            }
            try
            {
                if (TryPutItemOnBelt(beltPutdownPackets[0]))
                {
                    beltPutdownPackets.RemoveAt(0);
                    putDownTimer = 0;
                }
                else if (++putDownTimer > MAX_PUTDOWN_WAIT_TICK)
                {
                    Log.Warn($"Cannot put item{beltPutdownPackets[0].ItemId} on belt{beltPutdownPackets[0].BeltId}, planet{beltPutdownPackets[0].PlanetId}");
                    beltPutdownPackets.RemoveAt(0);
                    putDownTimer = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"BeltManager error! Cannot put item{beltPutdownPackets[0].ItemId} on belt{beltPutdownPackets[0].BeltId}, planet{beltPutdownPackets[0].PlanetId}");
                Log.Warn(ex);
                beltPutdownPackets.Clear();
                putDownTimer = 0;
            }
        }
    }
}
