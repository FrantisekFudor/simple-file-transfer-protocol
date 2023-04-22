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
    /// IPKServerClient accepted client class
    /// </summary>
    public class IPKServerClient
    {
        /// <summary>
        /// Global properties 
        /// </summary>
        private bool LoggedIn = false;
        private string Account = "";
        private string WorkingDirectoryPath = "";
        private string Hostname = "";
        private Dictionary<string, string> Credentials;
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;


        /// <summary>
        /// Server client constructor
        /// </summary>
        /// <param name="client">Accepted TcpClient socket</param>
        /// <param name="_workingDirectoryPath">Working directory</param>
        /// <param name="_hostname">Name of server</param>
        /// <param name="_credentials">Dictionary with credentials</param>
        public IPKServerClient(TcpClient client, string _workingDirectoryPath, string _hostname, Dictionary<string, string> _credentials)
        {
            _client = client;
            _stream = client.GetStream();
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream);
            WorkingDirectoryPath = _workingDirectoryPath;
            Hostname = _hostname;
            Credentials = _credentials;
        }

        /// <summary>
        /// Handling client's messages on server side
        /// </summary>
        /// <param name="obj">Callback object</param>
        public void IPKServerClientRun(Object obj)
        {
            //Send welcome message to client
            _writer.WriteLine("R: +" + Hostname + " SFTP Service" + null);
            _writer.Flush();


            //Read welcome message from client
            string data = null;
            data = _reader.ReadLine();
            Console.WriteLine("S: " + data);

            //Try reading from client
            try
            {
                //Infinite loop for reading stream from client
                while ((data = _reader.ReadLine()) != null)
                {
                    //Write message from client on output
                    Console.WriteLine("S: " + data + " <" + Account + ">");
                    string message = null;

                    //Get command from message
                    string cmd = GetCommand(data);

                    //If message is empty or invalid
                    if (cmd.Equals(""))
                    {
                        //Write invalid command
                        InvalidCommand();
                    }
                    else
                    {
                        //Execute command
                        switch (cmd)
                        {
                            case "USER":
                            case "ACCT":
                                ACCT(data, Credentials);
                                break;
                            case "PASS":
                                PASS(data, Credentials);
                                break;
                            case "TYPE":
                                TYPE(data);
                                break;
                            case "LIST":
                                LIST(data);
                                break;
                            case "CDIR":
                                CDIR(data);
                                break;
                            case "KILL":
                                KILL(data);
                                break;
                            case "NAME":
                                NAME(data);
                                break;
                            case "DONE":
                                DONE(data);
                                break;
                            case "RETR":
                                RETR(data);
                                break;
                            case "STOR":
                                STOR(data);
                                break;
                            default:
                                InvalidCommand();
                                break;
                        }
                    }
                }
            }
            //Client lost or disconnected
            catch (Exception ex)
            {
                Console.WriteLine("R: -" + Hostname + " closing connection (client disconnected)" + null);
            }
        }

        /// <summary>
        /// Split message from client and returns valid command
        /// </summary>
        /// <param name="data">Message from client</param>
        /// <returns>If success valid command otherwise empty string</returns>
        private string GetCommand(string data)
        {
            var tmp = data.Split(' ').First();
            if (tmp != null && IsValidCommand(tmp))
            {
                return tmp;
            }
            else
            {
                return "";
            }

        }

        /// <summary>
        /// Returns true if input string is equals valid SFTP command
        /// </summary>
        /// <param name="data">Input string</param>
        /// <returns>True if input string is valid command otherwise false</returns>
        private bool IsValidCommand(string data)
        {

            if (data.Equals("USER") || data.Equals("ACCT") || data.Equals("PASS") || data.Equals("TYPE") || data.Equals("LIST") || data.Equals("CDIR") || data.Equals("KILL") || data.Equals("NAME") || data.Equals("DONE") || data.Equals("RETR") || data.Equals("STOR"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Writes invalid command message to client
        /// </summary>
        private void InvalidCommand()
        {
            _writer.WriteLine("R: -Invalid command" + null);
            _writer.Flush();
            Console.WriteLine("R: -Invalid command" + null);
        }

        /// <summary>
        /// Writes already logged in message to client
        /// </summary>
        private void AlreadyLoggedIn()
        {
            _writer.WriteLine("R: ! " + Account + " already logged in" + null);
            _writer.Flush();
            Console.WriteLine("R: ! " + Account + " already logged in" + null);
        }

        /// <summary>
        /// Writes access denied message to client
        /// </summary>
        private void AccessDenied()
        {
            _writer.WriteLine("R: -Access denied, please logg in" + null);
            _writer.Flush();
            Console.WriteLine("R: -Access denied, please logg in" + null);
        }

        /// <summary>
        /// Executes ACCT SFTP command
        /// </summary>
        /// <param name="data">Message from client</param>
        /// <param name="credentials">Dictionary of existing credentials</param>
        private void ACCT(string data, Dictionary<string, string> credentials)
        {

            //If client is already logged in
            if (LoggedIn)
            {
                AlreadyLoggedIn();
                return;
            }

            //Split message to get argument
            var tmp = data.Split(' ');
            if (tmp.Length == 1)
            {
                //Write message to client
                _writer.WriteLine("R: -Wrong number of arguments" + null);
                _writer.Flush();
                Console.WriteLine("R: -Wrong number of arguments" + null);
            }
            else if (tmp.Length == 2)
            {
                //If arguments are valid
                if (tmp[0] != null && tmp[1] != null)
                {
                    //If dictionary credentials contains account
                    if (credentials.ContainsKey(tmp[1]))
                    {
                        //Set global property account
                        Account = tmp[1];

                        //Write message to client
                        _writer.WriteLine("R: +Account " + tmp[1] + " valid, send password" + null);
                        _writer.Flush();
                        Console.WriteLine("R: +Account " + tmp[1] + " valid, send password" + null);

                    }
                    //Invalid account
                    else
                    {
                        //Write message to client
                        _writer.WriteLine("R: -Invalid account " + tmp[1] + ", try again" + null);
                        _writer.Flush();
                        Console.WriteLine("R: -Invalid account " + tmp[1] + ", try again" + null);

                    }
                }
                //Invalid arguments
                else
                {
                    //Write message to client
                    _writer.WriteLine("R: -Wrong arguments given" + null);
                    _writer.Flush();
                    Console.WriteLine("R: -Wrong arguments given" + null);

                }
            }
            //Invalid number of arguments
            else
            {
                //Write message to client
                _writer.WriteLine("R: -Wrong number of arguments" + null);
                _writer.Flush();
                Console.WriteLine("R: -Wrong number of arguments" + null);

            }
        }

        /// <summary>
        /// Executes PASS SFTP command
        /// </summary>
        /// <param name="data">Message from client</param>
        /// <param name="credentials">Dictionary of existing credentials</param>
        private void PASS(string data, Dictionary<string, string> credentials)
        {
            //If client is already logged in
            if (LoggedIn)
            {
                AlreadyLoggedIn();
                return;
            }

            //Split message to get argument
            var tmp = data.Split(' ');

            if (tmp.Length == 1)
            {
                //Write message to client
                _writer.WriteLine("R: -Wrong number of arguments" + null);
                _writer.Flush();
                Console.WriteLine("R: -Wrong number of arguments" + null);
            }
            else if (tmp.Length == 2)
            {
                //If there is no account yet
                if (Account.Equals(""))
                {
                    //Write message to client
                    _writer.WriteLine("R: +Send account" + null);
                    _writer.Flush();
                    Console.WriteLine("R: +Send account" + null);
                    return;
                }

                //If arguments are valid
                if (tmp[0] != null && tmp[1] != null)
                {
                    //Try and get value of key in dictionary which means variable value will be password for global property account
                    string value;
                    credentials.TryGetValue(Account, out value);

                    //If password is valid
                    if (value != null && value.Equals(tmp[1]))
                    {
                        //Sets global property LoggedIn to true
                        LoggedIn = true;

                        //Write message to client
                        _writer.WriteLine("R: ! " + Account + " logged in " + null);
                        _writer.Flush();
                        Console.WriteLine("R: ! " + Account + " logged in" + null);
                    }
                    //Invalid password
                    else
                    {
                        //Write message to client
                        _writer.WriteLine("R: -Wrong password, try again" + null);
                        _writer.Flush();
                        Console.WriteLine("R: -Wrong password, try again" + null);
                    }
                }
                //Invalid arguments
                else
                {
                    //Write message to client
                    _writer.WriteLine("R: -Wrong arguments given" + null);
                    _writer.Flush();
                    Console.WriteLine("R: -Wrong arguments given" + null);
                }
            }
            //Invalid number of arguments
            else
            {
                //Write message to client
                _writer.WriteLine("R: -Wrong number of arguments" + null);
                _writer.Flush();
                Console.WriteLine("R: -Wrong number of arguments" + null);
            }

        }

        /// <summary>
        /// Executes DONE SFTP command
        /// </summary>
        /// <param name="hostname">Hostname of server</param>
        private void DONE(string hostname)
        {
            //Write message to client
            _writer.WriteLine("R: +" + hostname + " closing connection" + null);
            _writer.Flush();
            Console.WriteLine("R: +" + hostname + " closing connection" + null);

        }

        /// <summary>
        /// Executes KILL SFTP command
        /// </summary>
        /// <param name="data">Message from client</param>
        private void KILL(string data)
        {

            //If client is not logged in
            if (!LoggedIn)
            {
                AccessDenied();
                return;
            }

            //Try split message to get argument
            try
            {
                var tmpTry = data.Remove(0, 5);
            }
            //Split went wrong
            catch (Exception ex)
            {
                //Write message to client
                _writer.WriteLine("R: -Not deleted (file doesn't exists)" + null);
                _writer.Flush();
                Console.WriteLine("R: -Not deleted (file doesn't exists)" + null);
                return;
            }

            //Split message to get argument
            var tmp = data.Remove(0, 5);

            //If file exists
            if (File.Exists(WorkingDirectoryPath + "/" + tmp))
            {
                //Delete existing file
                File.Delete(WorkingDirectoryPath + "/" + tmp);

                //Write message to client
                _writer.WriteLine("R: +" + tmp + " deleted " + null);
                _writer.Flush();
                Console.WriteLine("R: +" + tmp + " deleted " + null);
            }
            //File doesn't exists
            else
            {
                //Write message to client
                _writer.WriteLine("R: -Not deleted (file doesn't exists)" + null);
                _writer.Flush();
                Console.WriteLine("R: -Not deleted (file doesn't exists)" + null);
            }
        }

        /// <summary>
        /// Executes CDIR SFTP command
        /// </summary>
        /// <param name="data">Message from client</param>
        private void CDIR(string data)
        {
            //If client is not logged in
            if (!LoggedIn)
            {
                AccessDenied();
                return;
            }

            //Try split message to get argument
            try
            {
                var tmpTry = data.Remove(0, 5);
            }
            //Split went wrong
            catch (Exception ex)
            {
                //Write message to client
                _writer.WriteLine("R: -Can't connect to directory (invalid directory)" + null);
                _writer.Flush();
                Console.WriteLine("R: -Can't connect to directory (invalid directory)" + null);
                return;
            }

            //Split message to get argument
            var tmp = data.Remove(0, 5);

            //If server already works in asked directory
            if (tmp.Equals(WorkingDirectoryPath))
            {
                //Write message to client
                _writer.WriteLine("R: +You are already working in this directory" + null);
                _writer.Flush();
                Console.WriteLine("R: +You are already working in this directory" + null);
                return;
            }

            //If directory exists
            if (Directory.Exists(tmp))
            {
                //Change working directory path
                WorkingDirectoryPath = tmp;

                //Write message to client
                _writer.WriteLine("R: !Changed working dir to " + tmp + null);
                _writer.Flush();
                Console.WriteLine("R: !Changed working dir to " + tmp + null);
            }
            //Directory doesn't exists
            else
            {
                //Write message to client
                _writer.WriteLine("R: -Can't connect to directory (invalid directory)" + null);
                _writer.Flush();
                Console.WriteLine("R: -Can't connect to directory (invalid directory)" + null);
            }
        }

        /// <summary>
        /// Executes NAME SFTP command
        /// </summary>
        /// <param name="data">Message from client</param>
        private void NAME(string data)
        {

            //If client is not logged in
            if (!LoggedIn)
            {
                AccessDenied();
                return;
            }

            //Try split message to get argument
            try
            {
                var tmpTry = data.Remove(0, 5);
            }
            //Split went wrong
            catch (Exception ex)
            {
                //Write message to client
                _writer.WriteLine("R: -Can't find this file, don't send TOBE" + null);
                _writer.Flush();
                Console.WriteLine("R: -Can't find this file, don't send TOBE" + null);
                return;
            }

            //Split message to get argument
            var tmp = data.Remove(0, 5);

            //If file to be renamed exists
            if (File.Exists(WorkingDirectoryPath + "/" + tmp))
            {

                //Write message to client
                _writer.WriteLine("R: +File exists, send 'TOBE <new_name_of_file>'" + null);
                _writer.Flush();
                Console.WriteLine("R: +File exists, send 'TOBE <new_name_of_file>'" + null);

                //Read message from client
                var _data = _reader.ReadLine();
                Console.WriteLine("S: {0}" + null, _data);

                //If recieved message is command TOBE
                string cmd = _data.Split(' ').First();
                if (cmd.Equals("TOBE"))
                {
                    //Try split message to get argument
                    try
                    {
                        var tmpTry = _data.Remove(0, 5);
                    }
                    //Split went wrong
                    catch (Exception ex)
                    {
                        //Write message to client
                        _writer.WriteLine("R: -Rename aborted, file could not be renamed, don't send TOBE" + null);
                        _writer.Flush();
                        Console.WriteLine("R: -Rename aborted, file could not be renamed, don't send TOBE" + null);
                        return;
                    }

                    //Split message to get argument
                    var _tmp = _data.Remove(0, 5);

                    //If file already exists with that name
                    if (File.Exists(WorkingDirectoryPath + "/" + _tmp))
                    {
                        //Write message to client
                        _writer.WriteLine("R: -Rename aborted, file with this name already exists, don't send TOBE" + null);
                        _writer.Flush();
                        Console.WriteLine("R: -Rename aborted, file with this name already exists, don't send TOBE" + null);
                    }
                    //If file doesn't exists 
                    else
                    {
                        //Try to rename file
                        try
                        {
                            //Creates file with new name and moves content from old file into it then deletes old file
                            FileInfo fi = new FileInfo(WorkingDirectoryPath + "/" + tmp);
                            fi.MoveTo(WorkingDirectoryPath + "/" + _tmp);

                            //Write message to client
                            _writer.WriteLine("R: +<" + tmp + "> renamed to <" + _tmp + ">" + null);
                            _writer.Flush();
                            Console.WriteLine("R: +< " + tmp + " > renamed to < " + _tmp + " > " + null);
                        }
                        //If something went wrong
                        catch (Exception ex)
                        {
                            //Write message to client
                            _writer.WriteLine("R: -Rename aborted, file could not be renamed, don't send TOBE" + null);
                            _writer.Flush();
                            Console.WriteLine("R: -Rename aborted, file could not be renamed, don't send TOBE" + null);
                        }

                    }

                }
                //Recieved command is invalid, expected TOBE command
                else
                {
                    //Write message to client
                    _writer.WriteLine("R: -Rename aborted, expected command 'TOBE, don't send TOBE'" + null);
                    _writer.Flush();
                    Console.WriteLine("R: -Rename aborted, expected command 'TOBE, don't send TOBE'" + null);
                }
            }
            //Invalid file to be renamed, file doesn't exists
            else
            {
                //Write message to client
                _writer.WriteLine("R: -Can't find <" + tmp + ">, don't send TOBE" + null);
                _writer.Flush();
                Console.WriteLine("R: -Can't find <" + tmp + ">, don't send TOBE" + null);
            }
        }

        /// <summary>
        /// Executes LIST SFTP command
        /// </summary>
        /// <param name="data">Message from client</param>
        private void LIST(string data)
        {

            //If client is not logged in
            if (!LoggedIn)
            {
                AccessDenied();
                return;
            }

            //Try to get substring 'LIST {F|V}' 
            try
            {
                var tmpTry = data.Substring(0, 6);
            }
            //Get substring went wrong
            catch (Exception ex)
            {
                //Write message to client
                _writer.WriteLine("R: -Wrong arguments, try 'LIST [F | V] {path}', where [] are required and {} optional" + null);
                _writer.Flush();
                Console.WriteLine("R: -Wrong arguments, try 'LIST [F | V] {path}', where [] are required and {} optional" + null);
                return;
            }

            //Get substring 'LIST {F|V}' 
            var tmp = data.Substring(0, 6);

            //Remove 'LIST '
            var param = tmp.Remove(0, 5);

            //If parameter was 'V'
            if (param[0].Equals('V'))
            {
                //Try to get path if path was explicitly inserted
                try
                {
                    //Remove 'LIST V '
                    var path = data.Remove(0, 7);

                    //If path leads to existing directory
                    if (Directory.Exists(path))
                    {

                        //Get name of directory
                        FileInfo dirInfo = new FileInfo(path);

                        //Write and send message "R: +{dir}: \n"
                        string message = "R: +" + dirInfo.Name + ":";
                        _writer.WriteLine(message);
                        _writer.Flush();
                        Console.WriteLine(message);

                        //Get list of files in asked directory
                        List<string> list = Directory.GetFiles(path).ToList();

                        //Foreach file
                        foreach (string s in list)
                        {
                            //Get file info
                            FileInfo fileInfo = new FileInfo(s);

                            //Concat message with informations of a file
                            message = string.Concat(fileInfo.Name + "\t" + fileInfo.Length + "B\t" + fileInfo.CreationTimeUtc);

                            //Write message to client
                            _writer.WriteLine(message);
                            _writer.Flush();
                            Console.WriteLine(message);
                        }

                        //Write message to client - this message indicates stop reading in cycle
                        _writer.WriteLine("done");
                        _writer.Flush();

                    }
                    //Invalid path to directory, directory doesn't exists
                    else
                    {
                        //Write message to client
                        _writer.WriteLine("R: -Can't connect to directory (directory doesn't exists)" + null);
                        _writer.Flush();
                        Console.WriteLine("R: -Can't connect to directory (directory doesn't exists)" + null);

                    }

                }
                //If path wasn't explicitly inserted, get files in working directory
                catch (Exception ex)
                {
                    //Get name of directory
                    FileInfo dirInfo = new FileInfo(WorkingDirectoryPath);

                    //Write and send message "R: +{dir}: \n"
                    string message = "R: +" + dirInfo.Name + ":";
                    _writer.WriteLine(message);
                    _writer.Flush();
                    Console.WriteLine(message);

                    //Get list of files in working directory
                    List<string> list = Directory.GetFiles(WorkingDirectoryPath).ToList();

                    foreach (string s in list)
                    {
                        //Get file info
                        FileInfo fileInfo = new FileInfo(s);

                        //Concat message with informations of a file
                        message = string.Concat(fileInfo.Name + "\t" + fileInfo.Length + "B\t" + fileInfo.CreationTimeUtc);

                        //Write message to client
                        _writer.WriteLine(message);
                        _writer.Flush();
                        Console.WriteLine(message);
                    }

                    //Write message to client - this message indicates stop reading in cycle
                    _writer.WriteLine("done");
                    _writer.Flush();

                }
            }
            //If parameter was 'F'
            else if (param[0].Equals('F'))
            {
                //Try get path if path was explicitly inserted
                try
                {
                    //Remove 'LIST F '
                    var path = data.Remove(0, 7);

                    //If path leads to existing directory
                    if (Directory.Exists(path))
                    {
                        //Get name of directory
                        FileInfo dirInfo = new FileInfo(path);

                        //Write and send message "R: +{dir}: \n"
                        string message = "R: +" + dirInfo.Name + ":";
                        Console.WriteLine(message);
                        _writer.WriteLine(message);
                        _writer.Flush();

                        //Get list of files in working directory
                        List<string> list = Directory.GetFiles(path).ToList();

                        foreach (string s in list)
                        {
                            //Get file info
                            FileInfo fileInfo = new FileInfo(s);

                            //Write message to client with file's name
                            message = fileInfo.Name;
                            Console.WriteLine(message);
                            _writer.WriteLine(message);
                            _writer.Flush();
                        }

                        //Write message to client - this message indicates stop reading in cycle
                        _writer.WriteLine("done");
                        _writer.Flush();


                    }
                    //Invalid path to directory, directory doesn't exists
                    else
                    {
                        //Write message to client
                        _writer.WriteLine("R: -Can't connect to directory (directory doesn't exists)" + null);
                        _writer.Flush();
                        Console.WriteLine("R: -Can't connect to directory (directory doesn't exists)" + null);

                    }

                }
                //If path wasn't explicitly inserted, get files in working directory
                catch (Exception ex)
                {

                    //Get name of directory
                    FileInfo dirInfo = new FileInfo(WorkingDirectoryPath);

                    //Write and send message "R: +{dir}: \n"
                    string message = "R: +" + dirInfo.Name + ":";
                    Console.WriteLine(message);
                    _writer.WriteLine(message);
                    _writer.Flush();

                    //Get list of files in working directory
                    List<string> list = Directory.GetFiles(WorkingDirectoryPath).ToList();

                    //Foreach file
                    foreach (string s in list)
                    {
                        //Get file info
                        FileInfo fileInfo = new FileInfo(s);

                        //Write message to client with file's name
                        message = fileInfo.Name;
                        Console.WriteLine(message);
                        _writer.WriteLine(message);
                        _writer.Flush();
                    }

                    //Write message to client - this message indicates stop reading in cycle
                    _writer.WriteLine("done");
                    _writer.Flush();

                }
            }
            //Wrong arguments given
            else
            {
                //Write message to client
                _writer.WriteLine("R: -Wrong arguments, try 'LIST [F | V] {path}', where [] are required and {} optional" + null);
                _writer.Flush();
                Console.WriteLine("R: -Wrong arguments, try 'LIST [F | V] {path}', where [] are required and {} optional" + null);
                return;
            }
        }

        /// <summary>
        /// Not implemented RETR SFTP command
        /// </summary>
        /// <param name="data"></param>
        private void RETR(string data)
        {
            //Write message to client
            _writer.WriteLine("R: " + data + " not implemented yet");
            _writer.Flush();
            Console.WriteLine("R: " + data + " not implemented yet");
        }

        /// <summary>
        /// Not implemented STOR SFTP command
        /// </summary>
        /// <param name="data"></param>
        private void STOR(string data)
        {
            //Write message to client
            _writer.WriteLine("R: " + data + " not implemented yet");
            _writer.Flush();
            Console.WriteLine("R: " + data + " not implemented yet");
        }

        /// <summary>
        /// Not implemented TYPE SFTP command
        /// </summary>
        /// <param name="data"></param>
        private void TYPE(string data)
        {
            //Write message to client
            _writer.WriteLine("R: " + data + " not implemented yet");
            _writer.Flush();
            Console.WriteLine("R: " + data + " not implemented yet");
        }
    }
}
