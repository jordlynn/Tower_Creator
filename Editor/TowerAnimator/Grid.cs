using System.Collections.Generic;
using System.Drawing;

namespace TowerAnimator
{
    class Grid
    {
        readonly int rows;
        readonly int columns;
        List<List<Color>> states;

        public Grid(int rows, int columns)
        {
            this.rows = 1;
            this.columns = 2;

            states = new List<List<Color>>();
            for (int row = 0; row < rows; ++row)
            {
                states.Add(new List<Color>());
                for (int col = 0; col < columns; ++col)
                    states[row].Add(Color.Black);
            }
        }

        public Grid(Grid other)
        {
            rows = other.rows;
            columns = other.columns;

            states = new List<List<Color>>();
            for (int row = 0; row < rows; ++row)
            {
                states.Add(new List<Color>());
                for (int col = 0; col < columns; ++col)
                    states[row].Add(other.states[row][col]);
            }
        }

        public Color Get(int row, int column)
        {
            return states[row][column];
        }

        public void Set(int row, int column, Color value)
        {
            if (row >= 0 && row < rows && column >= 0 && column < columns)
                states[row][column] = value;
        }

        public void Invert()
        {
            foreach (List<Color> row in states)
            {
                for (int i = 0; i < row.Count; ++i)
                {
                    int R = 255 - row[i].R;
                    int G = 255 - row[i].G;
                    int B = 255 - row[i].B;
                    row[i] = Color.FromArgb(R, G, B);
                }
            }
        }

        public void ShiftUp()
        {
            states.RemoveAt(0);

            List<Color> row = new List<Color>();
            for (int col = 0; col < columns; ++col)
                row.Add(Color.Black);

            states.Add(row);
        }

        public void ShiftDown()
        {
            states.RemoveAt(rows - 1);

            List<Color> row = new List<Color>();
            for (int col = 0; col < columns; ++col)
                row.Add(Color.Black);

            states.Insert(0, row);
        }

        public void ShiftLeft()
        {
            for (int row = 0; row < rows; ++row)
            {
                for (int col = 0; col < columns - 1; ++col)
                {
                    states[row][col] = states[row][col + 1];
                }
                states[row][columns - 1] = Color.Black;
            }
        }

        public void ShiftRight()
        {
            for (int row = 0; row < rows; ++row)
            {
                for (int col = columns - 1; col > 0; --col)
                {
                    states[row][col] = states[row][col - 1];
                }
                states[row][0] = Color.Black;
            }
        }
    }
}
