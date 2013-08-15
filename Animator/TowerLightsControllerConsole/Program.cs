using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using AnimatorClient;
using AnimationModels;

namespace TowerLightsControllerConsole
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            AnimatorClient.AnimatorClient controller = new AnimatorClient.AnimatorClient();
            controller.ServerAddress = "localhost";
            controller.ServerPort = 1334;

            Console.WriteLine("Welcome to the Tower Lights Controller!");
            Console.WriteLine("Type 'help' for a list of commands");

            string userInput;
            while (true)
            {
                Console.WriteLine();
                Console.Write("[::] ");
                userInput = Console.ReadLine();

                if (userInput == "quit" || userInput == "exit" || userInput == "q")
                {
                    Console.Write("Are you sure you want to quit? (Yes/No)");
                    userInput = Console.ReadLine();
                    if (userInput == "Yes" || userInput == "Y" || userInput == "y" || userInput == "yes")
                    {
                        break;
                    }
                }
                else if (userInput == "help")
                {
                    PrintCommands();
                }
                else if (userInput == "status")
                {
                    Console.WriteLine(controller.GetStatus());
                }
                else if (userInput == "test connection" || userInput == "test")
                {
                    Console.WriteLine("Not yet implemented");
                    //List<string> errors = controller.TestConnection();
                    //Console.WriteLine("Connection test results:");
                    //if (errors.Count == 0)
                    //    Console.WriteLine("Test successful!");
                    //else
                    //    foreach (string error in errors)
                    //        Console.WriteLine(error);
                }
                else if (userInput == "send message" || userInput == "send")
                {
                    Console.WriteLine("Not yet implemented");
                    //Console.Write("Enter a message: ");
                    //string message = Console.ReadLine();
                    //controller.SendMessage(message);
                }
                else if (userInput == "set server address")
                {
                    Console.Write("Enter the address of the server: ");
                    string strAddress = Console.ReadLine();
                    controller.ServerAddress = strAddress;
                }
                else if (userInput == "set server port")
                {
                    Console.Write("Enter the port of the server: ");
                    string strPort = Console.ReadLine();
                    controller.ServerPort = Convert.ToInt32(strPort);
                }
                else if (userInput == "upload animation" || userInput == "upload")
                {
                    OpenFileDialog fileDlg = new OpenFileDialog();
                    fileDlg.Filter = "Tower Animator Files (*.tan)|*.tan|All Files (*.*)|*.*";

                    fileDlg.ShowDialog();

                    Console.Write("Opening File.. ");
                    Animation animation = AnimationLoader.LoadAnimationFromFile(fileDlg.FileName);
                    Console.WriteLine("Done!");
                    Console.Write("Uploading... ");
                    controller.UploadAnimation(AnimationLoader.LoadAnimationFromFile(fileDlg.FileName));
                    Console.WriteLine("Done!");
                }
                else if (userInput == "measure network lag")
                {
                    Console.WriteLine("Not yet implemented");
                    //TimeSpan min, max, avg;
                    //List<string> errors = controller.MeasureNetworkLag(out avg, out min, out max);
                    //Console.WriteLine("Minimum {0,-30}", min);
                    //Console.WriteLine("Average {0,-30}", avg);
                    //Console.WriteLine("Maximum {0,-30}", max);
                    //Console.WriteLine("The lag was measured by sending and recieving one byte at a time for 100 " +
                    //        "iterations, and the times given represent the round trip time it takes for the byte to" +
                    //        " be sent and recieved.");

                }
                else if (userInput == "list animations" || userInput == "list")
                {
                    List<string> animationList = controller.GetAnimationTitles();

                    if (animationList.Count == 0)
                    {
                        Console.WriteLine("No animations have been uploaded");
                    }
                    else
                    {
                        for (int i = 0; i < animationList.Count; i++)
                        {
                            Console.WriteLine((i + 1) + ": " + animationList[i]);
                        }
                    }
                }
                else if (userInput == "delete animation" || userInput == "delete")
                {
                    List<string> animationList = controller.GetAnimationTitles();

                    if (animationList.Count == 0)
                    {
                        Console.WriteLine("No animations have been uploaded");
                    }
                    else
                    {
                        for (int i = 0; i < animationList.Count; i++)
                        {
                            Console.WriteLine((i + 1) + ": " + animationList[i]);
                        }
                    }
                    Console.Write("Enter the number of the animation to delete: ");
                    int index = Convert.ToInt32(Console.ReadLine());
                    controller.DeleteAnimation(index - 1);
                    Console.WriteLine("Animation " + index + " deleted");
                }
                else if (userInput == "play animation" || userInput == "play")
                {
                    List<string> animationList = controller.GetAnimationTitles();

                    if (animationList.Count == 0)
                    {
                        Console.WriteLine("No animations have been uploaded");
                    }
                    else
                    {
                        for (int i = 0; i < animationList.Count; i++)
                        {
                            Console.WriteLine((i + 1) + ": " + animationList[i]);
                        }
                    }

                    Console.Write("Enter the number of the animation to play: ");
                    int index = Convert.ToInt32(Console.ReadLine());
                    Console.Write("Enter the filename of the audio file to play: ");
                    string filename = Console.ReadLine();
                    controller.PlayAnimation(index - 1, new TimeSpan(), filename);
                    Console.WriteLine("Playing Animation");
                }
                else if (userInput == "stop animation" || userInput == "stop")
                {
                    Console.WriteLine("Not yet implemented");
                }
                else if (userInput == "pause animation" || userInput == "pause")
                {
                    Console.WriteLine("Not yet implemented");
                }
                else
                {
                    Console.WriteLine("Unrecognized command, type 'help' to print a list of commands");
                }
            }
        }

        private static void PrintCommands()
        {
            Console.WriteLine("");
            Console.WriteLine("help\tPrint this menu");
            Console.WriteLine("quit\tQuit Tower Lights Server");
            Console.WriteLine("status\tPrint a status report of the state of server");
            Console.WriteLine("test connection\tTest the connection to the server");
            Console.WriteLine("send message\tSend a message to the server");
            Console.WriteLine("set server address\tSet the ip address for the server");
            Console.WriteLine("set server port\tSet the port for the server");
            Console.WriteLine("list animations\tList the animations on the server");
            Console.WriteLine("select animation\tSelect an animation");
            Console.WriteLine("delete selected animation\tDelete the selected animation");
            Console.WriteLine("play selected animation\tPlay the selected animation");
            Console.WriteLine("pause selected animation\tPause the selected animation");
            Console.WriteLine("stop selected animation\tStop the selected animation");
        }
    }
}
