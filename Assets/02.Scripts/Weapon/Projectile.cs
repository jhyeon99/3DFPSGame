using Redcode.Pools;
using UnityEngine;

/// <summary>
/// 총알·폭탄 같은 투사체의 공통 부모. ProjectilePool에서 꺼내 쓰고 다 쓰면 ReturnToPool로 반납한다.
/// - 풀에서 꺼낼 때 Rigidbody 속도를 초기화한다. (꺼져 있어도 이전 속도가 남아 있기 때문)
/// - 같은 프레임에 충돌이 여러 번 들어와도 한 번만 반납되도록 막는다.
/// </summary>
public abstract class Projectile : MonoBehaviour, IPoolObject
{
    private Rigidbody _rigidbody;
    private bool _isReturned;

    public Rigidbody Rigidbody => _rigidbody;

    /// <summary>이미 풀에 반납했는지 (반납 뒤 들어온 충돌 무시용)</summary>
    protected bool IsReturned => _isReturned;

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>풀이 처음 복제본을 만들었을 때 (Pools가 호출)</summary>
    public void OnCreatedInPool()
    {
    }

    /// <summary>풀에서 꺼낼 때마다 (Pools가 호출). 재사용 전 상태 초기화</summary>
    public void OnGettingFromPool()
    {
        _isReturned = false;

        if (_rigidbody != null && !_rigidbody.isKinematic)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        OnSpawned();
    }

    /// <summary>자식 클래스에서 꺼낼 때 추가로 초기화할 내용</summary>
    protected virtual void OnSpawned()
    {
    }

    protected void ReturnToPool()
    {
        if (_isReturned)
        {
            return;
        }

        _isReturned = true;
        ProjectilePool.Instance.Return(this);
    }
}
