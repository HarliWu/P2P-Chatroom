using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace P2PChatroom
{
    class Program
    {
        private static UdpClient listener;
        private static List<UdpClient> UdpClients; // connect to others
        private static string name;
        private static int port;
        private static string GetIpAddress()
        {
            string hostname = Dns.GetHostName();
            IPHostEntry myIP = Dns.GetHostEntry(hostname);
            IPAddress[] addresses = myIP.AddressList;
            string ipv4 = "";

            foreach (IPAddress ip in addresses)
            {
                //Debug.Log(ip.ToString());
                if(ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if(ipv4 == "")
                    {
                        ipv4 = ip.ToString();
                    }
                    else
                    {
                        ipv4 = ipv4 + "; " + ip.ToString();
                    }
                }
            }

            return ipv4;
            //return addresses[0].ToString();
        }
        private static void EstablishConnection(byte[] data)
        {
            string[] seg = Encoding.ASCII.GetString(data).Split(' ');
            if (seg[0] == "Accept") {
                for(int i = 1; i < seg.Length; i = i + 2)
                {
                    port += 1;
                    UdpClient client = new UdpClient(port);
                    client.Connect(seg[i], int.Parse(seg[i + 1]));
                    byte[] content = Encoding.ASCII.GetBytes("Greeting " + name);
                    client.Send(content, content.Length);
                    UdpClients.Add(client);
                    new Task(() => Listen(client)).Start();
                }
            }
        }

        private static string RequestStatus;
        private static void Listen(UdpClient client)
        {
            while (true)
            {
                var result = client.ReceiveAsync().Result;
                IPEndPoint RemoteIPEndPoint = result.RemoteEndPoint;
                byte[] receiveBytes = result.Buffer;
                Console.WriteLine("Message from " + RemoteIPEndPoint.Address.ToString() + " Port " + RemoteIPEndPoint.Port.ToString() + ": " +
                        Encoding.ASCII.GetString(receiveBytes));
                switch (Encoding.ASCII.GetString(receiveBytes).Split(' ')[0])
                {
                    case "Join":
                        port += 1;
                        RequestStatus = "Accept " + GetIpAddress() + " " + port.ToString();
                        SendMessage("NewCommer " + Encoding.ASCII.GetString(receiveBytes).Split(' ')[1]);
                        while (RequestStatus != "Reject" && RequestStatus.Split(' ').Length < UdpClients.Count);
                        client.Connect(RemoteIPEndPoint.Address.ToString(), RemoteIPEndPoint.Port);
                        byte[] content = Encoding.ASCII.GetBytes(RequestStatus);
                        client.Send(content, content.Length);
                        break;
                    case "NewCommer":
                        port += 1;
                        UdpClient newClient = new UdpClient(port);
                        UdpClients.Add(newClient);
                        new Task(() => Listen(newClient)).Start();
                        content = Encoding.ASCII.GetBytes("AcceptRequest " + Encoding.ASCII.GetString(receiveBytes).Split(' ')[1] + " " + 
                            GetIpAddress() + " " + port.ToString());
                        client.Send(content, content.Length);
                        break;
                    case "Greeting":
                        client.Connect(RemoteIPEndPoint.Address, RemoteIPEndPoint.Port);
                        content = Encoding.ASCII.GetBytes("Hi, I am " + name);
                        client.Send(content, content.Length);
                        break;
                    case "RejectRequest":
                        RequestStatus = "Reject";
                        break;
                    case "AcceptRequest":
                        if(RequestStatus != "Reject")
                        {
                            RequestStatus = RequestStatus + " " + Encoding.ASCII.GetString(receiveBytes).Split(' ')[2] + " " +
                                Encoding.ASCII.GetString(receiveBytes).Split(' ')[3];
                        }
                        break;
                }
            }
        }

        private static void SendMessage(string msg)
        {
            byte[] content = Encoding.ASCII.GetBytes(msg);
            Parallel.ForEach(UdpClients, client =>
            {
                client.SendAsync(content, content.Length);
            });
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to P2P chatroom (beta version). Copyright (c) 2019 by Harli Ng. \n");
            Console.WriteLine("Please keep connection when you connect. Don't help me test the bugs because I know it has bugs!!! " +
                "\nPress TAB if you want to say something. " +
                "\nIf you want to leave, send 'Disconnect' (single quotation marks are excluded). \n");
            Console.Write("Your IP Address: ");
            Console.WriteLine(GetIpAddress());
            Console.Write("Port: ");
            port = int.Parse(Console.ReadLine());
            Console.Write("Name: ");
            while((name = Console.ReadLine()).Split(' ').Length > 1)
            {
                Console.Write("Invalid name. Give a new one. \nName: ");
            }

            
            SocketProgramming.Run(port, name);
            /*UdpClients = new List<UdpClient>();
            listener = new UdpClient(port);
            Console.WriteLine("Please select which one you prefer: (1) Connect to others; (2) Waiting others join");
            switch(Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    Console.Write("IP address connected to: ");
                    string ip = Console.ReadLine();
                    Console.Write("Destination port: ");
                    int destport = int.Parse(Console.ReadLine());
                    listener.Connect(ip, destport);
                    byte[] content = Encoding.ASCII.GetBytes("Join " + name);
                    listener.Send(content, content.Length);
                    IPEndPoint RemoteIPEndPoint = new IPEndPoint(IPAddress.Any, port);
                    byte[] receiveBytes = listener.Receive(ref RemoteIPEndPoint);
                    Console.WriteLine("Message from " + RemoteIPEndPoint.Address.ToString() + " Port " + RemoteIPEndPoint.Port.ToString() + ": " +
                        Encoding.ASCII.GetString(receiveBytes));
                    EstablishConnection(receiveBytes);
                    break;
                case ConsoleKey.D2:
                    
                    break;
            }
            new Task(() => Listen(listener)).Start();
            while (true) ;*/
        }
    }
}
