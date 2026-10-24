using UnityEngine;

namespace VersusFight
{
    public class Enemy : Entity
    {
        
        protected override void Start()
        {
            base.Start();

            // 엔티티 타입 설정
            entityType = EntityType.PLAYABLE;
        }

        public override void Hit(Vector3 attackAreaPosition, float attack = 1f, bool isReceive = false)
        {
            if (state != State.Hit) state = State.Hit;

            base.Hit(attackAreaPosition, attack, isReceive);
        }

        
        // ======네트워크 함수======
        // 적은 패킷을 보내지 않고 수신한 값으로만 이동하므로
        // override 하여 이동 패킷을 보내지 않도록 설정한다.
        protected override void NetworkLoop() { }
        // ======네트워크 함수======
    }
}