using UnityEngine;

namespace VersusFight
{
    public class InteractableObject : Entity
    {
        //private Packet packet;
        //private MovePacket movePacket;

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
            // 클라이언트는 이동 보간을 진행함
            MoveLerp();

            // 클라이언트는 패킷을 전송하지 않음
            if (!VNetworkManager.Instance.IsServer) return;

            // 가만히 있을 때는 패킷을 보내지 않음
            if (rigid.linearVelocity.sqrMagnitude < 0.01f &&
                rigid.angularVelocity.sqrMagnitude < 0.01f) return;

            base.Update();
        }

        public override void Hit(Vector3 attackAreaPosition, float attack = 1f, bool isReceive = false)
        {
            // 클라이언트일 떄만 서버로 패킷을 보냄
            if (!VNetworkManager.Instance.IsServer)
            {
                HitPacket hitPacket = new HitPacket()
                {
                    // networkId = VNetworkManager.Instance.NetworkId,

                    knockbackDirectionX = attackAreaPosition.x,
                    knockbackDirectionY = attackAreaPosition.y,
                    knockbackDirectionZ = attackAreaPosition.z,

                    attack = attack,
                };

                Packet p = new Packet()
                {
                    packetType = PacketType.Hit,
                    networkId = info.NetworkId, // 저장되어있는 id가 있는지 확인이 필요함
                    entityType = EntityType.OBJECTS,
                    data = PacketSerializer.SerializeHit(hitPacket)
                };

                VNetworkManager.Instance.SendPacket(p);

                return;
            }

            // 서버는 패킷 보낼 필요 없음
            // 서버라면 AddForce만 함 (상호작용 오브젝트 에서 이동 패킷 보냄)
            Vector3 hitDirection = transform.position - attackAreaPosition;
            float distance = Vector3.Distance(attackAreaPosition, transform.position);

            rigid.AddForce(distance * hitPower * hitDirection);
        }
    }
}