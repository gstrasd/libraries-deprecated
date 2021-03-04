using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Imaging
{
    public static class Graphics
    {
        public const uint UInt24MaxValue = 2 ^ (uint)24;

        public static float Contrast(float l1, float l2)
        {
            if (l1 < 0 || l1 > 1) throw new ArgumentOutOfRangeException(nameof(l1), "Luminance must be a value between 0 and 1.");
            if (l2 < 0 || l2 > 1) throw new ArgumentOutOfRangeException(nameof(l2), "Luminance must be a value between 0 and 1.");

            float lighter, darker;

            if (l1 < l2)
            {
                lighter = l2;
                darker = l1;
            }
            else
            {
                lighter = l1;
                darker = l2;
            }

            return Convert.ToSingle((lighter + 0.05f) / (darker + 0.05));
        }

        public static float Contrast8Bit(byte l1, byte l2)
        {
            return Contrast((float) l1 / Byte.MaxValue, (float) l2 / Byte.MaxValue);
        }

        public static float Contrast16Bit(ushort l1, ushort l2)
        {
            return Contrast((float)l1 / UInt16.MaxValue, (float)l2 / UInt16.MaxValue);
        }

        public static float Contrast24Bit(uint l1, uint l2)
        {
            if (l1 > UInt24MaxValue) throw new ArgumentOutOfRangeException(nameof(l1), $"Value cannot exceed {UInt24MaxValue:N0}.");
            if (l2 > UInt24MaxValue) throw new ArgumentOutOfRangeException(nameof(l2), $"Value cannot exceed {UInt24MaxValue:N0}.");

            return Contrast((float)l1 / UInt24MaxValue, (float)l2 / UInt24MaxValue);
        }

        public static float Contrast32Bit(uint l1, uint l2)
        {
            return Contrast((float)l1 / UInt32.MaxValue, (float)l2 / UInt32.MaxValue);
        }
    }
}
