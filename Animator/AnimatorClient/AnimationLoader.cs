// -----------------------------------------------------------------------
// <copyright file="AnimationLoader.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace AnimatorClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using AnimationModels;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class AnimationLoader
    {
        private static string[] timeInputFormats = {
                @"m\:s\.f", @"m\:s\.ff", @"m\:s\.fff",
                @"m\:s",
                @"s\.f", @"s\.ff", @"s\.fff",
                @"%s",
                @"\.%f", @"\.ff", @"\.fff"
            };

        /// <summary>
        /// Opens an animation from a file. 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Animation LoadAnimationFromFile(string fileName)
        {
            StreamReader file = new StreamReader(fileName);

            double version = Convert.ToDouble(file.ReadLine());

            if (version == 0.2)
            {
                Animation newAnimation = LoadV2AnimationFromFile(file);
                newAnimation.Name = Path.GetFileNameWithoutExtension(fileName);
                newAnimation.Author = "Unknown";
                return newAnimation;
            }
            else if (version == 0.3)
            {
                Animation newAnimation = LoadV3AnimationFromFile(file);
                newAnimation.Name = Path.GetFileNameWithoutExtension(fileName);
                newAnimation.Author = "Unknown";
                return newAnimation;
            }
            else if (version == 0.4)
            {
                return LoadV4AnimationFromFile(file);
            }
            else
            {
                throw new InvalidDataException("File version not supported");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static Animation LoadV4AnimationFromFile(StreamReader file)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            Animation newAnimation = (Animation)formatter.Deserialize(file.BaseStream);
            file.Close();
            return newAnimation;

        }

        /// <summary>
        /// Loads a v3 animation from an already open file
        /// 
        /// 2.  [toolcolor1 Red (int)] [toolcolor1 Green (int)] [toolcolor1 Blue (int)] [toolcolor2 Red (int)] [toolcolor2 Green (int)] [toolcolor2 Blue (int)] [toolcolor3 Red (int)] [toolcolor3 Green (int)] [toolcolor3 Blue (int)]
        /// 3.  [pcolor1 Red (int)] [ pcolor1 Green (int)] [pcolor1 Blue (int)] ... [pcolor9 Red (int)] [ pcolor9 Green (int)] [pcolor9 Blue (int)]
        /// 4.  [pcolor10 Red (int)] [ pcolor10 Green (int)] [pcolor10 Blue (int)] ... [pcolor18 Red (int)] [ pcolor18 Green (int)] [pcolor18 Blue (int)]
        /// 5.  [framecount] [rowcount] [columncount]
        /// 6.  [framestart (mm:ss:fff)]
        /// 7.  [(0,0) Red (byte)] [(0,0) Green (byte)] [(0,0) Blue (byte)] ... [(0,columncount) Red (byte)] [(0,columncount) Green (byte)] [(0,columncount) Blue (byte)]
        /// 8.  ...
        /// 9.  [(rowcount,0) Red (byte)] [(rowcount,0) Green (byte)] [(rowcount,0) Blue (byte)] ... [(rowcount,columncount) Red (byte)] [(rowcount,columncount) Green (byte)] [(rowcount,columncount) Blue (byte)]
        /// 10. (repeat for framecount times)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static Animation LoadV3AnimationFromFile(StreamReader file)
        {
            Animation newAnimation;

            // The first three lines have color palette information from the editor, simply read
            // and discard them. 
            string toolColors = file.ReadLine();
            string paletteRow1 = file.ReadLine();
            string paletteRow2 = file.ReadLine();

            // The next line has animation size information
            string[] size = file.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int numFrames = Convert.ToInt32(size[0]);
            int numRows = Convert.ToInt32(size[1]);
            int numCols = Convert.ToInt32(size[2]);

            // Now that we know the columncount and rowcount, create the animation class
            newAnimation = new Animation(numRows, numCols);

            // Now read in the frame information
            TimeSpan frameStartTime;

            for (int grid = 0; grid < numFrames; ++grid)
            {
                frameStartTime = TimeSpan.ParseExact(file.ReadLine(), timeInputFormats, null);
                KeyFrame newKeyFrame = new KeyFrame(numRows, numCols, frameStartTime, null);

                for (int row = 0; row < numRows; ++row)
                {
                    string[] line = file.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int col = 0; col < numCols; ++col)
                    {
                        byte R = Convert.ToByte(line[col * 3 + 0]);
                        byte G = Convert.ToByte(line[col * 3 + 1]);
                        byte B = Convert.ToByte(line[col * 3 + 2]);
                        newKeyFrame.Set(row, col, new Color(R, G, B));
                    }
                }

                newAnimation.Frames.Add(newKeyFrame);
            }

            file.Close();
            return newAnimation;
        }

        /// <summary>
        /// Loads a v2 animation file from the an already open file.
        /// 
        /// 1.  0.2
        /// 2.  [framecount] [rowcount] [columncount]
        /// 3.  [frameduration (double, seconds)]
        /// 4.  [row 0, col 0 (0 or 1)] [row 0, col 1 (0 or 1)] ... [row 0, col columncount, (0 or 1)]
        /// 5.  ...
        /// 6.  [row rowcount, col 0 (0 or 1)] [row rowcount, col 1 (0 or 1)] ... [row rowcount, col columncount, (0 or 1)]
        /// 7.  (repeat for framecount times)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static Animation LoadV2AnimationFromFile(StreamReader file)
        {
            Animation newAnimation;

            // The first line has information on the number of frames, rows and columns
            string[] size = file.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int numFrames = Convert.ToInt32(size[0]);
            int numRows = Convert.ToInt32(size[1]);
            int numCols = Convert.ToInt32(size[2]);

            // Now that we know the columncount and rowcount, create the animation class
            newAnimation = new Animation(numRows, numCols);

            // The rest of the file is frame information. Load in [numFrames] frames.
            TimeSpan frameStartTime;
            double totalDuration = 0;

            for (int grid = 0; grid < numFrames; ++grid)
            {
                // The first line is frame duration in seconds
                string durationString = file.ReadLine();
                double duration = Convert.ToDouble(durationString);

                // add 0.5 to round instead of simple truncate
                frameStartTime = new TimeSpan(0, 0, 0, 0, (int)(totalDuration * 1000 + 0.5)); 
                totalDuration += duration;

                // The next lines are grid information
                KeyFrame newKeyFrame = new KeyFrame(numRows, numCols, frameStartTime, null);
                for (int row = 0; row < numRows; ++row)
                {
                    string[] line = file.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int col = 0; col < numCols; ++col)
                    {
                        Color c = (line[col] == "0") ? new Color(0, 0, 0) : new Color(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                        newKeyFrame.Set(row, col, c);
                    }
                }

                newAnimation.Frames.Add(newKeyFrame);
            }

            file.Close();
            return newAnimation;
        }
    }
}
