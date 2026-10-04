using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Entity : MonoBehaviour
{
    Animator _animator;
    Rigidbody _rigid;

    [SerializeField] private float knockbackForce = 1.5f; // 넉백 힘
    
    [Space(10)]
    [SerializeField] private float invincibleDuration = 1f; // 무적 시간
    bool isInvincible = false;
    WaitForSecondsRealtime waitInvincible;

    [Space(10)]
    [SerializeField] float walkSpeed = 5f;

    [Space(10)]
    [SerializeField] private ParticleSystem hitParticle;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _rigid = GetComponent<Rigidbody>();

        _animator.enabled = true;
        waitInvincible = new WaitForSecondsRealtime(invincibleDuration);
    }

    void Update()
    {
        transform.position = Vector3.Slerp(
            transform.position,
            targetPosition,
            walkSpeed * Time.deltaTime
        );
        
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            walkSpeed * Time.deltaTime
        );
    }

    public void Hit(Vector3 attackAreaPosition)
    {
        // 무적 상태 확인
        if (isInvincible) return;

        // 피격 방향 벡터를 얻음
        Vector3 hitDirection = transform.position - attackAreaPosition;

        // 피격 받은 쪽으로 넉백
        _rigid.AddForce(hitDirection * knockbackForce);

        // 피격 애니메이션 및 파티클 재생
        _animator.SetTrigger("Hit");
        hitParticle.Play();

        // 무적 시간
        StartCoroutine(Invincible());
    }

    public void Move(Vector3 position, Quaternion rotation)
    {
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
