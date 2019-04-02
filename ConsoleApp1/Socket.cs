using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace P2PChatroom
{
    class SocketProgramming
    {
        private static string GetIpAddress()
        {
            string hostname = Dns.GetHostName();
            IPHostEntry myIP = Dns.GetHostEntry(hostname);
            IPAddress[] addresses = myIP.AddressList;

            foreach (IPAddress ip in addresses)
            {
                //Debug.Log(ip.ToString());
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return null;
            //return addresses[0].ToString();
        }

        private static Socket socket;
        private static List<IPEndPoint> destips;
        private static string Name;
        private static bool join;
        private static bool EstablishConnection(string msg, IPEndPoint sender)
        {
            string[] seg = msg.Split(' ');
            //Console.WriteLine(seg[0]);
            if (string.Compare(seg[0], "Accept") == 0)
            {
                destips.Add(sender);
                for (int i = 1; i < seg.Length - 3; i = i + 2)
                {
                    //UdpClient client = new UdpClient(port);
                    //client.Connect(seg[i], int.Parse(seg[i + 1]));
                    //destips.Add(new IPEndPoint(IPAddress.Parse(seg[i]), int.Parse(seg[i + 1])));
                    IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(seg[i]), int.Parse(seg[i + 1]));
                    //byte[] content = Encoding.ASCII.GetBytes("Greeting " + name);
                    //socket.SendTo(content, ipEndPoint);
                    destips.Add(ipEndPoint);
                    //client.Send(content, content.Length);
                    //UdpClients.Add(client);
                    //new Task(() => Listen(client)).Start();
                }
                Send("Greeting " + Name);
                return true;
            }
            return false;
        }
        /*private static void CheckConnection()
        {
            while (true)
            {
                foreach(var ipEndPoint in destips)
                {
                    try
                    {
                        socket.Connect(ipEndPoint);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }*/
        
        /*private static void receive()
        {
            while (true)
            {
                EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                byte[] reply = new byte[65527];
                socket.ReceiveFrom(reply, ref sender);
                Console.WriteLine("Message from " + ((IPEndPoint)sender).Address.ToString() + " Port " + ((IPEndPoint)sender).Port.ToString() + ": " +
                    Encoding.ASCII.GetString(reply));
                switch(Encoding.ASCII.GetString(reply).Split(' ')[0])
                {
                    case "Join":
                        RequestStatus = "Accept";
                        Send("NewComer " + Encoding.ASCII.GetString(reply).Split(' ')[1]);
                        Console.WriteLine(RequestStatus.Split(' ').Length);
                        Console.WriteLine(destips.Count);
                        while (RequestStatus != "Reject" && RequestStatus.Split(' ').Length < destips.Count * 2) ;
                        Console.WriteLine("heyheyehey");
                        socket.SendTo(Encoding.ASCII.GetBytes(RequestStatus), sender);
                        break;
                    case "NewComer":
                        socket.SendTo(Encoding.ASCII.GetBytes("AcceptRequest"), sender);
                        break;
                    case "Greeting":
                        destips.Add((IPEndPoint)sender);
                        socket.SendTo(Encoding.ASCII.GetBytes("Hi, I am " + name), sender);
                        break;
                    case "RejectRequest":
                        RequestStatus = "Reject";
                        break;
                    case "AcceptRequest":
                        if (RequestStatus != "Reject")
                        {
                            RequestStatus = RequestStatus + " " + ((IPEndPoint)sender).Address.ToString() + " " + ((IPEndPoint)sender).Port.ToString();
                        }
                        break;
                }
            }
        }*/
        private static void Send(string msg)
        {
            //Console.WriteLine("sending to " + destips.Count + " users... " + msg);
            Parallel.ForEach(destips, ip =>
            {
                Send(msg, ip);
            });
        }
        private static void Send(string msg, EndPoint client)
        {
            try
            {
               // Console.WriteLine("send " + msg + " to " + ((IPEndPoint)client).Address.ToString() + ":" + ((IPEndPoint)client).Port.ToString());
                byte[] content = Encoding.ASCII.GetBytes(msg + " (sending timestamp: " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ")");
                socket.BeginSendTo(content, 0, content.Length, 0, client, new AsyncCallback(SendData), null);
            }
            catch (SocketException e)
            {
                Console.WriteLine("666IP Address: " + ((IPEndPoint)sender).Address.ToString() + " Port: " + ((IPEndPoint)sender).Port.ToString() + " has gone. ");
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.StackTrace);
                // Console.WriteLine(e.Source);
                Console.WriteLine(e.Message);
            }
        }

        private static void SendData(IAsyncResult ar)
        {
            try
            {
                socket.EndSendTo(ar);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private static void Message()
        {
            while (true)
            {
                if(Console.ReadKey(true).Key == ConsoleKey.Tab)
                {
                    Console.Write("You: ");
                    Send(Console.ReadLine());
                }
            }
        }
        private static byte[] data;
        private static EndPoint sender;
        private static void Receive()
        {
            data = new byte[8192];
            sender = new IPEndPoint(IPAddress.Any, 0);
            socket.BeginReceiveFrom(data, 0, data.Length, 0, ref sender, new AsyncCallback(ReceiveData), socket);
            //Console.WriteLine("1Message from " + ((IPEndPoint)sender).Address.ToString() + " Port " + ((IPEndPoint)sender).Port.ToString());
        }

        private static byte[] Buffer(byte[] real)
        {
            int r = real.Length - 1;
            //Console.WriteLine("real length: " + real.Length);

            while (r >= 0 && real[r--] == 0) ;
            byte[] result = new byte[r + 2];
            
            //Console.WriteLine("expected length: " + (r + 2).ToString());
            Array.Copy(real, result, r + 2);
            return result;
        }

        private static Dictionary<string, string> RequestStatus = new Dictionary<string, string>();
        private static void Confirmation(IPEndPoint sender, string name)
        {
            string ipAddr = sender.Address.ToString();
            int port = sender.Port;
            IPEndPoint receiver = new IPEndPoint(IPAddress.Parse(ipAddr), port);
            while (RequestStatus[name] != "Reject" && RequestStatus[name].Split(' ').Length < destips.Count * 2) ;
            Send(RequestStatus[name], receiver);
        }

        private static void ReceiveData(IAsyncResult ar)
        {
            //Console.Write("hi");
            try
            {
                sender = new IPEndPoint(IPAddress.Any, 0);
                socket.EndReceiveFrom(ar, ref sender);
                //Console.WriteLine("2Message from " + ((IPEndPoint)sender).Address.ToString() + " Port " + ((IPEndPoint)sender).Port.ToString());
                //int size = socket.ReceiveBufferSize;
                byte[] result = Buffer(data);
                string msg = Encoding.ASCII.GetString(result);
                Console.WriteLine("Timestamp: " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() +
                    " Message from " + ((IPEndPoint)sender).Address.ToString() + " Port " + ((IPEndPoint)sender).Port.ToString() + ": " + msg);
                switch (msg.Split(' ')[0])
                {
                    case "Join":
                        string name = msg.Split(' ')[1];
                        RequestStatus.Add(name, "Accept");
                        

                        //Console.WriteLine(RequestStatus.Split(' ').Length);
                        //Console.WriteLine(destips.Count);
                        //new Task(() => Receive()).Start();
                        IPEndPoint endPoint = new IPEndPoint(((IPEndPoint)sender).Address, ((IPEndPoint)sender).Port);
                        //socket.BeginReceiveFrom(data, 0, data.Length, 0, ref sender, new AsyncCallback(ReceiveData), socket);
                        //string IPAddress = ((IPEndPoint)sender).Address.ToString();
                        //new Task(() => CheckAlive());
                        new Task(() => Confirmation(endPoint, name)).Start();
                        //socket.EndReceiveFrom(ar, ref sender);
                        //Console.WriteLine("heyheyehey");
                        //socket.SendTo(Encoding.ASCII.GetBytes(RequestStatus), sender);
                        //Send(RequestStatus, endPoint);

                        Send("NewComer " + name);
                        break;

                    case "NewComer":
                        name = msg.Split(' ')[1];
                        //socket.SendTo(Encoding.ASCII.GetBytes("AcceptRequest"), sender);
                        Send("AcceptRequest " + name, sender);
                        //Console.WriteLine("Sended...");
                        break;

                    case "Greeting":
                        destips.Add((IPEndPoint)sender);
                        //socket.SendTo(Encoding.ASCII.GetBytes("Hi, I am " + name), sender);
                        Send("Hi, I am " + Name, sender);
                        break;

                    case "RejectRequest":
                        name = msg.Split(' ')[1];
                        RequestStatus[name] = "Reject";
                        break;

                    case "AcceptRequest":
                        name = msg.Split(' ')[1];
                        if (RequestStatus[name] != "Reject")
                        {
                            RequestStatus[name] = RequestStatus[name] + " " + ((IPEndPoint)sender).Address.ToString() + " " + ((IPEndPoint)sender).Port.ToString();
                        }
                        break;

                    case "Accept":
                        EstablishConnection(msg, (IPEndPoint)sender);
                        join = true;
                        break;

                    case "Reject":
                        join = true;
                        break;

                    case "CheckAlive":
                        Send("Alive", sender);
                        break;

                    case "Alive":
                        AliveList.Add((IPEndPoint)sender);
                        break;

                    case "Leave":
                        foreach(var ep in destips)
                        {
                            if (ep.Address.ToString().CompareTo(msg.Split(' ')[1]) == 0
                                && ep.Port.ToString().CompareTo(msg.Split(' ')[2]) == 0)
                            {
                                destips.Remove(ep);
                                break;
                            }
                        }
                        break;

                    case "Disconnect":
                        foreach (var ep in destips)
                        {
                            if (ep.Address.ToString().CompareTo(((IPEndPoint)sender).Address.ToString()) == 0
                                && ep.Port.ToString().CompareTo(((IPEndPoint)sender).Port.ToString()) == 0)
                            {
                                destips.Remove(ep);
                                break;
                            }
                        }
                        break;
                }

                data = new byte[8192];
                //Console.WriteLine("hello");
                sender = new IPEndPoint(IPAddress.Any, 0);
                /*if (flag)
                {
                    return;
                }*/
                //Console.WriteLine("hey");
                new Task(() => socket.BeginReceiveFrom(data, 0, data.Length, 0, ref sender, new AsyncCallback(ReceiveData), socket)).Start();
                //Console.WriteLine("4Message from " + ((IPEndPoint)sender).Address.ToString() + " Port " + ((IPEndPoint)sender).Port.ToString());
                //Console.WriteLine("Lucky");
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.StackTrace);
                // Console.WriteLine(e.Source);
                Console.WriteLine(e.Message);
                Console.WriteLine(((IPEndPoint)sender).Address.ToString() + " " + ((IPEndPoint)sender).Port.ToString());

                Console.WriteLine("IP Address: " + ((IPEndPoint)sender).Address.ToString() + " Port: " + ((IPEndPoint)sender).Port.ToString() + " has gone. ");

                foreach (var ep in destips)
                {
                    if (ep.Address.ToString().CompareTo(((IPEndPoint)sender).Address.ToString()) == 0
                        && ep.Port.ToString().CompareTo(((IPEndPoint)sender).Port.ToString()) == 0)
                    {
                        destips.Remove(ep);
                        break;
                    }
                }

                Send("Leave " + ((IPEndPoint)sender).Address.ToString() + " " + ((IPEndPoint)sender).Port.ToString());

                data = new byte[8192];
                //Console.WriteLine("hello");
                sender = new IPEndPoint(IPAddress.Any, 0);
                /*if (flag)
                {
                    return;
                }*/
                //Console.WriteLine("hey");
                socket.BeginReceiveFrom(data, 0, data.Length, 0, ref sender, new AsyncCallback(ReceiveData), socket);
                //Console.WriteLine("4Message from " + ((IPEndPoint)sender).Address.ToString() + " Port " + ((IPEndPoint)sender).Port.ToString());
                //Console.WriteLine("Lucky");
                //Console.WriteLine("hey");
                //socket.BeginReceiveFrom(data, 0, data.Length, 0, ref sender, new AsyncCallback(ReceiveData), socket);
                //Console.WriteLine("4Message from " + ((IPEndPoint)sender).Address.ToString() + " Port " + ((IPEndPoint)sender).Port.ToString());
                //Console.WriteLine("Lucky");
                /*try
                {
                    socket.SendTo(Encoding.ASCII.GetBytes("CheckAlive"), new IPEndPoint(((IPEndPoint)sender).Address, ((IPEndPoint)sender).Port));
                    Thread.Sleep(1000);
                    socket.BeginReceiveFrom(data, 0, data.Length, 0, ref sender, new AsyncCallback(ReceiveData), socket);
                }
                catch (SocketException)
                {
                    Console.WriteLine("IP Address: " + ((IPEndPoint)sender).Address.ToString() + " Port: " + ((IPEndPoint)sender).Port.ToString() + " leave. ");
                    foreach (var ep in destips)
                    {
                        if (ep.Address.ToString().CompareTo(((IPEndPoint)sender).Address.ToString()) == 0 
                            && ep.Port.ToString().CompareTo(((IPEndPoint)sender).Port.ToString()) == 0)
                        {
                            destips.Remove(ep);
                            break;
                        }
                    }
                    Send("Leave " + ((IPEndPoint)sender).Address.ToString() + " " + ((IPEndPoint)sender).Port.ToString());
                    socket.BeginReceiveFrom(data, 0, data.Length, 0, ref sender, new AsyncCallback(ReceiveData), socket);
                }*/
            }
            catch (Exception e)
            {
                //socket.EndReceiveFrom()
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.StackTrace);
                // Console.WriteLine(e.Source);
                Console.WriteLine(e.Message);
                Console.WriteLine(((IPEndPoint)sender).Address.ToString() + " " + ((IPEndPoint)sender).Port.ToString());
            }
        }

        private static void IcmpListener()
        {
            Socket icmpListener = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            icmpListener.Bind(new IPEndPoint(IPAddress.Any, 0));
            icmpListener.IOControl(IOControlCode.ReceiveAll, new byte[] { 1, 0, 0, 0 }, new byte[] { 1, 0, 0, 0 });

            byte[] buffer = new byte[4096];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            int bytesRead = icmpListener.ReceiveFrom(buffer, ref remoteEndPoint);
            Console.WriteLine("ICMPListener received " + bytesRead + " from " + remoteEndPoint);
        }

        private static List<IPEndPoint> AliveList;
        private static void CheckAlive()
        {
            AliveList = new List<IPEndPoint>();
            Send("CheckAlive");
            Thread.Sleep(5 * 1000);
            Parallel.ForEach(destips, endPoint =>
            {
                bool flag = false;
                foreach (var ep in AliveList)
                {
                    if (endPoint.Address.ToString() == ep.Address.ToString() && endPoint.Port == ep.Port)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    Send("CheckAlive", endPoint);
                    Thread.Sleep(5 * 1000);
                    foreach (var ep in AliveList)
                    {
                        if (endPoint.Address.ToString() == ep.Address.ToString() && endPoint.Port == ep.Port)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        Send("Leave " + endPoint.Address.ToString() + " " + endPoint.Port.ToString());
                        destips.Remove(endPoint);
                    }
                }
            });
        }

        public static void Run(int port, string name)
        {
            join = false;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            //int SIO_UDP_CONNRESET = -1744830452;
            //socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            SocketProgramming.RequestStatus = new Dictionary<string, string>();
            //while (true) ;
            SocketProgramming.Name = name;
            destips = new List<IPEndPoint>();
            
            Receive();
        CheckConnection:
            Console.WriteLine("Please select which one you prefer: (1) Connect to others; (2) Waiting others join");
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    Console.Write("IP address connected to: ");
                    IPAddress destip;
                    try
                    {
                        destip = IPAddress.Parse(Console.ReadLine());
                        if (IPAddress.IsLoopback(destip))
                        {
                            throw new Exception("Connected localhost is strictly prohibited. ");
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                        goto CheckConnection;
                    }
                    Console.Write("Destination port: ");
                    int destport = int.Parse(Console.ReadLine());

                    /*byte[] content = Encoding.ASCII.GetBytes("Join " + name);
                    socket.SendTo(content, new IPEndPoint(IPAddress.Parse(destip), destport));
                    EndPoint Sender = new IPEndPoint(IPAddress.Any, 0);
                    byte[] reply = new byte [256];
                    socket.ReceiveFrom(reply, ref Sender);
                    Console.WriteLine("Message from " + ((IPEndPoint)Sender).Address.ToString() + " Port " + ((IPEndPoint)Sender).Port.ToString() + ": " +
                        Encoding.ASCII.GetString(reply));
                    if (EstablishConnection(reply))
                    {
                        destips.Add((IPEndPoint)Sender);
                        content = Encoding.ASCII.GetBytes("Greeting " + name);
                        socket.SendTo(content, Sender);
                    }*/

                    //Send("hello", new IPEndPoint(destip, destport));
                    Send("Join " + name, new IPEndPoint(destip, destport));
                    Thread.Sleep(5 * 1000);
                    if (!join)
                    {
                        Console.WriteLine("Destination cannot reach. Try another please. ");
                        goto CheckConnection;
                    }
                    break;
                case ConsoleKey.D2:
                    break;
            }
            //Console.WriteLine("System: Please use 'Disconnect' when you leave. Don't forcily leave. Thanks for your attention. ");
            new Task(() => Message()).Start();
            //new Task(() => IcmpListener()).Start();
            /*
            new Task(() =>
            {
                while (true)
                {
                    Thread.Sleep(5 * 60 * 1000);
                    CheckAlive();
                }
            }).Start();*/
            while (true) ;
        }
    }
}
