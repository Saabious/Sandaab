using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Sandaab.Core.Entities
{
    public class UdpEndPoint : IPEndPoint
    {
        public UdpEndPoint(IPAddress address, int port)
            : base(address, port)
        {
        }
    }

    public class TcpEndPoint : IPEndPoint
    {
        public TcpEndPoint(IPAddress address, int port)
            : base(address, port)
        {
        }
    }

    public class AdbEndPoint : TcpEndPoint
    {
        public AdbEndPoint(int port)
            : base(IPAddress.Loopback, port)
        {
        }
    }

    public class BtEndPoint : EndPoint
    {
        public const int AF_BTH = 32;

        public string Address { get { return ToString(); } }
        public override AddressFamily AddressFamily { get { return (AddressFamily)AF_BTH; } }
        public ulong BluetoothAddress { get; private set; }

        public BtEndPoint(ulong bluetoothAddress)
        {
            Debug.Assert(OperatingSystem.IsWindows());
            BluetoothAddress = bluetoothAddress;
        }

        public BtEndPoint(string address)
        {
            Debug.Assert(OperatingSystem.IsAndroid());
            BluetoothAddress = ulong.Parse(address.Replace(":", ""), NumberStyles.HexNumber);
        }

        public override bool Equals(object obj)
        {
            return obj is BtEndPoint
                && GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return BluetoothAddress.GetHashCode();
        }

        public override string ToString()
        {
            string result = string.Empty;
            var bytes = BitConverter.GetBytes(BluetoothAddress);
            for (var i = 6 - 1; i >= 0; i--)
            {
                result += bytes[i].ToString("X2");
                if (i > 0) result += ":";
            }
            return result;
        }
    }
}
