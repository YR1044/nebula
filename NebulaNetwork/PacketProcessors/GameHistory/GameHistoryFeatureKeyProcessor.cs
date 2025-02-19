﻿using NebulaAPI;
using NebulaModel.Networking;
using NebulaModel.Packets;
using NebulaModel.Packets.GameHistory;
using NebulaWorld;

namespace NebulaNetwork.PacketProcessors.GameHistory
{
    [RegisterPacketProcessor]
    internal class GameHistoryFeatureKeyProcessor : PacketProcessor<GameHistoryFeatureKeyPacket>
    {
        public override void ProcessPacket(GameHistoryFeatureKeyPacket packet, NebulaConnection conn)
        {
            if (IsHost)
            {
                Multiplayer.Session.Network.SendPacketExclude(packet, conn);
            }

            using (Multiplayer.Session.History.IsIncomingRequest.On())
            {
                if (packet.Add)
                {
                    GameMain.data.history.RegFeatureKey(packet.FeatureId);
                }
                else
                {
                    GameMain.data.history.UnregFeatureKey(packet.FeatureId);
                }

                if (packet.FeatureId == 1100002)
                {
                    // Update Quick Build button in dyson editor
                    UIRoot.instance.uiGame.dysonEditor.controlPanel.inspector.overview.autoConstructSwitch.SetToggleNoEvent(packet.Add);
                }
            }
        }
    }
}
