using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System;

namespace VersusFight
{
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
            Initialize = 2,
            Move = 3,
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
            public float rotW;     // 4
        }  // 33 bytes
        private const uint PACKET_SIZE = 33;

        private const int PORT = 7777;
        private const uint MAX_PLAYER = 2;

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

        [Space(10)]
        [SerializeField] private GameObject playerObj;
        [SerializeField] private GameObject enemyObj;

        [Header("UI")]
        [SerializeField] private Canvas startCanvas;
        [SerializeField] private Camera startCamera;

        // 오브젝트(엔티티)를 저장함
        GameObject[] entities;

        void Start()
        {
            _packet = new EntityPacket();
            _packet.packetType = PacketType.None; 

            entities = new GameObject[(int)MAX_PLAYER + 1];

            myEntityId = -1;
        }

        void Update()
        {
            // 서버는 클라이언트에게 패킷을 받았을 때 반응하면 됨
            if (isServer) return;

            if (isConnecting)
            {
                _time += Time.deltaTime;

                while (_time > _networkInterval)
                {
                    // 포지션 전달
                    MovePosition();
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

            // 내가 서버인 것을 저장함
            isServer = true;

            // 서버로 설정한 플레이어가 맨 처음 소환된다.
            Debug.Log($"myEntityId : {myEntityId}");
            entities[myEntityId] = Instantiate(playerObj, new Vector3(0, 0.2f, -7.1f), Quaternion.identity);

            // 테스트를 위한 캔버스 및 카메라 비활성화
            startCanvas.gameObject.SetActive(false);
            startCamera.gameObject.SetActive(false);

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

            // 캔버스 및 카메라 비활성화
            startCanvas.gameObject.SetActive(false);
            startCamera.gameObject.SetActive(false);

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

                // 연결 진행
                if (recvPacket.packetType == PacketType.Connect)
                {
                    // 만약 서버라면 
                    if (isServer)
                    {
                        // PacketType이 Connect인 클라이언트에게 접속 요청을 받음
                        // 아무것도 연결되어 있지 않았던 상태라면 연결 상태로 변경
                        if (!isConnecting) isConnecting = true;

                        // 일단 모든 클라이언트에게 연결됨을 알리는 패킷 전송
                        EntityPacket packet = new EntityPacket();
                        packet.packetType = PacketType.Connect; // 연결 완료를 알림
                        packet.entityId = entityId++; // 클라이언트의 id를 넘겨줌

                        // 서버 플레이어의 위치와 회전 정보를 패킷에 담아 전달함
                        GameObject entity = entities[myEntityId];
                        SetObjectPositionAndRotation(ref packet, entity);

                        byte[] sendPacket = Serialize(packet);

                        // 클라이언트에게 초기화 하라고 전송함
                        server.Send(sendPacket, sendPacket.Length, result.RemoteEndPoint);
                    }
                    // 클라이언트라면
                    else
                    {
                        if (isConnecting) continue; // 이미 연결된 경우 무시함

                        // 연결 되었을 경우 플래그 변경
                        isConnecting = true;

                        // id 저장
                        myEntityId = recvPacket.entityId;

                        // 자신의 오브젝트를 생성함
                        entities[myEntityId] = Instantiate(playerObj, new Vector3(0, 0.2f, 0), new Quaternion(0, 180f, 0, 1));

                        // 그 후 자신의 오브젝트를 서버에게 보내 초기화(소환)하라고 요청한다
                        _packet.packetType = PacketType.Initialize;
                        _packet.entityId = myEntityId;
                        SetObjectPositionAndRotation(ref _packet, entities[myEntityId]);

                        byte[] sendPacket = Serialize(_packet);

                        client.Send(sendPacket, sendPacket.Length, "127.0.0.1", PORT);

                        // 이후에는 패킷 타입을 Move로 바꾼다.
                        _packet.packetType = PacketType.Move;
                    }
                }
                else if (recvPacket.packetType == PacketType.Initialize)
                {
                    if (isServer)
                    {
                        // 클라이언트의 위치를 기반으로 오브젝트 생성
                        Vector3 enemyPos = GetObjectPosition(recvPacket);
                        Quaternion enemyRot = GetObjectRotation(recvPacket);

                        entities[recvPacket.entityId] = Instantiate(enemyObj, enemyPos, enemyRot);

                        // 자신의 위치와 회전 정보를 알려줌
                        _packet.packetType = PacketType.Initialize;
                        _packet.entityId = myEntityId;
                        SetObjectPositionAndRotation(ref _packet, entities[myEntityId]);

                        // 클라이언트에게 자신의 정보를 기반으로 초기화하라고 전송함
                        byte[] sendPacket = Serialize(_packet);
                        server.Send(sendPacket, sendPacket.Length, result.RemoteEndPoint);
                    }
                    else
                    {
                        // 서버의 위치를 받은 클라이언트는 서버의 오브젝트를 생성하고 접속 로직은 종료됨
                        Vector3 enemyPos = GetObjectPosition(recvPacket);
                        Quaternion enemyRot = GetObjectRotation(recvPacket);

                        entities[recvPacket.entityId] = Instantiate(enemyObj, enemyPos, enemyRot);
                    }
                }
                else if (recvPacket.packetType == PacketType.Move)
                {
                    if (isServer)
                    {
                        if (myEntityId == recvPacket.entityId) continue;

                        if (_packet.packetType != PacketType.Move) _packet.packetType = PacketType.Move;

                        // 클라이언트의 이동 정보를 업데이트 함
                        Vector3 pos = GetObjectPosition(recvPacket);
                        Quaternion rot = GetObjectRotation(recvPacket);

                        // Debug.Log($"client rotation : {rot.x}, {rot.y}, {rot.z}, {rot.w}");
                        // Debug.Log($"client rotation : {rot.eulerAngles}");

                        Enemy entity = entities[recvPacket.entityId].GetComponent<Enemy>();
                        entity.Move(pos, rot);

                        // 자신의 정보를 클라이언트에게 전송함
                        SetObjectPositionAndRotation(ref _packet, entities[myEntityId]);

                        byte[] sendPacket = Serialize(_packet);

                        server.Send(sendPacket, sendPacket.Length, result.RemoteEndPoint);
                    }
                    else
                    {
                        // 현재 내 아이디와 패킷의 아이디가 같다면 무시함
                        if (myEntityId == recvPacket.entityId) continue;

                        // 서버 오브젝트의 이동 정보를 갱신함
                        Vector3 pos = new Vector3(recvPacket.x, recvPacket.y, recvPacket.z);
                        Quaternion rot = new Quaternion(recvPacket.rotX, recvPacket.rotY, recvPacket.rotZ, recvPacket.rotW);

                        Enemy entity = entities[recvPacket.entityId].GetComponent<Enemy>();
                        entity.Move(pos, rot);
                    }
                }
            }
        }

        // EntityPacket -> byte[]
        byte[] Serialize(EntityPacket p)
        {
            byte[] data = new byte[PACKET_SIZE];

            data[0] = (byte)p.packetType;

            Buffer.BlockCopy(BitConverter.GetBytes(p.entityId), 0, data, 1, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(p.x), 0, data, 5, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(p.y), 0, data, 9, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(p.z), 0, data, 13, 4);

            Buffer.BlockCopy(BitConverter.GetBytes(p.rotX), 0, data, 17, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(p.rotY), 0, data, 21, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(p.rotZ), 0, data, 25, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(p.rotW), 0, data, 29, 4);

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
                rotZ = BitConverter.ToSingle(data, 25),
                rotW = BitConverter.ToSingle(data, 29),
            };

            return result;
        }

        void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 30;
            style.normal.textColor = Color.black;

            GUI.Label(new Rect(10, 10, 300, 100), $"my id : {myEntityId}", style);
            
            GUI.Label(new Rect(200, 10, 500, 100), $"isServer : {isServer}", style);

            GUI.Label(new Rect(200, 50, 500, 100), $"isConnecting : {isConnecting}", style);
        }

        // 위치 전달
        private void MovePosition()
        {
            // 패킷 종류를 Move로 변경함
            _packet.packetType = PacketType.Move;

            // 내 엔티티 ID를 저장함
            _packet.entityId = myEntityId;

            // 현재 player의 위치와 회전 정보를 담음
            SetObjectPositionAndRotation(ref _packet, entities[myEntityId]);

            Debug.Log($"rotation : {_packet.rotX}, {_packet.rotY}, {_packet.rotZ}, {_packet.rotW}");

            // 패킷을 전송함
            byte[] sendPacket = Serialize(_packet);

            client.Send(sendPacket, sendPacket.Length, "127.0.0.1", PORT);
        }

        // 오브젝트의 위치 정보 및 회전 정보 저장
        private void SetObjectPositionAndRotation(ref EntityPacket packet, GameObject obj)
        {
            // 위치 정보
            packet.x = obj.transform.position.x;
            packet.y = obj.transform.position.y;
            packet.z = obj.transform.position.z;

            // 회전 정보
            packet.rotX = obj.transform.rotation.x;
            packet.rotY = obj.transform.rotation.y;
            packet.rotZ = obj.transform.rotation.z;
            packet.rotW = obj.transform.rotation.w;
        }

        private Vector3 GetObjectPosition(EntityPacket packet)
        {
            Vector3 result = new Vector3(packet.x, packet.y, packet.z);
            return result;
        }

        private Quaternion GetObjectRotation(EntityPacket packet)
        {
            Quaternion result = new Quaternion(packet.rotX, packet.rotY, packet.rotZ, packet.rotW);
            return result;
        }
    }
}