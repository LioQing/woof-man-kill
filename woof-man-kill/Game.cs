

using System.Net.Sockets;

namespace WoofManKill
{
    class Game
    {
        private readonly object _startedLock = new();
        private bool _started = false;
        public bool Started
        {
            get
            {
                lock (_startedLock)
                {
                    return _started;
                }
            }
            set
            {
                lock (_startedLock)
                {
                    _started = value;
                }
            }
        }

        private readonly object playersLock = new();
        private IDictionary<string, Player> players = new Dictionary<string, Player>();

        private readonly object _dayTimeLock = new();
        private DayTime _dayTime = DayTime.Day;
        public DayTime DayTime
        {
            get
            {
                lock (_dayTimeLock) return _dayTime;
            }
            private set
            {
                lock (_dayTimeLock) _dayTime = value;
            }
        }

        public bool IsDead(string name)
        {
            lock (playersLock)
            {
                if (players.ContainsKey(name))
                {
                    return players[name].IsDead;
                }
            }
            return false;
        }

        public bool IsWoof(string name)
        {
            lock (playersLock)
            {
                if (players.ContainsKey(name))
                {
                    return players[name].Role!.Side == Role.SideType.Woof;
                }
            }
            return false;
        }

        public Role.Type? GetRoleType(string name)
        {
            lock (playersLock)
            {
                if (players.ContainsKey(name))
                {
                    return players[name].Role!.Ty;
                }
            }
            return null;
        }

        public string[] GetPrevAction(string name)
        {
            lock (playersLock)
            {
                if (players.ContainsKey(name))
                {
                    return players[name].PrevAction;
                }
            }
            return new string[0];
        }

        public void AddPlayer(Socket socket, string name)
        {
            lock (playersLock) players.Add(name, new(socket));
        }

        public void RemovePlayer(string name)
        {
            lock (playersLock) players.Remove(name);
        }

        public bool ContainsPlayer(string name)
        {
            lock (playersLock) return players.ContainsKey(name);
        }

        public int PlayerCount()
        {
            lock (playersLock) return players.Count;
        }

        public void LockPlayers(Action<IDictionary<string, Player>> func)
        {
            lock (playersLock) func(players);
        }

        public void AllocatePlayersRole()
        {
            Role.Type[] allocOrder = 
            {
                Role.Type.Killer,
                Role.Type.Doctor,
                Role.Type.Sheriff,
                Role.Type.Swapper,
                Role.Type.Spy,
                Role.Type.BodyGuard,
                Role.Type.Freezer,
            };

            Random rnd = new();
            LockPlayers((players) => 
                {
                    foreach (var (p, role) in players.Zip(
                            allocOrder
                                .Take(PlayerCount())
                                .OrderBy(x => rnd.Next())
                                .ToArray()
                        ))
                    {
                        p.Value.Role = new(role);
                    }
                }
            );
        }

        public bool Vote(string voter, string targetName)
        {
            bool success = true;
            LockPlayers((players) =>
                {
                    if (!players.ContainsKey(targetName) || players[targetName].IsDead)
                    {
                        success = false;
                        return;
                    }

                    players[targetName].Vote += 1;

                    if (players[voter].PrevVote != "")
                    {
                        players[players[voter].PrevVote].Vote -= 1;
                    }

                    players[voter].PrevVote = targetName;
                }
            );

            return success;
        }

        private bool IsEndGame(Action<Message, string[]?> broadcast)
        {
            // check if end game
            LockPlayers((players) =>
                {
                    if (players.Count(x => x.Value.IsDead) == players.Count)
                    {
                        Started = false;
                        broadcast(Message.Announcement("Game Over! Everyone Died!"), null);
                    }
                    else if (players.Where(x => !x.Value.IsDead).All(x => IsWoof(x.Key)))
                    {
                        Started = false;
                        broadcast(Message.Announcement("Game Over! Woofs Won!"), null);
                    }
                    else if (players.Where(x => !x.Value.IsDead).All(x => !IsWoof(x.Key)))
                    {
                        Started = false;
                        broadcast(Message.Announcement("Game Over! The Town Won!"), null);
                    }
                }
            );

            if (!Started)
            {
                broadcast(Message.End(), null);
                return true;
            }

            return false;
        }

        public void DayTimeThread(Action<Message, string[]?> broadcast)
        {
            int day = 0;
            while (Started)
            {
                if (day > 0)
                {
                    if (IsEndGame(broadcast))
                    {
                        break;
                    }

                    DayTime = DayTime.Day;
                    broadcast(Message.Announcement($"----day {day}----"), null);
                    broadcast(Message.Announcement("It's day time. (90 seconds)"), null);
                    broadcast(Message.Announcement("Chat by typing.\n"), null);
                    Thread.Sleep(60 * 1000);

                    broadcast(Message.Announcement("30 seconds until vote time."), null);
                    Thread.Sleep(15 * 1000);

                    broadcast(Message.Announcement("15 seconds until vote time."), null);
                    Thread.Sleep(10 * 1000);

                    broadcast(Message.Announcement("5 seconds until vote time."), null);
                    Thread.Sleep(5 * 1000);

                    DayTime = DayTime.Vote;
                    broadcast(Message.Announcement("It's vote time. (30 seconds)"), null);
                    broadcast(Message.Announcement("Chat by typing, or vote to execute with '/vote <player name>'\n"), null);

                    // clear everyone's vote
                    LockPlayers((players) => 
                        {
                            foreach (var (_, p) in players)
                            {
                                p.PrevVote = "";
                                p.Vote = 0;
                            }
                        }
                    );

                    Thread.Sleep(15 * 1000);

                    broadcast(Message.Announcement("15 seconds until night."), null);
                    Thread.Sleep(10 * 1000);
                    
                    broadcast(Message.Announcement("5 seconds until night."), null);
                    Thread.Sleep(5 * 1000);

                    // kill the player with the most votes
                    LockPlayers((players) =>
                        {
                            var ordered = players.OrderByDescending(x => x.Value.Vote).ToArray();

                            if (ordered[0].Value.Vote == ordered[1].Value.Vote)
                            {
                                broadcast(Message.Announcement("No one was executed."), null);
                            }
                            else
                            {
                                players[ordered[0].Key].IsDead = true;
                                broadcast(Message.Announcement($"Oof, {ordered[0].Key} was executed."), null);
                            }
                        }
                    );
                }
                
                if (IsEndGame(broadcast))
                {
                    break;
                }

                DayTime = DayTime.Night;
                broadcast(Message.Announcement("It's night time. (60 seconds)\n"), null);

                LockPlayers((players) => 
                    {
                        foreach (var (_, p) in players)
                        {
                            if (p.IsDead)
                            {
                                continue;
                            }

                            p.PrevAction = p.Action;
                            p.HasAction = false;
                            p.Action = new string[0];
                            SockUtils.SendMessage(p.Socket, Message.Announcement(p.Role!.NightTimePrompt()));
                        }
                    }
                );

                Thread.Sleep(30 * 1000);
                
                broadcast(Message.Announcement("30 seconds until morning."), null);
                Thread.Sleep(15 * 1000);

                broadcast(Message.Announcement("15 seconds until morning."), null);
                Thread.Sleep(10 * 1000);

                broadcast(Message.Announcement("5 seconds until morning."), null);
                Thread.Sleep(5 * 1000);

                LockPlayers((players) => 
                    {
                        Role.Type[] actionOrder =
                        {
                            Role.Type.Swapper,
                            Role.Type.Freezer,
                            Role.Type.BodyGuard,
                            Role.Type.Doctor,
                            Role.Type.Killer,
                            Role.Type.Sheriff,
                            Role.Type.Spy,
                        };

                        string[]? swapped = null;
                        string? frozen = null;
                        string? guarded = null;
                        string? guard = null;
                        string? healed = null;
                        string? doctor = null;
                        string? freezer = null;
                        string? killer = null;
                        string? killed = null;

                        foreach (var (name, player) in players.OrderBy(p => Array.IndexOf(actionOrder, p.Value.Role!.Ty)))
                        {
                            switch (player.Role!.Ty)
                            {
                                case Role.Type.Freezer:
                                    freezer = name;
                                    break;
                                
                                case Role.Type.BodyGuard:
                                    guard = name;
                                    break;

                                case Role.Type.Doctor:
                                    doctor = name;
                                    break;
                                
                                case Role.Type.Killer:
                                    killer = name;
                                    break;
                                
                                default:
                                    break;
                            }

                            if (player.IsDead || !player.HasAction)
                            {
                                continue;
                            }
                            else if (frozen is not null && frozen.Contains(name))
                            {
                                SockUtils.SendMessage(player.Socket, Message.Announcement("You are frozen."));
                                continue;
                            }

                            var action = player.Action
                                .Select((name) => 
                                    {
                                        if (swapped is not null)
                                        {
                                            int i = Array.IndexOf(swapped, name);
                                            if (i != -1)
                                            {
                                                name = swapped[1 - i];
                                            }
                                        }

                                        return name;
                                    }
                                )
                                .ToArray();

                            switch (player.Role!.Ty)
                            {
                                case Role.Type.Swapper:
                                    swapped = action;
                                    break;
                                
                                case Role.Type.Freezer:
                                    frozen = action[0];
                                    break;
                                
                                case Role.Type.BodyGuard:
                                    guarded = action[0];
                                    break;
                                
                                case Role.Type.Doctor:
                                    healed = action[0];
                                    break;
                                
                                case Role.Type.Killer:
                                    if (action[0] == guard)
                                    {
                                        players[guard].IsDead = true;
                                        player.IsDead = true;
                                        broadcast(Message.Announcement($"Oof, {guard} was found dead."), null);
                                        broadcast(Message.Announcement($"Oof, {name} was found dead."), null);
                                    }
                                    else if (action[0] == guarded)
                                    {
                                        SockUtils.SendMessage(player.Socket, Message.Announcement("Your attack was blocked."));
                                        SockUtils.SendMessage(players[guarded].Socket, Message.Announcement("You were attacked, but you survived."));
                                        SockUtils.SendMessage(players[guard!].Socket, Message.Announcement($"You saved {guarded} from an attack."));
                                    }
                                    else if (action[0] == healed)
                                    {
                                        SockUtils.SendMessage(players[healed].Socket, Message.Announcement("You were healed from an attack."));
                                        SockUtils.SendMessage(players[doctor!].Socket, Message.Announcement($"You saved {healed} from an attack."));
                                    }
                                    else
                                    {
                                        players[action[0]].IsDead = true;
                                        broadcast(Message.Announcement($"Oof, {action[0]} was found dead."), null);
                                    }
                                    killed = action[0];
                                    break;
                                
                                case Role.Type.Sheriff:
                                    if (IsWoof(action[0]) != IsWoof(action[1]))
                                    {
                                        SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} and {action[1]} are different side"));
                                    }
                                    else
                                    {
                                        SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} and {action[1]} are same side"));
                                    }
                                    break;

                                case Role.Type.Spy:
                                    if (action[0] == doctor && doctor != healed)
                                    {
                                        if (healed is not null)
                                            SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} visited {healed}"));
                                        else
                                            SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} visited nobody"));
                                    }
                                    else if (action[0] == guard && guard != guarded)
                                    {
                                        if (guarded is not null)
                                            SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} visited {guarded}"));
                                        else
                                            SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} visited nobody"));
                                    }
                                    else if (action[0] == freezer && freezer != frozen)
                                    {
                                        if (frozen is not null)
                                            SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} visited {frozen}"));
                                        else
                                            SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} visited nobody"));
                                    }
                                    else if (action[0] == killer && killer != killed)
                                    {
                                        if (killed is not null)
                                            SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} visited {killed}"));
                                        else
                                            SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} visited nobody"));
                                    }
                                    else
                                    {
                                        SockUtils.SendMessage(player.Socket, Message.Announcement($"{action[0]} visited nobody"));
                                    }
                                    break;
                            }
                        }
                    }
                );
                
                day += 1;
            }
        }

        public void HandleNightMsg(Socket client, string name, string action, string[] args)
        {
            if (action == "cancel")
            {
                LockPlayers((players) =>
                    {
                        players[name].HasAction = false;
                        players[name].Action = new string[0];
                    }
                );

                return;
            }

            if (
                action == "kill" &&
                GetRoleType(name) == Role.Type.Killer &&
                args.Length == 1 &&
                ContainsPlayer(args[0]) &&
                !IsDead(args[0])
                ||
                action == "heal" &&
                GetRoleType(name) == Role.Type.Doctor &&
                args.Length == 1 &&
                ContainsPlayer(args[0]) &&
                (GetPrevAction(name).ElementAtOrDefault(0) == name &&
                args[0] != name || GetPrevAction(name).ElementAtOrDefault(0) != name) &&
                !IsDead(args[0])
                ||
                action == "inve" &&
                GetRoleType(name) == Role.Type.Sheriff &&
                args.Length == 2 &&
                ContainsPlayer(args[0]) &&
                ContainsPlayer(args[1]) &&
                !IsDead(args[0]) &&
                !IsDead(args[1]) &&
                args[0] != args[1] &&
                args[0] != name &&
                args[1] != name
                ||
                action == "swap" &&
                GetRoleType(name) == Role.Type.Swapper &&
                args.Length == 2 &&
                ContainsPlayer(args[0]) &&
                ContainsPlayer(args[1]) &&
                !IsDead(args[0]) &&
                !IsDead(args[1]) &&
                args[0] != args[1]
                ||
                action == "spy" &&
                GetRoleType(name) == Role.Type.Spy &&
                args.Length == 1 &&
                ContainsPlayer(args[0]) &&
                !IsDead(args[0]) &&
                args[0] != name
                ||
                action == "freeze" &&
                GetRoleType(name) == Role.Type.Freezer &&
                args.Length == 1 &&
                ContainsPlayer(args[0]) &&
                !IsDead(args[0]) &&
                args[0] != name
                ||
                action == "prot" &&
                GetRoleType(name) == Role.Type.BodyGuard &&
                args.Length == 1 &&
                ContainsPlayer(args[0]) &&
                !IsDead(args[0]) &&
                args[0] != name
            )
            {
                LockPlayers((players) =>
                    {
                        players[name].HasAction = true;
                        players[name].Action = args;
                    }
                );

                return;
            }

            SockUtils.SendMessage(client, Message.Announcement("Invalid action or command."));
        }
    }
}