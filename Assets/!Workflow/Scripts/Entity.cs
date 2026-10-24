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
        // 패킷 데이터
        protected MovePacket movePacket;


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


        protected virtual void Awake() 
        {
            rigid = GetComponent<Rigidbody>();
            info = new EntityInfo();

            targetPosition = transform.position;
            targetRotation = transform.rotation;

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

        protected virtual void Start()
        {
            
        }

        protected virtual void Update()
        {
            MoveLerp();

            // 네트워크 루프 관련
            // 네트워크 연결이 안되어 있으면 무시
            if (!VNetworkManager.Instance.IsConnect) return;

            time += Time.deltaTime;
            if (time >= networkInterval)
            {
                NetworkLoop();
                time = 0.0f;
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

        public virtual void Move(Vector3 position, Quaternion rotation)
        {
            if (state != State.Move) state = State.Move;
            targetPosition = position;
            targetRotation = rotation;
        }


        // ========네트워크 함수========
        protected virtual void NetworkLoop()
        {
            MoveNetwork();
        }

        protected virtual void MoveLerp()
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

        protected void MoveNetwork()
        {
            SetObjectPositionAndRotation(ref movePacket, transform);

            // Test
            if (entityType == EntityType.OBJECTS)
            {
                Debug.Log($"id : {Info.NetworkId}, name: {name}");
            }

            // 패킷을 전송함
            VNetworkManager.Instance.SendPacket(PacketType.Move, Info.NetworkId, entityType,
                        PacketSerializer.SerializeMove(movePacket));
        }

        protected void SetObjectPositionAndRotation(ref MovePacket packet, Transform obj)
        {
            // 위치 정보
            packet.x = obj.position.x;
            packet.y = obj.position.y;
            packet.z = obj.position.z;

            // 회전 정보
            packet.rotX = obj.rotation.x;
            packet.rotY = obj.rotation.y;
            packet.rotZ = obj.rotation.z;
            packet.rotW = obj.rotation.w;
        }
        // ========네트워크 함수========


        // ========코루틴========
        // 무적 코루틴
        IEnumerator Invincible()
        {
            isInvincible = true;

            yield return waitInvincible;

            isInvincible = false;
        }
    }
}