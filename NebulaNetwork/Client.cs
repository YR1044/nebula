﻿using HarmonyLib;
using NebulaAPI;
using NebulaModel;
using NebulaModel.Logger;
using NebulaModel.Networking;
using NebulaModel.Packets.GameStates;
using NebulaModel.Packets.Players;
using NebulaModel.Packets.Routers;
using NebulaModel.Packets.Session;
using NebulaModel.Utils;
using NebulaWorld;
using NebulaWorld.GameStates;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

namespace NebulaNetwork
{
    public class Client : NetworkProvider, IClient
    {
        private const float FRAGEMENT_UPDATE_INTERVAL = 0.1f;
        private const float GAME_STATE_UPDATE_INTERVAL = 1f;
        private const float MECHA_SYNCHONIZATION_INTERVAL = 30f;

        private readonly IPEndPoint serverEndpoint;
        private readonly string serverPassword;
        public IPEndPoint ServerEndpoint => serverEndpoint;
        
        private WebSocket clientSocket;
        private NebulaConnection serverConnection;
        private bool websocketAuthenticationFailure;
        
        private float fragmentUpdateTimer = 0f;
        private float mechaSynchonizationTimer = 0f;
        private float gameStateUpdateTimer = 0f;

        public Client(string url, int port, string password = "")
            : this(new IPEndPoint(Dns.GetHostEntry(url).AddressList[0], port), password)
        {
        }

        public Client(IPEndPoint endpoint, string password = "") : base(null)
        {
            serverEndpoint = endpoint;
            serverPassword = password;

        }

        public override void Start()
        {
            foreach (Assembly assembly in AssembliesUtils.GetNebulaAssemblies())
            {
                PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor);
            }
            PacketUtils.RegisterAllPacketProcessorsInCallingAssembly(PacketProcessor, false);

            foreach (Assembly assembly in NebulaModAPI.TargetAssemblies)
            {
                PacketUtils.RegisterAllPacketNestedTypesInAssembly(assembly, PacketProcessor);
                PacketUtils.RegisterAllPacketProcessorsInAssembly(assembly, PacketProcessor, false);
            }
#if DEBUG
            PacketProcessor.SimulateLatency = true;
#endif

            clientSocket = new WebSocket($"ws://{serverEndpoint}/socket");
            clientSocket.Log.Level = LogLevel.Debug;
            clientSocket.Log.Output = Log.SocketOutput;
            clientSocket.OnOpen += ClientSocket_OnOpen;
            clientSocket.OnClose += ClientSocket_OnClose;
            clientSocket.OnMessage += ClientSocket_OnMessage;

            var currentLogOutput = clientSocket.Log.Output;
            clientSocket.Log.Output = (logData, arg2) =>
            {
                currentLogOutput(logData, arg2);

                // This method of detecting an authentication failure is super finicky, however there is no other way to do this in the websocket package we are currently using
                if (logData.Level == LogLevel.Fatal && logData.Message == "Requires the authentication.")
                {
                    websocketAuthenticationFailure = true;
                }
            };

            if (!string.IsNullOrWhiteSpace(serverPassword))
            {
                clientSocket.SetCredentials("nebula-player", serverPassword, true);
            }

            websocketAuthenticationFailure = false;

            clientSocket.Connect();

            ((LocalPlayer)Multiplayer.Session.LocalPlayer).IsHost = false;

            if (Config.Options.RememberLastIP)
            {
                // We've successfully connected, set connection as last ip, cutting out "ws://" and "/socket"
                Config.Options.LastIP = serverEndpoint.ToString();
                Config.SaveOptions();
            }

            if (Config.Options.RememberLastClientPassword && !string.IsNullOrWhiteSpace(serverPassword))
            {
                Config.Options.LastClientPassword = serverPassword;
                Config.SaveOptions();
            }

            try
            {
                NebulaModAPI.OnMultiplayerGameStarted?.Invoke();
            }
            catch (System.Exception e)
            {
                Log.Error("NebulaModAPI.OnMultiplayerGameStarted error:\n" + e);
            }
        }

        public override void Stop()
        {
            clientSocket?.Close((ushort)DisconnectionReason.ClientRequestedDisconnect, "Player left the game");

            // load settings again to dispose the temp soil setting that could have been received from server
            Config.LoadOptions();
            try
            {
                NebulaModAPI.OnMultiplayerGameEnded?.Invoke();
            }
            catch (System.Exception e)
            {
                Log.Error("NebulaModAPI.OnMultiplayerGameEnded error:\n" + e);
            }
        }

        public override void Dispose()
        {
            Stop();
        }

        public override void SendPacket<T>(T packet)
        {
            serverConnection?.SendPacket(packet);
        }
        public override void SendPacketExclude<T>(T packet, INebulaConnection exclude)
        {
            // Only possible from host
            throw new System.NotImplementedException();
        }

        public override void SendPacketToLocalStar<T>(T packet)
        {
            serverConnection?.SendPacket(new StarBroadcastPacket(PacketProcessor.Write(packet), GameMain.data.localStar?.id ?? -1));
        }

        public override void SendPacketToLocalPlanet<T>(T packet)
        {
            serverConnection?.SendPacket(new PlanetBroadcastPacket(PacketProcessor.Write(packet), GameMain.mainPlayer.planetId));
        }

        public override void SendPacketToPlanet<T>(T packet, int planetId)
        {
            // Only possible from host
            throw new System.NotImplementedException();
        }

        public override void SendPacketToStar<T>(T packet, int starId)
        {
            // Only possible from host
            throw new System.NotImplementedException();
        }

        public override void SendPacketToStarExclude<T>(T packet, int starId, INebulaConnection exclude)
        {
            // Only possible from host
            throw new System.NotImplementedException();
        }

        public override void Update()
        {
            PacketProcessor.ProcessPacketQueue();

            if (Multiplayer.Session.IsGameLoaded)
            {
                mechaSynchonizationTimer += Time.deltaTime;
                if (mechaSynchonizationTimer > MECHA_SYNCHONIZATION_INTERVAL)
                {
                    SendPacket(new PlayerMechaData(GameMain.mainPlayer));
                    mechaSynchonizationTimer = 0f;
                }

                gameStateUpdateTimer += Time.deltaTime;
                if (gameStateUpdateTimer >= GAME_STATE_UPDATE_INTERVAL)
                {
                    if (!GameMain.isFullscreenPaused)
                    {
                        SendPacket(new GameStateRequest());
                    }
                    gameStateUpdateTimer = 0f;
                }
            }

            fragmentUpdateTimer += Time.deltaTime;
            if (fragmentUpdateTimer >= FRAGEMENT_UPDATE_INTERVAL)
            {
                if (GameStatesManager.FragmentSize > 0)
                {
                    GameStatesManager.UpdateBufferLength(GetFragmentBufferLength());
                }
                fragmentUpdateTimer = 0f;
            }
        }

        private void ClientSocket_OnMessage(object sender, MessageEventArgs e)
        {
            if (!Multiplayer.IsLeavingGame)
            {
                PacketProcessor.EnqueuePacketForProcessing(e.RawData, serverConnection);
            }
        }

        private void ClientSocket_OnOpen(object sender, System.EventArgs e)
        {
            DisableNagleAlgorithm(clientSocket);

            Log.Info($"Server connection established");
            serverConnection = new NebulaConnection(clientSocket, serverEndpoint, PacketProcessor);

            //TODO: Maybe some challenge-response authentication mechanism?

            SendPacket(new LobbyRequest(
                CryptoUtils.GetPublicKey(CryptoUtils.GetOrCreateUserCert()),
                !string.IsNullOrWhiteSpace(Config.Options.Nickname) ? Config.Options.Nickname : GameMain.data.account.userName));
        }

        private void ClientSocket_OnClose(object sender, CloseEventArgs e)
        {
            serverConnection = null;

            UnityDispatchQueue.RunOnMainThread(() =>
            {
                // If the client is Quitting by himself, we don't have to inform him of his disconnection.
                if (e.Code == (ushort)DisconnectionReason.ClientRequestedDisconnect)
                {
                    return;
                }

                // Opens the pause menu on disconnection to prevent NRE when leaving the game
                if (Multiplayer.Session?.IsGameLoaded ?? false)
                {
                    GameMain.instance._paused = true;
                }

                if (e.Code == (ushort)DisconnectionReason.ModIsMissing)
                {
                    InGamePopup.ShowWarning(
                        "Mod Mismatch".Translate(),
                        string.Format("You are missing mod {0}".Translate(), e.Reason),
                        "OK".Translate(),
                        Multiplayer.LeaveGame);
                    return;
                }

                if (e.Code == (ushort)DisconnectionReason.ModIsMissingOnServer)
                {
                    InGamePopup.ShowWarning(
                        "Mod Mismatch".Translate(),
                        string.Format("Server is missing mod {0}".Translate(), e.Reason),
                        "OK".Translate(),
                        Multiplayer.LeaveGame);
                    return;
                }

                if (e.Code == (ushort)DisconnectionReason.ModVersionMismatch)
                {
                    string[] versions = e.Reason.Split(';');
                    InGamePopup.ShowWarning(
                        "Mod Version Mismatch".Translate(),
                        string.Format("Your mod {0} version is not the same as the Host version.\nYou:{1} - Remote:{2}".Translate(), versions[0], versions[1], versions[2]),
                        "OK".Translate(),
                        Multiplayer.LeaveGame);
                    return;
                }

                if (e.Code == (ushort)DisconnectionReason.GameVersionMismatch)
                {
                    string[] versions = e.Reason.Split(';');
                    InGamePopup.ShowWarning(
                        "Game Version Mismatch".Translate(),
                        string.Format("Your version of the game is not the same as the one used by the Host.\nYou:{0} - Remote:{1}".Translate(), versions[0], versions[1]),
                        "OK".Translate(),
                        Multiplayer.LeaveGame);
                    return;
                }

                if (e.Code == (ushort)DisconnectionReason.ProtocolError && websocketAuthenticationFailure)
                {
                    InGamePopup.AskInput(
                        "Server Requires Password".Translate(),
                        "Server is protected. Please enter the correct password:".Translate(),
                        InputField.ContentType.Password,
                        serverPassword,
                        (password) =>
                        {
                            Multiplayer.ShouldReturnToJoinMenu = false;
                            Multiplayer.LeaveGame();
                            Multiplayer.ShouldReturnToJoinMenu = true;
                            Multiplayer.JoinGame(new Client(serverEndpoint, password));
                        },
                        Multiplayer.LeaveGame
                        );
                    return;
                }

                if (e.Code == (ushort)DisconnectionReason.HostStillLoading)
                {
                    InGamePopup.ShowWarning(
                        "Server Busy".Translate(),
                        "Server is not ready to join. Please try again later.".Translate(),
                        "OK".Translate(),
                        Multiplayer.LeaveGame);
                    return;
                }

                if (Multiplayer.Session.IsGameLoaded || Multiplayer.Session.IsInLobby)
                {
                    InGamePopup.ShowWarning(
                        "Connection Lost".Translate(),
                        "You have been disconnected from the server.".Translate() + "\n" + e.Reason,
                        "Quit",
                        Multiplayer.LeaveGame);
                    if (Multiplayer.Session.IsInLobby)
                    {
                        Multiplayer.ShouldReturnToJoinMenu = false;
                        Multiplayer.Session.IsInLobby = false;
                        UIRoot.instance.galaxySelect.CancelSelect();
                    }
                }
                else
                {
                    Log.Warn("Disconnect code: " + e.Code + ", reason:" + e.Reason);
                    InGamePopup.ShowWarning(
                        "Server Unavailable".Translate(),
                        "Could not reach the server, please try again later.".Translate(),
                        "OK".Translate(),
                        Multiplayer.LeaveGame);
                }
            });
        }

        private static void DisableNagleAlgorithm(WebSocket socket)
        {
            TcpClient tcpClient = AccessTools.FieldRefAccess<WebSocket, TcpClient>("_tcpClient")(socket);
            if (tcpClient != null)
            {
                tcpClient.NoDelay = true;
            }
        }

        private readonly AccessTools.FieldRef<WebSocket, MemoryStream> fragmentsBufferRef = AccessTools.FieldRefAccess<WebSocket, MemoryStream>("_fragmentsBuffer");
        private int GetFragmentBufferLength()
        {
            MemoryStream fragmentsBuffer = fragmentsBufferRef(clientSocket);
            return (int)(fragmentsBuffer?.Length ?? 0);
        }
    }
}
