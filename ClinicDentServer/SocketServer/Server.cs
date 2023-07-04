using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ClinicDentServer.SocketServer
{
    public class Server
    {
        public static string RequiredClientVersion { get; set; } = "0.01";
        IPAddress ipAddr;
        IPEndPoint ipEndPoint;
        System.Net.Sockets.Socket sListener;
        System.Threading.Thread thread;
        public object UsersLocker = new object();
        public List<User> Users { get; set; } = new List<User>();
        public void Start()
        {
            thread = new System.Threading.Thread(createConnection);
            thread.Start();
        }
        public void createConnection()
        {
            int portNumber = 12495;
            ipAddr = IPAddress.Parse("192.168.0.102");
            ipEndPoint = new IPEndPoint(ipAddr, portNumber);
            sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                sListener.Bind(ipEndPoint);
                sListener.Listen(10000);

                while (true)
                {
                    User newUser = new User(this, sListener.Accept());
                }
            }
            catch (Exception ex)
            {
                PrintMessage("Error 01: " + ex.Message);
            }
        }
        public void PrintMessage(string message)
        {
            Console.WriteLine(message);
        }
        public void Close()
        {
            sListener.Close();
            foreach (User u in Users)
            {
                u.leaveDetected();
            }
        }
    }
}
