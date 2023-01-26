namespace Sandaab.Core.Constantes
{
    public enum DevicePlatform
    {
        Unknown = 0,
        Windows = 1,
        Android = 2
    }

    public enum DeviceState
    {
        Initialized = 0,
        Found = 1,
        Paired = 2,
        Removed = 3,
    }

    public enum GrantState
    {
        Pending,
        Granted,
        Rejected
    }

    public enum NetworkType
    {
        // In order of Speed
        Bluetooth,
        Adb,
        Ip,
    }

    public enum NetworkEvent
    {
        Unknown,
        Connected,
        Sleep,
        Disconnected,
    }

    public enum NetworkResult
    {
        Success,
        NotConnected,
        UnknownRemoteEndPoint,
        RemoteEndPointNotListen,
        RemoteEndPointInUse,
        ConnectFailure,
        InvalidRemoteListener,
        ConnectionLost,
        SendFailure,
        ReceivedIncomplete,
        UnexpectedDataReceived,
        Exception,
        WaitingForConnection,
        ConnectionClosed,
        LoginRequested,
        Disconnect,
        PingTimeout,
    }

    public enum ConnectionAction
    {
        Read,
        Write,
        Flush,
    }

    public enum DeviceEvent
    {
        Found,
        Paired,
        Removed,
        Connected,
        Disconnected,
        Updated,
    }

    public enum ScreenCaptureServiceChange
    {
        Stopped,
        Started,
    }
}
