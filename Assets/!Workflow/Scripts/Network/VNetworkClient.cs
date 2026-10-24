using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;


namespace VersusFight
{
    public class VNetworkClient : VNetwork
    {
        private string ip = "127.0.0.1";
        private int port = 7777;

        public override bool Connect(GameObject player, GameObject enemy)
        {
            // Initialize Prefab
            if (playerObj == null) playerObj = player;
            if (enemyObj == null) enemyObj = enemy;

            // UdpClient 초기화
            udp = new UdpClient();

            // 패킷 타입을 Connect로 하고 서버에게 접속함을 알림
            packet.packetType = PacketType.Connect;
            packet.networkId = 0;
            packet.entityType = EntityType.NONE;
            packet.data = new byte[28];

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

        protected override async Task ReceiveLoop()
        {
            UdpReceiveResult result;
            while (true)
            {
                try
                {
                    result = await udp.ReceiveAsync();
                }
                catch (ObjectDisposedException)
                {
                    Debug.Log("Server ReceiveLoop 종료");
                    break;
                }
                catch (SocketException e)
                {
                    Debug.LogWarning($"UDP 수신 중단: {e.SocketErrorCode}");
                    break;
                }

                // 패킷 정보 저장
                Packet getPacket = PacketSerializer.DeserializePacket(result.Buffer);
                // 패킷 종류 저장
                PacketType packetType = getPacket.packetType;

                switch (packetType)
                {
                    case PacketType.Connect:
                    {
                        MovePacket recvPacket = PacketSerializer.DeserializeMove(getPacket.data);
                        // id 저장
                        myNetworkId = getPacket.networkId;
                        VNetworkManager.Instance.NetworkId = myNetworkId;

                        // 자신의 오브젝트를 생성함
                        entities[myNetworkId] = Instantiate(playerObj
                                                    , new Vector3(0, 0.2f, 0)
                                                    , new Quaternion(0, 180f, 0, 1));
                        entities[myNetworkId].GetComponent<Entity>().Info.NetworkId = myNetworkId;

                        // 자신의 위치 및 회전 정보를 서버에 전송함
                        packet.packetType = PacketType.Initialize; // 서버에게 초기화 요청

                        // 패킷 데이터 초기화
                        packet.networkId = myNetworkId;
                        packet.entityType = EntityType.PLAYABLE;
                        SetObjectPositionAndRotation(ref recvPacket, entities[myNetworkId]);

                        packet.data = PacketSerializer.SerializeMove(recvPacket);

                        SendPacket();

                        // 패킷 전송 후 연결 상태로 변경
                        isConnecting = true;
                        VNetworkManager.Instance.IsConnect = true;
                    }   
                    break;
                    case PacketType.Initialize:
                    {
                        MovePacket recvPacket = PacketSerializer.DeserializeMove(getPacket.data);
                        int networkId = getPacket.networkId;

                        // 서버 플레이어의 오브젝트 생성
                        Vector3 enemyPos = GetObjectPosition(recvPacket);
                        Quaternion enemyRot = GetObjectRotation(recvPacket);

                        entities[networkId] = Instantiate(enemyObj, enemyPos, enemyRot);
                        entities[networkId].GetComponent<Entity>().Info.NetworkId = networkId;
                    }
                    break;
                    case PacketType.Move:
                    {
                        MovePacket recvPacket = PacketSerializer.DeserializeMove(getPacket.data);
                            
                        // 오브젝트의 이동 정보를 갱신함
                        Vector3 pos = GetObjectPosition(recvPacket);
                        Quaternion rot = GetObjectRotation(recvPacket);

                        int entityId = getPacket.networkId;
                        EntityType entityType = getPacket.entityType;
                        bool isPlayable = entityType == EntityType.PLAYABLE;

                        // 클라이언트에서는 상호작용 오브젝트도 패킷을 받아 진행함
                        Entity entity = isPlayable ?
                            entities[entityId].GetComponent<Entity>() :
                            interactableObjects[entityId].GetComponent<InteractableObject>();
                        
                        entity.Move(pos, rot);
                    }
                    break;
                    case PacketType.Hit:
                    {
                        HitPacket recvPacket = PacketSerializer.DeserializeHit(getPacket.data);
                        int entityId = getPacket.networkId;
                        EntityType entityType = getPacket.entityType;

                        // 공격을 받았을 경우
                        bool isPlayable = entityType == EntityType.PLAYABLE;
                        Entity entity = isPlayable ?
                            entities[entityId].GetComponent<Entity>() :
                            interactableObjects[entityId].GetComponent<InteractableObject>();
                        // Entity entity = entities[myNetworkId].GetComponent<Entity>();
                        Debug.Log(entity.name);

                        Vector3 attackDir = new Vector3(recvPacket.knockbackDirectionX,
                                                        recvPacket.knockbackDirectionY,
                                                        recvPacket.knockbackDirectionZ);
                        if (entity == null)
                        {
                            Debug.Log("[Hit] entity is null");
                        }
                        entity.Hit(attackDir, recvPacket.attack, true);
                    }
                    break;
                    case PacketType.Attack:
                    {
                        AttackPacket recvPacket = PacketSerializer.DeserializeAttack(getPacket.data);
                        int entityId = getPacket.networkId;
                        EntityType entityType = getPacket.entityType;
                        // 공격을 받았을 경우
                        bool isPlayable = entityType == EntityType.PLAYABLE;
                        Entity entity = isPlayable ?
                            entities[entityId].GetComponent<Entity>() :
                            interactableObjects[entityId].GetComponent<InteractableObject>();
                        // Entity entity = entities[myNetworkId].GetComponent<Entity>();

                        // Vector3 attackDir = new Vector3(recvPacket.attackDirectionX,
                        //                                 recvPacket.attackDirectionY,
                        //                                 recvPacket.attackDirectionZ);
                        // Debug.Log($"Client attackDir : {attackDir}");
                        // entity.Hit(attackDir, recvPacket.attack);

                        // 공격 애니메이션 진행
                        // entity.Attack();
                    }
                    break;
                    case PacketType.Disconnect:
                    {
                        // DisconnectPacket recvPacket = PacketSerializer.DeserializeDiconnect(getPacket.data);
                        // 만약 recvPacket의 networkId가 1이라면 서버가 종료된 것
                        int networkId = getPacket.networkId;
                        if (networkId == 1)
                        {
                            Debug.Log("Server Shutdown");
                            // 서버 종료 UI 표시

                            // 접속을 끊음
                            GameManager.Instance.Disconnect();

                            return;
                        }
                        // 자신의 id와 다를 경우
                        else if (networkId != myNetworkId)
                        {
                            Debug.Log("Other Client Down");
                            // 다른 클라이언트 종료
                            GameObject outEntity = entities[networkId];
                            Destroy(outEntity);
                        }
                        // 자신과 id가 같으면 이미 접속 종료한 것을 의미함
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
                Debug.LogWarning("udp is null");
                return;
            }

            byte[] sendPacket = PacketSerializer.SerializePacket(packet);
            udp.Send(sendPacket, sendPacket.Length, ip, port);
        }

        public override void SendPacket(Packet p)
        {
            if (udp == null)
            {
                Debug.LogWarning("udp is null");
                return;
            }

            byte[] sendPacket = PacketSerializer.SerializePacket(p);
            udp.Send(sendPacket, sendPacket.Length, ip, port);
        }
    }
}