using UnityEngine;

using System;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace VersusFight
{
    public abstract class VNetwork : MonoBehaviour
    {
        // Packet
        protected Packet packet;
        protected const uint MAX_PLAYERS = 2;


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
        public bool Disconnect()
        {
            if (udp == null) return false;

            // 모든 플레이어와 적 오브젝트를 파괴한다.
            foreach (GameObject obj in entities)
            {
                Destroy(obj);
            }
            for (int i = 0; i < MAX_PLAYERS + 1; ++i)
            {
                entities[i] = null;
            }

            isConnecting = false;
            udp.Close();
            udp = null;

            return true;
        }

        // 응답 루프
        protected abstract Task ReceiveLoop();

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


        // entites를 순회하며 가능한 곳에 오브젝트를 추가한다
        protected void AddObject(GameObject obj, Vector3 spawnPos, Quaternion spawnRot)
        {
            for (int i = 1; i < MAX_PLAYERS + 1; ++i)
            {
                if (entities[i] == null)
                {
                    entities[i] = Instantiate(obj, spawnPos, spawnRot);
                    return;
                }
            }
        }
    }
}