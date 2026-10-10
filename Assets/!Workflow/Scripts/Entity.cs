using System;
using System.Collections;
using UnityEngine;


namespace VersusFight
{
    [
        RequireComponent(typeof(Rigidbody)),
        RequireComponent(typeof(EntityInfo)),
    ]
    public class Entity : MonoBehaviour
    {
        [Serializable]
        protected enum State
        {
            Idle,
            Walk,
            Run,
            Sprint,
            Hit,
            Move,
        };
        [SerializeField] protected State state = State.Idle;

        protected Rigidbody _rigid;
        protected EntityInfo _info;

        [SerializeField] protected float knockbackForce = 1.5f; // 넉백 힘
        
        [Space(10)]
        [SerializeField] protected float invincibleDuration = 1f; // 무적 시간
        protected bool isInvincible = false;
        protected WaitForSecondsRealtime waitInvincible;

        [Space(10)]
        [SerializeField] protected float walkSpeed = 5f;

        [Space(10)]
        [SerializeField] protected Animator animator;
        [SerializeField] protected ParticleSystem hitParticle;

        protected virtual void Start()
        {
            _rigid = GetComponent<Rigidbody>();
            _info = GetComponent<EntityInfo>();

            waitInvincible = new WaitForSecondsRealtime(invincibleDuration);

            if (hitParticle == null)
            {
                Debug.LogError("not assigned hitParticle");
            }

            if (animator == null)
            {
                Debug.LogError("not assigned animator");
            }
            else animator.enabled = true;
        }

        public virtual void Hit(Vector3 attackAreaPosition, float attack)
        {
            // 무적 상태 확인
            if (isInvincible) return;

            // 피격 방향 벡터를 얻음
            Vector3 hitDirection = transform.position - attackAreaPosition;
            // Vector3 hitDirection = Vector3.forward;

            // 피격 받은 쪽으로 넉백
            _rigid.AddForce(hitDirection * knockbackForce);

            // 피격 애니메이션 및 파티클 재생
            if (animator.HasParameter("Hit"))
            {
                animator.SetTrigger("Hit");
            }
            hitParticle.Play();

            // 네트워크로 피격 패킷을 전송함
            // HitPacket hitPacket = new HitPacket()
            // {
            //     networkId = VNetworkManager.Instance.NetworkId,

            //     knockbackDirectionX = hitDirection.x,
            //     knockbackDirectionY = hitDirection.y,
            //     knockbackDirectionZ = hitDirection.z,
            // };
            // VNetworkManager.Instance.SendPacket(PacketType.Hit,
            //         PacketSerializer.SerializeHit(hitPacket));

            // 무적 시간
            StartCoroutine(Invincible());
        }

        // 무적 코루틴
        IEnumerator Invincible()
        {
            isInvincible = true;

            yield return waitInvincible;

            isInvincible = false;
        }
    }
}