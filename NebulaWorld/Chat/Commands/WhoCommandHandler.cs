﻿using NebulaAPI;
using NebulaModel.DataStructures;
using NebulaModel.Packets.Players;
using NebulaWorld.MonoBehaviours.Local;
using System.Text;
using static NebulaWorld.Chat.NavigateChatLinkHandler;

namespace NebulaWorld.Chat.Commands
{
    public class WhoCommandHandler : IChatCommandHandler
    {
        public void Execute(ChatWindow window, string[] parameters)
        {
            if (!Multiplayer.Session.LocalPlayer.IsHost)
            {
                // send a request to host
                Multiplayer.Session.Network.SendPacket(new ChatCommandWhoPacket(true, null));
            }
            else
            {
                IPlayerData[] playerDatas = Multiplayer.Session.Network.PlayerManager.GetAllPlayerDataIncludingHost();
                ILocalPlayer hostPlayer = Multiplayer.Session.LocalPlayer;
                string messageContent = BuildResultPayload(playerDatas, hostPlayer);
                window.SendLocalChatMessage(messageContent, ChatMessageType.CommandOutputMessage);
            }
        }

        public string GetDescription()
        {
            return "List all players and their locations".Translate();
        }

        public string[] GetUsage()
        {
            return new string[] { "" };
        }

        public static string BuildResultPayload(IPlayerData[] allPlayers, ILocalPlayer hostPlayer)
        {
            StringBuilder sb = new StringBuilder(string.Format("/who results: ({0} players)\r\n".Translate(), allPlayers.Length));
            foreach (IPlayerData playerData in allPlayers)
            {
                sb.Append(BuildWhoMessageTextForPlayer(playerData, hostPlayer)).Append("\r\n");
            }

            return sb.ToString();
        }

        private static string BuildWhoMessageTextForPlayer(IPlayerData playerData, ILocalPlayer localPlayer)
        {
            StringBuilder sb = new StringBuilder(string.Format("[{0}] {1}", playerData.PlayerId, FormatNavigateString(playerData.Username)));
            if (localPlayer.Id == playerData.PlayerId)
            {
                sb.Append(" (host)".Translate());
            }

            string playerPlanetString = null;
            if (playerData.LocalPlanetId > 0)
            {
                PlanetData playerPlanet = GameMain.galaxy.PlanetById(playerData.LocalPlanetId);
                if (playerPlanet != null)
                {
                    playerPlanetString = ", " + playerPlanet.name;
                }
            }

            string playerSystemString = null;
            if (playerData.LocalStarId > 0)
            {
                StarData starData = GameMain.galaxy.StarById(playerData.LocalStarId);
                if (starData != null)
                {
                    playerSystemString = ", " + starData.name;
                }
            }


            if (!string.IsNullOrWhiteSpace(playerPlanetString))
            {
                sb.Append(playerPlanetString);
            }
            else
            {
                sb.Append(", in space".Translate());
            }

            if (!string.IsNullOrWhiteSpace(playerSystemString))
            {
                sb.Append(playerSystemString);
            }
            else
            {
                sb.Append(", at coordinates ".Translate() + playerData.UPosition);
            }

            return sb.ToString();
        }
    }
}