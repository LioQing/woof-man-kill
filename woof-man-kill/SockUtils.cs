using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WoofManKill
{
    static class SockUtils
    {
        static public string ReceiveString(Socket sock)
        {
            // length
            byte[] lbuf = new byte[16];
            int length = sock.Receive(lbuf, 0, lbuf.Length, SocketFlags.None);
            int len = int.Parse(Encoding.ASCII.GetString(lbuf));

            // string
            byte[] buf = new byte[len];
            int bytesRec = sock.Receive(buf, 0, buf.Length, SocketFlags.None);
            return Encoding.ASCII.GetString(buf, 0, bytesRec);
        }

        static public void SendString(Socket sock, string str)
        {
            byte[] buf = Encoding.ASCII.GetBytes($"{str.Length:D16}" + str);
            sock.Send(buf);
        }

        static public void CloseConnection(Socket sock)
        {
            sock.Shutdown(SocketShutdown.Both);
            sock.Close();
        }

        static public void SendMessage(Socket sock, Message msg)
        {
            sock.Send(new byte[] { (byte)msg.Ty });
            SendString(sock, msg.Content);
            SendString(sock, msg.Sender);
            SendString(sock, msg.Target);
        }

        static public Message ReceiveMessage(Socket sock)
        {
            byte[] buf = new byte[1];

            int bytesRec = sock.Receive(buf, 0, 1, SocketFlags.None);
            string content = ReceiveString(sock);
            string sender = ReceiveString(sock);
            string target = ReceiveString(sock);

            return new Message((Message.Type)buf[0], content, sender, target);
        }
    }
}