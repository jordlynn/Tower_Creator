// -----------------------------------------------------------------------
// <copyright file="Color.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace AnimationModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Serializable]
    public class Color
    {
        public Color()
            : this(0, 0, 0)
        {
        }

        public Color(byte red, byte green, byte blue)
        {
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
        }

        private byte red;

        public byte Red
        {
            get { return red; }
            set { red = value; }
        }

        private byte green;

        public byte Green
        {
            get { return green; }
            set { green = value; }
        }

        private byte blue;

        public byte Blue
        {
            get { return blue; }
            set { blue = value; }
        }

    }
}
