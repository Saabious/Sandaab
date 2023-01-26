using System.Net;

namespace Sandaab.Core.Entities
{
    public record Route
    {
        public bool Authenticated;
        public EndPoint LocalEndPoint;
        public EndPoint RemoteEndPoint;

        public Route(EndPoint localEndPoint, EndPoint remoteEndPoint)
        {
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
        }

        public override int GetHashCode()
        {
            int hash = 27;
            if (LocalEndPoint != null)
                hash += LocalEndPoint.GetHashCode();
            hash += RemoteEndPoint.GetHashCode();
            return hash;
        }

        public override string ToString()
        {
            return (LocalEndPoint == null ? "???" : LocalEndPoint.ToString())
                + " - "
                + RemoteEndPoint.ToString();
        }
    }
}
