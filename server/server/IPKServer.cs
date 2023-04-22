using System;

using CommandLine;

using System.Net;

using System.IO;

using System.Net.Sockets;

using System.Net.NetworkInformation;

using System.Collections.Generic;

using System.Linq;

using System.Threading;



namespace server

{

    /// <summary>

    /// IPKServer server class

    /// </summary>

    internal class IPKServer

    {

        /// <summary>

        /// Global properties such as paths, hostname, listener, dictionary with credentials and threads for async communication

        /// </summary>

        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static string CredentialsPath = "";

        public static string Hostname = "";

        public static string GlobalWorkingDirectory = "";

        public static TcpListener _server;

        public static Dictionary<string, string> credentials;



        /// <summary>

        /// Enumeration for local exit codes

        /// </summary>

        enum ExitCodes : int

        {

            WrongCredentialsPath = 1,

            WrongWorkingDirPath = 2

        }



        /// <summary>

        /// Nuget CommandLine options for parsing console arguments

        /// Inspired by: https://github.com/commandlineparser/commandline

        /// </summary>

        public class Options

        {

            [Option('i', "interface", Default = "any", Required = false, HelpText = "Interface on which server will be listening.")]

            public string _interface { get; set; }



            [Option('p', "port", Default = 115, Required = false, HelpText = "Port on which server will be listening.")]

            public int _port { get; set; }



            [Option('u', "path_to_credentials", Required = true, HelpText = "Absolute path to database with credentials.")]

            public string _path { get; set; }



            [Option('f', "path_to_working_directory", Required = true, HelpText = "Absolute path to working directory.")]

            public string _dir { get; set; }



        }



        /// <summary>

        /// Main method calls ServerRun() method if arguments were parsed successfully otherwise CommandLine writes help on console output

        /// </summary>

        /// <param name="args">Console arguments</param>

        static void Main(string[] args)

        {

            CommandLine.Parser.Default.ParseArguments<Options>(args)

                .WithParsed(ServerRun);

        }



        /// <summary>

        /// ServerRun method creates server socket and starts listening, server runs in infinite loop and it is possible to terminate it either with CTRL+C in server console

        /// Inspired by: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener?view=net-6.0#examples

        /// </summary>

        /// <param name="opts">List of parsed console arguments</param>

        public static void ServerRun(Options opts)

        {

            //If console arguments -u and -f are invalid (paths to non-existing file or directory) exit with proper exitcode

            if (!File.Exists(opts._path))

            {

                Console.WriteLine("Wrong path for credentials given, file doesn't exits!");

                Environment.Exit((int)ExitCodes.WrongCredentialsPath);

            }

            if (!Directory.Exists(opts._dir))

            {

                Console.WriteLine("Wrong path for working directory given, directory doesn't exits!");

                Environment.Exit((int)ExitCodes.WrongWorkingDirPath);

            }



            //Inicialize credentials path

            CredentialsPath = opts._path;



            //Gets credentials from console argument -u and store it into dictionary in format 'username = password'

            //Inspired by: https://stackoverflow.com/a/39992125

            List<string> credentialsList = File.ReadAllLines(opts._path).ToList();

            credentials = new Dictionary<string, string>();

            foreach (string line in credentialsList)

            {

                string[] keyvalue = line.Split(':');

                if (keyvalue.Length == 2)

                {

                    credentials.Add(keyvalue[0], keyvalue[1]);

                }

            }



            //Gives server a name (localhost)

            //Sets server address to IPv6 any (::0)

            IPHostEntry host = Dns.GetHostEntry("localhost");

            IPAddress address = IPAddress.IPv6Any;



            //Declare endpoint

            IPEndPoint endPoint;



            //Set global properties hostname and working directory

            Hostname = host.HostName;

            GlobalWorkingDirectory = opts._dir;



            //Get all interfaces and try to find interface with name of console argument -f

            //Inspired by: https://stackoverflow.com/a/10060249

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())

            {

                //If interface is found get its IP address and rewrite variable address with it

                if (ni.Name.Equals(opts._interface))

                {

                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)

                    {

                        address = ip.Address;

                    }

                }

            }



            //Set server endpoint (IP Address:Port)

            endPoint = new IPEndPoint(address, opts._port);



            //Create TcpListener as server, get socket and set socket options to support both IPv6 and IPv4

            //Inspired by: https://chiplegend.wordpress.com/2013/05/10/c-server-that-supports-ipv6-and-ipv4-on-the-same-port/

            _server = new TcpListener(endPoint);

            Socket serverSocket = _server.Server;

            serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);



            //Server start and write it's properties on console output

            Console.Write("Starting server... ");

            _server.Start();

            Console.WriteLine("Server running!");

            Console.WriteLine("---------------------------------------------------------------");

            Console.WriteLine("Hostname: " + host.HostName.ToString());

            Console.WriteLine("Interface: " + opts._interface);



            //If server IP address is '::0' write on output '0.0.0.0' just for better visualisation

            if (address.Equals(IPAddress.IPv6Any))

            {

                Console.WriteLine("IP Address: 0.0.0.0");

            }

            else

            {

                Console.WriteLine("IP Address: " + address);

            }

            Console.WriteLine("---------------------------------------------------------------");





            //Server run

            try

            {

                //Start listening 

                Console.WriteLine("R: (listening for connection)" + null);

                while (true)

                {

                    //Threads reset

                    allDone.Reset();



                    //Begin listening asynchronously with callback

                    try

                    {

                        _server.BeginAcceptTcpClient(new AsyncCallback(TcpAcceptClient), _server);

                    }

                    //Client got disconnected

                    catch (Exception ex)

                    {

                        Console.WriteLine("R: -" + Hostname + " closing connection (client disconnected)" + null);

                    }



                    //Threads wait one

                    allDone.WaitOne();



                }

            }

            //Client got disconnected

            catch (Exception ex)

            {

                Console.WriteLine("R: -" + Hostname + " closing connection (client disconnected)" + null);

            }

        }



        /// <summary>

        /// TcpAcceptClient callback method - accept client (end of accepting) then start accepting again for another client (async)

        /// </summary>

        /// <param name="result">Callback result</param>

        public static void TcpAcceptClient(IAsyncResult result)

        {

            //Threads set

            allDone.Set();



            //Accept client

            TcpClient client = _server.EndAcceptTcpClient(result);



            //Start listening again

            _server.BeginAcceptTcpClient(TcpAcceptClient, _server);



            //Initialize new connected client

            IPKServerClient serverClient = new IPKServerClient(client, GlobalWorkingDirectory, Hostname, credentials);



            //Threads for queueing ClientRun() method in client class

            ThreadPool.QueueUserWorkItem(serverClient.IPKServerClientRun, client);

        }



    }

}