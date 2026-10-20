using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using VRC.OSCQuery;

namespace MeijiChatboxStatus.Core;

/// <summary>
/// Sends OSC to VRChat and receives avatar parameters from it.
/// Receiving prefers OSCQuery (VRChat discovers us on a free port, so we never fight other OSC apps over 9001)
/// and falls back to a fixed UDP port.
/// </summary>
public sealed class OscService : IDisposable
{
    private const string ServiceName = "Meiji Chatbox Status";
    private const int SioUdpConnReset = -1744830452; // stops ICMP "port unreachable" from breaking the socket when VRChat is closed

    private readonly Logger _log;
    private readonly UdpClient _sender;
    private IPEndPoint _target = new(IPAddress.Loopback, 9000);
    private UdpClient? _receiver;
    private OSCQueryService? _query;
    private CancellationTokenSource? _cts;
    private OscSettings? _applied;
    private bool _sendErrorLogged;

    /// <summary>Raised on a background thread for every received message.</summary>
    public event Action<OscMessage>? Received;

    /// <summary>Recently seen avatar parameters (name → last message, UTC time), used to help users pick counter parameters.</summary>
    /// <remarks>Stores the raw message: VRChat can send hundreds per second, so formatting waits until the UI asks.</remarks>
    public ConcurrentDictionary<string, (OscMessage Message, DateTime Utc)> SeenParameters { get; } = new();

    public string ListenMode { get; private set; } = "off"; // "oscquery" | "port" | "off"
    public int ListenPort { get; private set; }
    public string? ListenError { get; private set; }
    public DateTime LastReceivedUtc { get; private set; } = DateTime.MinValue;

    public OscService(Logger log)
    {
        _log = log;
        _sender = new UdpClient(AddressFamily.InterNetwork);
        DisableConnReset(_sender);
    }

    /// <summary>Applies OSC settings, restarting the listener only when something relevant changed.</summary>
    public void Configure(OscSettings s)
    {
        if (!IPAddress.TryParse(s.Ip, out var ip))
        {
            _log.Error($"IP \"{s.Ip}\" ไม่ถูกต้อง ใช้ 127.0.0.1 แทน");
            ip = IPAddress.Loopback;
        }
        _target = new IPEndPoint(ip, s.SendPort);

        if (_applied is not null && _applied.UseOscQuery == s.UseOscQuery && _applied.ListenPort == s.ListenPort) return;
        _applied = new OscSettings { Ip = s.Ip, SendPort = s.SendPort, ListenPort = s.ListenPort, UseOscQuery = s.UseOscQuery };
        StartListener(s);
    }

    public bool Send(string address, params object[] args)
    {
        try
        {
            byte[] packet = Osc.Encode(address, args);
            _sender.Send(packet, packet.Length, _target);
            _sendErrorLogged = false;
            return true;
        }
        catch (SocketException e)
        {
            if (!_sendErrorLogged) _log.Error($"ส่ง OSC ไป {_target} ไม่สำเร็จ: {e.Message}");
            _sendErrorLogged = true;
            return false;
        }
    }

    private void StartListener(OscSettings s)
    {
        StopListener();
        ListenError = null;

        if (s.UseOscQuery)
        {
            try
            {
                var udp = Bind(0);
                int port = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
                _query = new OSCQueryServiceBuilder()
                    .WithServiceName(ServiceName)
                    .WithHostIP(IPAddress.Loopback)
                    .WithOscIP(IPAddress.Loopback)
                    .WithTcpPort(Extensions.GetAvailableTcpPort())
                    .WithUdpPort(port)
                    .WithDiscovery(new MeaModDiscovery())
                    .StartHttpServer()
                    .AdvertiseOSC()
                    .AdvertiseOSCQuery()
                    .Build();
                // Having this endpoint in our tree is what makes VRChat send us avatar parameters.
                _query.AddEndpoint("/avatar/parameters", "b", Attributes.AccessValues.WriteOnly);
                _query.AddEndpoint("/avatar/change", "s", Attributes.AccessValues.WriteOnly);
                _query.RefreshServices();

                RunReceiver(udp, "oscquery", port);
                _log.Info($"เปิดรับ OSC ผ่าน OSCQuery แล้ว (UDP {port}) — VRChat จะหาแอปนี้เจอเอง");
                return;
            }
            catch (Exception e)
            {
                _query?.Dispose(); _query = null;
                _receiver?.Dispose(); _receiver = null;
                _log.Warn($"เปิด OSCQuery ไม่สำเร็จ ({e.Message}) จะใช้พอร์ต {s.ListenPort} แทน");
            }
        }

        try
        {
            RunReceiver(Bind(s.ListenPort), "port", s.ListenPort);
            _log.Info($"เปิดรับ OSC ที่พอร์ต {s.ListenPort} แล้ว");
        }
        catch (SocketException e)
        {
            ListenMode = "off";
            ListenError = e.SocketErrorCode == SocketError.AddressAlreadyInUse
                ? $"พอร์ต {s.ListenPort} ถูกแอปอื่นใช้อยู่ — ลองเปิด OSCQuery หรือเปลี่ยนพอร์ต"
                : e.Message;
            _log.Error($"เปิดรับ OSC ไม่ได้: {ListenError} (ระบบนับ/AFK จะใช้ไม่ได้ แต่ยังส่ง Chatbox ได้ปกติ)");
        }
    }

    private static UdpClient Bind(int port)
    {
        var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
        DisableConnReset(udp);
        return udp;
    }

    private void RunReceiver(UdpClient udp, string mode, int port)
    {
        _receiver = udp;
        ListenMode = mode;
        ListenPort = port;
        var cts = _cts = new CancellationTokenSource();
        _ = Task.Run(() => ReceiveLoop(udp, cts.Token));
    }

    private async Task ReceiveLoop(UdpClient udp, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await udp.ReceiveAsync(ct); }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { continue; }

            var now = DateTime.UtcNow;
            LastReceivedUtc = now;
            foreach (var msg in Osc.Decode(result.Buffer))
            {
                if (msg.Address.StartsWith("/avatar/parameters/", StringComparison.Ordinal))
                {
                    SeenParameters[msg.Address[19..]] = (msg, now);
                    if (SeenParameters.Count > 512) SeenParameters.Clear(); // avatars with huge parameter lists: keep it bounded
                }
                try { Received?.Invoke(msg); }
                catch (Exception e) { _log.Error($"จัดการข้อความ OSC {msg.Address} ผิดพลาด: {e.Message}"); }
            }
        }
    }

    private void StopListener()
    {
        _cts?.Cancel();
        _cts = null;
        _receiver?.Dispose();
        _receiver = null;
        try { _query?.Dispose(); } catch (Exception e) { _log.Warn($"ปิด OSCQuery ผิดพลาด: {e.Message}"); }
        _query = null;
        ListenMode = "off";
    }

    private static void DisableConnReset(UdpClient udp)
    {
        try { udp.Client.IOControl(SioUdpConnReset, [0, 0, 0, 0], null); }
        catch (Exception e) when (e is SocketException or PlatformNotSupportedException) { }
    }

    public void Dispose()
    {
        StopListener();
        _sender.Dispose();
    }
}
