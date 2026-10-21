using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace RiskOfCSharp2.Net
{
    public class DiscoveredRoom
    {
        public string Ip = "127.0.0.1";
        public int Port = NetworkManager.BasePort;
        public string Code = "";
        public double TimeStamp;
    }

    public class NetworkManager
    {
        public const int BasePort = 27777;
        public const int DiscoveryPort = 27788;
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static NetworkManager Instance { get; private set; } = null!;

        public string? Mode = null; // null, "host", "client"
        public string MyId = "host";
        public string RoomCode = "";
        public string HostIp;
        public int Port = BasePort;
        public volatile bool Connected = false;

        private TcpListener? _server;
        private TcpClient? _client;
        private readonly Dictionary<string, TcpClient> _clients = new();
        private readonly object _lock = new();
        private readonly ConcurrentQueue<JsonObject> _inbox = new();
        public readonly ConcurrentDictionary<string, DiscoveredRoom> DiscoveredRooms = new();
        private volatile bool _running = true;

        public NetworkManager()
        {
            Instance = this;
            HostIp = GetLocalIp();
            StartLanListener();
        }

        public static string GetLocalIp()
        {
            try
            {
                using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                s.Connect("10.255.255.255", 1);
                if (s.LocalEndPoint is IPEndPoint ep)
                    return ep.Address.ToString();
            }
            catch { }
            return "127.0.0.1";
        }

        public static string EncodeRoomCode(string ipStr, int port = BasePort)
        {
            byte[] bytes;
            try { bytes = IPAddress.Parse(ipStr).GetAddressBytes(); }
            catch { bytes = IPAddress.Parse("127.0.0.1").GetAddressBytes(); }

            ulong ipInt = ((ulong)bytes[0] << 24) | ((ulong)bytes[1] << 16) | ((ulong)bytes[2] << 8) | bytes[3];
            ulong offset = (ulong)Math.Clamp(port - BasePort, 0, 15);
            ulong val = (ipInt << 4) | offset;

            var chars = new List<char>();
            while (val > 0)
            {
                ulong rem = val % 36;
                val /= 36;
                chars.Add(Alphabet[(int)rem]);
            }
            chars.Reverse();
            string raw = chars.Count > 0 ? new string(chars.ToArray()) : "0";
            return raw.Length > 3 ? $"{raw[..^3]}-{raw[^3..]}" : raw;
        }

        public static (string Ip, int Port) DecodeRoomCode(string codeStr)
        {
            string clean = codeStr.Trim().ToUpperInvariant().Replace(" ", "");
            if (string.IsNullOrEmpty(clean)) return ("127.0.0.1", BasePort);

            if (clean.Contains('.'))
            {
                if (clean.Contains(':'))
                {
                    var parts = clean.Split(':', 2);
                    return (parts[0], int.Parse(parts[1]));
                }
                return (clean, BasePort);
            }

            clean = clean.Replace("-", "");
            ulong val = 0;
            foreach (char ch in clean)
            {
                int idx = Alphabet.IndexOf(ch);
                if (idx < 0) throw new ArgumentException("Неверный символ в коде комнаты");
                val = val * 36UL + (ulong)idx;
            }

            int offset = (int)(val & 0xFUL);
            ulong ipInt = val >> 4;
            byte[] b =
            {
                (byte)((ipInt >> 24) & 0xFF),
                (byte)((ipInt >> 16) & 0xFF),
                (byte)((ipInt >> 8) & 0xFF),
                (byte)(ipInt & 0xFF)
            };
            return (new IPAddress(b).ToString(), BasePort + offset);
        }

        private void StartLanListener()
        {
            var th = new Thread(() =>
            {
                UdpClient? udp = null;
                try
                {
                    udp = new UdpClient();
                    udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udp.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
                }
                catch { return; }

                var remoteEp = new IPEndPoint(IPAddress.Any, 0);
                while (_running)
                {
                    try
                    {
                        byte[] data = udp.Receive(ref remoteEp);
                        var node = JsonNode.Parse(Encoding.UTF8.GetString(data))?.AsObject();
                        if (node != null && node["magic"]?.GetValue<string>() == "ROR2_LAN")
                        {
                            string code = node["code"]?.GetValue<string>() ?? "";
                            string ip = remoteEp.Address.ToString();
                            if (ip == "0.0.0.0") ip = node["ip"]?.GetValue<string>() ?? "127.0.0.1";
                            int port = node["port"]?.GetValue<int>() ?? BasePort;
                            DiscoveredRooms[code] = new DiscoveredRoom
                            {
                                Ip = ip, Port = port, Code = code,
                                TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0
                            };
                        }
                    }
                    catch { Thread.Sleep(100); }
                }
            }) { IsBackground = true };
            th.Start();
        }

        public (bool Ok, string Info) HostRoom()
        {
            StopSession();
            HostIp = GetLocalIp();
            bool bound = false;

            for (int offset = 0; offset < 16; offset++)
            {
                try
                {
                    Port = BasePort + offset;
                    _server = new TcpListener(IPAddress.Any, Port);
                    _server.Start(4);
                    bound = true;
                    break;
                }
                catch { _server = null; }
            }

            if (!bound || _server == null)
                return (false, "Не удалось занять сетевой порт");

            Mode = "host";
            MyId = "host";
            Connected = true;
            RoomCode = EncodeRoomCode(HostIp, Port);

            new Thread(AcceptLoop) { IsBackground = true }.Start();
            new Thread(BroadcastLoop) { IsBackground = true }.Start();
            return (true, RoomCode);
        }

        private void BroadcastLoop()
        {
            using var udp = new UdpClient { EnableBroadcast = true };
            while (Mode == "host" && Connected)
            {
                try
                {
                    var obj = new JsonObject
                    {
                        ["magic"] = "ROR2_LAN",
                        ["code"] = RoomCode,
                        ["ip"] = HostIp,
                        ["port"] = Port
                    };
                    byte[] payload = Encoding.UTF8.GetBytes(obj.ToJsonString());
                    udp.Send(payload, payload.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
                    udp.Send(payload, payload.Length, new IPEndPoint(IPAddress.Loopback, DiscoveryPort));
                }
                catch { }
                Thread.Sleep(1000);
            }
        }

        private void AcceptLoop()
        {
            int nextIdx = 2;
            while (Mode == "host" && Connected && _server != null)
            {
                try
                {
                    var conn = _server.AcceptTcpClient();
                    conn.NoDelay = true;
                    string pid = $"p{nextIdx++}";
                    lock (_lock) { _clients[pid] = conn; }

                    SendJson(conn, new JsonObject
                    {
                        ["type"] = "welcome",
                        ["player_id"] = pid,
                        ["room_code"] = RoomCode
                    });
                    _inbox.Enqueue(new JsonObject
                    {
                        ["type"] = "client_joined",
                        ["player_id"] = pid
                    });

                    new Thread(() => ClientRecvLoop(conn, pid)) { IsBackground = true }.Start();
                }
                catch { break; }
            }
        }

        public (bool Ok, string Info) JoinByCode(string codeStr)
        {
            try
            {
                var (ip, port) = DecodeRoomCode(codeStr);
                return JoinDirect(ip, port, codeStr.Trim().ToUpperInvariant());
            }
            catch (Exception e)
            {
                return (false, $"Ошибка кода: {e.Message}");
            }
        }

        public (bool Ok, string Info) JoinLanAuto()
        {
            double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            var active = DiscoveredRooms.Values.Where(r => now - r.TimeStamp < 5.0).ToList();
            if (active.Count == 0)
                return JoinDirect("127.0.0.1", BasePort, EncodeRoomCode(GetLocalIp(), BasePort));
            var room = active[0];
            return JoinDirect(room.Ip, room.Port, room.Code);
        }

        public (bool Ok, string Info) JoinDirect(string ip, int port, string displayCode = "")
        {
            StopSession();
            try
            {
                var cli = new TcpClient();
                var res = cli.BeginConnect(ip, port, null, null);
                if (!res.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3.5)) || !cli.Connected)
                {
                    cli.Close();
                    return (false, $"Не удалось подключиться ({ip}:{port})");
                }
                cli.EndConnect(res);
                cli.NoDelay = true;
                _client = cli;
            }
            catch
            {
                return (false, $"Не удалось подключиться ({ip}:{port})");
            }

            Mode = "client";
            Connected = true;
            RoomCode = string.IsNullOrEmpty(displayCode) ? EncodeRoomCode(ip, port) : displayCode;
            new Thread(() => ClientRecvLoop(_client, "host")) { IsBackground = true }.Start();
            return (true, RoomCode);
        }

        private void ClientRecvLoop(TcpClient tcp, string senderId)
        {
            try
            {
                using var reader = new StreamReader(tcp.GetStream(), Encoding.UTF8);
                while (Connected)
                {
                    string? line = reader.ReadLine();
                    if (line == null) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var msg = JsonNode.Parse(line)?.AsObject();
                    if (msg == null) continue;

                    if (Mode == "host")
                    {
                        msg["_sender"] = senderId;
                        Broadcast(msg, excludePid: senderId);
                    }
                    _inbox.Enqueue(msg);
                }
            }
            catch { }

            if (Mode == "host")
            {
                lock (_lock) { _clients.Remove(senderId); }
                _inbox.Enqueue(new JsonObject { ["type"] = "player_left", ["player_id"] = senderId });
                Broadcast(new JsonObject { ["type"] = "player_left", ["player_id"] = senderId });
            }
            else
            {
                _inbox.Enqueue(new JsonObject { ["type"] = "disconnected" });
                Connected = false;
            }
        }

        private static bool SendJson(TcpClient sock, JsonObject data)
        {
            try
            {
                byte[] raw = Encoding.UTF8.GetBytes(data.ToJsonString() + "\n");
                sock.GetStream().Write(raw, 0, raw.Length);
                return true;
            }
            catch { return false; }
        }

        public void Broadcast(JsonObject data, string? excludePid = null)
        {
            if (!Connected) return;
            if (Mode == "host")
            {
                List<KeyValuePair<string, TcpClient>> snapshot;
                lock (_lock) { snapshot = _clients.ToList(); }
                var dead = new List<string>();
                foreach (var (pid, sock) in snapshot)
                {
                    if (pid == excludePid) continue;
                    if (!SendJson(sock, data)) dead.Add(pid);
                }
                if (dead.Count > 0)
                {
                    lock (_lock) { foreach (var d in dead) _clients.Remove(d); }
                }
            }
            else if (Mode == "client" && _client != null)
            {
                SendJson(_client, data);
            }
        }

        public List<JsonObject> PollMessages()
        {
            var list = new List<JsonObject>();
            while (_inbox.TryDequeue(out var msg))
                list.Add(msg);
            return list;
        }

        public void StopSession()
        {
            Connected = false;
            Mode = null;
            lock (_lock)
            {
                foreach (var c in _clients.Values) { try { c.Close(); } catch { } }
                _clients.Clear();
            }
            while (_inbox.TryDequeue(out _)) { }
            try { _client?.Close(); } catch { }
            _client = null;
            try { _server?.Stop(); } catch { }
            _server = null;
        }
    }
}