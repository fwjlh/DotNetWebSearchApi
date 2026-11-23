using System.Net;
using System.Net.Sockets;

namespace DotNetWebSearchApi.Search;

public static class PublicNetworkSocketsHandler
{
    public static SocketsHttpHandler Create(CookieContainer? cookieContainer = null)
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            ConnectCallback = ConnectToPublicAddressAsync,
            CookieContainer = cookieContainer ?? new CookieContainer(),
            UseCookies = true,
            UseProxy = false
        };
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return IsPublicAddress(address.MapToIPv4());
        }

        if (IPAddress.IsLoopback(address)
            || address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.IsIPv6LinkLocal
            || address.IsIPv6Multicast)
        {
            return false;
        }

        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return bytes[0] is >= 0x20 and <= 0x3F
                && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] <= 0x01)
                && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8)
                && !(bytes[0] == 0x20 && bytes[1] == 0x02)
                && !(bytes[0] == 0x3F && bytes[1] == 0xFF && (bytes[2] & 0xF0) == 0);
        }

        return bytes[0] != 0
            && bytes[0] != 10
            && !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127)
            && bytes[0] != 127
            && !(bytes[0] == 169 && bytes[1] == 254)
            && !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            && !(bytes[0] == 192 && (bytes[1] == 168 || bytes[1] == 0 && (bytes[2] == 0 || bytes[2] == 2) || bytes[1] == 88 && bytes[2] == 99))
            && !(bytes[0] == 198 && (bytes[1] == 18 || bytes[1] == 19 || bytes[1] == 51 && bytes[2] == 100))
            && !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113)
            && bytes[0] < 224;
    }

    private static async ValueTask<Stream> ConnectToPublicAddressAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(
            context.DnsEndPoint.Host,
            cancellationToken).ConfigureAwait(false);
        if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
        {
            throw new HttpRequestException("Outbound connection resolved to a non-public IP address.");
        }

        Exception? lastError = null;
        foreach (IPAddress address in addresses)
        {
            Socket socket = new(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(
                    new IPEndPoint(address, context.DnsEndPoint.Port),
                    cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException exception)
            {
                lastError = exception;
                socket.Dispose();
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        throw new HttpRequestException("Could not connect to any public address for the provider host.", lastError);
    }
}
