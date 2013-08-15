// -----------------------------------------------------------------------
// <copyright file="Commands.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;

    public enum Command : byte
    {
        UploadAnimation,
        GetAnimatorStatus,
        DeleteAnimation,
        GetUploadedAnimationTitles,
        GetUploadedAnimations,
        PlayAnimation,
        StopAnimation
    }

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class CommandHandler
    {
        /// <summary>
        /// Sends a command over the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="command"></param>
        public static void SendCommand(Stream stream, Command command)
        {
            stream.Write(new byte[] { (byte)command }, 0, 1);
        }

        /// <summary>
        /// Recieves the command on the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Command RecieveCommand(Stream stream)
        {
            byte[] buffer = new byte[1];
            stream.Read(buffer, 0, 1);
            return (Command)buffer[0];
        }
    }
}
