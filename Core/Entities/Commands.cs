using Newtonsoft.Json;
using Sandaab.Core.Constantes;

namespace Sandaab.Core.Entities
{
    public abstract class Command
    {
        public override string ToString()
        {
            return GetType().Name;
        }
    }

    public abstract class RequestCommand : Command
    {
    }

    public abstract class AttachmentsCommand : Command
    {
        [JsonIgnore]
        public byte[][] Attachments;
    }

    public class PingCommand : Command
    {
    }

    public class SearchRequestCommand : RequestCommand
    {
        public string ReceiverDeviceId;
        public string SenderDeviceId;
        public string SenderDeviceName;
        public DevicePlatform SenderDevicePlatform;
        public int TcpListenerPort;
    }

    public class SearchResponseCommand : Command
    {
        public string DeviceName;
        public DevicePlatform DevicePlatform;
    }

    public class PairRequestCommand : Command
    {
    }

    public class PairResponseCommand : Command
    {
        public bool Granted;
    }

    public class DisconnectCommand : Command
    {
        public bool Unpair;
    }

    public class RemoteControlRequestCommand : Command
    {
        public enum EventType
        {
            Start,
            Change,
            Stop,
        }

        public ScreenArea Area;
        public EventType Event;
    }

    public class RemoteControlResponseCommand : AttachmentsCommand
    {
        public enum ImageFormat
        {
            Jpeg = 0,
            Png = 1,
        }

        public enum ResultType
        {
            AccessDenied = 0,
            Valid = 1,
            Stopped = 2,
        }

        public record FrameRecord
        {
            public int X;
            public int Y;
            public ImageFormat ImageFormat;
        }

        public float DpiX;
        public float DpiY;
        public FrameRecord Frame;
        public int ScreenHeight;
        public bool Scaled;
        public ResultType Result;
        public int ScreenWidth;
    }
}
