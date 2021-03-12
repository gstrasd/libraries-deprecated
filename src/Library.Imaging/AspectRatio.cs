using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Library.Imaging
{
    public readonly struct AspectRatio
    {
        public AspectRatio(int width, int height)
        {
            Width = width;
            Height = height;
            Value = width / (float) height;
        }

        public readonly int Width;
        public readonly int Height;
        public readonly float Value;

        public override string ToString() => $"{Width}:{Height}";

        public static explicit operator AspectRatio(string value) => AspectRatio.Parse(value);

        public static AspectRatio Parse(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var width = Int32.Parse(value[..value.IndexOf(':')]);
            var height = Int32.Parse(value[(value.IndexOf(':') + 1)..]);

            return new AspectRatio(width, height);
        }

        public static bool TryParse(string value, out AspectRatio aspectRatio)
        {
            var valid = value != null;
            valid &= Int32.TryParse(value[..value.IndexOf(':')], out var width);
            valid &= Int32.TryParse(value[(value.IndexOf(':') + 1)..], out var height);

            aspectRatio = valid ? new AspectRatio(width, height) : default;
            return valid;
        }
    }
}
