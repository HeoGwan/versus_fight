using UnityEngine;

using System;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace VersusFight
{
    public class VNetworkClient : VNetwork
    {
        private string ip = "127.0.0.1";
        private int port = 7777;

        void Update()
        {
            if (udp == null || !isConnecting) return;

            time += Time.deltaTime;

            if (time >= networkInterval)
            {
                // 이동
                MovePosition();
                time = 0.0f;
            }
        }

        public override bool Connect(GameObject player, GameObject enemy)
        {
            // Initialize Prefab
            playerObj = player;
            enemyObj = enemy;

            // UdpClient 초기화
            udp = new UdpClient();

            // 패킷 타입을 Connect로 하고 서버에게 접속함을 알림
            packet.packetType = PacketType.Connect;

            SendPacket();
            
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

                // 패킷 정보 저장
                Packet recvPacket = Deserialize(result.Buffer);
                // 패킷 종류 저장
                PacketType packetType = recvPacket.packetType;

                // 연결
                if (packetType == PacketType.Connect)
                {
                    // id 저장
                    myNetworkId = recvPacket.networkId;

                    // 자신의 오브젝트를 생성함
                    entities[myNetworkId] = Instantiate(playerObj
                                                , new Vector3(0, 0.2f, 0)
                                                , new Quaternion(0, 180f, 0, 1));

                    // 자신의 위치 및 회전 정보를 서버에 전송함
                    packet.packetType = PacketType.Initialize; // 서버에게 초기화 요청
                    packet.networkId = myNetworkId;
                    SetObjectPositionAndRotation(ref packet, entities[myNetworkId]);

                    SendPacket();

                    isConnecting = true;
                }
                // 초기화
                else if (packetType == PacketType.Initialize)
                {
                    // 서버 플레이어의 오브젝트 생성
                    Vector3 enemyPos = GetObjectPosition(recvPacket);
                    Quaternion enemyRot = GetObjectRotation(recvPacket);

                    entities[recvPacket.networkId] = Instantiate(enemyObj, enemyPos, enemyRot);
                }
                // 이동
                else if (packetType == PacketType.Move)
                {
                    // 서버 오브젝트의 이동 정보를 갱신함
                    Vector3 pos = new Vector3(recvPacket.x, recvPacket.y, recvPacket.z);
                    Quaternion rot = new Quaternion(recvPacket.rotX, recvPacket.rotY, recvPacket.rotZ, recvPacket.rotW);

                    Entity entity = entities[recvPacket.networkId].GetComponent<Entity>();
                    entity.Move(pos, rot);
                }
            }
        }

        // 위치 전달
        private void MovePosition()
        {
            // Exception Handle
            if (udp == null) return;

            // 패킷 종류를 Move로 변경함
            packet.packetType = PacketType.Move;

            // 내 엔티티 ID를 저장함
            packet.networkId = myNetworkId;

            // 현재 player의 위치와 회전 정보를 담음
            SetObjectPositionAndRotation(ref packet, entities[myNetworkId]);

            // 패킷을 전송함
            byte[] sendPacket = Serialize(packet);

            udp.Send(sendPacket, sendPacket.Length, ip, port);
        }

        // 패킷 전송
        private void SendPacket()
        {
            byte[] sendPacket = Serialize(packet);
            udp.Send(sendPacket, sendPacket.Length, ip, port);
        }

        private void SendPacket(Packet p)
        {
            byte[] sendPacket = Serialize(p);
            udp.Send(sendPacket, sendPacket.Length, ip, port);
        }
    }
}