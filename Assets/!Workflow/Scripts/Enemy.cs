using UnityEngine;

namespace VersusFight
{
    public class Enemy : Entity
    {
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        protected override void Start()
        {
            base.Start();

            targetPosition = transform.position;
        }

        void Update()
        {
            if (state == State.Move)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    walkSpeed * Time.deltaTime
                );
                
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    walkSpeed * Time.deltaTime
                );
            }
            else if (state == State.Hit)
            {
                if (_rigid.linearVelocity == Vector3.zero)
                {
                    state = State.Idle;
                }
            }
        }

        public override void Hit(Vector3 attackAreaPosition, float attack)
        {
            if (state != State.Hit) state = State.Hit;

            base.Hit(attackAreaPosition, attack);
        }

        public void Move(Vector3 position, Quaternion rotation)
        {
            if (state != State.Move) state = State.Move;
            targetPosition = position;
            targetRotation = rotation;
        }
    }
}