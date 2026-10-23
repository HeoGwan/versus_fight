using System;
using System.Collections;

using UnityEditor;
using UnityEngine;


namespace VersusFight
{
    [
        RequireComponent(typeof(Rigidbody)),
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

        protected Rigidbody rigid;
        [SerializeField] protected EntityInfo info;
        public EntityInfo Info => info;

        // 네트워크 루프 간격
        protected float networkInterval = 1f / 30f;
        protected float time = 0.0f;
        protected Vector3 targetPosition;
        protected Quaternion targetRotation;

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
            rigid = GetComponent<Rigidbody>();
            info = new EntityInfo();

            waitInvincible = new WaitForSecondsRealtime(invincibleDuration);

            if (hitParticle == null)
            {
                Debug.LogWarning("not assigned hitParticle");
            }

            if (animator == null)
            {
                Debug.LogWarning("not assigned animator");
            }
            else animator.enabled = true;
        }

        protected virtual void Update()
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
                if (rigid.linearVelocity == Vector3.zero)
                {
                    state = State.Idle;
                }
            }
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

            // 피격 받은 쪽으로 넉백 (서버만 넉백됨)
            if (VNetworkManager.Instance.IsServer)
                rigid.AddForce(hitDirection * knockbackForce);

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

        public void Move(Vector3 position, Quaternion rotation)
        {
            if (state != State.Move) state = State.Move;
            targetPosition = position;
            targetRotation = rotation;
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