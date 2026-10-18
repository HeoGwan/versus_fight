using System;
using System.Collections;

using UnityEditor;
using UnityEngine;


namespace VersusFight
{
    public enum EntityType : byte
    {
        NONE = 0,
        PLAYABLE = 1,
        OBJECTS = 2,
    };

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
        public EntityInfo Info => _info;

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

        [Space(10)]
        [SerializeField] private string _sceneId;
        public string SceneId => _sceneId;

        [Space(10)]
        [SerializeField] protected EntityType entityType;

        protected virtual void Awake() {}

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

        void OnValidate()
        {
            #if UNITY_EDITOR
            GlobalObjectId id =
                GlobalObjectId.GetGlobalObjectIdSlow(gameObject);
            _sceneId = id.ToString();
            #endif
        }

        public virtual void Hit(Vector3 attackAreaPosition, float attack = 1f, bool isReceive = false)
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

            // 무적 시간
            StartCoroutine(Invincible());

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

        // 무적 코루틴
        IEnumerator Invincible()
        {
            isInvincible = true;

            yield return waitInvincible;

            isInvincible = false;
        }
    }
}