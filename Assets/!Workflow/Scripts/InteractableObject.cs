using UnityEngine;

namespace VersusFight
{
    public class InteractableObject : Entity
    {
        [Space(10)]
        [SerializeField] private float hitPower = 10f;

        protected override void Awake()
        {
            base.Awake();

            // 엔티티 타입 설정
            entityType = EntityType.OBJECTS;
        }

        public override void Hit(Vector3 attackAreaPosition, float attack = 1f, bool isReceive = false)
        {
            Vector3 hitDirection = transform.position - attackAreaPosition;
            float distance = Vector3.Distance(attackAreaPosition, transform.position);

            _rigid.AddForce(hitDirection * distance * hitPower);

            // 네트워크로 피격 패킷을 전송함
            if (isReceive) return;

            HitPacket hitPacket = new HitPacket()
            {
                // networkId = VNetworkManager.Instance.NetworkId,

                knockbackDirectionX = attackAreaPosition.x,
                knockbackDirectionY = attackAreaPosition.y,
                knockbackDirectionZ = attackAreaPosition.z,
            };
            VNetworkManager.Instance.SendPacket(PacketType.Hit, entityType,
                    PacketSerializer.SerializeHit(hitPacket));
        }
    }
}