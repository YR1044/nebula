﻿using NebulaAPI;
using NebulaModel.Packets.Factory.Belt;

namespace NebulaNetwork.PacketProcessors.Factory.Belt
{
    [RegisterPacketProcessor]
    internal class BeltReverseProcessor : BasePacketProcessor<BeltReversePacket>
    {
        public override void ProcessPacket(BeltReversePacket packet, INebulaConnection conn)
        {
            PlanetFactory factory = GameMain.galaxy.PlanetById(packet.PlanetId).factory;
            if (factory == null)
                return;

            using (NebulaModAPI.MultiplayerSession.Factories.IsIncomingRequest.On())
            {
                NebulaModAPI.MultiplayerSession.Factories.EventFactory = factory;
                NebulaModAPI.MultiplayerSession.Factories.TargetPlanet = packet.PlanetId;
                if (NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                {
                    // Load planet model
                    NebulaModAPI.MultiplayerSession.Factories.AddPlanetTimer(packet.PlanetId);
                }

                UIBeltWindow beltWindow = UIRoot.instance.uiGame.beltWindow;
                beltWindow._Close(); // close the window first to avoid changing unwant variable when setting beltId
                PlanetFactory tmpFactory = beltWindow.factory;
                int tmpBeltId = beltWindow.beltId;
                beltWindow.factory = factory;
                beltWindow.beltId = packet.BeltId;
                beltWindow.OnReverseButtonClick(0);
                beltWindow.factory = tmpFactory;
                beltWindow.beltId = tmpBeltId;

                NebulaModAPI.MultiplayerSession.Factories.EventFactory = null;
                NebulaModAPI.MultiplayerSession.Factories.TargetPlanet = NebulaModAPI.PLANET_NONE;
            }
        }
    }
}
