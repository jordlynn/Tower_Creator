using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace AnimationModels
{

    /// <summary>
    /// Represents a frame that starts at a specified time and displays a grid of colors. 
    /// </summary>
    [Serializable]
    public class KeyFrame
    {
        private List<List<Color>> grid;

        private TimeSpan startTime;

        public KeyFrame()
            : this(0, 0, new TimeSpan(), null)
        {
        }

        public KeyFrame(int rowCount, int columnCount, TimeSpan startTime, ITween tween)
        {
            this.grid = new List<List<Color>>();

            for (int row = 0; row < rowCount; row++)
            {
                this.grid.Add(new List<Color>());
                for (int col = 0; col < columnCount; col++)
                {
                    this.grid[row].Add(new Color());
                }
            }

            this.StartTime = startTime;
            this.Tween = tween;
        }

        public List<List<Color>> Grid
        {
            get
            {
                return this.grid;
            }
        }


        public TimeSpan StartTime
        {
            get
            {
                return this.startTime;
            }
            set
            {
                if (value != this.startTime)
                {
                    this.startTime = value;
                }
            }
        }

        public int RowCount
        {
            get
            {
                return this.grid.Count;
            }
        }

        public int ColumnCount
        {
            get
            {
                return this.grid[0].Count;
            }
        }

        public void Set(int Row, int Column, Color Color)
        {
            this.grid[Row][Column] = Color;
        }

        public Color Get(int Row, int Column)
        {
            return this.grid[Row][Column];
        }

        public ITween Tween
        {
            get;
            set;
        }
    }
}
