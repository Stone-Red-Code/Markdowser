using System;
using System.Globalization;

namespace Markdowser.Utilities;

internal static class BytesToString
{
    public static string Convert(long value)
    {
        string suffix;
        double readable;
        switch (Math.Abs(value))
        {
            case >= 0x1000000000000000:
                suffix = "EiB";
                readable = value >> 50;
                break;

            case >= 0x4000000000000:
                suffix = "PiB";
                readable = value >> 40;
                break;

            case >= 0x10000000000:
                suffix = "TiB";
                readable = value >> 30;
                break;

            case >= 0x40000000:
                suffix = "GiB";
                readable = value >> 20;
                break;

            case >= 0x100000:
                suffix = "MiB";
                readable = value >> 10;
                break;

            case >= 0x400:
                suffix = "KiB";
                readable = value;
                break;

            default:
                return value.ToString("0 B");
        }

        return (readable / 1024).ToString("0.00 ", CultureInfo.InvariantCulture) + suffix;
    }
}