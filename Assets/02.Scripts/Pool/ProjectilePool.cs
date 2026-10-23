using UnityEngine;

/// <summary>
/// 총알·폭탄 같은 투사체(Projectile)를 담는 풀.
/// 씬에 배치하지 않아도 처음 Instance에 접근할 때 자동으로 만들어진다.
/// 최대 개수나 미리 만들기를 조정하려면 씬에 빈 오브젝트를 만들어 이 컴포넌트를 붙이고 인스펙터에서 설정한다.
/// </summary>
public class ProjectilePool : PrefabPoolBehaviour<Projectile>
{
    private static ProjectilePool _instance;

    public static ProjectilePool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ProjectilePool>();
            }

            if (_instance == null)
            {
                _instance = new GameObject(nameof(ProjectilePool)).AddComponent<ProjectilePool>();
            }

            return _instance;
        }
    }

    protected override void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        base.Awake();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 투사체를 꺼내 지정 위치·회전에 둔다. 풀이 가득 찼으면 null.
    /// 꺼낼 때 Rigidbody 속도가 초기화되므로 꺼낸 뒤에 힘을 가하면 된다.
    /// </summary>
    public TProjectile Get<TProjectile>(TProjectile prefab, Vector3 position, Quaternion rotation)
        where TProjectile : Projectile
    {
        return Spawn(prefab, position, rotation) as TProjectile;
    }
}
