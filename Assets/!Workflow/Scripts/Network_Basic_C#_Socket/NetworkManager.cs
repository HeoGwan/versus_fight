using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;

public class NetworkManager : MonoBehaviour
{
    public void SetServer()
    {
        // Create UdpClient Object. Listeing from 7777 port
        UdpClient server = new UdpClient(7777);

        // variable what get the client ip
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (!DoServer(server, remoteEP)) {}
    }

    private bool DoServer(UdpClient server, IPEndPoint remoteEP)
    {
        for (int i = 0; i < 100000000; ++i)
        {
            // receive a data
            byte[] dgram = server.Receive(ref remoteEP);
            Debug.Log($"[Server] Received {dgram.Length} bytes from {remoteEP.ToString()}");

            // send a data
            server.Send(dgram, dgram.Length, remoteEP);
            Debug.Log($"[Server] Send {dgram.Length} bytes to {remoteEP.ToString()}");

            Thread.Sleep(1000);

            return true;
        }
        return false;
    }

    public void SetClient()
    {
        // Create UdpClient Object
        UdpClient client = new UdpClient();

        string msg = "Hello Network World";
        byte[] datagram = Encoding.UTF8.GetBytes(msg);

        // Send a data
        client.Send(datagram, datagram.Length, "127.0.0.1", 7777);
        Debug.Log($"[Client] send {datagram.Length} bytes to 127.0.0.1:7777");

        // Receive a data
        IPEndPoint epRemote = new IPEndPoint(IPAddress.Any, 0);
        byte[] bytes = client.Receive(ref epRemote);
        Debug.Log($"[Client] Received {bytes.Length} from {epRemote.ToString()}");

        // Close the UdpClient Object
        client.Close();
    }
}
