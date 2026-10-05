using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace VersusFight
{
    public class VNetworkServer : VNetwork
    {
        // Server Setting
        private const int PORT = 7777;


        // grant
        private int networkId = 1;
        private IPEndPoint remoteEndPoint;


        public override bool Connect(GameObject player, GameObject enemy)
        {
            // Initialize Prefab
            playerObj = player;
            enemyObj = enemy;

            // UdpClient 초기화
            udp = new UdpClient(PORT);

            // entityId를 저장함
            myNetworkId = networkId++;

            // 서버로 설정한 플레이어가 맨 처음 소환된다.
            entities[myNetworkId] = Instantiate(playerObj, new Vector3(0, 0.2f, -7.1f), Quaternion.identity);

            _ = ReceiveLoop();

            return true;
        }

        public override bool Connect(GameObject player, GameObject enemy, Action connectAction)
        {
            if (Connect(player, enemy))
            {
                connectAction();
                return true;
            }

            return false;
        }

        public override bool Disconnect()
        {
            if (udp == null) return false;

            udp.Close();

            return true;
        }

        protected override async Task ReceiveLoop()
        {
            while (true)
            {
                var result = await udp.ReceiveAsync();

                // 요청을 보낸 클라이언트의 정보 저장
                remoteEndPoint = result.RemoteEndPoint;
                // 패킷 정보 저장
                Packet recvPacket = Deserialize(result.Buffer);
                // 패킷 종류 저장
                PacketType packetType = recvPacket.packetType;

                // 클라이언트 접속 진행
                if (packetType == PacketType.Connect)
                {
                    Packet packet = new Packet()
                    {
                        packetType = PacketType.Connect,
                        networkId = networkId++,
                    };

                    // 서버 플레이어의 위치 및 회전 정보 전달
                    GameObject entity = entities[myNetworkId];
                    SetObjectPositionAndRotation(ref packet, entity);

                    // 클라이언트에게 접속 되었다고 알림
                    SendPacket(packet);

                    isConnecting = true;
                }
                // 서버 초기화 진행
                else if (packetType == PacketType.Initialize)
                {
                    // 전달받은 클라이언트 위치 및 회전 정보를 기반으로 오브젝트 생성
                    Vector3 enemyPos = GetObjectPosition(recvPacket);
                    Quaternion enemyRot = GetObjectRotation(recvPacket);

                    entities[recvPacket.networkId] = Instantiate(enemyObj, enemyPos, enemyRot);

                    // 클라이언트에게 자신의 위치 정보를 알려줌
                    packet.packetType = PacketType.Initialize;
                    packet.networkId = myNetworkId;
                    SetObjectPositionAndRotation(ref packet, entities[myNetworkId]);

                    // 클라이언트에게 초기화 하라고 요청함
                    SendPacket();
                }
                // 이동 진행
                else if (packetType == PacketType.Move)
                {
                    // 클라이언트의 이동 정보를 기반으로 업데이트 진행
                    Vector3 pos = GetObjectPosition(recvPacket);
                    Quaternion rot = GetObjectRotation(recvPacket);

                    Entity entity = entities[recvPacket.networkId].GetComponent<Entity>();
                    entity.Move(pos, rot);

                    // 자신의 정보를 클라이언트에게 전송함
                    if (packet.packetType != PacketType.Move) packet.packetType = PacketType.Move;

                    SetObjectPositionAndRotation(ref packet, entities[myNetworkId]);

                    // 클라이언트에게 자신의 이동 정보 전송
                    SendPacket();
                }
            }
        }

        // 패킷 전송
        private void SendPacket()
        {
            byte[] sendPacket = Serialize(packet);
            udp.Send(sendPacket, sendPacket.Length, remoteEndPoint);
        }

        private void SendPacket(Packet p)
        {
            byte[] sendPacket = Serialize(p);
            udp.Send(sendPacket, sendPacket.Length, remoteEndPoint);
        }
    }
}