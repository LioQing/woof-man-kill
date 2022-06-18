using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using ConsoleUtils;

namespace WoofManKill
{
    class Host
    {
        public const int PORT = 11000;
        public IPAddress IpAddr { get; private set; } = IPAddress.Parse(GetLocalIPAddress());

        private Game game = new();

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public void Start()
        {
            IPEndPoint localEndPoint = new(IpAddr, PORT);

            Socket listener = new(IpAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(localEndPoint);
            listener.Listen(10);

            ConsoleExt.PrependLine($"Listening on {IpAddr}");

            Thread cmdThread = new(() => Cmd());
            cmdThread.Start();

            while (true)
            {
                Socket client = listener.Accept();
                string name = SockUtils.ReceiveString(client);

                if (game.Started)
                {
                    SockUtils.SendString(client, "Game has already started.");
                    SockUtils.CloseConnection(client);
                    continue;
                }

                // if name is empty or longer than 64 characters, reject client
                if (name.Length < 1 || name.Length > 64)
                {
                    SockUtils.SendString(client, "Name length should be more than 0 and less than 64 characters.");
                    SockUtils.CloseConnection(client);
                    continue;
                }

                // if name does not match [0-9a-zA-Z_]+, reject client
                if (!Regex.IsMatch(name, @"^[0-9a-zA-Z_]+$"))
                {
                    SockUtils.SendString(client, "Name should only be letters, digits, or underscores.");
                    SockUtils.CloseConnection(client);
                    continue;
                }

                // if name is used, reject client
                if (game.ContainsPlayer(name))
                {
                    SockUtils.SendString(client, "Name already used in the game you attempted to join.");
                    SockUtils.CloseConnection(client);
                    continue;
                }

                game.AddPlayer(client, name);

                SockUtils.SendString(client, "Ok");
                
                ConsoleExt.PrependLine($"{name} joined.");
                Broadcast(Message.Announcement($"{name} joined."));

                Thread t = new(() => HandleClient(client, name));
                t.Start();
            }
        }

        private void Cmd()
        {
            while (true)
            {
                string? cmd = ConsoleExt.ReadLine();
                if (cmd == "start")
                {
                    game.Started = true;
                    break;
                }
            }

            Broadcast(Message.Announcement("Game started."));

            game.AllocatePlayersRole();

            game.LockPlayers((players) =>
                {
                    foreach (var player in players)
                    {
                        SockUtils.SendMessage(
                            player.Value.Socket,
                            Message.Announcement(player.Value.Role!.Description())
                        );
                    }
                }
            );

            Thread t = new(() => 
                {
                    game.DayTimeThread((msg, names) => Broadcast(msg, names));
                    Environment.Exit(0);
                }
            );
            t.Start();
        }

        private void HandleClient(Socket client, string name)
        {
            Message msg;
            do
            {
                try
                {
                    msg = SockUtils.ReceiveMessage(client);
                }
                catch (SocketException)
                {
                    DisconnectClient(client, name);
                    break;
                }

                if (!game.Started)
                {
                    WaitingForStartHandleMsg(client, name, msg);
                }
                else if (game.DayTime == DayTime.Day)
                {
                    DayHandleMsg(client, name, msg);
                }
                else if (game.DayTime == DayTime.Vote)
                {
                    VoteHandleMsg(client, name, msg);
                }
                else if (game.DayTime == DayTime.Night)
                {
                    NightHandleMsg(client, name, msg);
                }
            }
            while (msg.Ty != Message.Type.Disconnect);

            SockUtils.CloseConnection(client);
        }

        public void WaitingForStartHandleMsg(Socket client, string name, Message msg)
        {
            switch (msg.Ty)
            {
                case Message.Type.Disconnect:
                    DisconnectClient(client, name);
                    SockUtils.SendMessage(client, msg);
                    break;

                case Message.Type.Chat:
                    Broadcast(msg, new string[] { name });
                    break;

                default:
                    break;
            }
        }

        public void DayHandleMsg(Socket client, string name, Message msg)
        {
            switch (msg.Ty)
            {
                case Message.Type.Disconnect:
                    DisconnectClient(client, name);
                    SockUtils.SendMessage(client, msg);
                    break;

                case Message.Type.Chat:
                    Broadcast(msg, new string[] { name });
                    break;

                default:
                    break;
            }
        }

        public void VoteHandleMsg(Socket client, string name, Message msg)
        {
            switch (msg.Ty)
            {
                case Message.Type.Disconnect:
                    DisconnectClient(client, name);
                    SockUtils.SendMessage(client, msg);
                    break;

                case Message.Type.Chat:
                    if (game.IsDead(name))
                    {
                        SockUtils.SendMessage(client, Message.Announcement("(You are dead, so nothing happen)"));
                        break;
                    }

                    // check is vote or not
                    if (msg.Content.StartsWith("/vote"))
                    {
                        string target = msg.Content.Substring(6);

                        if (game.Vote(name, target))
                        {
                            Broadcast(Message.Vote(name, target), new string[] { name });
                            game.LockPlayers((players) =>
                                {
                                    Broadcast(Message.Announcement(
                                        $"Current Votes:\n{String.Join("\n", players.Select(p => p.Key + ": " + p.Value.Vote + " votes"))}\n"
                                    ));
                                }
                            );
                        }
                        else
                        {
                            SockUtils.SendMessage(client, Message.Announcement("Invalid vote target name."));
                        }
                    }
                    else
                    {
                        Broadcast(msg, new string[] { name });
                    }
                    break;

                default:
                    break;
            }
        }

        public void NightHandleMsg(Socket client, string name, Message msg)
        {
            switch (msg.Ty)
            {
                case Message.Type.Disconnect:
                    DisconnectClient(client, name);
                    SockUtils.SendMessage(client, msg);
                    break;

                case Message.Type.Chat:
                    if (game.IsDead(name))
                    {
                        SockUtils.SendMessage(client, Message.Announcement("(You are dead, so nothing happen)"));
                        break;
                    }

                    // check if is command or not
                    if (msg.Content.StartsWith("/"))
                    {
                        string[] segs = msg.Content.Split(' ');
                        game.HandleNightMsg(client, name, segs[0].Substring(1), segs.Skip(1).ToArray());
                    }
                    else if (game.IsWoof(name))
                    {
                        game.LockPlayers((players) =>
                            {
                                players
                                    .Where(p => p.Value.Role!.Side == Role.SideType.Woof && p.Key != name)
                                    .Select(p => p.Value)
                                    .ToList()
                                    .ForEach(player => SockUtils.SendMessage(player.Socket, msg));
                            }
                        );
                    }

                    break;

                default:
                    break;
            }
        }

        private void Broadcast(Message msg, string[]? exclude = null)
        {
            game.LockPlayers((clients) =>
                {
                    clients
                        .Where(p => exclude == null || !exclude.Contains(p.Key))
                        .Select(p => p.Value)
                        .ToList()
                        .ForEach(player => SockUtils.SendMessage(player.Socket, msg));
                }
            );
        }

        private void DisconnectClient(Socket client, string name)
        {
            game.RemovePlayer(name);

            ConsoleExt.PrependLine($"{name} disconnected.");
            Broadcast(Message.Announcement($"{name} disconnected."));
        }
    }
}