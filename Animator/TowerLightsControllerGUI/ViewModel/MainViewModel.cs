// -----------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace TowerLightsControllerGUI.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Input;
    using AnimationModels;
    using AnimatorClient;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;
    using GalaSoft.MvvmLight.Messaging;
    using Microsoft.Win32;

    /// <summary>
    /// The state that the playing mechanism is in 
    /// </summary>
    public enum PlayingStatus
    {
        Playing,
        Stopped,
        Paused
    }

    /// <summary>
    /// 
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private AnimatorClient animatorClient;
        private KeyFrame frameShowing;
        private Animation selectedAnimation;
        private bool isBusy;
        private PlayingStatus playingStatus;
        private int currentFrameIndex;
        private int serverCOMPort;
        private bool isServerSeriallyConnected;

        /// <summary>
        /// 
        /// </summary>
        public MainViewModel()
        {
            this.Animations = new ObservableCollection<Animation>();

            this.UploadCommand = new RelayCommand(this.UploadAnimation);
            this.PlayPauseCommand = new RelayCommand(this.PlayPause);
            this.StopCommand = new RelayCommand(this.Stop);
            this.RefreshAnimationListCommand = new RelayCommand(this.RefreshAnimations);
            this.ExitCommand = new RelayCommand(this.Exit);

            this.PlayingStatus = PlayingStatus.Stopped;
            this.animatorClient = new AnimatorClient();
            
            // Retrieve these values instead of setting them to magic numbers
            this.ServerAddress = "localhost";
            this.ServerPort = 1337;
            this.ServerCOMPort = 4;
            this.currentFrameIndex = 0;
        }

        public ICommand UploadCommand { get; set; }

        public ICommand PlayPauseCommand { get; set; }

        public ICommand StopCommand { get; set; }

        public ICommand RefreshAnimationListCommand { get; set; }

        public ICommand ExitCommand { get; set; }

        /// <summary>
        /// Gets or sets the address of the server
        /// </summary>
        public string ServerAddress
        {
            get
            {
                return this.animatorClient.ServerAddress;
            }

            set
            {
                if (value != this.animatorClient.ServerAddress)
                {
                    this.animatorClient.ServerAddress = value;
                    ////this.RaisePropertyChanged("ServerAddress");
                }
            }
        }

        /// <summary>
        /// Gets or sets the port that the server is listening on
        /// </summary>
        public int ServerPort
        {
            get
            {
                return this.animatorClient.ServerPort;
            }
            set
            {
                if (value != this.animatorClient.ServerPort)
                {
                    this.animatorClient.ServerPort = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the COM port that the server connects to
        /// the microcontroller on. 
        /// </summary>
        public int ServerCOMPort
        {
            get
            {
                return this.serverCOMPort;
            }

            set
            {
                if (value != this.serverCOMPort)
                {
                    this.serverCOMPort = value;
                    // Try to change to com port
                }
            }
        }

        /// <summary>
        /// Gets the state of the server
        /// </summary>
        public string ServerState
        {
            get
            {
                try
                {
                    return this.animatorClient.GetStatus();
                }
                catch (InvalidOperationException ioe)
                {
                    MessageBox.Show(GenerateMessageFromException(ioe), "Tower Lights Controller", MessageBoxButton.OK, MessageBoxImage.Error);
                    return "Error retrieving state";
                }
            }
        }

        /// <summary>
        /// Gets or sets the serial connectivity of the server. This property
        /// is used to disconnect or reconnect to serial connection. 
        /// This should be two seperate commands. 
        /// </summary>
        public bool IsServerSeriallyConnected
        {
            get
            {
                return this.isServerSeriallyConnected;
            }

            set
            {
                if (value != this.isServerSeriallyConnected)
                {
                    this.animatorClient.SetSerialState(value);
                    this.isServerSeriallyConnected = value;
                }
            }
        }

        /// <summary>
        /// Gets the list of animations
        /// </summary>
        public ObservableCollection<Animation> Animations
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the currently selected animation. Changing
        /// the selected animation when the playing state is paused
        /// changes the state to stopped. 
        /// </summary>
        public Animation SelectedAnimation
        {
            get
            {
                return this.selectedAnimation;
            }

            set
            {
                if (value != this.selectedAnimation)
                {
                    this.selectedAnimation = value;

                    if (this.PlayingStatus == PlayingStatus.Paused)
                    {
                        this.PlayingStatus = PlayingStatus.Stopped;
                    }

                    if (this.selectedAnimation != null &&
                        this.selectedAnimation.Frames.Count > 0)
                    {
                        this.FrameShowing = this.selectedAnimation.Frames[0];
                    }
                }
            }
        }

        /// <summary>
        /// Gets the frame that is being displayed currently. 
        /// </summary>
        public KeyFrame FrameShowing
        {
            get
            {
                return this.frameShowing;
            }

            private set
            {
                this.Set<KeyFrame>("FrameShowing", ref this.frameShowing, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating that a background task is
        /// being performed, such as uploading an animation. 
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return this.isBusy;
            }

            set
            {
                if (value != this.isBusy)
                {
                    this.isBusy = value;
                    this.RaisePropertyChanged("IsBusy");
                }
            }
        }
        
        /// <summary>
        /// Gets the status of the playing mechanism. This does
        /// not necessarily reflect the playing status of the
        /// server. 
        /// </summary>
        public PlayingStatus PlayingStatus
        {
            get
            {
                return this.playingStatus;
            }

            private set
            {
                if (value != this.playingStatus)
                {
                    this.playingStatus = value;
                    this.RaisePropertyChanged("PlayingStatus");
                }
            }
        }

        /// <summary>
        /// Downloads the list of animations from the server
        /// </summary>
        private void RefreshAnimations()
        {
            this.IsBusy = true;
            List<Animation> newAnimations = null;
            BackgroundWorker bgworker = new BackgroundWorker();

            bgworker.DoWork += (sender, e) =>
                {
                    List<Animation> currentAnimations = new List<Animation>(this.Animations.AsEnumerable());

                    try
                    {
                        newAnimations = this.animatorClient.GetAnimations(currentAnimations);
                    }
                    catch (InvalidOperationException ioe)
                    {
                        MessageBox.Show(GenerateMessageFromException(ioe), "Tower Lights Controller", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

            bgworker.RunWorkerCompleted += (sender, e) =>
                {
                    if (newAnimations != null)
                    {
                        this.Animations.Clear();

                        foreach (var animation in newAnimations)
                        {
                            this.Animations.Add(animation);
                        }
                    }

                    this.IsBusy = false;
                };

            bgworker.RunWorkerAsync();
        }

        /// <summary>
        /// Creates string that the message from the exception and the inner exception if
        /// that is defined
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        private string GenerateMessageFromException(Exception exception)
        {
            string message = exception.Message;

            if (exception.InnerException != null)
            {
                message += ": " + exception.InnerException.Message;
            }

            return message;
        }

        /// <summary>
        /// Uploads an animation to the server
        /// </summary>
        private void UploadAnimation()
        {
            bool uploadFailed = false;

            this.IsBusy = true;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Animation Files (*.tan)|*.tan";
            if (openFileDialog.ShowDialog() != true)
            {
                this.IsBusy = false;
                return;
            }

            Animation newAnimation = AnimationLoader.LoadAnimationFromFile(openFileDialog.FileName);

            if (newAnimation.MusicFilename == "")
            {
                openFileDialog.Filter = "Music files (*.wav, *.mp3)|*.wav;*.mp3";
                if (openFileDialog.ShowDialog() == true)
                {
                    newAnimation.MusicFilename = openFileDialog.FileName;
                }
            }

            BackgroundWorker bgworker = new BackgroundWorker();

            bgworker.DoWork += (sender, e) =>
                {
                    try
                    {
                        this.animatorClient.UploadAnimation(newAnimation);
                    }
                    catch (InvalidOperationException ioe)
                    {
                        uploadFailed = true;
                        MessageBox.Show(this.GenerateMessageFromException(ioe), "Tower Lights Controller", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
            bgworker.RunWorkerCompleted += (sender, e) =>
                {
                    if (uploadFailed == false)
                    {
                        this.Animations.Add(newAnimation);
                    }

                    this.IsBusy = false;
                };

            bgworker.RunWorkerAsync();
        }

        /// <summary>
        /// Plays or pauses the selected animation. This function handles the 
        /// PlayPause command. 
        /// </summary>
        private void PlayPause()
        {
            if (this.PlayingStatus == PlayingStatus.Playing)
            {
                // Set the playing status flag to paused. The thread playing the animation will
                // detect the change and then stop playing the animation. 
                this.PlayingStatus = PlayingStatus.Paused;
            }
            else if (this.PlayingStatus == PlayingStatus.Stopped || this.PlayingStatus == PlayingStatus.Paused)
            {
                if (this.SelectedAnimation == null)
                {
                    this.PlayingStatus = PlayingStatus.Stopped;
                }
                else
                {
                    // start playing
                    this.PlayingStatus = PlayingStatus.Playing;
                    Thread playThread = new Thread(new ThreadStart(this.Play));
                    playThread.Name = "Tower Lights Controller Animation Thread";
                    playThread.Start();
                }
            }
        }

        /// <summary>
        /// Plays the selected animation. Designed to run in a different thread. It 
        /// will start playing at currentFrameIndex, or it will start at the beginning of 
        /// the animation. 
        /// </summary>
        private void Play()
        {
            // Get the index of the selected animation
            int animationIndex = this.Animations.IndexOf(this.SelectedAnimation);
            // The list of frames that we are playing
            List<KeyFrame> frames = this.SelectedAnimation.Frames;
            // The time that has already passed before the current frame index
            TimeSpan delta = this.currentFrameIndex < 0 ? new TimeSpan() : frames[this.currentFrameIndex].StartTime;

            // Tell the server to start playing
            try
            {
                this.animatorClient.PlayAnimation(animationIndex, delta, this.SelectedAnimation.MusicFilename);
            }
            catch(InvalidOperationException e)
            {
                this.PlayingStatus = ViewModel.PlayingStatus.Stopped;
                MessageBox.Show(GenerateMessageFromException(e), "Tower Lights Controller", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // If there is no current frame, set the current frame to the first frame. 
            if (this.currentFrameIndex < 0)
            {
                this.currentFrameIndex = 0;
            }

            Stopwatch frameStopwatch = new Stopwatch();
            frameStopwatch.Start();

            // Run this loop while there are frames left to play and the playing state hasn't 
            // been changed to something besides playing. 
            while (this.currentFrameIndex < frames.Count && this.PlayingStatus == PlayingStatus.Playing)
            {
                // Only go to the next frame if we are that frames start time. 
                // Delta is the time into the animation that it starts at (0 for playing
                // from a stopped postion, > 0 for playing from a paused position).
                if (frameStopwatch.Elapsed >= frames[this.currentFrameIndex].StartTime - delta)
                {
                    // Show the frame
                    this.FrameShowing = frames[this.currentFrameIndex];
                    this.currentFrameIndex++;
                }
                else
                {
                    // Sleep the thread for a while to stop the thread from burning up too many cycles here ...
                    Thread.Sleep(1);
                }
            }

            // After the loop quits, check the playing state to see if any 
            // further actions are needed.
            if (this.PlayingStatus == PlayingStatus.Playing)
            {
                // A normal stop, the animation is done playing so just stop. 
                // No need to send stop signal, the server will stop automatically
                this.PlayingStatus = PlayingStatus.Stopped;
                this.FrameShowing = this.SelectedAnimation.Frames[0];
                this.currentFrameIndex = -1;
            }
            else if (this.PlayingStatus == PlayingStatus.Paused)
            {
                // The loop quit because the state changed to paused, and
                // the playing needs to be paused.
                // Send pause signal to server, keep FrameShowing and currentFrameIndex as is
                try
                {
                    this.animatorClient.StopAnimation(false);
                }
                catch (InvalidOperationException ioe)
                {
                    MessageBox.Show(GenerateMessageFromException(ioe), "Tower Lights Controller", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (this.PlayingStatus == PlayingStatus.Stopped)
            {
                // The loop quit because the state changed to stopped, and
                // the playing needs to be stopped.
                // Send stop signal to server, reset FrameShowing and currentFrameIndex
                try
                {
                    this.animatorClient.StopAnimation(true);
                }
                catch (InvalidOperationException ioe)
                {
                    MessageBox.Show(GenerateMessageFromException(ioe), "Tower Lights Controller", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                this.FrameShowing = this.SelectedAnimation.Frames[0];
                this.currentFrameIndex = -1;
            }
        }

        private void Exit()
        {
            foreach (var frame in this.SelectedAnimation.Frames)
            {

            }
        }


        /// <summary>
        /// Stops the current animation
        /// </summary>
        private void Stop()
        {
            // If the playing state is paused, we need to send the stop signal
            // to the server to clear out anything that is being displayed. 
            if (this.PlayingStatus == PlayingStatus.Paused)
            {
                this.currentFrameIndex = -1;
                this.FrameShowing = this.SelectedAnimation.Frames[0];

                try
                {
                    this.animatorClient.StopAnimation(true);
                }
                catch (InvalidOperationException ioe)
                {
                    MessageBox.Show(GenerateMessageFromException(ioe), "Tower Lights Controller", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Change the state to stopped. This will also cause the 
            // playing thread to stop playing and subsequently send 
            // the stop signal to the server. 
            this.PlayingStatus = PlayingStatus.Stopped;
        }
    }
}
