using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TowerAnimator
{
    class ColorWheel
    {
        public Point Selected { get; private set; }
        Bitmap wheel;
        int PixelRadius { get; set; }

        public ColorWheel(int pixelRadius)
        {
            PixelRadius = pixelRadius;
            Selected = new Point(PixelRadius, PixelRadius);
            GenerateWheel();
        }

        public Image GetImage()
        {
            return wheel;
        }

        public void Draw(Graphics g, int x, int y)
        {
            g.DrawImage(wheel, x, y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y">Expected to grow downward.</param>
        /// <returns></returns>
        public Color GetColor(int x, int y)
        {
            double xPrime = x - PixelRadius;
            double yPrime = PixelRadius - y;
            double radius = Math.Sqrt(Math.Pow(yPrime, 2) + Math.Pow(xPrime, 2));
            if (radius > PixelRadius)
                return Color.Transparent;

            double saturation = radius / PixelRadius;
            double hue = Math.Atan2(yPrime, xPrime) * 180.0 / Math.PI;
            if (hue < 0)
                hue += 360;
            return Utility.HsvToRgb(hue, saturation, 1.0);
        }

        public void SetSelected(Point p)
        {
            int xPrime = p.X - PixelRadius;
            int yPrime = PixelRadius - p.Y;
            double radius = Math.Sqrt(Math.Pow(yPrime, 2) + Math.Pow(xPrime, 2));
            if (radius <= PixelRadius)
                Selected = p;
        }

        void GenerateWheel()
        {
            wheel = new Bitmap(PixelRadius * 2, PixelRadius * 2);

            for (int row = 0; row < wheel.Height; ++row)
            {
                for (int col = 0; col < wheel.Width; ++col)
                {
                    Color pixelColor = GetColor(col, row);
                    wheel.SetPixel(col, row, pixelColor);
                }
            }
        }
    }
}
