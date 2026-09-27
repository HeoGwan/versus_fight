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
    [SerializeField] private ParticleSystem hitParticle;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _rigid = GetComponent<Rigidbody>();
        waitInvincible = new WaitForSecondsRealtime(invincibleDuration);
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

    // 무적 코루틴
    IEnumerator Invincible()
    {
        isInvincible = true;

        yield return waitInvincible;

        isInvincible = false;
    }
}
