using System;

namespace eft_dma_radar
{
    public static class Misc
    {
        public static readonly Dictionary<string, string> Bosses = new Dictionary<string, string>()
        {
            ["Решала"] = "Reshala",
            ["Килла"] = "Killa",
            ["Тагилла"] = "Tagilla",
            ["Санитар"] = "Sanitar",
            ["Глухарь"] = "Gluhar",
            ["Штурман"] = "Shturman"
        };
    }
    public static class Extensions
    {
        public static void Reset(this System.Timers.Timer t)
        {
            t.Stop();
            t.Start();
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }

    public enum ShuffleSel
    {
        XFromX,
        XFromY,
        XFromZ,
        XFromW,

        YFromX = 0x00,
        YFromY = 0x04,
        YFromZ = 0x08,
        YFromW = 0x0C,

        ZFromX = 0x00,
        ZFromY = 0x10,
        ZFromZ = 0x20,
        ZFromW = 0x30,

        WFromX = 0x00,
        WFromY = 0x40,
        WFromZ = 0x80,
        WFromW = 0xC0,

        ExpandX = XFromX | YFromX | ZFromX | WFromX,
        ExpandY = XFromY | YFromY | ZFromY | WFromY,
        ExpandZ = XFromZ | YFromZ | ZFromZ | WFromZ,
        ExpandW = XFromW | YFromW | ZFromW | WFromW,

        ExpandXY = XFromX | YFromX | ZFromY | WFromY,
        ExpandZW = XFromZ | YFromZ | ZFromW | WFromW,

        ExpandInterleavedXY = XFromX | YFromY | ZFromX | WFromY,
        ExpandInterleavedZW = XFromZ | YFromW | ZFromZ | WFromW,

        RotateRight = XFromY | YFromZ | ZFromW | WFromX,
        RotateLeft = XFromW | YFromX | ZFromY | WFromZ,

        Swap = XFromW | YFromZ | ZFromY | WFromX
    }

}
