using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    enum State
    {
        Idle,
        Move,
    };

    // 변수
    Rigidbody _rigid;
    State _state = State.Idle;
    Vector3 _direction = Vector3.zero;
    [SerializeField] float speed = 5f;

    [SerializeField] Camera playerCamera;
    [SerializeField] float lookUpDownLimit = 60f; // 위아래 회전 각도

    void Start()
    {
        _rigid = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (_state == State.Move)
        {
            // // 부드럽게 회전하도록 보간
            // transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_direction), Time.deltaTime * 10f);
            transform.Translate(_direction * speed * Time.deltaTime);
        }
    }

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
        if (value.isPressed)
        {
            
            _rigid.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }
    }

    void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            // 공격 처리
            Debug.Log("Attack!");
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
}
