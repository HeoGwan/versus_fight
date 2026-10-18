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
        private int players = 1;

        // grant
        private int networkId = 1;
        private IPEndPoint remoteEndPoint; // 여러 클라이언트가 접속하면 리스트 형태로 바뀌어야 함


        public override bool Connect(GameObject player, GameObject enemy)
        {
            // Initialize Prefab
            if (playerObj == null) playerObj = player;
            if (enemyObj == null) enemyObj = enemy;

            // UdpClient 초기화
            udp = new UdpClient(PORT);

            // entityId를 저장함
            players = 1;
            myNetworkId = networkId++;
            VNetworkManager.Instance.NetworkId = myNetworkId;

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

        protected override async Task ReceiveLoop()
        {
            while (true)
            {
                var result = await udp.ReceiveAsync();

                // 요청을 보낸 클라이언트의 정보 저장
                remoteEndPoint = result.RemoteEndPoint;                
                // 패킷 정보 저장
                Packet getPacket = PacketSerializer.DeserializePacket(result.Buffer);
                // 패킷 종류 저장
                PacketType packetType = getPacket.packetType;

                // Debug.Log($"Receive Packet : {getPacket}");

                // 클라이언트 접속 진행
                switch(packetType)
                {
                    case PacketType.Connect:
                    {
                        Debug.Log("Receive Connect");
                        if (players == Setting.MAX_PLAYERS)
                        {
                            // 더이상 접근할 수 없음을 알림
                            packet.packetType = PacketType.MaxConnect;
                            SendPacket();
                            continue;
                        }

                        MovePacket recvPacket = PacketSerializer.DeserializeMove(getPacket.data);

                        // 패킷 타입 설정
                        packet.packetType = PacketType.Connect;

                        // 서버 플레이어의 위치 및 회전 정보 전달
                        players++;
                        packet.networkId = networkId++;
                        packet.entityType = EntityType.PLAYABLE;
                        GameObject entity = entities[myNetworkId];
                        SetObjectPositionAndRotation(ref recvPacket, entity);
                        packet.data = PacketSerializer.SerializeMove(recvPacket);

                        // 클라이언트에게 접속 되었다고 알림
                        SendPacket();
                        isConnecting = true;
                    }
                    break;
                    // 서버 초기화 진행
                    case PacketType.Initialize:
                    {
                        MovePacket recvPacket = PacketSerializer.DeserializeMove(getPacket.data);
                        // 전달받은 클라이언트 위치 및 회전 정보를 기반으로 오브젝트 생성
                        Vector3 enemyPos = GetObjectPosition(recvPacket);
                        Quaternion enemyRot = GetObjectRotation(recvPacket);
                        entities[getPacket.networkId] = Instantiate(enemyObj, enemyPos, enemyRot);

                        // 클라이언트에게 자신의 위치 정보를 알려줌
                        packet.packetType = PacketType.Initialize;

                        // 위치 정보 저장
                        packet.networkId = myNetworkId;
                        packet.entityType = EntityType.PLAYABLE;
                        GameObject entity = entities[myNetworkId];
                        SetObjectPositionAndRotation(ref recvPacket, entity);
                        packet.data = PacketSerializer.SerializeMove(recvPacket);

                        // 클라이언트에게 초기화 하라고 요청함
                        SendPacket();
                    }
                    break;
                    // 이동 진행
                    case PacketType.Move:
                    {
                        MovePacket recvPacket = PacketSerializer.DeserializeMove(getPacket.data);
                        // 클라이언트의 이동 정보를 기반으로 업데이트 진행
                        Vector3 pos = GetObjectPosition(recvPacket);
                        Quaternion rot = GetObjectRotation(recvPacket);

                        Enemy entity = entities[getPacket.networkId].GetComponent<Enemy>();
                        entity.Move(pos, rot);

                        // 자신의 정보를 클라이언트에게 전송함
                        if (packet.packetType != PacketType.Move) packet.packetType = PacketType.Move;

                        packet.networkId = myNetworkId;
                        packet.entityType = EntityType.PLAYABLE;
                        SetObjectPositionAndRotation(ref recvPacket, entities[myNetworkId]);
                        packet.data = PacketSerializer.SerializeMove(recvPacket);

                        // 클라이언트에게 자신의 이동 정보 전송
                        SendPacket();
                    }
                    break;
                    case PacketType.Hit:
                    {
                        HitPacket recvPacket = PacketSerializer.DeserializeHit(getPacket.data);
                        int entityId = getPacket.networkId;
                        EntityType entityType = getPacket.entityType;
                        // 공격을 받았을 경우
                        // entityType이 PLAYABLE일 때랑 OBJECT일 때랑 다르게 작동한다.
                        bool isPlayable = entityType == EntityType.PLAYABLE;
                        Entity entity = isPlayable ?
                            entities[myNetworkId].GetComponent<Entity>() :
                            interactableObjects[entityId].GetComponent<InteractableObject>();

                        Vector3 attackDir = new Vector3(recvPacket.knockbackDirectionX,
                                                        recvPacket.knockbackDirectionY,
                                                        recvPacket.knockbackDirectionZ);
                        Debug.Log($"Server attackDir : {attackDir}");
                        entity.Hit(attackDir, recvPacket.attack, true);

                        // 공격 정보를 전달함
                        packet.packetType = PacketType.Attack;
                        packet.networkId = isPlayable ? myNetworkId : getPacket.networkId;
                        packet.entityType = getPacket.entityType;
                        packet.data = PacketSerializer.SerializeHit(recvPacket);
                        SendPacket();
                    }
                    break;
                    case PacketType.Attack:
                    {
                        AttackPacket recvPacket = PacketSerializer.DeserializeAttack(getPacket.data);
                        int entityId = getPacket.networkId;
                        EntityType entityType = getPacket.entityType;
                        // 공격을 받았을 경우
                        // entityType이 PLAYABLE일 때랑 OBJECT일 때랑 다르게 작동한다.
                        bool isPlayable = entityType == EntityType.PLAYABLE;
                        Entity entity = isPlayable ?
                            entities[myNetworkId].GetComponent<Entity>() :
                            interactableObjects[entityId].GetComponent<Entity>();

                        // Vector3 attackDir = new Vector3(recvPacket.attackDirectionX,
                        //                                 recvPacket.attackDirectionY,
                        //                                 recvPacket.attackDirectionZ);
                        // Debug.Log($"Server attackDir : {attackDir}");
                        // entity.Hit(attackDir, recvPacket.attack);
                        
                        // 공격 애니메이션 진행
                        // entity.Attack();

                        // 공격 정보를 전달함
                        packet.packetType = PacketType.Attack;
                        packet.networkId = isPlayable ? myNetworkId : getPacket.networkId;
                        packet.entityType = getPacket.entityType;
                        packet.data = PacketSerializer.SerializeAttack(recvPacket);
                        SendPacket();
                    }
                    break;
                    case PacketType.Disconnect:
                    {
                        Debug.Log("Client Shutdown");
                        // DisconnectPacket recvPacket = PacketSerializer.DeserializeDiconnect(getPacket.data);
                        // 연결 종료 시 전달받은 networkId에 해당하는 오브젝트를 지움
                        GameObject outEntity = entities[packet.networkId];
                        if (outEntity == null) continue;
                        Destroy(outEntity);

                        players--;
                        networkId--;

                        // // 다른 클라이언트에게 해당 오브젝트가 서버에서 나갔다는 것을 알려주기 위해
                        // // 패킷을 보냄 (지금은 1:1이므로 필요는 없음)
                        // packet.packetType = PacketType.Disconnect;
                        // packet.data = getPacket.data;
                        // SendPacket();
                    }
                    break;
                }
            }
        }

        // 패킷 전송
        private void SendPacket()
        {
            if (udp == null)
            {
                Debug.LogError("udp is null");
                return;
            }
            
            byte[] sendPacket = PacketSerializer.SerializePacket(packet);
            int sent = udp.Send(sendPacket, sendPacket.Length, remoteEndPoint);
        }

        public override void SendPacket(Packet p)
        {
            if (udp == null)
            {
                Debug.LogError("udp is null");
                return;
            }

            byte[] sendPacket = PacketSerializer.SerializePacket(p);
            udp.Send(sendPacket, sendPacket.Length, remoteEndPoint);
        }
    }
}