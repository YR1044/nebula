﻿using BepInEx.Bootstrap;
using NebulaModel.DataStructures;
using NebulaModel.Networking;
using NebulaModel.Utils;
using NebulaWorld.MonoBehaviours.Local;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static NebulaWorld.Chat.CopyTextChatLinkHandler;

namespace NebulaWorld.Chat.Commands
{
    public class InfoCommandHandler : IChatCommandHandler
    {
        public void Execute(ChatWindow window, string[] parameters)
        {
            if (!Multiplayer.IsActive)
            {
                window.SendLocalChatMessage("This command can only be used in multiplayer!".Translate(), ChatMessageType.CommandErrorMessage);
                return;
            }
            
            bool full = parameters.Length > 0 && parameters[0].Equals("full");

            if (Multiplayer.Session.Network is IServer server)
            {
                string output = GetServerInfoText(
                    server, 
                    new IPUtils.IpInfo {
                        LANAddress = "Pending...".Translate(),
                        WANv4Address = "Pending...".Translate(),
                        WANv6Address = "Pending...".Translate(),
                        PortStatus = "Pending...".Translate(),
                        DataState = IPUtils.DataState.Unset
                    }, 
                    full
                );
                ChatMessage message = window.SendLocalChatMessage(output, ChatMessageType.CommandOutputMessage);

                // This will cause the temporary (Pending...) info to be dynamically replaced with the correct info once it is in
                IPUtils.GetIPInfo(server.Port).ContinueWith(async (ipInfo) =>
                {
                    string newOutput = GetServerInfoText(server, await ipInfo, full);
                    message.Text = newOutput;
                });
            }
            else if (Multiplayer.Session.Network is IClient client)
            {
                string output = GetClientInfoText(client, full);
                window.SendLocalChatMessage(output, ChatMessageType.CommandOutputMessage);
            }
            
        }

        public static string GetServerInfoText(IServer server, IPUtils.IpInfo ipInfo, bool full)
        {
            StringBuilder sb = new("Server info:".Translate());

            string lan = ipInfo.LANAddress;
            if (IPUtils.IsIPv4(lan))
            {
                lan = $"{FormatCopyString($"{ipInfo.LANAddress}:{server.Port}")}";
            }
            sb.Append("\n  ").Append("Local IP address: ".Translate()).Append(lan);

            string wanv4 = ipInfo.WANv4Address;
            if(IPUtils.IsIPv4(wanv4))
            {
                wanv4 = $"{FormatCopyString($"{ipInfo.WANv4Address}:{server.Port}", true, IPFilter)}";
            }
            sb.Append("\n  ").Append("WANv4 IP address: ".Translate()).Append(wanv4);

            string wanv6 = ipInfo.WANv6Address;
            if (IPUtils.IsIPv6(wanv6))
            {
                wanv6 = $"{FormatCopyString($"{ipInfo.WANv6Address}:{server.Port}", true, IPFilter)}";
            }
            sb.Append("\n  ").Append("WANv6 IP address: ".Translate()).Append(wanv6);

            if (server.NgrokEnabled)
            {
                if (server.NgrokActive)
                {
                    sb.Append("\n  ").Append("Ngrok address: ".Translate()).Append(FormatCopyString(server.NgrokAddress, true, NgrokAddressFilter));
                }
                else
                {
                    sb.Append("\n ").Append("Ngrok address: Tunnel Inactive!".Translate());
                }

                if (server.NgrokLastErrorCode != null)
                {
                    sb.Append($" ({FormatCopyString(server.NgrokLastErrorCode)})");
                }
            }

            sb.Append("\n  ").Append("Port status: ".Translate()).Append(ipInfo.PortStatus);
            sb.Append("\n  ").Append("Data state: ".Translate()).Append(ipInfo.DataState);
            TimeSpan timeSpan = DateTime.Now.Subtract(Multiplayer.Session.StartTime);
            sb.Append("\n  ").Append("Uptime: ".Translate()).Append($"{(int) Math.Round(timeSpan.TotalHours)}:{timeSpan.Minutes}:{timeSpan.Seconds}");

            sb.Append("\n\n").Append("Game info:".Translate());
            sb.Append("\n  ").Append("Game Version: ".Translate()).Append(GameConfig.gameVersion.ToFullString());
            sb.Append("\n  ").Append("Mod Version: ".Translate()).Append(ThisAssembly.AssemblyFileVersion);

            if (full)
            {
                sb.Append("\n\n").Append("Mods installed:".Translate());
                int index = 1;
                foreach (var kv in Chainloader.PluginInfos)
                {
                    sb.Append($"\n[{index++:D2}] {kv.Value.Metadata.Name} - {kv.Value.Metadata.Version}");
                }
            }
            else
            {
                sb.Append('\n').Append("Use '/info full' to see mod list.".Translate());
            }

            return sb.ToString();
        }

        private static string GetClientInfoText(IClient client, bool full)
        {
            StringBuilder sb = new("Client info:".Translate());

            string ipAddress = client.ServerEndpoint.ToString();

            sb.Append("\n  ").Append("Host IP address: ".Translate()).Append(FormatCopyString(ipAddress, true));
            sb.Append("\n  ").Append("Game Version: ".Translate()).Append(GameConfig.gameVersion.ToFullString());
            sb.Append("\n  ").Append("Mod Version: ".Translate()).Append(ThisAssembly.AssemblyFileVersion);

            if (full)
            {
                sb.Append("\n\n").Append("Mods installed:".Translate());
                int index = 1;
                foreach (var kv in Chainloader.PluginInfos)
                {
                    sb.Append($"\n[{index++:D2}] {kv.Value.Metadata.Name} - {kv.Value.Metadata.Version}");
                }
            }
            else
            {
                sb.Append('\n').Append("Use '/info full' to see mod list.".Translate());
            }

            return sb.ToString();
        }

        private static string IPFilter(string ip)
        {
            if (!NebulaModel.Config.Options.StreamerMode) return ip;

            if (!ip.Contains("]:")) {
                string[] parts = ip.Split(':');
                string safeIp = ip;
                if (parts.Length == 2)
                {
                    safeIp = $"{Regex.Replace(parts[0], @"\w", "*")}:{parts[1]}";
                }
                else
                {
                    safeIp = Regex.Replace(safeIp, @"\w", "*");
                }
                return safeIp;
            } else
            {
                string[] parts = ip.Split(new string[] { "]:" }, StringSplitOptions.None);
                string safeIp = ip;
                if (parts.Length == 2)
                {
                    safeIp = $"{Regex.Replace(parts[0], @"\w", "*")}]:{parts[1]}";
                }
                else
                {
                    safeIp = Regex.Replace(safeIp, @"\w", "*");
                }
                return safeIp;
            }
        }

        private static string NgrokAddressFilter(string address)
        {
            if (!NebulaModel.Config.Options.StreamerMode) return address;

            return Regex.Replace(address, @"\w", "*");
        }

        private static string ReplaceChars(string s, string targetSymbols, char newVal)
        {
            StringBuilder sb = new(s);
            for (int i = 0; i < sb.Length; i++)
            {
                if (targetSymbols.Contains(sb[i]))
                {
                    sb[i] = newVal;
                }
            }
            return sb.ToString();
        }

        public string GetDescription()
        {
            return "Get information about server".Translate();
        }
        
        public string[] GetUsage()
        {
            return new string[] { "[full]" };
        }
    }
}