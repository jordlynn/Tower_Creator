using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TowerAnimator
{
    static class Utility
    {
        /// <summary>
        /// Converts an HSV color specification to a System.Drawing.Color.
        /// </summary>
        /// <param name="hue">Hue in [0, 360)</param>
        /// <param name="saturation">Saturation in [0, 1)</param>
        /// <param name="value">Value in [0, 1)</param>
        /// <returns></returns>
        public static Color HsvToRgb(double hue, double saturation, double value)
        {
            double chroma = value * saturation;
            double hPrime = hue / 60.0;
            double blend = chroma * (1 - Math.Abs(hPrime % 2 - 1));
            double m = value - chroma;

            int C = (int)(255 * (chroma + m));
            int B = (int)(255 * (blend + m));
            int other = (int)(255 * m);

            Color result;
            int sequence = (int)Math.Floor(hPrime);
            //int chromaIndex = (int)Math.Floor((sequence + 1) / 2.0) % 3;
            //int blendIndex = (4 - (sequence % 3)) % 3;
            switch (sequence)
            {
                case 0:
                    result = Color.FromArgb(C, B, other);
                    break;
                case 1:
                    result = Color.FromArgb(B, C, other);
                    break;
                case 2:
                    result = Color.FromArgb(other, C, B);
                    break;
                case 3:
                    result = Color.FromArgb(other, B, C);
                    break;
                case 4:
                    result = Color.FromArgb(B, other, C);
                    break;
                case 5:
                    result = Color.FromArgb(C, other, B);
                    break;
                default:
                    result = Color.Black;
                    break;
            }

            return result;
        }
    }
}
