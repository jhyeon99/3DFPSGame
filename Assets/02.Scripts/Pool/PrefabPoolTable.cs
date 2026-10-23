using System.Collections.Generic;
using Redcode.Pools;
using UnityEngine;

public enum PoolReturnResult
{
    Returned,
    AlreadyReturned,
    NotFromPool,
}

/// <summary>
/// 프리팹마다 Redcode.Pools의 Pool을 하나씩 만들어 관리하는 표.
/// - 프리팹 → 풀: 같은 프리팹을 요청하면 같은 풀에서 꺼낸다.
/// - 복제본 → 풀: 반납할 때 검색 없이 원래 풀을 찾는다.
/// - 꺼내 간 복제본 목록: 같은 오브젝트를 두 번 반납하는 실수를 막는다. (Pool.Take는 이때 예외를 던짐)
/// </summary>
public class PrefabPoolTable<T> where T : Component
{
    private readonly Transform _root;
    private readonly Dictionary<T, IPool<T>> _poolByPrefab = new Dictionary<T, IPool<T>>();
    private readonly Dictionary<T, IPool<T>> _poolByClone = new Dictionary<T, IPool<T>>();
    private readonly HashSet<T> _activeClones = new HashSet<T>();

    public PrefabPoolTable(Transform root)
    {
        _root = root;
    }

    public bool IsRegistered(T prefab)
    {
        return prefab != null && _poolByPrefab.ContainsKey(prefab);
    }

    /// <param name="maxCount">풀이 만들 수 있는 최대 개수. 0이면 무제한</param>
    /// <param name="preload">true면 maxCount만큼 미리 만들어 둔다 (maxCount가 0이면 무시)</param>
    public void Register(T prefab, int maxCount, bool preload)
    {
        if (prefab == null || _poolByPrefab.ContainsKey(prefab))
        {
            return;
        }

        // 프리팹별 빈 오브젝트 아래에 복제본을 모아 Hierarchy를 정리한다
        Transform container = new GameObject($"{prefab.name} Pool").transform;
        container.SetParent(_root, false);

        IPool<T> pool = Pool.Create(prefab, maxCount, container);
        if (preload && maxCount > 0)
        {
            pool.NonLazy();
        }

        _poolByPrefab.Add(prefab, pool);
    }

    /// <summary>
    /// 풀에서 복제본을 꺼내 위치와 회전을 맞춘다. 풀이 가득 찼으면 null.
    /// 등록되지 않은 프리팹이면 null (먼저 Register 호출 필요).
    /// </summary>
    public T Get(T prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null || !_poolByPrefab.TryGetValue(prefab, out IPool<T> pool))
        {
            return null;
        }

        T clone = pool.Get();
        if (clone == null)
        {
            return null;
        }

        _poolByClone[clone] = pool;
        _activeClones.Add(clone);
        clone.transform.SetPositionAndRotation(position, rotation);
        return clone;
    }

    public PoolReturnResult Return(T clone)
    {
        if (clone == null || !_poolByClone.TryGetValue(clone, out IPool<T> pool))
        {
            return PoolReturnResult.NotFromPool;
        }

        if (!_activeClones.Remove(clone))
        {
            return PoolReturnResult.AlreadyReturned;
        }

        pool.Take(clone);
        return PoolReturnResult.Returned;
    }
}
