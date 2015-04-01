using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace SumServer
{
    internal class Program
    {
        //This service host attend and read service entries
        private static ServiceHost sumHostReadSvc = null;

        //This service host send data to connected clients
        private static ServiceHost sumHostWriteSvc = null;

        private const string UriPattern = "http://localhost:{0}/SumServer/{1}";

        private enum ConfigPortMode
        {
            Read, Write
        }

        private static void Main(string[] args)
        {
            Console.Title = "Sum Server";
            SendMessageToConsole("\t\t\tWelcome to Sum server", ConsoleColor.Green);
            SendMessageToConsole(string.Empty);
            int readPort = -100;
            int writePort = -100;
            readPort = RequestPortConfig(ConfigPortMode.Read);
            writePort = RequestPortConfig(ConfigPortMode.Write);
            while (readPort.Equals(writePort))
            {
                Console.Clear();
                SendMessageToConsole("\t\t\tWelcome to Sum server", ConsoleColor.Green);
                SendMessageToConsole(string.Empty);
                SendMessageToConsole("The port reading and writing should be different", ConsoleColor.Red);
                readPort = RequestPortConfig(ConfigPortMode.Read);
                writePort = RequestPortConfig(ConfigPortMode.Write);
                //Exit
                if (readPort.Equals(-200) || writePort.Equals(-200))
                {
                    SendMessageToConsole("Bye!!!", ConsoleColor.Green);
                    Environment.Exit(0);
                }
            }

            if (readPort > 0 && writePort > 0)
            {
                bool isConfirmed = RequestPortConfigConfirm(readPort, writePort);
                while (!isConfirmed)
                {
                    Console.Clear();
                    SendMessageToConsole("\t\t\tWelcome to Sum server", ConsoleColor.Green);
                    SendMessageToConsole(string.Empty);
                    readPort = RequestPortConfig(ConfigPortMode.Read);
                    writePort = RequestPortConfig(ConfigPortMode.Write);
                    isConfirmed = RequestPortConfigConfirm(readPort, writePort);
                }
                //Start services

                int readServiceStartStatus = StartReadService(readPort);
                int writeServiceStartStatus = StartWriteService(writePort);

                SendMessageToConsole("Press <Enter> to stop the service config tool.", ConsoleColor.Red, false);
                Console.ReadKey();

                SendMessageToConsole("Please wait while the ports are closed and resources are disposed...", ConsoleColor.Blue);

                if (sumHostReadSvc != null && (sumHostReadSvc.State == CommunicationState.Opened || sumHostReadSvc.State == CommunicationState.Opening))
                    sumHostReadSvc.Close();
                if (sumHostWriteSvc != null && (sumHostWriteSvc.State == CommunicationState.Opened || sumHostWriteSvc.State == CommunicationState.Opening))
                    sumHostWriteSvc.Close();
                sumHostReadSvc = null;
                sumHostWriteSvc = null;
            }
        }

        private static bool RequestPortConfigConfirm(int readPort, int writePort)
        {
            bool vRet = false;

            string readBaseAddress = string.Format(UriPattern, readPort.ToString(), "ReadData");
            string writeBaseAddress = string.Format(UriPattern, writePort.ToString(), "WriteData");

            SendMessageToConsole("\r\nThe server configuration is:", ConsoleColor.DarkYellow);

            SendMessageToConsole("\r\nRead URI: " + readBaseAddress, ConsoleColor.White);
            SendMessageToConsole("\r\nWrite URI: " + writeBaseAddress, ConsoleColor.White);
            SendMessageToConsole("You confirm this setting? (<<Y>> for confirm: ", ConsoleColor.DarkRed, false);
            if (Console.ReadLine().ToLower().Equals("y"))
                vRet = true;
            return vRet;
        }

        /// <summary>
        /// Request the user configuration port
        /// </summary>
        /// <param name="mode">Read/Write</param>
        /// <returns>The user specified port</returns>
        private static int RequestPortConfig(ConfigPortMode mode)
        {
            string msg = "Please specify the port listenig to {0} client data:  ";

            if (mode == ConfigPortMode.Read)

                msg = string.Format(msg, "read");

            if (mode == ConfigPortMode.Write)
                msg = string.Format(msg, "write");

            SendMessageToConsole(msg, ConsoleColor.DarkBlue, false);
            int readPort = -100;
            string userEntryForReadPort = Console.ReadLine();
            if (int.TryParse(userEntryForReadPort, out readPort))
            {
                readPort = int.Parse(userEntryForReadPort);
                if (readPort < 1)
                {
                    SendMessageToConsole("The specified port is invalid. Please specify numeric values only positive integers. Press <<Q>> for exit Sum server.", ConsoleColor.Red);
                    readPort = RequestPortConfig(mode);
                }
            }
            else
            {
                if (userEntryForReadPort.ToLower().Equals("q"))
                    return -200;
                SendMessageToConsole("The specified port is invalid. Please specify numeric values only positive integers. Press <<Q>> for exit Sum server.", ConsoleColor.Red);
                readPort = RequestPortConfig(mode);
            }
            return readPort;
        }

        private static void SendMessageToConsole(string message, ConsoleColor color = ConsoleColor.Black, bool isLine = true)
        {
            Console.ForegroundColor = color;
            if (isLine)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.Write(message);
            }
            Console.ResetColor();
        }

        /// <summary>
        /// Method starts the listener data
        /// </summary>
        /// <param name="port">The listening port</param>
        private static int StartReadService(int port)
        {
            int vRet = -100;
            Uri baseAddress = new Uri(string.Format(UriPattern, port.ToString(), "ReadData"));
            // Create the ServiceHost.
            sumHostReadSvc = new ServiceHost(typeof(SumReadSocketServer), baseAddress);
            // Enable metadata publishing.
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
            sumHostReadSvc.Description.Behaviors.Add(smb);
            CustomBinding binding = new CustomBinding();
            binding.Elements.Add(new ByteStreamMessageEncodingBindingElement());
            HttpTransportBindingElement transport = new HttpTransportBindingElement();
            transport.WebSocketSettings.TransportUsage = WebSocketTransportUsage.Always;
            transport.WebSocketSettings.CreateNotificationOnConnection = true;
            binding.Elements.Add(transport);

            sumHostReadSvc.AddServiceEndpoint(typeof(ISumSocketServer), binding, "");

            //Set event handlers
            sumHostReadSvc.Faulted += sumHostReadSvc_Faulted;
            sumHostReadSvc.Opened += sumHostReadSvc_Opened;
            sumHostReadSvc.Closed += sumHostReadSvc_Closed;
            try
            {
                sumHostReadSvc.Open();
                SendMessageToConsole(string.Format("The read service is ready at {0}", baseAddress), ConsoleColor.Green);
                vRet = 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (ex is AddressAlreadyInUseException)
                {
                    SendMessageToConsole("Sorry, the selected port is already in use and can not be re-used.", ConsoleColor.Red);
                }
                else
                {
                    if (ex is AddressAccessDeniedException)
                    {
                        SendMessageToConsole("Sorry, the process is running without administrative privileges. You can not configure the port for the service.", ConsoleColor.Red);
                    }
                    else
                    {
                        SendMessageToConsole("Sorry, an unknown error occurred." + ex.GetType().ToString(), ConsoleColor.Red);
                    }
                }
            }

            return vRet;
        }

        /// <summary>
        /// Method starts the writer data
        /// </summary>
        /// <param name="port">The listening port</param>
        private static int StartWriteService(int port)
        {
            int vRet = -100;
            Uri baseAddress = new Uri(string.Format(UriPattern, port.ToString(), "WriteData"));
            // Create the ServiceHost.
            sumHostWriteSvc = new ServiceHost(typeof(SumWriteSocketServer), baseAddress);

            // Enable metadata publishing.
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
            sumHostWriteSvc.Description.Behaviors.Add(smb);

            CustomBinding binding = new CustomBinding();
            binding.Elements.Add(new ByteStreamMessageEncodingBindingElement());
            HttpTransportBindingElement transport = new HttpTransportBindingElement();
            transport.WebSocketSettings.TransportUsage = WebSocketTransportUsage.Always;
            transport.WebSocketSettings.CreateNotificationOnConnection = true;
            binding.Elements.Add(transport);

            sumHostWriteSvc.AddServiceEndpoint(typeof(ISumSocketServer), binding, "");

            //Set event handlers
            sumHostWriteSvc.Faulted += sumHostWriteSvc_Faulted;
            sumHostWriteSvc.Opened += sumHostWriteSvc_Opened;
            sumHostWriteSvc.Closed += sumHostWriteSvc_Closed;
            try
            {
                sumHostWriteSvc.Open();
                SendMessageToConsole(string.Format("The write service is ready at {0}", baseAddress), ConsoleColor.Green);
                vRet = 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                if (ex is AddressAlreadyInUseException)
                {
                    SendMessageToConsole("Sorry, the selected port is already in use and can not be re-used.", ConsoleColor.Red);
                }
                else
                {
                    if (ex is AddressAccessDeniedException)
                    {
                        SendMessageToConsole("Sorry, the process is running without administrative privileges. You can not configure the port for the service.", ConsoleColor.Red);
                    }
                    else
                    {
                        SendMessageToConsole("Sorry, an unknown error occurred." + ex.GetType().ToString(), ConsoleColor.Red);
                    }
                }
            }

            return vRet;
        }

        #region Svc EventHandlers

        private static void sumHostReadSvc_Faulted(object sender, EventArgs e)
        {
            if (sumHostReadSvc != null && (sumHostReadSvc.State == CommunicationState.Opened || sumHostReadSvc.State == CommunicationState.Opening))
            {
                sumHostReadSvc.Close();
            }
        }

        private static void sumHostReadSvc_Opened(object sender, EventArgs e)
        {
            SendMessageToConsole("The read service is now open...", ConsoleColor.Gray);
        }

        private static void sumHostReadSvc_Closed(object sender, EventArgs e)
        {
            SendMessageToConsole("The read service is now closed...", ConsoleColor.Gray);
        }

        private static void sumHostWriteSvc_Closed(object sender, EventArgs e)
        {
            SendMessageToConsole("The write service is now closed...", ConsoleColor.Gray);
        }

        private static void sumHostWriteSvc_Opened(object sender, EventArgs e)
        {
            SendMessageToConsole("The write service is now open...", ConsoleColor.Gray);
        }

        private static void sumHostWriteSvc_Faulted(object sender, EventArgs e)
        {
            if (sumHostWriteSvc != null && (sumHostWriteSvc.State == CommunicationState.Opened || sumHostWriteSvc.State == CommunicationState.Opening))
            {
                sumHostWriteSvc.Close();
            }
        }

        #endregion Svc EventHandlers
    }
}