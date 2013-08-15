using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualBasic;
using System.Diagnostics;

namespace TowerAnimator
{
    public partial class MainForm : Form
    {
        private void clearFrame_Click(object sender, EventArgs e)
        {
            ClearFrame();
        }

        private void removeFrame_Click(object sender, EventArgs e)
        {
            RemoveFrame();
        }

        private void addFrame_Click(object sender, EventArgs e)
        {
            AddFrameAfter(false);
        }

        private void addFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddFrameAfter(false);
        }

        private void frameSelector_ValueChanged(object sender, EventArgs e)
        {
            gridNum = frameSelector.Value;
            currentFrameLabel.Text = gridNum.ToString();

            TimeSpan startTime = frameStarts[gridNum];
            frameStartPicker.Text = startTime.ToString(timeOutputFormat);
            SetFrameStartPickerEntries();

            pictureBox.Invalidate();
            frameSliderPictureBox.Invalidate();
        }

        private void playPauseButton_Click(object sender, EventArgs e)
        {
            if (Animating)
            {
                Animating = false;
            }
            else
            {
                Thread animateThread = new Thread(new ThreadStart(PlayFromHere));
                animateThread.Name = "animateThread";
                animateThread.Start();
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            Animating = false;

            frameSelector.Value = 0;
        }

        private void frameStartPicker_Validating(object sender, CancelEventArgs e)
        {
            TimeSpan startTime;
            if (TimeSpanFromString(frameStartPicker.Text, out startTime))
            {
                frameStarts[gridNum] = startTime;

                SortCurrentFrame();
                SetFrameStartPickerEntries();
                MarkModified();
            }
            else
            {
                frameStartPicker.Text = frameStarts[gridNum].ToString(timeOutputFormat);
            }
        }

        private void frameStartPicker_TextChanged(object sender, EventArgs e)
        {
            // Only handle a change due to a scroll event
            if (frameStartPicker.SelectedIndex != -1)
            {
                SetFrameStartPickerEntries();
                MarkModified();
            }
        }

        private void frameStartDeltaPicker_Validating(object sender, CancelEventArgs e)
        {
            TimeSpan deltaTime;
            if (!TimeSpanFromString(frameStartDeltaPicker.Text, out deltaTime))
            {
                frameStartDeltaPicker.Text = "0.500"; // TODO: bad hardcode
            }

            SetFrameStartDeltaPickerEntries();
            SetFrameStartPickerEntries();
        }

        private void frameStartDeltaPicker_TextChanged(object sender, EventArgs e)
        {
            // Only handle a change due to a scroll event
            if (frameStartDeltaPicker.SelectedIndex != -1)
            {
                SetFrameStartDeltaPickerEntries();
                SetFrameStartPickerEntries();
            }
        }

        private void incrementTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            --frameStartPicker.SelectedIndex;
        }

        private void decrementTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ++frameStartPicker.SelectedIndex;
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            pictureBox_MouseMove(sender, e);
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            int pixelRow = e.Y;
            int pixelCol = e.X;

            int pixelsPerTileLocal = pixelsPerTile; // cache property
            int row = pixelRow / pixelsPerTileLocal;
            int col = pixelCol / pixelsPerTileLocal;

            double pixelRowMin = (row + windowInflatePercent) * pixelsPerTileLocal;
            double pixelRowMax = (row + 1 - windowInflatePercent) * pixelsPerTileLocal;
            double pixelColMin = (col + windowInflatePercent) * pixelsPerTileLocal;
            double pixelColMax = (col + 1 - windowInflatePercent) * pixelsPerTileLocal;

            if (pixelRow < pixelRowMin || pixelRow > pixelRowMax || pixelCol < pixelColMin || pixelCol > pixelColMax)
                return;

            switch (e.Button)
            {
                case MouseButtons.Left:
                    ControlLight(row, col, firstColor);
                    break;
                case MouseButtons.Middle:
                    ControlLight(row, col, secondColor);
                    break;
                case MouseButtons.Right:
                    ControlLight(row, col, thirdColor);
                    break;
                case MouseButtons.XButton1:
                    ControlLight(row, col, Color.Black);
                    break;
                case MouseButtons.XButton2:
                    ControlLight(row, col, Color.White);
                    break;
            }
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawGrid(e.Graphics);
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            pictureBox.Invalidate();
        }

        private void frameSliderPictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawFrameSliderGrids(e.Graphics);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            New();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Animating || !AllowClose())
                e.Cancel = true;
        }

        private void invertFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InvertFrame();
        }

        private void selectAudioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectAudio();
        }

        private void upToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grids[gridNum].ShiftUp();
            pictureBox.Invalidate();
            frameSliderPictureBox.Invalidate();
            MarkModified();
        }

        private void downToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grids[gridNum].ShiftDown();
            pictureBox.Invalidate();
            frameSliderPictureBox.Invalidate();
            MarkModified();
        }

        private void leftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grids[gridNum].ShiftLeft();
            pictureBox.Invalidate();
            frameSliderPictureBox.Invalidate();
            MarkModified();
        }

        private void rightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grids[gridNum].ShiftRight();
            pictureBox.Invalidate();
            frameSliderPictureBox.Invalidate();
            MarkModified();
        }

        private void previousFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AdvanceFrame(-1);
        }

        private void nextFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AdvanceFrame(1);
        }

        private void previousFrameButton_Click(object sender, EventArgs e)
        {
            AdvanceFrame(-1);
        }

        private void nextFrameButton_Click(object sender, EventArgs e)
        {
            AdvanceFrame(1);
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clipBoardFrame = new Grid(grids[gridNum]);

            pasteToolStripMenuItem.Enabled = true;
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (clipBoardFrame != null)
            {
                grids[gridNum] = new Grid(clipBoardFrame);

                pictureBox.Invalidate();
                frameSliderPictureBox.Invalidate();
                MarkModified();
            }
        }

        private void frameSelector_MouseDown(object sender, MouseEventArgs e)
        {
            int margin = 12;
            double selectionPercent = ((double)e.X - margin) / ((double)frameSelector.Width - 2 * margin);
            selectionPercent = Math.Max(selectionPercent, 0);
            selectionPercent = Math.Min(selectionPercent, 1);

            int range = frameSelector.Maximum - frameSelector.Minimum;
            frameSelector.Value = Convert.ToInt32(selectionPercent * range);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About aboutWindow = new About();
            aboutWindow.SetVersion(fileVersion);
            aboutWindow.ShowDialog(this);
        }

        private void tipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string message = "Use an audio visualization tool like Audacity to get a precise tempo." +
                             " Editing the tempo as you go is much easier than editing it afterwards." +
                             " See the website for directions on submitting animations.";
            MessageBox.Show(message, "Tips");
        }

        private void duplicateButton_Click(object sender, EventArgs e)
        {
            AddFrameAfter(true);
        }

        private void colorPalettePictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawColorPalette(e.Graphics);
        }

        private void colorPalettePictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            palette.SetHover(e.Location.X, e.Location.Y);
            colorPalettePictureBox.Invalidate();
        }

        private void colorPalettePictureBox_MouseLeave(object sender, EventArgs e)
        {
            palette.UnHover();
            colorPalettePictureBox.Invalidate();
        }

        private void colorWheelPictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawColorWheel(e.Graphics);
        }

        private void colorWheelPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                colorWheel.SetSelected(e.Location);

                // TODO: set color of appropriate mouse tool in a better way
                Color selectedColor = colorWheel.GetColor(colorWheel.Selected.X, colorWheel.Selected.Y);
                if (selectedMouseTool == firstColorPictureBox)
                    firstColor = selectedColor;
                else if (selectedMouseTool == secondColorPictureBox)
                    secondColor = selectedColor;
                else if (selectedMouseTool == thirdColorPictureBox)
                    thirdColor = selectedColor;

                addToPaletteButton.BackColor = selectedColor;
                UpdateRGBValues(selectedColor);

                colorWheelPictureBox.Invalidate();
                selectedMouseTool.Invalidate();

                MarkModified();
            }
        }

        private void redSpinner_ValueChanged(object sender, EventArgs e)
        {
            UpdateColorFromRGB();
        }

        private void greenSpinner_ValueChanged(object sender, EventArgs e)
        {
            UpdateColorFromRGB();
        }

        private void blueSpinner_ValueChanged(object sender, EventArgs e)
        {
            UpdateColorFromRGB();
        }

        private void firstColorPictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawMouseToolPictureBox(firstColorPictureBox, firstColor, e.Graphics);
        }

        private void secondColorPictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawMouseToolPictureBox(secondColorPictureBox, secondColor, e.Graphics);
        }

        private void thirdColorPictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawMouseToolPictureBox(thirdColorPictureBox, thirdColor, e.Graphics);
        }

        private void firstColorPictureBox_Click(object sender, EventArgs e)
        {
            selectedMouseTool.Invalidate();
            selectedMouseTool = firstColorPictureBox;
            firstColorPictureBox.Invalidate();
            addToPaletteButton.BackColor = firstColor;
            UpdateRGBValues(firstColor);
        }

        private void secondColorPictureBox_Click(object sender, EventArgs e)
        {
            selectedMouseTool.Invalidate();
            selectedMouseTool = secondColorPictureBox;
            secondColorPictureBox.Invalidate();
            addToPaletteButton.BackColor = secondColor;
            UpdateRGBValues(secondColor);
        }

        private void thirdColorPictureBox_Click(object sender, EventArgs e)
        {
            selectedMouseTool.Invalidate();
            selectedMouseTool = thirdColorPictureBox;
            thirdColorPictureBox.Invalidate();
            addToPaletteButton.BackColor = thirdColor;
            UpdateRGBValues(thirdColor);
        }

        private void addToPaletteButton_MouseClick(object sender, MouseEventArgs e)
        {
            if (addToPaletteButton.FlatStyle == FlatStyle.Flat)
            {
                addToPaletteButton.FlatStyle = FlatStyle.Standard;
            }
            else
            {
                addToPaletteButton.FlatStyle = FlatStyle.Flat;
            }
        }

        private void colorPalettePictureBox_Click(object sender, EventArgs e)
        {
            // TODO: Should use independent variable to track this state (in future, appearance may change, but state will mean same thing).
            if (addToPaletteButton.FlatStyle == FlatStyle.Flat)
            {
                // Add color to palette
                palette.SetColor(addToPaletteButton.BackColor);
                addToPaletteButton.FlatStyle = FlatStyle.Standard;
                colorPalettePictureBox.Invalidate();
            }
            else
            {
                // Change to selected color
                Color selectedColor = palette.GetColor();

                // TODO: set color of appropriate mouse tool in a better way
                if (selectedMouseTool == firstColorPictureBox)
                    firstColor = selectedColor;
                else if (selectedMouseTool == secondColorPictureBox)
                    secondColor = selectedColor;
                else if (selectedMouseTool == thirdColorPictureBox)
                    thirdColor = selectedColor;

                addToPaletteButton.BackColor = selectedColor;
                UpdateRGBValues(selectedColor);

                selectedMouseTool.Invalidate();
            }

            MarkModified();
        }
    }
}
