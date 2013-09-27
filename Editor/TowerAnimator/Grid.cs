/* Grid.cs
				===== READ THIS! ====

	Originally written by I'm assuming Josh Armstrong, now maintained by Jordan Lynn.
this is currently in the works of being ported over the a *.tan file creator for the Kibbie
Dome light project. When I (Jordan Lynn) adopted this code there wasn't any comments inside
so anyting you see commented you can assume is accurate/up to date and should be followed.
DO NOT use this code for the tower light show, in it's current state it will not work. It's
been changed over to Kibbie Dome code for testing and that's our current most pressing matter.
I did comment where I made changes and how to change them back if you ever need to for the
full on tower lights show. IF YOU make any changes COMMENT explaining what you did, what it was,
how to change it back, and your reason for changing. Ultimatly this code should be built to handle
both cases but until that time we'll make due with this. Thanks!
*/

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
            this.rows = 17; // Hardcoding this in for Kibbie lights adjust back to "rows" as needed.
            this.columns = 8; // same here adjust back to "columns" as needed.

            states = new List<List<Color>>();
	/* Commenting this out it is currently tower lights code and will build a solid grid
	    we need one with a hollow middle for the kibbie lights. */
            /*
            for (int row = 0; row < rows; ++row)
            {
                states.Add(new List<Color>());
                for (int col = 0; col < columns; ++col)
                    states[row].Add(Color.Black);
            }
            */
            
			// Here I'm working on getting just a border for the grid instead of a whole rectangle,
			// I achomplish this by going through each row and col but only giving values to the 
			// ones that are either == to the begining row && col or end row && col.
            
			for( int row = 0; row < rows; ++row ){
                states.Add(new List<Color>());
				for( int col = 0; col < columns; ++col ){
                    if (col == 0 || col == columns || row == 0 || row == rows){
                        states[row].Add(Color.Black);
                    }else{
                        states[row].Add(Color.Black);
                    }
				}
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
