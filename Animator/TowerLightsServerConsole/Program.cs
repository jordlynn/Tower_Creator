using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimatorServer;

namespace TowerLightsServerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            AnimationServer server = new AnimationServer();
            server.Port = 1337;
            server.COMPort = 4;

            Console.WriteLine("Welcome to the Tower Lights Server!");
            Console.WriteLine("Type 'help' for a list of commands");
            server.IsListening = true;
            
            Console.WriteLine("A new thread has been created and it is listening for connections");
            try
            {
                server.IsConnectedToSerial = true;
                Console.WriteLine("Connected to serial, ready to send out packets.");
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Couldn't connect to com port " + server.COMPort + ": " + e.Message);
            }

            string userInput;
            while (true)
            {
                Console.WriteLine();
                Console.Write("[::] ");
                userInput = Console.ReadLine();

                if (userInput == "quit" ||
                    userInput == "exit")
                {
                    Console.Write("Are you sure you want to quit? (Yes/No)");
                    userInput = Console.ReadLine();
                    if (userInput == "Yes" || userInput == "Y" || userInput == "y" || userInput == "yes")
                    {
                        if (server.IsListening == true)
                        {
                            server.IsListening = false;
                            Console.WriteLine("The server stopped listening.");
                        }
                        break;
                    }
                }
                else if (userInput == "help")
                {
                    PrintCommands();
                }
                else if (userInput == "status")
                {
                    Console.WriteLine("Server state: " + server.Status.ToString());
                    Console.WriteLine("Listening for connections: " + server.IsListening);
                    Console.WriteLine("Connected to serial: " + server.IsConnectedToSerial);
                    Console.WriteLine("COM Port: " + server.COMPort);
                    Console.WriteLine("Port: " + server.Port);
                }
                else if (userInput == "start listening" ||
                    userInput == "start")
                {
                    if (server.IsListening == true)
                    {
                        Console.WriteLine("The server is already listening");
                    }
                    else
                    {
                        server.IsListening = true;
                        Console.WriteLine("A new thread has been created and it is listening for connections");
                    }
                }
                else if (userInput == "stop listening" || 
                    userInput == "stop")
                {
                    if (server.IsListening == false)
                    {
                        Console.WriteLine("The server is already not listening");
                    }
                    else
                    {
                        server.IsListening = false;
                        Console.WriteLine("The server stopped listening");
                    }
                }
                else if (userInput == "list animations" ||
                    userInput == "list")
                {
                    List<string> titles = server.GetAnimationTitles();

                    if (titles.Count == 0)
                    {
                        Console.WriteLine("There are no animations uploaded.");
                    }
                    else
                    {
                        for (int i = 0; i < titles.Count; i++)
                        {
                            Console.WriteLine((i + 1) + ": " + titles[i]);
                        }
                    }
                }
                else if (userInput == "connect to serial")
                {
                    try
                    {
                        server.IsConnectedToSerial = true;
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine("Couldn't connect to com port " + server.COMPort + ": " + e.Message);
                    }
                }
                else if (userInput == "disconnect from serial")
                {
                    server.IsConnectedToSerial = false;
                }
                else if (userInput == "set com port")
                {
                    Console.Write("Enter COM port number: ");
                    server.COMPort = Convert.ToInt32(Console.ReadLine());
                }
                else if (userInput == "set port")
                {
                    Console.Write("Enter port: ");
                    server.Port = Convert.ToInt32(Console.ReadLine());
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
            Console.WriteLine("start listening\tStart listening for incoming connections");
            Console.WriteLine("stop listening\tStop listening for incoming connections");
            Console.WriteLine("");
        }
    }
}
