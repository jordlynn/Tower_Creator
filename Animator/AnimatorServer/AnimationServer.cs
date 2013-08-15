// -----------------------------------------------------------------------
// <copyright file="Server.cs" company="">
// TODO: Update copyright text.
// </copyright>

// TODO:
// Create a mechanism to handle errors
// Prevent deletions occuring on animations that are playing
// Prevent playing while playing is already occuring
// -----------------------------------------------------------------------

namespace AnimatorServer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;
    using AnimationModels;
    using Commands;
    using serial_packet_protocol;

    [Flags]
    public enum ServerStatus
    {
        Idle,
        Uploading,
        Deleting,
        GettingStatus,
        ListingAnimations,
        PlayingAnimation,
    }

    /// <summary>
    /// Communicates with clients to upload and start playing animations
    /// </summary>
    public class AnimationServer
    {
        /// <summary>
        /// Private backing field for IsListening
        /// </summary>
        private bool isListening;

        /// <summary>
        /// Private backing field for IsSeriallyConnected
        /// </summary>
        private bool isConnectedToSerial;

        /// <summary>
        /// Private backing field for Port
        /// </summary>
        private int port;

        /// <summary>
        /// The index of the currently playing animation
        /// </summary>
        private int currentlyPlayingAnimationIndex;

        /// <summary>
        /// The current frame indexs
        /// </summary>
        private int currentFrameIndex;

        /// <summary>
        /// The thread that listens for connections from any clients
        /// </summary>
        private Thread listeningThread;

        /// <summary>
        /// Private backing field for COMPort
        /// </summary>
        private int comPort;

        /// <summary>
        /// The packet protocol used to communicate with the 
        /// microcontroller
        /// </summary>
        private packet_protocol packetProtocol;

        /// <summary>
        /// Initializes a new instance of the AnimationServer class. Sets the status to Idle.
        /// </summary>
        public AnimationServer()
        {
            this.Animations = new List<Animation>();
            this.Status = ServerStatus.Idle;
            this.listeningThread = new Thread(new ThreadStart(this.ListenForClients));
        }

        /// <summary>
        /// Spawns a thread that listens for new connections. It executes
        /// any commands and then exits the thread. If the Server is already
        /// listening, then the method does nothing. 
        /// </summary>
        private void StartListening()
        {
            listeningThread.Start();
        }

        /// <summary>
        /// This function is designed to be run in a seperator thread. It listens for a new client, 
        /// accepts the client and processes it, checks if the the server is still in listening
        /// mode, and then either exits or waits for another client. 
        /// </summary>
        private void ListenForClients()
        {
            // Create the listener
            TcpListener tcpListener = new TcpListener(IPAddress.Any, this.Port);

            try
            {
                // Start listening for clients
                tcpListener.Start();

                // Set the IsListening value to false to exit the listening thread
                while (this.IsListening == true)
                {
                    if (tcpListener.Pending() == true)
                    {
                        // Accept a client in the queue of pending clients
                        TcpClient tcpClient = tcpListener.AcceptTcpClient();

                        // Process the data from the client
                        ProcessClient(tcpClient);

                        // Close the client
                        tcpClient.Close();
                    }
                }

                // Stop listening for clients
                tcpListener.Stop();
            }
            catch (SocketException se)
            {
                // An error occured....
                this.Status = ServerStatus.Idle;
                this.IsListening = false;
            }

            this.IsListening = false;
        }

        /// <summary>
        /// Process the data from a client
        /// </summary>
        /// <param name="networkStream"></param>
        private void ProcessClient(TcpClient tcpClient)
        {
            Command recievedCommand = CommandHandler.RecieveCommand(tcpClient.GetStream());

            switch (recievedCommand)
            {
                case Command.UploadAnimation:
                    ProcessUploadAnimationCommand(tcpClient.GetStream());
                    break;
                case Command.GetAnimatorStatus:
                    ProcessGetStatusCommand(tcpClient.GetStream());
                    break;
                case Command.DeleteAnimation:
                    ProcessDeleteAnimationCommand(tcpClient.GetStream());
                    break;
                case Command.GetUploadedAnimationTitles:
                    ProcessGetTitlesCommand(tcpClient.GetStream());
                    break;
                case Command.PlayAnimation:
                    ProcessPlayCommand(tcpClient.GetStream());
                    break;
                case Command.StopAnimation:
                    ProcessStopCommand(tcpClient.GetStream());
                    break;
                case Command.GetUploadedAnimations:
                    ProcessGetUploadedAnimationsCommand(tcpClient.GetStream());
                    break;
            }
            
        }

        /// <summary>
        /// Gets the state the server is in (what its doing right now).
        /// In addition to these states, the server could be listening
        /// or not listening, and serially connected or disconnected. 
        /// </summary>
        public ServerStatus Status
        {
            private set;
            get;
        }

        /// <summary>
        /// Gets or sets the port the server listens on. 
        /// </summary>
        public int Port
        {
            get
            {
                return this.port;
            }

            set
            {
                if (this.Status != ServerStatus.Idle)
                {
                    throw new InvalidOperationException("The port cannot be set when the server is listening for connections.");
                }

                if (value != this.port)
                {
                    this.port = value;
                }
            }
        }

        /// <summary>
        /// The port that the microcontroller is on. 
        /// </summary>
        public int COMPort
        {
            get
            {
                return this.comPort;
            }

            set
            {
                if (value != this.comPort)
                {
                    this.comPort = value;
                }
            }
        }

        /// <summary>
        /// Gets the serial packet protocol com port, based on what the
        /// integer COMPort is.
        /// </summary>
        private spp_COMPorts sppCOMPort
        {
            get
            {
                return (spp_COMPorts)(this.COMPort - 1);
            }
        }

        /// <summary>
        /// Gets or sets the list of animations uploaded to the server
        /// </summary>
        public List<Animation> Animations
        {
            get;
            set;
        }

        /// <summary>
        /// Gets ors sets a value indicating whether the server is listening for new clients 
        /// or not.
        /// </summary>
        public bool IsListening
        {
            get
            {
                return this.isListening;
            }

            set
            {
                if (value != this.isListening)
                {
                    this.isListening = value;
                    if (this.isListening == true)
                    {
                        this.StartListening();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indiciating whether the server is
        /// connected to serial or not. If the server cannot
        /// successfully connect to the serial, then an exception is thrown. 
        /// </summary>
        public bool IsConnectedToSerial
        {
            get
            {
                return this.isConnectedToSerial;
            }

            set
            {
                if (value != this.isConnectedToSerial)
                {
                    if (value == true)
                    {
                        this.ConnectToSerial();
                        this.isConnectedToSerial = true;
                    }
                    else
                    {
                        this.DisconnectFromSerial();
                        this.isConnectedToSerial = false;
                    }
                }
            }
        }

        /// <summary>
        /// Try to initialize the serial communications. 
        /// </summary>
        private void ConnectToSerial()
        {
            try
            {
                this.packetProtocol = new packet_protocol(this.sppCOMPort, spp_BaudRates.BAUD_115200);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Disconnect from serial and free up the com port. 
        /// </summary>
        private void DisconnectFromSerial()
        {
            this.packetProtocol.close();
        }

        /// <summary>
        /// Gets or sets the index of currently playing animation. 
        /// </summary>
        private int CurrentlyPlayingAnimationIndex
        {
            get
            {
                return this.currentlyPlayingAnimationIndex;
            }

            set
            {
                if (value != this.currentlyPlayingAnimationIndex)
                {
                    if (this.Status == ServerStatus.PlayingAnimation)
                    {
                        throw new InvalidOperationException("Cannot change the currently playing animation while an animation is already playing. Stop the animation first.");
                    }

                    this.currentlyPlayingAnimationIndex = value;
                }
            }
        }

        /// <summary>
        /// Process an upload command from a client. Deserializes the Animation send over the 
        /// stream and adds it to the list of animations.
        /// TODO: Throw exceptions when this function is called and the server is in the wrong state.
        /// </summary>
        /// <param name="clientStream"></param>
        private void ProcessUploadAnimationCommand(NetworkStream clientStream)
        {
            this.Status = ServerStatus.Uploading;

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            Animation newAnimation = (Animation)binaryFormatter.Deserialize(clientStream);
            this.Animations.Add(newAnimation);

            this.Status = ServerStatus.Idle;
        }

        /// <summary>
        /// Process a delete command. Gets 4 bytes that represent the animation index
        /// integer and deletes the corresponding animation in the list.
        /// </summary>
        /// <param name="clientStream"></param>
        private void ProcessDeleteAnimationCommand(NetworkStream clientStream)
        {
            this.Status = ServerStatus.Deleting;

            // Get 4 byte array representing the index of the animation to delete
            byte[] buffer = new byte[4];
            clientStream.Read(buffer, 0, 4);

            // Convert the byte array to a 32 bit integer
            int index = BitConverter.ToInt32(buffer, 0);

            Animations.RemoveAt(index);

            this.Status = ServerStatus.Idle;
        }

        /// <summary>
        /// Returns a list of animation titles.
        /// </summary>
        /// <returns>A list of animation titles</returns>
        public List<string> GetAnimationTitles()
        {
            return new List<string>(from animation in this.Animations select animation.Name);
        }

        /// <summary>
        /// Process a list command. 
        /// </summary>
        /// <param name="clientStream"></param>
        private void ProcessGetTitlesCommand(NetworkStream clientStream)
        {
            this.Status = ServerStatus.ListingAnimations;

            // get the list of animation titles
            List<string> titles = this.GetAnimationTitles();

            // send the list back
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(clientStream, titles);

            this.Status= ServerStatus.Idle;
        }

        /// <summary>
        /// Process a get status command.
        /// </summary>
        /// <param name="clientStream"></param>
        private void ProcessGetStatusCommand(NetworkStream clientStream)
        {
            clientStream.WriteByte((byte)this.Status);
        }

        /// <summary>
        /// Process a play command. Starts a new thread and plays the animation. 
        /// </summary>
        /// <param name="clientStream"></param>
        private void ProcessPlayCommand(NetworkStream clientStream)
        {
            Debug.WriteLine("Recieved play command at " + DateTime.Now.TimeOfDay.ToString("c"));

            // Measure network lag
            while (clientStream.ReadByte() == 1)
            {
                clientStream.WriteByte(1);
            }
            clientStream.WriteByte(1);

            // Get the index of the animation to play
            this.CurrentlyPlayingAnimationIndex = clientStream.ReadByte();

            // Get the starting delta
            //BinaryFormatter formatter = new BinaryFormatter();
            //TimeSpan startTime = (TimeSpan)formatter.Deserialize(clientStream);

            
            int bufferSize = clientStream.ReadByte();
            byte[] buffer = new byte[bufferSize];
            clientStream.Read(buffer, 0, bufferSize);
            int totalMilliseconds = BitConverter.ToInt32(buffer, 0);
            TimeSpan startTime = new TimeSpan(0, 0, 0, 0, totalMilliseconds);

            Debug.WriteLine("Starting animation at delta " + startTime.ToString("c"));
            Debug.WriteLine("Starting animation at " + DateTime.Now.TimeOfDay.ToString("c"));

            // Only play if the serial packet protocol is connected
            if (this.IsConnectedToSerial == true)
            {
                // Set the current frame index based on the time recieved
                var frames = this.Animations[this.CurrentlyPlayingAnimationIndex].Frames;
                this.currentFrameIndex = 0;
                for (int index = 0; index < frames.Count; index++)
                {
                    if (frames[index].StartTime > startTime)
                    {
                        this.currentFrameIndex = index;
                        break;
                    }
                }

                this.Status = ServerStatus.PlayingAnimation;
                Thread playThread = new Thread(new ThreadStart(Play));
                playThread.Name = "Tower Lights Server Animation Thread";
                playThread.Start();
            }
        }

        /// <summary>
        /// Process a stop command. Sets a flags that stops the animation playing thread and
        /// optionally sends a blank frame out. 
        /// </summary>
        /// <param name="clientStream"></param>
        private void ProcessStopCommand(NetworkStream clientStream)
        {
            bool sendBlank = Convert.ToBoolean(clientStream.ReadByte());

            this.Status = ServerStatus.Idle;

            Debug.WriteLine("Stopping animation");
        }

        /// <summary>
        /// Process a get uploaded animations command. It deseralizes a list of md5 hashes sent
        /// over the stream, and then only sends the animations that don't have hashes listed.
        /// </summary>
        /// <param name="clientStream">The stream to read and write from.</param>
        private void ProcessGetUploadedAnimationsCommand(NetworkStream clientStream)
        {
            // Set the listing animations flag. 
            this.Status = ServerStatus.ListingAnimations;

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(clientStream, this.Animations);

            // Unset the list animations flag
            this.Status = ServerStatus.Idle;
        }

        /// <summary>
        /// Plays the selected animation. Designed to be run in a seperate thread. 
        /// </summary>
        private void Play()
        {
            List<KeyFrame> frames = this.Animations[this.CurrentlyPlayingAnimationIndex].Frames;
            Stopwatch stopwatch = new Stopwatch();
            TimeSpan delta = this.currentFrameIndex < 0 ? new TimeSpan() : frames[this.currentFrameIndex].StartTime;

            if (this.currentFrameIndex < 0)
            {
                this.currentFrameIndex = 0;
            }

            stopwatch.Start();

            Debug.WriteLine("Server: Started stopwatch: " + DateTime.Now.TimeOfDay.ToString("c"));
            
            

            while (this.currentFrameIndex < frames.Count && this.Status == ServerStatus.PlayingAnimation)
            {
                if (stopwatch.Elapsed >= frames[this.currentFrameIndex].StartTime - delta)
                {
                    // Send it out over serial
                    this.SendFrameToSerial(frames[this.currentFrameIndex]);
                    this.currentFrameIndex++;
                }
                else
                {
                    // Sleep the thread for a while to stop the thread from burning up too many cycles here ...
                    Thread.Sleep(1);
                }
            }

            KeyFrame blankFrame = new KeyFrame(
                    this.Animations[this.CurrentlyPlayingAnimationIndex].Frames[0].RowCount,
                    this.Animations[this.CurrentlyPlayingAnimationIndex].Frames[0].ColumnCount,
                    new TimeSpan(),
                    null);

            this.SendFrameToSerial(blankFrame);
            
            this.Status = ServerStatus.Idle;
        }

        /// <summary>
        /// Send a keyframe to the microcontroller using the packet protocol
        /// </summary>
        /// <param name="currentFrame"></param>
        private void SendFrameToSerial(KeyFrame currentFrame)
        {
            int packetSize = currentFrame.RowCount * currentFrame.ColumnCount * 3;
            byte[] packet = new byte[packetSize];

            for (int row = 0; row < currentFrame.RowCount; ++row)
            {
                for (int col = 0; col < currentFrame.ColumnCount; ++col)
                {
                    int windowOffset = (row * currentFrame.ColumnCount  + col) * 3;
                    packet[windowOffset + 0] = currentFrame.Get(row, col).Red;
                    packet[windowOffset + 1] = currentFrame.Get(row, col).Green;
                    packet[windowOffset + 2] = currentFrame.Get(row, col).Blue;
                }
            }

            Debug.WriteLine("Sending frame to serial");

            packetProtocol.snd_ascii_hex(serial_packet_protocol.packet_types.SEND_BUFFER_TYPE, packet, packet.Length);
        }
    }
}
