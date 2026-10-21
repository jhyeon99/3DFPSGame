using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 풀로 돌아가는 폭탄
public class Bomb : Projectile
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    protected override void Awake()
    {
        base.Awake();

        // 폭발 이펙트 풀을 미리 만들어 첫 폭발 때 생성 비용이 몰리지 않게 함
        EffectPool.Instance.Register(_explosionEffectPrefab);
    }

    // 다른 콜라이더와 부딪힌 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        // 같은 프레임에 충돌이 여러 번 들어와도 한 번만 폭발
        if (IsReturned) return;

        EffectPool.Instance.Play(_explosionEffectPrefab, transform.position, Quaternion.identity);
        ReturnToPool();
    }
}
