using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Serializable]
    enum State
    {
        Idle,
        Walk,
        // Attack,
        // Jump,
        Run,
    };

    // 변수
    Rigidbody _rigid;

    [SerializeField] State _state = State.Idle;
    // State _prevState = State.Idle;
    Vector3 _direction = Vector3.zero;
    float velocity = 0.0f;

    [Header("Variables")]
    [SerializeField] float speed = 0.0f;
    [SerializeField] float walkSpeed = 5f;
    float runTime = 0.0f;
    [SerializeField] float runDuration = 2f;
    [SerializeField] float runSpeedMultiple = 1.75f;
    [SerializeField] float sprintSpeedMultiple = 3f;
    bool isSprinting = false;

    [Space(10)]
    [SerializeField] float jumpForce = 5f;
    bool isJumping = false;

    [Space(10)]
    [SerializeField] float dashSpeed = 10f;

    [Space(10)]
    [SerializeField] private float attackCoolTime = 0.5f;
    bool isAttacking = false;
    [SerializeField] float attackDuration = 0.0f;

    [Header("Components")]
    [Space(10)]
    [SerializeField] Camera playerCamera;
    [SerializeField] float lookUpDownLimit = 60f; // 위아래 회전 각도

    [Space(10)]
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip attackClip;
    float attackClipFrameRate;

    [Space(10)]
    [SerializeField] private BoxCollider attackArea;

    void Start()
    {
        _rigid = GetComponent<Rigidbody>();

        // 1. 커서를 화면 중앙에 고정합니다.
        Cursor.lockState = CursorLockMode.Locked;

        // 2. 커서를 보이지 않게 숨깁니다.
        Cursor.visible = false;

        // 기본 속도는 걷는 속도로 설정한다.
        speed = walkSpeed;

        // 공격 범위는 비활성화한다.
        attackArea.enabled = false;

        // 공격 애니메이션 설정
        attackClipFrameRate = attackClip.frameRate;
        attackDuration = attackClip.length;
    }

    void LateUpdate()
    {
        if (_state == State.Walk && !isSprinting)
        {
            // 걷는 것에서 살짝 달리는 것으로 변경
            runTime += Time.deltaTime;

            if (runTime >= runDuration)
            {
                _state = State.Run;
                speed = walkSpeed * runSpeedMultiple;
                runTime = 0.0f;
            }
        }

        // 일단 State가 Idle 혹은 움직이는 것(Walk, Run, Sprint) 밖에 없으므로 
        // Idle이 아닌 경우는 전부 움직이도록 설정함
        if (_state != State.Idle)
        {
            transform.Translate(_direction * speed * Time.deltaTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 땅에 닿았을 경우 점프를 멈춤
        if (collision.gameObject.CompareTag("Floor"))
        {
            isJumping = false;
        }
    }


    // =====Input System 메서드=====
    // 기본 이동
    void OnMove(InputValue value)
    {
        // 이동 처리 (앞, 뒤, 좌, 우에 대한 벡터)
        Vector2 move = value.Get<Vector2>();

        // 이동을 멈췄을 경우
        if (move == null || move == Vector2.zero)
        {
            // 상태를 Idle로 변경하고 속도도 걷는 속도로 변경함
            _state = State.Idle;
            speed = walkSpeed;
            return;
        }

        // Idle 상태였을 경우에만 Walk로 변경, Run 상태는 그대로 유지
        if (_state == State.Idle) _state = State.Walk;

        // 이동하는 방향을 저장함
        _direction = new Vector3(move.x, 0, move.y);
    }

    // 전력질주
    void OnSprint(InputValue value)
    {
        if (value.isPressed)    // 전력질주 키를 눌렀을 떄
        {
            // 걷는 속도에 전력질주 배수를 곱한다.
            speed = walkSpeed * sprintSpeedMultiple;
            // 전력질주 중임을 알림
            isSprinting = true;
        }
        else                    // 전력질주 키를 뗐을 떄
        {
            // 만약 달리던 중이라면 뛰는 속도로 변경함
            if (_state == State.Run) speed = walkSpeed * runSpeedMultiple;
            // 만약 달리던 중이 아니라면 걷는 속도도 변경함
            else speed = walkSpeed;
            
            // 전력질주가 끝났음을 알림
            isSprinting = false;
            // 달리는 시간 초기화 (달리는 시간이 쌓이던 도중 전력질주를 했을 때
            // 전력질주가 끝나고 바로 달리기로 변경되는 것 방지)
            runTime = 0.0f;
        }
    }

    // 점프
    void OnJump(InputValue value)
    {
        // 점프 중이 아닌 경우에만 점프 진행
        if (value.isPressed && !isJumping)
        {
            // 현재 점프 중임을 알리는 플래그 설정
            isJumping = true;
            // 점프 속도를 jumpForce로 초기화함
            velocity = jumpForce;
            StartCoroutine(JumpCoroutine());
        }
    }

    // 공격
    void OnAttack(InputValue value)
    {
        // 공격 중이 아닐 경우에만 공격 진행
        if (value.isPressed && !isAttacking)
        {
            // 현재 공격 중임을 알리는 플래그 설정
            isAttacking = true;
            // 애니메이션 실행
            animator.SetTrigger("Attack");
            // 공격 범위 활성화
            attackArea.enabled = true;
            StartCoroutine(AttackCoroutine());
        }
    }

    // 카메라 시야 회전
    void OnLook(InputValue value)
    {
        // 마우스 혹은 패드가 움직인 방향을 가져옴
        Vector2 look = value.Get<Vector2>();

        // 예외 처리
        if (look == null) return;

        // 위아래 회전 제한을 위해 카메라의 현재 회전 각도를 가져옵니다.
        Vector3 currentRotation = playerCamera.transform.localEulerAngles;

        // 마우스 이동에 따라 카메라 회전 처리
        transform.Rotate(Vector3.up, look.x);

        // 카메라는 플레이어의 자식으로 설정되어 있으므로, 카메라 회전은 플레이어의 회전에 따라 자동으로 이루어집니다.
        // 카메라 회전 시 위아래 적절한 범위를 제한하려면 카메라의 회전 각도를 직접 제어해야 합니다.)
        if (look.y < 0) // 아래로 보기
        {
            playerCamera.transform.Rotate(Vector3.right, -look.y);

            if (currentRotation.x > lookUpDownLimit && currentRotation.x < 180f)
            {
                playerCamera.transform.localEulerAngles = new Vector3(lookUpDownLimit, currentRotation.y, currentRotation.z);
            }
        }
        else    // 위로 보기
        {
            float maxLookLimit = 360f - lookUpDownLimit;
            playerCamera.transform.Rotate(Vector3.right, -look.y);

            if (currentRotation.x < maxLookLimit && currentRotation.x > 180f)
            {
                playerCamera.transform.localEulerAngles = new Vector3(maxLookLimit, currentRotation.y, currentRotation.z);
            }
        }
    }


    // Public 메서드
    // 공격이 종료되었을 경우 호출 (Animation Event 활용)
    public void EndAttack()
    {
        // 공격이 끝났음을 알림
        isAttacking = false;
        // 공격 범위 비활성화
        attackArea.enabled = false;
        // 가속도를 0으로 설정
        _rigid.linearVelocity = Vector3.zero;

        // 공격 쿨타임 지정
        float time = 0.0f;
        
        while (time < attackCoolTime)
        {
            time += Time.deltaTime;
        }
    }


    // Coroutine 메서드
    // 공격 코루틴
    IEnumerator AttackCoroutine()
    {
        // 공격이 진행되는 시간 설정
        float elapsedTime = 0f;
        // 공격 방향(앞, 뒤로 움직이는 방향) 확인
        Vector3 position = Vector3.zero;
        float moveWay = _direction.z == 0 ? 1 : _direction.z;
        float dashVelocity = dashSpeed * moveWay;

        // 공격 시 기본 대쉬 진행
        while (isAttacking)
        {
            // V = V_0(1 - t) : 마찰력 (선형 감소)
            dashVelocity = dashSpeed * (1 - (elapsedTime / attackDuration));

            position.z = dashVelocity;

            transform.Translate(position * Time.deltaTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // 점프 코루틴
    IEnumerator JumpCoroutine()
    {
        // 점프하는 방향 벡터
        Vector3 position = Vector3.zero;

        // 점프 중일 경우 반복
        while (isJumping)
        {
            // velocity는 JumpCoroutine에 들어오기 전 velocity = jumpForce로 초기화된다.
            // velocity는 매 틱당 가속도 X deltaTime 만큼 작아진다.
            // 즉, 처음에는 위로 올라가지만 서서히 속도가 줄어들다가
            // 나중에는 아래로 서서히 속도가 올라간다.
            velocity -= 9.8f * Time.deltaTime;
            // 점프하여 위로 올라갔다가 아래로 내려가기 때문에 position의 y속성만 변경한다.
            position.y = velocity;

            // 위치를 position에 맞게 이동한다.
            transform.Translate(position * Time.deltaTime);

            yield return null;
        }
    }
}
