using System.Collections.Specialized;
using Unity.Mathematics;
using UnityEngine;

namespace VersusFight
{
    public class InteractableObject : Entity
    {
        private Packet packet;
        private MovePacket movePacket;

        [Space(10)]
        [SerializeField] private float hitPower = 10f;

        protected override void Awake()
        {
            base.Awake();

            // 엔티티 타입 설정
            entityType = EntityType.OBJECTS;
        }

        protected override void Update()
        {
            // 연결되어있지 않으면 아무런 반응하지 않음
            if (!VNetworkManager.Instance.IsConnect) return;

            // 클라이언트는 패킷을 전송하지 않음
            if (!VNetworkManager.Instance.IsServer) return;

            // 가만히 있을 때는 패킷을 보내지 않음
            // if (rigid.IsSleeping()) return;
            if (rigid.linearVelocity.sqrMagnitude < 0.01f) return;

            time += Time.deltaTime;

            if (time >= networkInterval)
            {
                time = 0.0f;

                // 현재 위치와 회전 값을 전달한다
                // Vector3 pos = transform.position;
                // Quaternion rot = transform.rotation;
                transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);

                movePacket.x = pos.x;
                movePacket.y = pos.y;
                movePacket.z = pos.z;

                movePacket.rotX = rot.x;
                movePacket.rotY = rot.y;
                movePacket.rotZ = rot.z;
                movePacket.rotW = rot.w;

                if (packet.packetType != PacketType.Move) packet.packetType = PacketType.Move;
                if (packet.networkId != info.NetworkId) packet.networkId = info.NetworkId;
                if (packet.entityType != EntityType.OBJECTS) packet.entityType = EntityType.OBJECTS;
                packet.data = PacketSerializer.SerializeMove(movePacket);
                
                VNetworkManager.Instance.SendPacket(packet);
            }
        }

        public override void Hit(Vector3 attackAreaPosition, float attack = 1f, bool isReceive = false)
        {
            Vector3 hitDirection = transform.position - attackAreaPosition;
            float distance = Vector3.Distance(attackAreaPosition, transform.position);

            if (VNetworkManager.Instance.IsServer)
                rigid.AddForce(hitDirection * distance * hitPower);

            // 네트워크로 피격 패킷을 전송함
            if (isReceive) return;

            HitPacket hitPacket = new HitPacket()
            {
                // networkId = VNetworkManager.Instance.NetworkId,

                knockbackDirectionX = attackAreaPosition.x,
                knockbackDirectionY = attackAreaPosition.y,
                knockbackDirectionZ = attackAreaPosition.z,
            };
            
            Packet p = new Packet()
            {
                packetType = PacketType.Hit,
                networkId = info.NetworkId, // 저장되어있는 id가 있는지 확인이 필요함
                entityType = EntityType.OBJECTS,
                data = PacketSerializer.SerializeHit(hitPacket)
            };

            VNetworkManager.Instance.SendPacket(p);
        }
    }
}