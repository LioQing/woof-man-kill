using System.Net;
using System.Net.Sockets;
using ConsoleUtils;

namespace WoofManKill
{
    static class Client
    {
        private const int PORT = 11000;
        static private bool running = true;

        static public void Start()
        {
            while (true)
            {
                Console.WriteLine("When in the menu, enter 'quit' any time to exit.");
                Console.WriteLine();

                Console.WriteLine("Enter the IP address of the server:");
                string? ip = Console.ReadLine();

                if (ip == "quit")
                {
                    break;
                }

                Console.WriteLine();
                Console.WriteLine("Enter your name: ");
                string? name = Console.ReadLine();

                if (name == "quit")
                {
                    break;
                }

                if (ip is null || name is null)
                {
                    continue;
                }

                if (name == "")
                {
                    Console.WriteLine("Name cannot be empty.");
                    continue;
                }

                Console.WriteLine();
                Join(IPAddress.Parse(ip), name);
            }
        }

        static public void Join(IPAddress ipAddr, string name)
        {
            IPEndPoint hostEndPoint = new(ipAddr, PORT);

            Socket host = new(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            host.Connect(hostEndPoint);

            Console.WriteLine($"Connected to host at {ipAddr}");

            SockUtils.SendString(host, name);

            string res = SockUtils.ReceiveString(host);
            if (res == "Ok")
            {
                Console.WriteLine("You joined the game.");
            }
            else
            {
                Console.WriteLine(res);
                return;
            }

            Console.WriteLine();

            InGame(host, name);

            SockUtils.CloseConnection(host);
        }

        static private void InGame(Socket host, string name)
        {
            Console.WriteLine("When in the game, enter 'disconnect' any time to exit.");
            Console.WriteLine();

            Thread t = new(() => InGameHandleHostMsg(host));
            t.Start();

            running = true;
            while (running)
            {
                string msg = ConsoleExt.ReadLine();

                if (!running)
                {
                    break;
                }

                if (msg == "disconnect")
                {
                    SockUtils.SendMessage(host, Message.Disconnect());
                    break;
                }

                SockUtils.SendMessage(host, Message.Chat(msg, name));
            }

            t.Join();
        }

        static private void InGameHandleHostMsg(Socket host)
        {
            while (true)
            {
                Message msg = SockUtils.ReceiveMessage(host);

                switch (msg.Ty)
                {
                    case Message.Type.Disconnect:
                        ConsoleExt.PrependLine($"You disconnected.");
                        ConsoleExt.PrependLine("");
                        return;

                    case Message.Type.Chat:
                        ConsoleExt.PrependLine($"{msg.Sender}: {msg.Content}");
                        break;
                    
                    case Message.Type.Announcement:
                        ConsoleExt.PrependLine($"Game: {msg.Content}");
                        break;

                    case Message.Type.Vote:
                        ConsoleExt.PrependLine($"{msg.Sender} voted for {msg.Target}");
                        break;
                    
                    case Message.Type.End:
                        SockUtils.SendMessage(host, Message.Disconnect());
                        running = false;
                        Console.WriteLine("Press Enter to Continue...");
                        return;

                    default:
                        break;
                }
            }
        }
    }
}