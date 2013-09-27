using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using Microsoft.VisualBasic;

namespace TowerAnimator
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Holds every frame of an animation.
        /// </summary>
        List<Grid> grids = new List<Grid>();

        Color firstColor = Color.White;
        Color secondColor = Color.Black;
        Color thirdColor = Color.Blue;
        PictureBox selectedMouseTool;

        /// <summary>
        /// Holds the most recently copied frame.
        /// </summary>
        Grid clipBoardFrame;

        /// <summary>
        /// Holds the starting time of every frame of an animation.
        /// </summary>
        List<TimeSpan> frameStarts = new List<TimeSpan>();

        const string timeOutputFormat = @"mm\:ss\.fff";
        readonly string[] timeInputFormats = {
            @"m\:s\.f", @"m\:s\.ff", @"m\:s\.fff",
            @"m\:s",
            @"s\.f", @"s\.ff", @"s\.fff",
            @"%s",
            @"\.%f", @"\.ff", @"\.fff"
        };

        /// <summary>
        /// The currently selected frame of the animation.
        /// </summary>
        int gridNum = 0;

        /// <summary>
        /// The number of rows of lights.
        /// </summary>
        int numRows = 10;

        /// <summary>
        /// The number of columns of lights.
        /// </summary>
        int numCols = 4;

        ColorWheel colorWheel = new ColorWheel(64);
        ColorPalette palette = new ColorPalette(16, 18);

        const double fileVersion = 0.3;

        /// <summary>
        /// The filename to use when saving the current file.
        /// Set after a file is opened or first saved.
        /// </summary>
        string lastSaveFilename = "";

        /// <summary>
        /// The filename to use when no filename has been selected.
        /// </summary>
        const string untiledFilename = "Untitled";

        /// <summary>
        /// The name of the program, initally extracted from the title of the form.
        /// </summary>
        readonly string programName;

        /// <summary>
        /// The file type(s) recognized by the program, which can be opened or saved.
        /// </summary>
        const string openSaveFilter = "Tower Animator File (*.tan)|*.tan";

        /// <summary>
        /// The audio file type(s) recognized by the program, which can be opened.
        /// </summary>
        const string audioSelectFilter = "Wave File (*.wav)|*.wav";

        /// <summary>
        /// True if the current file has been modified since its last open or save.
        /// </summary>
        bool fileModified = false;

        /// <summary>
        /// Defines the interface to an audio playback device.
        /// </summary>
        IWavePlayer waveOutDevice;

        /// <summary>
        /// Stores the audio sample.
        /// </summary>
        WaveChannel32 waveOutputStream;

        /// <summary>
        /// True while playing an animation.
        /// </summary>
        bool animating = false;
        bool Animating
        {
            get { return animating; }
            set
            {
                animating = value;
                if (Animating)
                {
                    playPauseButton.Image = Properties.Resources.PauseHS;
                }
                else
                {
                    playPauseButton.Image = Properties.Resources.PlayHS;
                    StopAudio();
                }
            }
        }

        const double windowInflatePercent = .25;

        /// <summary>
        /// Determines how many pixels tall/wide each window tile should be.
        /// </summary>
        int pixelsPerTile
        {
            get
            {
                int width = pictureBox.Width / numCols;
                int height = pictureBox.Height / numRows;
                return Math.Min(width, height);
            }
        }

        public MainForm()
        {
            InitializeComponent();

            programName = Text;

            grids.Add(new Grid(numRows, numCols));
            frameStarts.Add(TimeSpan.ParseExact(frameStartPicker.Text, timeInputFormats, null));

            try
            {
                waveOutDevice = new NAudio.Wave.DirectSoundOut();
            }
            catch (Exception driverCreateException)
            {
                MessageBox.Show(driverCreateException.Message);
            }

            previousFrameToolTip.SetToolTip(previousFrameButton, "Previous frame (Alt+Left)");
            nextFrameToolTip.SetToolTip(nextFrameButton, "Next frame (Alt+Right)");
            secondsPerFrameToolTip.SetToolTip(frameStartPicker, "Set the start time of the current frame (Alt+Up/Down).\nIncrement/decrement amount specified in the box below.");

            UpdateTitleText();

            colorWheelPictureBox.Image = colorWheel.GetImage();
            colorPalettePictureBox.Image = palette.GetImage();

            selectedMouseTool = firstColorPictureBox;

            SetFrameStartDeltaPickerEntries();
            SetFrameStartPickerEntries();
        }


        // This is where edits need to happen for Kibbie dome lights and compile those animations.
        // so far I've runn into the issue of the other side of the grid not being drawn. What I've
        // changed is that instead of the whole 4 x 10 grid being drawn just column and row 0 get drawn
        // along with numRows and numCols. Unfourtunatly numRows and numCols isn't working, I don't know why
        // but the values getting passed in are larger than the displayed grid ( 0 -> 3 is the true value but
        // the numRows and numCols operate on the 1 -> 4 method ) SO that means FOR NOW this will be hard coded
        // I will also add a TODO to get this fixed. -Jordan Lynn 09/25/2013
        private void DrawGrid(Graphics g)
        {
            Grid currentGrid;
            for (int row = 0; row < numRows; ++row)
            {
                for (int col = 0; col < numCols; ++col)
                {
                    if (col == 0 || col == 3 || row == 0 || row == 9)
                    {
                        Rectangle tile = new Rectangle(col * pixelsPerTile, row * pixelsPerTile, pixelsPerTile, pixelsPerTile);
                        currentGrid = grids[gridNum];

                        g.FillRectangle(new SolidBrush(Color.FromArgb(170, 123, 123)), tile); // TODO: make variable for hard-coded brick color
                        tile.Inflate((int)(-pixelsPerTile * windowInflatePercent), (int)(-pixelsPerTile * windowInflatePercent));
                        g.FillRectangle(new SolidBrush(currentGrid.Get(row, col)), tile);
                    }
                }
            }
        }

        private void DrawFrameSliderGrids(Graphics g)
        {
            int tileSize = frameSliderPictureBox.Height / numRows;
            int xDelta = frameSliderPictureBox.Width / 7;
            int gap = xDelta - tileSize * numCols;

            int firstGrid = Math.Max(0, gridNum - 3);
            int lastGrid = Math.Min(grids.Count - 1, gridNum + 3);
            for (int grid = firstGrid; grid <= lastGrid; ++grid)
            {
                int xStart = (grid - (gridNum - 3)) * xDelta + (gap / 2); // (gap / 2) provides horizontal centering
                for (int row = 0; row < numRows; ++row)
                {
                    for (int col = 0; col < numCols; ++col)
                    {
                        Rectangle tile = new Rectangle(xStart + col * tileSize, row * tileSize, tileSize, tileSize);

                        g.FillRectangle(new SolidBrush(Color.FromArgb(170, 123, 123)), tile);
                        tile.Inflate((int)(-tileSize * windowInflatePercent), (int)(-tileSize * windowInflatePercent));
                        g.FillRectangle(new SolidBrush(grids[grid].Get(row, col)), tile);
                    }
                }

                // Draw surrounding frame to highlight active frame.
                if (grid == gridNum)
                {
                    Rectangle leftBar = new Rectangle(xStart - tileSize, 0, tileSize, frameSliderPictureBox.Height);
                    Rectangle rightBar = new Rectangle(xStart + tileSize * numCols, 0, tileSize, frameSliderPictureBox.Height);

                    g.FillRectangle(Brushes.Blue, leftBar);
                    g.FillRectangle(Brushes.Blue, rightBar);
                }
            }
        }

        private void DrawColorPalette(Graphics g)
        {
            if (palette.Hovering)
            {
                Rectangle hoverBox = new Rectangle(palette.HoverCol * palette.SquarePixelWidth, palette.HoverRow * palette.SquarePixelWidth, palette.SquarePixelWidth, palette.SquarePixelWidth);
                const int outerBorderThickness = 1;
                const int innerBorderThickness = 1;
                g.FillRectangle(new SolidBrush(Color.Black), hoverBox);
                hoverBox.Inflate(-outerBorderThickness, -outerBorderThickness);
                g.FillRectangle(new SolidBrush(Color.White), hoverBox);
                hoverBox.Inflate(-innerBorderThickness, -innerBorderThickness);
                g.FillRectangle(new SolidBrush(palette.GetColor(palette.HoverRow, palette.HoverCol)), hoverBox);
            }
        }

        private void DrawColorWheel(Graphics g)
        {
            Rectangle selectionBox = new Rectangle(colorWheel.Selected, new Size(1, 1));
            g.DrawRectangle(new Pen(Color.White), selectionBox);
            selectionBox.Inflate(1, 1);
            g.DrawRectangle(new Pen(Color.Black), selectionBox);
        }

        /// <summary>
        /// Sets the state of a light.
        /// </summary>
        /// <param name="row">The row of the light.</param>
        /// <param name="col">The column of the light.</param>
        /// <param name="color">The color to set the light to.</param>
        private void ControlLight(int row, int col, Color color)
        {
            grids[gridNum].Set(row, col, color);

            MarkModified();
            pictureBox.Invalidate(new Rectangle(col * pixelsPerTile, row * pixelsPerTile, pixelsPerTile, pixelsPerTile));
            frameSliderPictureBox.Invalidate();
        }

        /// <summary>
        /// Turns all the lights off in the currently selected frame.
        /// </summary>
        private void ClearFrame()
        {
            for (int row = 0; row < numRows; ++row)
            {
                for (int col = 0; col < numCols; ++col)
                    grids[gridNum].Set(row, col, Color.Black);
            }

            MarkModified();
            pictureBox.Invalidate();
            frameSliderPictureBox.Invalidate();
        }

        /// <summary>
        /// Removes the currently selected frame.
        /// </summary>
        private void RemoveFrame()
        {
            if (grids.Count > 1)
            {
                grids.RemoveAt(gridNum);
                frameStarts.RemoveAt(gridNum);

                gridNum--;
                if (gridNum < 0)
                    gridNum = 0;
                frameSelector.Value = gridNum;
                frameSelector.Maximum--;

                MarkModified();
                pictureBox.Invalidate();
                frameSliderPictureBox.Invalidate();
            }
        }

        /// <summary>
        /// Adds a frame after the currently selected frame.
        /// <param name="duplicateCurrent">Indicates if the new frame should be a copy of the current frame.</param>
        /// </summary>
        private void AddFrameAfter(bool duplicateCurrent)
        {
            TimeSpan frameStart = TimeSpan.ParseExact(frameStartPicker.Text, timeInputFormats, null);
            TimeSpan frameStartDelta = TimeSpan.ParseExact(frameStartDeltaPicker.Text, timeInputFormats, null);
            TimeSpan sum = frameStart + frameStartDelta;
            frameStartPicker.Text = sum.ToString(timeOutputFormat);
            frameStarts.Insert(gridNum + 1, sum);

            if (duplicateCurrent)
                grids.Insert(gridNum + 1, new Grid(grids[gridNum]));
            else
                grids.Insert(gridNum + 1, new Grid(numRows, numCols));

            frameSelector.Maximum++;
            gridNum++;
            frameSelector.Value = gridNum;

            SortCurrentFrame();
            MarkModified();
            pictureBox.Invalidate();
            frameSliderPictureBox.Invalidate();
        }

        /// <summary>
        /// Begins a new animation.
        /// If an animation is already open and modified, will prompt the user to save.
        /// </summary>
        private void New()
        {
            if (AllowClose())
            {
                grids.Clear();
                frameStarts.Clear();
                grids.Add(new Grid(numRows, numCols));
                frameStarts.Add(new TimeSpan());
                gridNum = 0;

                frameStartPicker.Text = frameStarts[0].ToString(timeOutputFormat);

                frameSelector.Value = gridNum;
                frameSelector.Maximum = 0;

                lastSaveFilename = "";
                MarkUnmodified();
                pictureBox.Invalidate();
                frameSliderPictureBox.Invalidate();
            }
        }

        /// <summary>
        /// Prompts the user for a file to open.
        /// If the current file has been changed, prompts the user to save it first.
        /// </summary>
        private void Open()
        {
            if (AllowClose())
            {
                OpenFileDialog inputFileSelector = new OpenFileDialog();
                inputFileSelector.Filter = openSaveFilter;
                if (inputFileSelector.ShowDialog() == DialogResult.OK)
                {
                    OpenFile(inputFileSelector.FileName);
                }
            }
        }

        /// <summary>
        /// Immediately opens the file.
        /// </summary>
        internal void OpenFile(string path)
        {
            StreamReader file = new StreamReader(path);

            double version = Convert.ToDouble(file.ReadLine());
            if (version == fileVersion || version == 0.2)
            {
                if (version == fileVersion)
                {
                    // Mouse tool colors
                    string[] toolColors = file.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    List<int> colors = new List<int>();
                    foreach (string s in toolColors)
                        colors.Add(Convert.ToInt32(s));

                    firstColor = Color.FromArgb(colors[0], colors[1], colors[2]);
                    secondColor = Color.FromArgb(colors[3], colors[4], colors[5]);
                    thirdColor = Color.FromArgb(colors[6], colors[7], colors[8]);

                    // Color palette
                    for (int paletteRow = 0; paletteRow < palette.NumRows; ++paletteRow)
                    {
                        string[] paletteColors = file.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int paletteCol = 0; paletteCol < palette.NumColumns; ++paletteCol)
                        {
                            int R = Convert.ToInt32(paletteColors[paletteCol * 3 + 0]);
                            int G = Convert.ToInt32(paletteColors[paletteCol * 3 + 1]);
                            int B = Convert.ToInt32(paletteColors[paletteCol * 3 + 2]);
                            palette.SetColor(paletteRow, paletteCol, Color.FromArgb(R, G, B));
                        }
                    }
                }

                // Animation frames
                string[] size = file.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int numGrids = Convert.ToInt32(size[0]);
                numRows = Convert.ToInt32(size[1]);
                numCols = Convert.ToInt32(size[2]);

                gridNum = 0;
                frameSelector.Value = gridNum;
                frameSelector.Maximum = numGrids - 1;

                grids = new List<Grid>(numGrids);
                frameStarts = new List<TimeSpan>(numGrids);
                double totalDuration = 0;
                for (int grid = 0; grid < numGrids; ++grid)
                {
                    if (version == fileVersion)
                    {
                        frameStarts.Add(TimeSpan.ParseExact(file.ReadLine(), timeInputFormats, null));
                    }
                    else
                    {
                        double duration = Convert.ToDouble(file.ReadLine());
                        TimeSpan startTime = new TimeSpan(0, 0, 0, 0, (int)(totalDuration * 1000 + 0.5)); // add 0.5 to round instead of simple truncate
                        frameStarts.Add(startTime);
                        totalDuration += duration;
                    }

                    grids.Add(new Grid(numRows, numCols));
                    for (int row = 0; row < numRows; ++row)
                    {
                        string[] line = file.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int col = 0; col < numCols; ++col)
                        {
                            if (version == fileVersion)
                            {
                                int R = Convert.ToInt32(line[col * 3 + 0]);
                                int G = Convert.ToInt32(line[col * 3 + 1]);
                                int B = Convert.ToInt32(line[col * 3 + 2]);
                                grids[grid].Set(row, col, Color.FromArgb(R, G, B));
                            }
                            else
                            {
                                Color c = (line[col] == "0") ? Color.Black : Color.White;
                                grids[grid].Set(row, col, c);
                            }
                        }
                    }
                }

                frameStartPicker.Text = frameStarts[0].ToString(timeOutputFormat);

                selectedMouseTool = firstColorPictureBox;
                addToPaletteButton.BackColor = firstColor;
                UpdateRGBValues(firstColor);

                firstColorPictureBox.Invalidate();
                secondColorPictureBox.Invalidate();
                thirdColorPictureBox.Invalidate();
                colorPalettePictureBox.Invalidate();
                pictureBox.Invalidate();
                frameSliderPictureBox.Invalidate();

                lastSaveFilename = path;
                MarkUnmodified();
            }
            else
            {
                MessageBox.Show("File version could not be read.");
            }

            file.Close();
        }

        /// <summary>
        /// Saves the animation.
        /// If the animation has been saved before, it uses the previous filename.
        /// Otherwise the user is prompted with a "save as".
        /// Returns true if the file was actually saved.
        /// </summary>
        private bool Save()
        {
            if (lastSaveFilename.Length > 0)
            {
                Save(lastSaveFilename);
                return true;
            }
            else
            {
                return SaveAs();
            }
        }

        /// <summary>
        /// Saves the animation to a file.
        /// </summary>
        /// <param name="path">The filename and path to save to.</param>
        private void Save(string path)
        {
            StreamWriter file = new StreamWriter(path);

            file.WriteLine(fileVersion);

            // Mouse tool colors
            file.Write("{0} {1} {2} ", firstColor.R, firstColor.G, firstColor.B);
            file.Write("{0} {1} {2} ", secondColor.R, secondColor.G, secondColor.B);
            file.Write("{0} {1} {2} ", thirdColor.R, thirdColor.G, thirdColor.B);
            file.WriteLine();

            // Color Palette
            for (int paletteRow = 0; paletteRow < palette.NumRows; ++paletteRow)
            {
                for (int paletteCol = 0; paletteCol < palette.NumColumns; ++paletteCol)
                {
                    Color c = palette.GetColor(paletteRow, paletteCol);
                    file.Write("{0} {1} {2} ", c.R, c.G, c.B);
                }
                file.WriteLine();
            }

            // Animation frames
            file.WriteLine("{0:d} {1:d} {2:d}", grids.Count, numRows, numCols);
            for (int grid = 0; grid < grids.Count; ++grid)
            {
                string startTime = frameStarts[grid].ToString(timeOutputFormat);
                file.WriteLine(startTime);
                for (int row = 0; row < numRows; ++row)
                {
                    for (int col = 0; col < numCols; ++col)
                    {
                        Color c = grids[grid].Get(row, col);
                        file.Write("{0} {1} {2} ", c.R, c.G, c.B);
                    }
                    file.WriteLine();
                }
            }

            file.Close();

            lastSaveFilename = path;
            MarkUnmodified();
        }

        /// <summary>
        /// Prompts the user unconditionally to save the file.
        /// Returns true if the file was actually saved.
        /// </summary>
        private bool SaveAs()
        {
            SaveFileDialog outputFileSelector = new SaveFileDialog();
            outputFileSelector.Filter = openSaveFilter;
            if (outputFileSelector.ShowDialog() == DialogResult.OK)
            {
                Save(outputFileSelector.FileName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Prompts user to save current file before closing it if file has been modified.
        /// Returns true if the user has chosen option(s) that allows the file to be closed.
        /// </summary>
        private bool AllowClose()
        {
            bool close = true;
            if (fileModified)
            {
                string documentName = lastSaveFilename;
                if (documentName.Length == 0)
                    documentName = untiledFilename;
                DialogResult buttonPressed = MessageBox.Show("Do you want to save changes to " + documentName + "?", "Verify File Close", MessageBoxButtons.YesNoCancel);
                switch (buttonPressed)
                {
                    case DialogResult.Yes:
                        if (!Save())
                            close = false;
                        break;
                    case DialogResult.No:
                        // do nothing (allow close)
                        break;
                    case DialogResult.Cancel:
                        close = false;
                        break;
                    default:
                        throw new NotImplementedException("Unexpected dialog result.");
                }
            }
            return close;
        }

        /// <summary>
        /// Marks the file as modified and updates the title bar text if necessary.
        /// </summary>
        private void MarkModified()
        {
            if (!fileModified)
            {
                fileModified = true;
                UpdateTitleText();
            }
        }

        /// <summary>
        /// Marks the file as not modified and updates the title bar text if necessary.
        /// </summary>
        private void MarkUnmodified()
        {
            fileModified = false;
            UpdateTitleText();
        }

        /// <summary>
        /// Updates the title bar text.
        /// Shows the current filename and mentions if it is modified.
        /// </summary>
        private void UpdateTitleText()
        {
            if (lastSaveFilename.Length > 0)
                Text = System.IO.Path.GetFileName(lastSaveFilename);
            else
                Text = untiledFilename;

            if (fileModified)
                Text += " - Modified";
            Text += " - " + programName;
        }

        /// <summary>
        /// Plays out the animation in real time.
        /// Designed to run in a thread other than the GUI thread.
        /// </summary>
        private void PlayFromHere()
        {
            Animating = true;

            PlayAudio();

            Invoke(new MethodInvoker(() =>
            {
                foreach (Control ctrl in Controls)
                    ctrl.Enabled = false;
                stopButton.Enabled = true;
                playPauseButton.Enabled = true;
            }));

            int frame = gridNum;
            DateTime beginTime = DateTime.Now - frameStarts[frame]; // Playback begins at the current frame time.
            while (Animating)
            {
                TimeSpan elapsed = DateTime.Now - beginTime;
                if (elapsed >= frameStarts[frame])
                {
                    Invoke(new MethodInvoker(() => { if (Animating) frameSelector.Value = frame; }));

                    if (frame == grids.Count - 1)
                    {
                        Animating = false;
                    }
                    else
                    {
                        ++frame;
                    }
                }
                else
                {
                    int sleepMilliseconds = (int)(frameStarts[frame] - elapsed).TotalMilliseconds;
                    Thread.Sleep(Math.Min(10, sleepMilliseconds));
                }
            }

            Invoke(new MethodInvoker(() =>
            {
                foreach (Control ctrl in Controls)
                    ctrl.Enabled = true;
            }));
        }

        /// <summary>
        /// Plays the currently loaded audio file.
        /// </summary>
        private void PlayAudio()
        {
            if (waveOutputStream != null)
            {
                // seek to the correct position in the audio stream
                waveOutputStream.Position = 0;
                double seconds = frameStarts[gridNum].TotalSeconds;
                double bytesPerSec = waveOutputStream.Length / waveOutputStream.TotalTime.TotalSeconds;
                waveOutputStream.Position = Convert.ToInt64(seconds * bytesPerSec);

                try
                {
                    waveOutDevice.Init(waveOutputStream);
                }
                catch (Exception initException)
                {
                    MessageBox.Show(initException.Message, "Error Initializing Audio Output");
                    return;
                }

                waveOutDevice.Play();
            }
        }

        /// <summary>
        /// Stops any currently playing audio stream.
        /// </summary>
        private void StopAudio()
        {
            waveOutDevice.Stop();
        }

        private void InvertFrame()
        {
            grids[gridNum].Invert();

            MarkModified();
            pictureBox.Invalidate();
            frameSliderPictureBox.Invalidate();
        }

        /// <summary>
        /// Opens a file chooser for the user to select an audio file.
        /// </summary>
        private void SelectAudio()
        {
            OpenFileDialog audioFileSelector = new OpenFileDialog();
            audioFileSelector.Title = "Select Audio File";
            audioFileSelector.Filter = audioSelectFilter;
            if (audioFileSelector.ShowDialog() == DialogResult.OK)
            {
                waveOutputStream = CreateInputStream(audioFileSelector.FileName);
            }
        }

        /// <summary>
        /// Creates a wave stream from the given file.
        /// </summary>
        /// <param name="fileName">A WAVE or MP3 file.</param>
        private WaveChannel32 CreateInputStream(string fileName)
        {
            WaveChannel32 inputStream;
            if (fileName.EndsWith(".wav"))
            {
                WaveStream readerStream = new WaveFileReader(fileName);
                if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
                {
                    readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                    readerStream = new BlockAlignReductionStream(readerStream);
                }
                if (readerStream.WaveFormat.BitsPerSample != 16)
                {
                    var format = new WaveFormat(readerStream.WaveFormat.SampleRate, 16, readerStream.WaveFormat.Channels);
                    readerStream = new WaveFormatConversionStream(format, readerStream);
                }
                inputStream = new WaveChannel32(readerStream);
            }
            else if (fileName.EndsWith(".mp3"))
            {
                WaveStream mp3Reader = new Mp3FileReader(fileName);
                WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(mp3Reader);
                WaveStream blockAlignedStream = new BlockAlignReductionStream(pcmStream);
                inputStream = new WaveChannel32(blockAlignedStream);
            }
            else
            {
                throw new InvalidOperationException("Unsupported audio extension");
            }

            return inputStream;
        }

        private void AdvanceFrame(int direction)
        {
            if (direction < 0 && gridNum > 0)
                gridNum--;
            else if (direction > 0 && gridNum < grids.Count - 1)
                gridNum++;

            frameSelector.Value = gridNum;
        }

        private void DrawMouseToolPictureBox(PictureBox box, Color color, Graphics g)
        {
            Rectangle boxRect = new Rectangle(0, 0, box.Width, box.Height);
            g.FillRectangle(new SolidBrush(color), boxRect);
            if (box == selectedMouseTool)
            {
                Rectangle selectionBox = new Rectangle(0, 0, box.Width - 1, box.Height - 1);
                g.DrawRectangle(new Pen(Color.Black), selectionBox);
                selectionBox.Inflate(-1, -1);
                g.DrawRectangle(new Pen(Color.White), selectionBox);
            }
        }

        private bool TimeSpanFromString(string input, out TimeSpan result)
        {
            if (TimeSpan.TryParseExact(input, timeInputFormats, null, out result))
            {
                return true;
            }
            else
            {
                SystemSounds.Asterisk.Play();
                return false;
            }
        }

        private void SetFrameStartPickerEntries()
        {
            TimeSpan current = TimeSpan.ParseExact(frameStartPicker.Text, timeInputFormats, null);
            TimeSpan delta = TimeSpan.ParseExact(frameStartDeltaPicker.Text, timeInputFormats, null);
            
            TimeSpan low = current - delta;
            TimeSpan high = current + delta;

            frameStartPicker.Items.Clear();
            frameStartPicker.Items.Add(high.ToString(timeOutputFormat));
            frameStartPicker.Items.Add(current.ToString(timeOutputFormat));
            if (low >= TimeSpan.Zero)
                frameStartPicker.Items.Add(low.ToString(timeOutputFormat));

            frameStartPicker.SelectedIndex = 1;
        }

        private void SetFrameStartDeltaPickerEntries()
        {
            TimeSpan current = TimeSpan.ParseExact(frameStartDeltaPicker.Text, timeInputFormats, null);
            TimeSpan delta = new TimeSpan(0, 0, 0, 0, 125);

            TimeSpan low = current - delta;
            TimeSpan high = current + delta;

            frameStartDeltaPicker.Items.Clear();
            frameStartDeltaPicker.Items.Add(high.ToString(timeOutputFormat));
            frameStartDeltaPicker.Items.Add(current.ToString(timeOutputFormat));
            if (low >= TimeSpan.Zero)
                frameStartDeltaPicker.Items.Add(low.ToString(timeOutputFormat));

            frameStartDeltaPicker.SelectedIndex = 1;
        }

        private void SortCurrentFrame()
        {
            TimeSpan current = frameStarts[gridNum];
            Grid currentFrame = grids[gridNum];
            
            while (gridNum + 1 < frameStarts.Count && current > frameStarts[gridNum + 1])
            {
                frameStarts[gridNum] = frameStarts[gridNum + 1];
                grids[gridNum] = grids[gridNum + 1];
                
                frameStarts[gridNum + 1] = current;
                grids[gridNum + 1] = currentFrame;

                ++frameSelector.Value;
            }

            while (gridNum - 1 >= 0 && current < frameStarts[gridNum - 1])
            {
                frameStarts[gridNum] = frameStarts[gridNum - 1];
                grids[gridNum] = grids[gridNum - 1];

                frameStarts[gridNum - 1] = current;
                grids[gridNum - 1] = currentFrame;

                --frameSelector.Value;
            }
        }

        private void UpdateColorFromRGB()
        {
            Color c = Color.FromArgb(
                Convert.ToInt32(redSpinner.Value),
                Convert.ToInt32(greenSpinner.Value),
                Convert.ToInt32(blueSpinner.Value));

            if (selectedMouseTool == firstColorPictureBox)
                firstColor = c;
            else if (selectedMouseTool == secondColorPictureBox)
                secondColor = c;
            else
                thirdColor = c;

            addToPaletteButton.BackColor = c;
            selectedMouseTool.Invalidate();
        }
        
        private void UpdateRGBValues(Color selectedColor)
        {
            redSpinner.Value = selectedColor.R;
            greenSpinner.Value = selectedColor.G;
            blueSpinner.Value = selectedColor.B;
        }
    }
}
