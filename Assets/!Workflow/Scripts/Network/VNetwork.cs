using UnityEngine;

using System;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace VersusFight
{
    public abstract class VNetwork : MonoBehaviour
    {
        // // Packet
        // // 패킷 타입
        // public enum PacketType : byte
        // {
        //     None = 0,
        //     Connect = 1,
        //     Initialize = 2,
        //     Move = 3,
        //     Run = 4,
        //     Sprint = 5,
        //     DisConnect = 255,
        // }
        // // 패킷 구조체
        // public struct Packet
        // {
        //     public PacketType packetType;   // 1
        //     public int networkId;   // 4

        //     public float x;        // 4
        //     public float y;        // 4
        //     public float z;        // 4

        //     public float rotX;     // 4
        //     public float rotY;     // 4
        //     public float rotZ;     // 4
        //     public float rotW;     // 4
        // }  // 33 bytes
        // public struct MovePacket
        // {  
        //     public int networkId;   // 4

        //     public float x;        // 4
        //     public float y;        // 4
        //     public float z;        // 4

        //     public float rotX;     // 4
        //     public float rotY;     // 4
        //     public float rotZ;     // 4
        //     public float rotW;     // 4
        // }
        // public struct HitPacket
        // {
        //     public int networkId;
        //     public Vector3 knockbackDirection;

        // }
        // protected const uint PACKET_SIZE = 33;
        protected Packet packet;

        private const uint MAX_PLAYERS = 2;


        // UDP Client
        protected UdpClient udp;
        protected bool isConnecting = false;

        // 네트워크 루프 간격
        protected float networkInterval = 1f / 30f;
        protected float time = 0.0f;

        // Network Id
        [SerializeField] protected int myNetworkId = -1;

        // Prefabs
        protected GameObject playerObj;
        protected GameObject enemyObj;

        // 자신을 포함한 모든 오브젝트 저장
        protected GameObject[] entities;


        // Constructor
        public VNetwork()
        {
            packet = new Packet();
            packet.packetType = PacketType.None;

            entities = new GameObject[MAX_PLAYERS + 1];
        }

        // virtual methods
        // 연결
        public abstract bool Connect(GameObject playerObj, GameObject enemyObj);
        public abstract bool Connect(GameObject playerObj, GameObject enemyObj, Action connectAction);

        // 연결 끊기
        public abstract bool Disconnect();

        // 응답 루프
        protected abstract Task ReceiveLoop();


        // // 직렬화
        // public byte[] Serialize(Packet p)
        // {
        //     byte[] data = new byte[PACKET_SIZE];

        //     data[0] = (byte)p.packetType;

        //     Buffer.BlockCopy(BitConverter.GetBytes(p.networkId), 0, data, 1, 4);

        //     Buffer.BlockCopy(BitConverter.GetBytes(p.x), 0, data, 5, 4);
        //     Buffer.BlockCopy(BitConverter.GetBytes(p.y), 0, data, 9, 4);
        //     Buffer.BlockCopy(BitConverter.GetBytes(p.z), 0, data, 13, 4);

        //     Buffer.BlockCopy(BitConverter.GetBytes(p.rotX), 0, data, 17, 4);
        //     Buffer.BlockCopy(BitConverter.GetBytes(p.rotY), 0, data, 21, 4);
        //     Buffer.BlockCopy(BitConverter.GetBytes(p.rotZ), 0, data, 25, 4);
        //     Buffer.BlockCopy(BitConverter.GetBytes(p.rotW), 0, data, 29, 4);

        //     return data;
        // }

        // // 역직렬화
        // protected Packet Deserialize(byte[] data)
        // {
        //     Packet result = new Packet
        //     {
        //         packetType = (PacketType)data[0],

        //         networkId = BitConverter.ToInt32(data, 1),

        //         x = BitConverter.ToSingle(data, 5),
        //         y = BitConverter.ToSingle(data, 9),
        //         z = BitConverter.ToSingle(data, 13),

        //         rotX = BitConverter.ToSingle(data, 17),
        //         rotY = BitConverter.ToSingle(data, 21),
        //         rotZ = BitConverter.ToSingle(data, 25),
        //         rotW = BitConverter.ToSingle(data, 29),
        //     };

        //     return result;
        // }

        // 오브젝트의 이동 정보 저장
        protected void SetObjectPositionAndRotation(ref MovePacket packet, GameObject obj)
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

        protected Vector3 GetObjectPosition(MovePacket packet)
        {
            Vector3 result = new Vector3(packet.x, packet.y, packet.z);
            return result;
        }

        protected Quaternion GetObjectRotation(MovePacket packet)
        {
            Quaternion result = new Quaternion(packet.rotX, packet.rotY, packet.rotZ, packet.rotW);
            return result;
        }

        public abstract void SendPacket(Packet p);
    }
}