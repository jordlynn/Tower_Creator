

namespace AnimationModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    [Serializable]
    public class Animation
    {   
        private string name;

        private string author;

        private string musicFileName;

        private List<KeyFrame> frames;

        private int columnCount;

        private int rowCount;

        public Animation(int rowCount, int columnCount)
            : this(string.Empty, string.Empty, string.Empty, rowCount, columnCount, new List<KeyFrame>())
        {
        }

        public Animation(
            string name, 
            string author, 
            string musicFilename, 
            int rowCount, 
            int columnCount, 
            List<KeyFrame> frames)
        {
            this.Name = name;
            this.Author = author;
            this.MusicFilename = musicFilename;
            this.frames = frames;
            this.RowCount = rowCount;
            this.ColumnCount = columnCount;

            // Check to make sure each key frame has the correct number of columns and rows
            // This is to help catch errors when the frame is created and not when the frame 
            // is being played. 
            foreach (var frame in frames)
            {
                if (frame.ColumnCount != this.ColumnCount)
                {
                    throw new ArgumentException("All keyframes must have " + this.ColumnCount + " columns", "Frames");
                }

                if (frame.RowCount != this.RowCount)
                {
                    throw new ArgumentException("All keyframes must have " + this.RowCount + " rows", "Frames");
                }
            }

            this.Frames = frames;
        }

        public int ColumnCount
        {
            get 
            {
                return this.columnCount; 
            }

            set
            { 
                this.columnCount = value;
            }
        }

        public int RowCount
        {
            get { return this.rowCount; }
            set { this.rowCount = value; }
        }

        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                if (value != this.name)
                {
                    this.name = value;
                }
            }
        }

        public string Author
        {
            get
            {
                return this.author;
            }

            set
            {
                if (value != this.author)
                {
                    this.author = value;
                }
            }
        }

        public List<KeyFrame> Frames
        {
            get
            {
                return this.frames;
            }

            private set
            {
                if (value != this.frames)
                {
                    this.frames = value;
                }
            }
        }

        public string MusicFilename
        {
            get
            {
                return this.musicFileName;
            }

            set
            {
                if (value != this.musicFileName)
                {
                    this.musicFileName = value;
                }
            }
        }

        public TimeSpan Duration
        {
            get
            {
                return this.Frames[this.Frames.Count - 1].StartTime;
            }
        }

        public void InsertFrame(int index, KeyFrame frame)
        {
            if (frame.ColumnCount != this.ColumnCount)
            {
                throw new ArgumentException("All keyframes must have " + this.ColumnCount + " columns", "frame");
            }

            if (frame.RowCount != this.RowCount)
            {
                throw new ArgumentException("All keyframes must have " + this.RowCount + " rows", "frame");
            }

            if (index < 0 || index >= this.Frames.Count - 1)
            {
                throw new ArgumentException("Index is out of range.", "index");
            }

            this.frames.Insert(index, frame);
        }

        public void DeleteFrame(int index)
        {
            if (index < 0 || index >= this.Frames.Count - 1)
            {
                throw new ArgumentException("Index is out of range.", "index");
            }

            this.frames.RemoveAt(index);
        }
    }
}
