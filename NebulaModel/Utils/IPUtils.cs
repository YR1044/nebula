﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NebulaModel.Utils
{
    public static class IPUtils
    {
        static readonly HttpClient client = new();

        public enum IPConfiguration
        {
            Both,
            IPv4,
            IPv6
        }

        public enum DataState
        {
            Unset,
            Fresh,
            Cached
        }

        public enum Status
        {
            None,
            Unsupported,
            Unavailable
        }

        public enum PortStatus
        {
            Open,
            Closed
        }

        public struct IpInfo
        {
            public string LANAddress;
            public string WANv4Address;
            public string WANv6Address;
            public string PortStatus;
            public DataState DataState;
        }

        static IpInfo ipInfo;

        static readonly Timer timer;

        static IPUtils()
        {
            timer = new Timer()
            {
                Enabled = false,
                Interval = TimeSpan.FromMinutes(1).TotalMilliseconds,
            };
            timer.Elapsed += (s, e) => { timer.Stop(); };
            client.Timeout = TimeSpan.FromSeconds(15);
        }

        public static string GetLocalAddress()
        {
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address.ToString();
        }

        public static async Task<string> GetWANv4Address()
        {
            try
            {
                string response = await client.GetStringAsync("https://api.ipify.org");

                if(IsIPv4(response))
                {
                    return response;
                }

                return Status.Unsupported.ToString();
            }
            catch(Exception e)
            {
                Logger.Log.Warn(e);
                return ipInfo.WANv4Address ?? Status.Unavailable.ToString();
            }
        }

        public static async Task<string> GetWANv6Address()
        {
            try
            {
                string response = await client.GetStringAsync("https://api64.ipify.org");

                if(IsIPv6(response))
                {
                    return $"[{response}]";
                }

                return Status.Unsupported.ToString();
            }
            catch (Exception e)
            {
                Logger.Log.Warn(e);
                return ipInfo.WANv6Address ?? Status.Unavailable.ToString();
            }
        }

        public static async Task<string> GetPortStatus(ushort port)
        {
            try
            {
                string response = await client.GetStringAsync($"https://ifconfig.co/port/{port}");
                Dictionary<string, object> jObject = MiniJson.Deserialize(response) as Dictionary<string, object>;
                if (IsIPv4((string)jObject["ip"]))
                {
                    return (bool)jObject["reachable"] ? PortStatus.Open.ToString() : PortStatus.Closed.ToString() + "(IPv4)";
                }
                else
                {
                    // if client has IPv6, extra test for IPv4 port status
                    string result = ((bool)jObject["reachable"] ? PortStatus.Open.ToString() : PortStatus.Closed.ToString()) + "(IPv6) ";
                    try
                    {
                        IPAddress iPv4Address = null;
                        foreach (IPAddress ip in Dns.GetHostEntry(string.Empty).AddressList)
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetwork)
                            {
                                string str = ip.ToString();
                                if (!str.StartsWith("127.0") && !str.StartsWith("192.168"))
                                {
                                    iPv4Address = ip;
                                    break;
                                }
                            }
                        }
                        if (iPv4Address != null)
                        {
                            // TODO: More respect about rate limit?
                            HttpWebRequest httpWebRequest = HttpWebRequest.Create($"https://ifconfig.co/port/{port}") as HttpWebRequest;
                            httpWebRequest.Timeout = 5000;
                            httpWebRequest.ServicePoint.BindIPEndPointDelegate = (servicePoint, remoteEndPoint, retryCount) => new IPEndPoint(iPv4Address, 0);

                            using WebResponse webResponse = await httpWebRequest.GetResponseAsync();
                            using Stream stream = webResponse.GetResponseStream();
                            using StreamReader readStream = new(stream, Encoding.UTF8);
                            response = readStream.ReadToEnd();
                            jObject = MiniJson.Deserialize(response) as Dictionary<string, object>;
                            result += ((bool)jObject["reachable"] ? PortStatus.Open.ToString() : PortStatus.Closed.ToString()) + "(IPv4)";
                        }
                    }
                    catch(Exception e)
                    {
                        Logger.Log.Warn(e);
                    }
                    return result;
                }
            }
            catch (Exception e)
            {
                Logger.Log.Warn(e);
                return ipInfo.PortStatus ?? Status.Unavailable.ToString();
            }
        }

        public static async Task<IpInfo> GetIPInfo(ushort port = default)
        {
            if(timer.Enabled && ipInfo.DataState != DataState.Unset)
            {
                return ipInfo;
            }

            var rawInfo = new IpInfo()
            {
                LANAddress = GetLocalAddress().ToString(),
                WANv4Address = await GetWANv4Address(),
                WANv6Address = await GetWANv6Address(),
                DataState = DataState.Fresh
            };

            rawInfo.PortStatus = await GetPortStatus(port);

            ipInfo = rawInfo;
            ipInfo.DataState = DataState.Cached;
            timer.Start();

            return rawInfo;
        }

        public static bool IsIPv6(string ip)
        {
            if (IPAddress.TryParse(ip, out IPAddress ipAddress))
            {
                return ipAddress.AddressFamily == AddressFamily.InterNetworkV6;
            }
            return false;
        }

        public static bool IsIPv4(string ip)
        {
            if (IPAddress.TryParse(ip, out IPAddress ipAddress))
            {
                return ipAddress.AddressFamily == AddressFamily.InterNetwork;
            }
            return false;
        }

        public static async Task<bool> IsIPv6Supported()
        {
            return IsIPv6(await GetWANv6Address());
        }
    }
}