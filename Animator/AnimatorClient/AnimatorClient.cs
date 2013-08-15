// -----------------------------------------------------------------------
// <copyright file="AnimatorClient.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace AnimatorClient
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Runtime.Serialization.Formatters.Binary;
    using AnimationModels;
    using Commands;
    using System.Security.Cryptography;
    using System.IO;
    using System.Linq;
    using System.Diagnostics;
    using NAudio.Wave;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class AnimatorClient
    {
        /// <summary>
        /// The audio playing device
        /// </summary>
        private IWavePlayer waveOutDevice;

        /// <summary>
        /// Stores the audio sample.
        /// </summary>
        WaveChannel32 waveOutputStream;

        /// <summary>
        /// 
        /// </summary>
        public AnimatorClient()
        {
            try
            {
                waveOutDevice = new NAudio.Wave.DirectSoundOut();
            }
            catch (Exception driverCreateException)
            {
                throw new InvalidOperationException("Could not initialize audio player", driverCreateException);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="animation"></param>
        public void UploadAnimation(Animation animation)
        {
            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect(this.ServerAddress, this.ServerPort);

                CommandHandler.SendCommand(tcpClient.GetStream(), Command.UploadAnimation);

                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(tcpClient.GetStream(), animation);

                MemoryStream ms = new MemoryStream();
                binaryFormatter.Serialize(ms, animation);

                Debug.WriteLine("Size of animation: " + ms.ToArray().Count() + " bytes");

                tcpClient.Close();
            }
            catch (SocketException se)
            {
                tcpClient.Close();
                throw new InvalidOperationException("Could not upload animation.", se);
            }
        }

        public string GetStatus()
        {
            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect(this.ServerAddress, this.ServerPort);

                CommandHandler.SendCommand(tcpClient.GetStream(), Command.GetAnimatorStatus);

                byte[] buffer = new byte[1];
                tcpClient.GetStream().Read(buffer, 0, 1);

                tcpClient.Close();

                return buffer[0].ToString();
            }
            catch (SocketException se)
            {
                tcpClient.Close();
                throw new InvalidOperationException("Could not get status", se);
            }
        }

        public void DeleteAnimation(int index)
        {
            TcpClient tcpClient = new TcpClient();

            try
            {
                tcpClient.Connect(this.ServerAddress, this.ServerPort);

                CommandHandler.SendCommand(tcpClient.GetStream(), Command.DeleteAnimation);

                tcpClient.GetStream().WriteByte(Convert.ToByte(index));

                tcpClient.Close();
            }
            catch (SocketException se)
            {
                tcpClient.Close();
                throw new InvalidOperationException("Could not delete animation", se);
            }
        }

        public List<string> GetAnimationTitles()
        {
            TcpClient tcpClient = new TcpClient();

            try
            {
                tcpClient.Connect(this.ServerAddress, this.ServerPort);

                CommandHandler.SendCommand(tcpClient.GetStream(), Command.GetUploadedAnimationTitles);

                BinaryFormatter binaryFormatter = new BinaryFormatter();
                List<string> animationTitles = (List<string>)binaryFormatter.Deserialize(tcpClient.GetStream());

                tcpClient.Close();

                return animationTitles;
            }
            catch (SocketException se)
            {
                tcpClient.Close();
                throw new InvalidOperationException("Could not get animation titles", se);
            }

            
        }

        /// <summary>
        /// Downloads the list of animations from the server. It will not send any animations
        /// that have the an MD5 hash given. 
        /// </summary>
        /// <param name="currentAnimations">A list of animations that have already been downloaded</param>
        /// <returns>The list of animations on the server, including any animations that were already downloaded</returns>
        public List<Animation> GetAnimations(List<Animation> currentAnimations)
        {
            
            TcpClient tcpClient = new TcpClient();
            try
            {
                // Try to connect to the server
                tcpClient.Connect(this.ServerAddress, this.ServerPort);

                // Send the get uploaded animations command
                CommandHandler.SendCommand(tcpClient.GetStream(), Command.GetUploadedAnimations);

                // Get the animations from the server
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                List<Animation> animations = (List<Animation>)binaryFormatter.Deserialize(tcpClient.GetStream());

                // Close the connection
                tcpClient.Close();

                return new List<Animation>(animations);
            }
            catch (SocketException se)
            {
                throw new InvalidOperationException("Could not connect to server", se);
            }
        }

        /// <summary>
        /// Send a signal to the server to start playing the animation at the specified index and
        /// start time. Start playing the audio at the specified time. 
        /// </summary>
        /// <param name="index">The index of the animation</param>
        /// <param name="delta">The start time into the animation</param>
        public void PlayAnimation(int index, TimeSpan delta, string audioFileName)
        {
            Debug.WriteLine("Client: starting animation at delta " + delta.ToString("c"));

            // create the audio stream
            if (audioFileName != "")
            {
                waveOutputStream = CreateInputStream(audioFileName);
            }
            else
            {
                waveOutputStream = null;
            }

            // Open the audio file
            if (waveOutputStream != null)
            {
                // seek to the correct position in the audio stream
                waveOutputStream.Position = 0;
                double seconds = delta.TotalSeconds;
                double bytesPerSec = waveOutputStream.Length / waveOutputStream.TotalTime.TotalSeconds;
                waveOutputStream.Position = Convert.ToInt64(seconds * bytesPerSec);

                //try
                //{
                waveOutDevice.Init(waveOutputStream);
                //}
                //catch (Exception initException)
                //{
                //    MessageBox.Show(initException.Message, "Error Initializing Audio Output");
                //    return;
                //}
            }

            TcpClient tcpClient = new TcpClient();

            try
            {
                // Connect to the server
                tcpClient.Connect(this.ServerAddress, this.ServerPort);

                // Save the stream
                NetworkStream stream = tcpClient.GetStream();

                // Send the play command
                CommandHandler.SendCommand(tcpClient.GetStream(), Command.PlayAnimation);

                // Measure network lag
                TimeSpan total = new TimeSpan();
                Stopwatch stopwatch = new Stopwatch();
                int trials = 10;
                for (int i = 0; i < trials; i++)
                {
                    stopwatch.Restart();

                    if (i == trials - 1)
                    {
                        stream.WriteByte(2);
                    }
                    else
                    {
                        stream.WriteByte(1);
                    }

                    stream.ReadByte();
                    stopwatch.Stop();

                    if (i != 0)
                    {
                        total += stopwatch.Elapsed;
                    }

                    Debug.WriteLine("Network lag measurement: " + stopwatch.Elapsed.ToString("c"));
                }

                TimeSpan networkLag = new TimeSpan(0, 0, 0, 0, (int)(total.TotalMilliseconds / trials));
                Debug.WriteLine("Average network lag: " + networkLag.ToString("c") + ", " + total.TotalMilliseconds / trials);

                // Send the index of the animation
                stream.WriteByte(Convert.ToByte(index));

                // Send the delta start time
                //BinaryFormatter formatter = new BinaryFormatter();
                //formatter.Serialize(tcpClient.GetStream(), delta);
                byte[] totalMillisecondBytes = BitConverter.GetBytes((int)delta.TotalMilliseconds);
                stream.WriteByte((byte)totalMillisecondBytes.Count());
                stream.Write(totalMillisecondBytes, 0, totalMillisecondBytes.Count());

                Debug.WriteLine("Client: Sent play command: " + DateTime.Now.TimeOfDay.ToString("c"));

                // Start playing the audio. 
                if (waveOutputStream != null)
                {
                    Stopwatch lagtimer = new Stopwatch();
                    lagtimer.Start();
                    while (lagtimer.ElapsedMilliseconds < networkLag.TotalMilliseconds) ;

                    Debug.WriteLine("Client: starting audio: " + DateTime.Now.TimeOfDay.ToString("c"));
                    waveOutDevice.Play();
                }

                // Close the connection
                tcpClient.Close();
            }
            catch (IOException ioe)
            {
                tcpClient.Close();
                waveOutDevice.Stop();
                throw new InvalidOperationException("Could not start playing animation due to an IO error", ioe);
            }
            catch (SocketException se)
            {
                // These are unnecessary precautions
                tcpClient.Close();
                waveOutDevice.Stop();
                throw new InvalidOperationException("Could not start playing the animation.", se);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sendBlank"></param>
        public void StopAnimation(bool sendBlank)
        {
            TcpClient tcpClient = new TcpClient();

            try
            {
                tcpClient.Connect(this.ServerAddress, this.ServerPort);

                CommandHandler.SendCommand(tcpClient.GetStream(), Command.StopAnimation);

                tcpClient.GetStream().WriteByte(Convert.ToByte(sendBlank));

                this.waveOutDevice.Stop();

                tcpClient.Close();
            }
            catch (SocketException se)
            {
                tcpClient.Close();
                waveOutDevice.Stop();
                throw new InvalidOperationException("Could not send stop command", se);
            }
        }

        /// <summary>
        /// Sets the serial state, connected or disconnected. True is connected,
        /// false is disconnected. 
        /// </summary>
        /// <param name="value"></param>
        public void SetSerialState(bool value)
        {
            
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

        public string ServerAddress
        {
            get;
            set;
        }

        public int ServerPort
        {
            get;
            set;
        }
    }
}
