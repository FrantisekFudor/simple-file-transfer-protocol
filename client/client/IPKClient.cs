using System;
using System.Net.Sockets;
using System.Text;
using CommandLine;
using System.IO;


namespace client

{

    /// <summary>
    /// IPKClient client class
    /// </summary>
    internal class IPKClient

    {
        /// <summary>
        /// Enumeration for local exit codes
        /// </summary>
        enum ExitCodes : int
        {
            Success = 0
        }

        /// <summary>
        /// Nuget CommandLine options for parsing console arguments
        /// Inspired by: https://github.com/commandlineparser/commandline
        /// </summary>
        public class Options
        {
            [Option('h', "IP", Required = true, HelpText = "IPv4 or IPv6 address of server.")]
            public string _address { get; set; }
            [Option('p', "port", Default = 115, Required = false, HelpText = "Port on which server will be listening.")]
            public int _port { get; set; }
            [Option('f', "cesta_k_adresari", Required = true, HelpText = "Absolute path to working directory.")]
            public string _dir { get; set; }
        }

        /// <summary>
        /// Main method calls ClientStart() method if arguments were parsed successfully otherwise CommandLine writes help on console output
        /// </summary>
        /// <param name="args">Console arguments</param>
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(ClientStart);
        }

        /// <summary>
        /// ClientStart() method creates client socket and connects to server, client run in infinite loop and it is possible to terminate it either with CTRL+C in server console or DONE command
        /// Inspired by: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient?view=net-6.0
        /// </summary>
        /// <param name="opts">List of parsed console arguments</param>
        static void ClientStart(Options opts)
        {
            bool done = false;
            //Always trying to connect to the server
            while (!done)
            {
                //Intialize client socket
                TcpClient client = new TcpClient();
                try
                {
                    //Connecting to the server
                    Console.Write("Connecting to " + opts._address + ":" + opts._port + "... ");
                    client.Connect(opts._address, opts._port);
                    Console.WriteLine("Connected!");

                    //Get network stream and it's writer and reader
                    NetworkStream stream = client.GetStream();
                    StreamWriter writer = new StreamWriter(stream, encoding: Encoding.ASCII);
                    StreamReader reader = new StreamReader(stream, encoding: Encoding.ASCII);
                    Console.WriteLine("-----------------------------------------------");

                    //Write welcome message to server
                    writer.WriteLine("(opens connection to R)");
                    writer.Flush();
                    Console.WriteLine("S: (opens connection to R)");

                    //Get welcome message from server
                    string data = reader.ReadLine();
                    Console.WriteLine(data);

                    //Client start writing commands
                    while (true)
                    {
                        //If input is empty line
                        String line = Console.ReadLine();
                        if (line.Equals(""))
                        {
                            continue;
                        }

                        //Write message to server
                        writer.WriteLine(line);
                        writer.Flush();

                        //If message was DONE SFTP command
                        if (line.Equals("DONE"))
                        {
                            //End client
                            done = true;
                            data = reader.ReadLine();
                            Console.WriteLine(data);
                            stream.Close();
                            client.Close();
                            break;
                        }

                        //Try to parse LIST SFT command
                        try
                        {
                            //If command was LIST {F|V}
                            if (line.Substring(0, 6).Equals("LIST F") || line.Substring(0, 6).Equals("LIST V"))
                            {
                                //Read messages from server until message "done" was sent from server 
                                while ((data = reader.ReadLine()) != null)
                                {
                                    //If message from server was "done"
                                    if (data.Equals("done"))
                                    {
                                        //Stop reading in cycle
                                        break;
                                    }
                                    //If message from server was access denied
                                    if (data.Equals("R: -Access denied, please logg in"))
                                    {
                                        //Write access denied on console
                                        Console.WriteLine(data);

                                        //Stop reading in cycle
                                        break;
                                    }

                                    //Write message from server on console
                                    Console.WriteLine(data);
                                }
                                //Continue writing commands
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            //Empty
                        }

                        //Get message from server and write it on console
                        data = reader.ReadLine();
                        Console.WriteLine(data);
                    }

                    //Close connection
                    stream.Close();
                    client.Close();
                }
                //If fail to connect to the server
                catch (Exception ex)
                {
                    //Write on console
                    Console.WriteLine("Server unreachable!");
                    Console.WriteLine("Trying to restore connection... ");

                    //Wait a bit and try again
                    System.Threading.Thread.Sleep(3500);
                }
            }
        }
    }
}

