using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Security;
using System.Data.SqlTypes;
using System;

public class NetworkManager : MonoBehaviour
{
    #region Network Test
    // public void SetServer()
    // {
    //     // Create UdpClient Object. Listeing from 7777 port
    //     UdpClient server = new UdpClient(7777);

    //     // variable what get the client ip
    //     IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

    //     while (!DoServer(server, remoteEP)) {}

    //     server.Close();
    // }

    // private bool DoServer(UdpClient server, IPEndPoint remoteEP)
    // {
    //     for (int i = 0; i < 100000000; ++i)
    //     {
    //         // receive a data
    //         byte[] dgram = server.Receive(ref remoteEP);
    //         Debug.Log($"[Server] Received {dgram.Length} bytes from {remoteEP.ToString()}");

    //         // send a data
    //         server.Send(dgram, dgram.Length, remoteEP);
    //         Debug.Log($"[Server] Send {dgram.Length} bytes to {remoteEP.ToString()}");

    //         Thread.Sleep(1000);

    //         return true;
    //     }
    //     return false;
    // }

    // public void SetClient()
    // {
    //     // Create UdpClient Object
    //     UdpClient client = new UdpClient();

    //     string msg = "Hello Network World";
    //     byte[] datagram = Encoding.UTF8.GetBytes(msg);

    //     // Send a data
    //     client.Send(datagram, datagram.Length, "127.0.0.1", 7777);
    //     Debug.Log($"[Client] send {datagram.Length} bytes to 127.0.0.1:7777");

    //     // Receive a data
    //     IPEndPoint epRemote = new IPEndPoint(IPAddress.Any, 0);
    //     byte[] bytes = client.Receive(ref epRemote);
    //     Debug.Log($"[Client] Received {bytes.Length} from {epRemote.ToString()}");

    //     // Close the UdpClient Object
    //     client.Close();
    // }
    #endregion

    public enum PacketType : byte
    {
        None = 0,
        Connect = 1,
        DisConnect = 5,
    }

    public struct EntityPacket
    {
        public PacketType packetType;   // 1
        public int entityId;   // 4

        public float x;        // 4
        public float y;        // 4
        public float z;        // 4

        public float rotX;     // 4
        public float rotY;     // 4
        public float rotZ;     // 4
    }  // 29 bytes

    private const int PORT = 7777;

    private UdpClient server;
    private UdpClient client;
    private IPEndPoint endPoint;

    private bool isConnecting = false;
    private bool isServer = false;

    public static Vector3 Position { get; private set; }

    private float _networkInterval = 1f / 30f;
    private float _time = 0.0f;

    [SerializeField] private int entityId = 1;
    [SerializeField] private int myEntityId = 1;

    EntityPacket _packet;

    void Start()
    {
        _packet = new EntityPacket();
        _packet.packetType = PacketType.None; 
        myEntityId = -1;
    }

    void Update()
    {
        if (isConnecting)
        {
            _time += Time.deltaTime;

            while (_time > _networkInterval)
            {
                // 포지션 전달
                _time = 0.0f;
            }
        }
    }

    void OnDisable()
    {
        server?.Close();
        client?.Close();
    }

    // 서버 연결
    public void ConnectServer()
    {
        if (client != null)
        {
            Debug.Log("You can not be a server. you are client.");
            return;
        }
        server = new UdpClient(PORT);

        endPoint = new IPEndPoint(IPAddress.Any, 0);

        // entityId를 저장함
        myEntityId = entityId++;
        _packet.entityId = entityId;

        // 내가 서버인 것을 저장함
        isServer = true;

        // 서버만 열어놓고 응답을 기다린다.
        _ = ReceiveLoop();
    }


    // 클라이언트 연결
    public void ConnectClient()
    {
        if (server != null) 
        {
            Debug.Log("You can not be a client. you are server.");
            return;
        }
        // UdpClient 연결 및 EndPoint 연결
        client = new UdpClient();

        endPoint = new IPEndPoint(IPAddress.Any, 0);

        // 내가 서버가 아닌 것을 알림
        isServer = false;

        // 연결이 완료됨을 전달하기 위해 packetType을 Connect로 변경함
        _packet.packetType = PacketType.Connect;

        byte[] packet = Serialize(_packet);

        // 연결 시 연결이 완료되었다는 패킷을 보냄
        client.Send(packet, packet.Length, "127.0.0.1", PORT);

        // 연결을 받음
        _ = ReceiveLoop();
    }

    async Task ReceiveLoop()
    {
        UdpClient udp = server ?? client;

        while (true)
        {
            // UDP 데이터 가져오기
            var result = await udp.ReceiveAsync();

            EntityPacket recvPacket = Deserialize(result.Buffer);

            // 만약 서버라면 
            if (isServer)
            {
                Debug.Log("Receive from client");
                // 아무것도 연결되어 있지 않았던 상태라면 연결 상태로 변경
                if (!isConnecting) isConnecting = true;

                // 일단 모든 클라이언트에게 연결됨을 알리는 패킷 전송
                EntityPacket packet = new EntityPacket();
                packet.packetType = PacketType.Connect; // 연결 완료를 알림
                packet.entityId = entityId++; // 클라이언트의 id를 넘겨줌

                byte[] sendPacket = Serialize(packet);

                server.Send(sendPacket, sendPacket.Length, result.RemoteEndPoint);
            }
            // 클라이언트라면
            else
            {
                if (recvPacket.packetType == PacketType.Connect)
                {
                    if (isConnecting) continue; // 이미 연결된 경우 무시함
                    // 연결 되었을 경우 플래그 변경
                    isConnecting = true;

                    // id 저장
                    myEntityId = recvPacket.entityId;
                }
            }

            // // 연결이 되지 않았을 경우에는 연결 확인을 진행함
            // if (!isConnecting)
            // {
            //     if (recvPacket.packetType == PacketType.Connect)
            //     {
            //         // 연결이 완료됨을 플래그로 알림
            //         isConnecting = true;

            //         // 만약 서버라면 여기서 종료
            //         if (isServer) 
            //         {
            //             // 연결이 완료되었다는 것을 알림 (클라이언트가 서버로 보내는 것)
            //             _packet.packetType = PacketType.Connect;

            //             byte[] p = Serialize(_packet);

            //             udp.Send(p, p.Length, endPoint);

            //             continue;
            //         }

            //         // 클라이언트라면 전달받은 entityId로 myEntityId를 저장함
            //         myEntityId = recvPacket.entityId;

            //         // 연결이 완료되었다는 것을 알림 (클라이언트가 서버로 보내는 것)
            //         _packet.packetType = PacketType.Connect;

            //         byte[] packet = Serialize(_packet);

            //         udp.Send(packet, packet.Length, endPoint);
            //     }
            // }

            // Position = ParsePosition(result.Buffer);
        }
    }

    Vector3 ParsePosition(byte[] bytes)
    {
        Vector3 result = new Vector3(bytes[1], bytes[2], bytes[3]);
        return result;
    }

    // EntityPacket -> byte[]
    byte[] Serialize(EntityPacket p)
    {
        byte[] data = new byte[29];

        data[0] = (byte)p.packetType;

        Buffer.BlockCopy(BitConverter.GetBytes(p.entityId), 0, data, 1, 4);

        Buffer.BlockCopy(BitConverter.GetBytes(p.x), 0, data, 5, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(p.y), 0, data, 9, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(p.z), 0, data, 13, 4);

        Buffer.BlockCopy(BitConverter.GetBytes(p.rotX), 0, data, 17, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(p.rotY), 0, data, 21, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(p.rotZ), 0, data, 25, 4);

        return data;
    }

    // byte[] -> EntityPacket
    EntityPacket Deserialize(byte[] data)
    {
        EntityPacket result = new EntityPacket
        {
            packetType = (PacketType)data[0],

            entityId = BitConverter.ToInt32(data, 1),

            x = BitConverter.ToSingle(data, 5),
            y = BitConverter.ToSingle(data, 9),
            z = BitConverter.ToSingle(data, 13),

            rotX = BitConverter.ToSingle(data, 17),
            rotY = BitConverter.ToSingle(data, 21),
            rotZ = BitConverter.ToSingle(data, 25)
        };

        return result;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 30;
        style.normal.textColor = Color.black;

        GUI.Label(new Rect(10, 10, 100, 100), $"my id : {myEntityId}", style);
        
        GUI.Label(new Rect(200, 10, 500, 100), $"isServer : {isServer}", style);
    }
}
