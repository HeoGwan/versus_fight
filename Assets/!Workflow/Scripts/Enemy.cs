using UnityEngine;

namespace VersusFight
{
    public class Enemy : Entity
    {
        Packet packet;
        MovePacket movePacket;
        
        protected override void Start()
        {
            base.Start();

            targetPosition = transform.position;

            // 엔티티 타입 설정
            entityType = EntityType.PLAYABLE;
        }

        protected override void Update()
        {
            if (!VNetworkManager.Instance.IsConnect) return;

            base.Update();

            time += Time.deltaTime;

            if (time >= networkInterval)
            {
                // 이동
                MovePosition();
                time = 0.0f;
            }
        }

        public override void Hit(Vector3 attackAreaPosition, float attack = 1f, bool isReceive = false)
        {
            if (state != State.Hit) state = State.Hit;

            base.Hit(attackAreaPosition, attack, isReceive);
        }

        // 위치 전달
        private void MovePosition()
        {
            // // 패킷 종류를 Move로 변경함
            // if (packet.packetType != PacketType.Move) packet.packetType = PacketType.Move;

            // // 내 엔티티 ID를 저장함
            // if (packet.networkId != info.NetworkId) packet.networkId = info.NetworkId;
            // if (packet.entityType != EntityType.PLAYABLE) packet.entityType = EntityType.PLAYABLE;
            // // 현재 player의 위치와 회전 정보를 담음
            // SetObjectPositionAndRotation(ref movePacket, entities[myNetworkId]);

            // packet.data = PacketSerializer.SerializeMove(movePacket);

            // 패킷을 전송함
            VNetworkManager.Instance.SendPacket(PacketType.Move, entityType,
                        PacketSerializer.SerializeMove(movePacket));
        }

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
    }
}