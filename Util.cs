using System.Net;

namespace Zamboni14Legacy;

public static class Util
{
    public static uint GetIPAddressAsUInt(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            throw new ArgumentException(nameof(ipAddress));
        var address = IPAddress.Parse(ipAddress);
        var bytes = address.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    public static string GetUIntAsIPAddress(uint ip)
    {
        var bytes = BitConverter.GetBytes(ip);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        var address = new IPAddress(bytes);
        return address.ToString();
    }

    public static uint TimeNow()
    {
        return (uint)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }
}