using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    enum State
    {
        Idle,
        Move,
        // Attack,
        // Jump,
    };

    // 변수
    Rigidbody _rigid;

    State _state = State.Idle;
    // State _prevState = State.Idle;
    Vector3 _direction = Vector3.zero;
    float velocity = 0.0f;

    [Header("Variables")]
    [SerializeField] float speed = 5f;

    [Space(10)]
    bool isJumping = false;
    [SerializeField] float jumpForce = 5f;

    [Space(10)]
    [SerializeField] float dashSpeed = 10f;
    [SerializeField] float dashDuration = 0.25f;

    [Space(10)]
    bool isAttacking = false;
    [SerializeField] private float attackCoolTime = 0.5f;

    [Header("Components")]
    [Space(10)]
    [SerializeField] Camera playerCamera;
    [SerializeField] float lookUpDownLimit = 60f; // 위아래 회전 각도

    [Space(10)]
    [SerializeField] private Animator animator;

    void Start()
    {
        _rigid = GetComponent<Rigidbody>();

        // 1. 커서를 화면 중앙에 고정합니다.
        Cursor.lockState = CursorLockMode.Locked;

        // 2. 커서를 보이지 않게 숨깁니다.
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (_state == State.Move)
        {
            // // 부드럽게 회전하도록 보간
            // transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_direction), Time.deltaTime * 10f);
            transform.Translate(_direction * speed * Time.deltaTime);
        }
        // else if (_state == State.Jump)
        // {
        //     float gravity = _rigid.linearVelocity.y < 0 ? 9.8f : -9.8f;
    
        //     velocity -= gravity * Time.deltaTime;

        //     Vector3 position = transform.position;
        //     position.y += velocity;

        //     _direction.y = position.y;

        //     transform.Translate(_direction * speed * Time.deltaTime);

        //     // transform.SetPositionAndRotation(position, transform.rotation);

        //     // // y 값이 0이하(떨어질 떄) y에 9.8(중력 가속도)를 곱하여 더함
        //     // if (_rigid.linearVelocity.y < 0)
        //     // {
        //     //     Vector3 fallVelocity = new Vector3(0, _rigid.linearVelocity.y * 9.8f * Time.deltaTime, 0);
                
        //     //     _rigid.linearVelocity += fallVelocity;
        //     // }
        //     // else
        //     // {
                
        //     // }
        // }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            isJumping = false;
        }
    }


    // Player Input 메서드
    void OnMove(InputValue value)
    {
        // 이동 처리
        Vector2 move = value.Get<Vector2>();

        if (move == null) return;

        _state = move == Vector2.zero ? State.Idle : State.Move;

        _direction = new Vector3(move.x, 0, move.y);
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed && !isJumping)
        {
            // _rigid.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
            velocity = jumpForce;
            StartCoroutine(JumpCoroutine());
            // _state = State.Dash;
        }
    }

    void OnAttack(InputValue value)
    {
        if (value.isPressed && !isAttacking)
        {
            // 공격 처리
            // 대쉬 처리
            // _rigid.AddForce(transform.forward * dashSpeed, ForceMode.Impulse);
            // _prevState = _state;
            // _state = State.Attack;
            isAttacking = true;
            animator.SetTrigger("Attack");
            StartCoroutine(AttackCoroutine());
        }
    }

    void OnLook(InputValue value)
    {
        Vector2 look = value.Get<Vector2>();

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
        else
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
    public void EndAttack()
    {
        float time = 0.0f;
        
        while (time < attackCoolTime)
        {
            time += Time.deltaTime;
        }

        isAttacking = false;
    }


    // Coroutine 메서드
    IEnumerator AttackCoroutine()
    {
        float elapsedTime = 0f;
        float moveWay = _direction.z == 0 ? 1 : _direction.z;

        while (elapsedTime < dashDuration)
        {
            _rigid.linearVelocity = dashSpeed * moveWay * transform.forward;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _rigid.linearVelocity = Vector3.zero;

        // _state = _prevState;
    }

    IEnumerator JumpCoroutine()
    {
        // float gravity = _rigid.linearVelocity.y < 0 ? 9.8f : -9.8f;
    
        Vector3 position = Vector3.zero;

        while (isJumping)
        {
            velocity -= 9.8f * Time.deltaTime;
            Debug.Log($"velocity : {velocity}");

            position.y = velocity;

            transform.Translate(position * Time.deltaTime);
            // transform.SetPositionAndRotation(position, transform.rotation);

            yield return null;
        }

        // transform.SetPositionAndRotation(position, transform.rotation);

        // // y 값이 0이하(떨어질 떄) y에 9.8(중력 가속도)를 곱하여 더함
        // if (_rigid.linearVelocity.y < 0)
        // {
        //     Vector3 fallVelocity = new Vector3(0, _rigid.linearVelocity.y * 9.8f * Time.deltaTime, 0);
            
        //     _rigid.linearVelocity += fallVelocity;
        // }
        // else
        // {
            
        // }
    }
}
