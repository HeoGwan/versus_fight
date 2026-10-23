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
    }
}