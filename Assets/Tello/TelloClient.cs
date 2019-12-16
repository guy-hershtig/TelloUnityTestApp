using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TelloClient : IDisposable
{
    #region Private Constants

    const int TelloCommandMinSize = 11;
    private const byte TelloCommandMagic = 0xCC;
    private const byte TelloAckMagic = 0x63;

    #endregion Private Constants

    #region Defaults

    public const int DefaultControlUdpPort = 8889;
    public const int DefaultVideoUdpPort = 6037;
    public const string DefaultHostName = "192.168.10.1";
    public const double DefaultPollingIntervalMilliseconds = 10 * 1000; // 10 seconds

    #endregion Defaults

    #region Private Fields

    private readonly UdpClient _client;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly SemaphoreSlim _sync = new SemaphoreSlim(1);
    private readonly System.Timers.Timer _pollingTimer = new System.Timers.Timer();

    #endregion Private Fields

    #region Public Properties

    public float BatteryPercent
    {
        get; private set;
    }

    public string HostName { get; private set; }

    public int ControlUdpPort { get; private set; }

    public int VideoUdpPort { get; private set; }

    public bool IsDisposed { get; private set; }

    #endregion Public Properties

    #region Construction & Destruction

    public TelloClient(string hostname = DefaultHostName, int controlUdpPort = DefaultControlUdpPort, int videoUdpPort = DefaultVideoUdpPort)
    {
        if (hostname == null)
            throw new ArgumentNullException(nameof(hostname));
        if (controlUdpPort <= 0 || controlUdpPort > System.Net.IPEndPoint.MaxPort)
            throw new ArgumentOutOfRangeException(nameof(controlUdpPort), controlUdpPort,
                $"Argument '{controlUdpPort}' is out of range.");
        if (videoUdpPort <= 0 || videoUdpPort > System.Net.IPEndPoint.MaxPort)
            throw new ArgumentOutOfRangeException(nameof(videoUdpPort), videoUdpPort,
                $"Argument '{videoUdpPort}' is out of range.");

        HostName = hostname;
        ControlUdpPort = controlUdpPort;
        VideoUdpPort = videoUdpPort;
        _client = new UdpClient(hostname, controlUdpPort);
    }

    ~TelloClient()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (_pollingTimer != null)
                _pollingTimer.Dispose();
            if (_client != null)
                _client.Dispose();
            if (_cts != null)
                _cts.Dispose();
            IsDisposed = true;
            if (disposing)
                GC.SuppressFinalize(this);
        }
    }

    #endregion Construction & Destruction

    #region Open API / Tello SDK

    private static void LogUnexpectedResponse(string response)
    {
        Debug.LogError("Tello returned an unexpected response: \"" + response + "\"");
    }

    private async Task<string> SendCommandAsync(string command)
    {
        var commandBytes = Encoding.ASCII.GetBytes(command);
        await _sync.WaitAsync(_cts.Token);
        try
        {
            await _client.SendAsync(commandBytes, commandBytes.Length);
            var recvResult = await _client.ReceiveAsync();
            var ascii = Encoding.ASCII.GetString(recvResult.Buffer);
            return ascii;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task<bool> SendCommandBooleanAsync(string command)
    {
        var response = await SendCommandAsync(command);
        if (response != "ok")
        {
            LogUnexpectedResponse(response);
            return false;
        }
        return true;
    }

    private async Task<float> SendCommandFloatAsync(string command)
    {
        var response = await SendCommandAsync(command);
        if (float.TryParse(response, out float result))
            return result;
        LogUnexpectedResponse(response);
        return float.NaN;
    }

    public async Task<bool> StartAsync(double pollingIntervalMilliseconds = DefaultPollingIntervalMilliseconds)
    {
        var success = await SendCommandBooleanAsync("command");
        if (success && pollingIntervalMilliseconds > 0)
        {
            _pollingTimer.Interval = pollingIntervalMilliseconds;
            _pollingTimer.Elapsed += PollingTimer_Elapsed;
            _pollingTimer.Start();
        }
        return success;
    }

    #endregion Open API / Tello SDK

    #region Event Handlers

    private void PollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            var task = GetBatteryPercentAsync();
            task.Wait(_cts.Token);
            BatteryPercent = task.Result;
        }
        catch (ObjectDisposedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
        finally
        {
            _pollingTimer.Start();
        }
    }

    #endregion Event Handlers

    #region Public Methods

    public void Close()
    {
        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // suppress exception.
        }
        catch (AggregateException)
        {
            // suppress exception.
        }
        _client.Close();
    }

    public async Task<bool> TakeOffAsync()
    {
        return await SendCommandBooleanAsync("takeoff");
    }

    public async Task<bool> LandAsync()
    {
        return await SendCommandBooleanAsync("land");
    }

    public async Task<bool> StreamOnAsync()
    {
        return await SendCommandBooleanAsync("streamon");
    }

    public async Task<bool> StreamOffAsync()
    {
        return await SendCommandBooleanAsync("streamoff");
    }

    public async Task<bool> SetEmergencyAsync()
    {
        return await SendCommandBooleanAsync("emergency");
    }

    public async Task<bool> GoUpAsync(byte cm)
    {
        return await SendCommandBooleanAsync("up " + cm);
    }

    public async Task<bool> GoDownAsync(byte cm)
    {
        return await SendCommandBooleanAsync("down " + cm);
    }

    public async Task<bool> GoLeftAsync(byte cm)
    {
        return await SendCommandBooleanAsync("left " + cm);
    }

    public async Task<bool> GoRightAsync(byte cm)
    {
        return await SendCommandBooleanAsync("right " + cm);
    }

    public async Task<bool> GoForward(byte cm)
    {
        return await SendCommandBooleanAsync("forward " + cm);
    }

    public async Task<bool> GoBackAsync(byte cm)
    {
        return await SendCommandBooleanAsync("back " + cm);
    }

    public async Task<bool> TurnClockwiseAsync(ushort degrees)
    {
        return await SendCommandBooleanAsync("cw " + degrees);
    }

    public async Task<bool> TurnCounterClockwiseAsync(ushort degrees)
    {
        return await SendCommandBooleanAsync("ccw " + degrees);
    }

    public async Task<bool> SetSpeedAsync(ushort cmps)
    {
        return await SendCommandBooleanAsync("speed " + cmps);
    }

    public async Task<float> GetSpeedAsync()
    {
        return await SendCommandFloatAsync("speed?");
    }

    public async Task<float> GetBatteryPercentAsync()
    {
        return await SendCommandFloatAsync("battery?");
    }

    #region IDisposable Members

    public void Dispose()
    {
        Dispose(true);
    }

    #endregion IDisposable Members

    #endregion Public Methods
}
