using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TowerAnimator
{
    class ColorPalette
    {
        Bitmap canvas;
        List<List<Color>> palette;

        /// <summary>
        /// The width (and height) of a color square.
        /// </summary>
        public int SquarePixelWidth { get; private set; }
        public int NumColumns { get; private set; }
        public int NumRows { get; private set; }
        public bool Hovering { get; private set; }
        public int HoverRow { get; private set; }
        public int HoverCol { get; private set; }

        public ColorPalette(int squarePixelWidth, int numColumns)
        {
            this.SquarePixelWidth = squarePixelWidth;
            this.NumColumns = numColumns;
            this.NumRows = 2;
            this.Hovering = false;
            this.HoverRow = -1;
            this.HoverCol = -1;

            GenerateSwatches();
            GenerateCanvas();
        }

        public Image GetImage()
        {
            return canvas;
        }

        public Color GetColor(int row, int col)
        {
            try
            {
                return palette[row][col];
            }
            catch (ArgumentOutOfRangeException e)
            {
                return Color.Black;
            }
        }

        /// <summary>
        /// Gets the color of the currently hovered cell.
        /// </summary>
        /// <returns></returns>
        public Color GetColor()
        {
            if (Hovering)
            {
                return palette[HoverRow][HoverCol];
            }
            else
            {
                return Color.Black;
            }
        }

        public void SetColor(int row, int col, Color c)
        {
            try
            {
                palette[row][col] = c;

                int y = row * SquarePixelWidth;
                int x = col * SquarePixelWidth;
                Graphics g = Graphics.FromImage(canvas);
                g.FillRectangle(new SolidBrush(c), x, y, SquarePixelWidth, SquarePixelWidth);
            }
            catch (ArgumentOutOfRangeException e)
            {
                // silently fail
            }
        }

        /// <summary>
        ///  Sets the color of the currently hovered cell.
        /// </summary>
        /// <param name="c"></param>
        public void SetColor(Color c)
        {
            if (Hovering)
            {
                palette[HoverRow][HoverCol] = c;

                int y = HoverRow * SquarePixelWidth;
                int x = HoverCol * SquarePixelWidth;
                Graphics g = Graphics.FromImage(canvas);
                g.FillRectangle(new SolidBrush(c), x, y, SquarePixelWidth, SquarePixelWidth);
            }
        }

        /// <summary>
        /// This function assumes the canvas has not been scaled on screen, or that (x, y) has been appropriately scaled.
        /// </summary>
        /// <param name="x">Mouse location x.</param>
        /// <param name="y">Mouse location y.</param>
        public void SetHover(int x, int y)
        {
            int row = y / SquarePixelWidth;
            int col = x / SquarePixelWidth;
            if (row < NumRows && col < NumColumns)
            {
                HoverRow = row;
                HoverCol = col;
                Hovering = true;
            }
        }

        public void UnHover()
        {
            Hovering = false;
        }

        //public void Draw(Graphics g, int x, int y)
        //{
        //    g.DrawImage(canvas, x, y);
        //}

        void GenerateSwatches()
        {
            palette = new List<List<Color>>(NumRows);
            for (int row = 0; row < NumRows; ++row)
                palette.Add(new List<Color>(NumColumns));

            // set primary 4 greys
            if (NumRows > 0 && NumColumns > 0)
                palette[0].Add(Color.Black);
            if (NumRows > 0 && NumColumns > 1)
                palette[0].Add(Color.Gray);
            if (NumRows > 1 && NumColumns > 0)
                palette[1].Add(Color.White);
            if (NumRows > 1 && NumColumns > 1)
                palette[1].Add(Color.LightGray);

            // all remaining colors are from all hues
            int remainingColumns = NumColumns - 2;
            for (int col = 0; col < remainingColumns; ++col)
            {
                double percent = ((double)col) / remainingColumns;
                double hue = 360.0 * percent;
                palette[0].Add(Utility.HsvToRgb(hue, 1.0, 1.0));
                palette[1].Add(Utility.HsvToRgb(hue, 1.0, 0.5));
            }
        }

        void GenerateCanvas()
        {
            canvas = new Bitmap(SquarePixelWidth * NumColumns, SquarePixelWidth * palette.Count);
            Graphics g = Graphics.FromImage(canvas);

            for (int row = 0; row < palette.Count; ++row)
            {
                int y = row * SquarePixelWidth;
                for (int col = 0; col < palette[row].Count; ++col)
                {
                    int x = col * SquarePixelWidth;
                    g.FillRectangle(new SolidBrush(palette[row][col]), x, y, SquarePixelWidth, SquarePixelWidth);
                }
            }
        }
    }
}
