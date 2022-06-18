using System.Net;
using System.Net.Sockets;

namespace WoofManKill
{
    class Player
    {
        public Socket Socket { get; set; }

        public Role? Role { get; set; }

        public int Vote { get; set; } = 0;

        public string PrevVote { get; set; } = "";

        public bool IsDead { get; set; } = false;

        public bool HasAction { get; set; } = false;

        public string[] Action { get; set; } = new string[0];
        public string[] PrevAction { get; set; } = new string[0];

        public Player(Socket socket)
        {
            Socket = socket;
        }
    }
}