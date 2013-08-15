// -----------------------------------------------------------------------
// <copyright file="Tween.cs" company="">
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
    /// Represents an inbetween transition
    /// </summary>
    [Serializable]
    public class LinearTween : ITween
    {
        public List<KeyFrame> GenerateInbetweenFrames(KeyFrame firstKeyFrame, KeyFrame secondKeyFrame)
        {
            List<KeyFrame> newFrames = new List<KeyFrame>(this.GeneratedFrameCount);
            double interval = (secondKeyFrame.StartTime - firstKeyFrame.StartTime).TotalMilliseconds;

            for (int index = 0; index < this.GeneratedFrameCount; index++)
            {
                TimeSpan frameStart = firstKeyFrame.StartTime + new TimeSpan(0, 0, 0, 0, (int)(interval * index));
                KeyFrame newFrame = new KeyFrame(secondKeyFrame.RowCount, secondKeyFrame.ColumnCount, frameStart, null);
                for (int row = 0; row < secondKeyFrame.RowCount; row++)
                {
                    for (int col = 0; col < secondKeyFrame.ColumnCount; col++)
                    {
                        int redDistance = secondKeyFrame.Get(row, col).Red - firstKeyFrame.Get(row, col).Red;
                        int blueDistance = secondKeyFrame.Get(row, col).Green - firstKeyFrame.Get(row, col).Green;
                        int greenDistance = secondKeyFrame.Get(row, col).Blue - firstKeyFrame.Get(row, col).Blue;

                        newFrame.Set(row, col, new Color(
                            (byte)(redDistance * index), 
                            (byte)(greenDistance * index), 
                            (byte)(blueDistance * index)));
                    }
                }
                newFrames[index] = newFrame;
            }

            return newFrames;
        }


        public int GeneratedFrameCount
        {
            get;
            set;
        }
    }
}
