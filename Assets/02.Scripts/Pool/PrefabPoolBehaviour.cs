using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>프리팹 하나의 풀 설정 (인스펙터용)</summary>
[Serializable]
public class PrefabPoolSetting<T> where T : Component
{
    public T Prefab;

    [Tooltip("이 프리팹의 복제본 최대 개수. 0이면 무제한")]
    [Min(0)]
    public int MaxCount;

    [Tooltip("시작할 때 MaxCount만큼 미리 만들어 둠 (MaxCount가 0이면 무시)")]
    public bool Preload;
}

/// <summary>
/// ProjectilePool, EffectPool의 공통 부모.
/// - 인스펙터에 적은 프리팹은 Awake에서 풀을 만든다 (미리 만들기 옵션 포함).
/// - 인스펙터에 없는 프리팹은 처음 꺼낼 때 기본 설정으로 풀을 만든다.
/// 복제본은 이 오브젝트의 자식으로 생성되므로, 이 오브젝트는 원점·회전 없음·스케일 1로 두는 것이 안전하다.
/// </summary>
public abstract class PrefabPoolBehaviour<T> : MonoBehaviour where T : Component
{
    [SerializeField] private List<PrefabPoolSetting<T>> _settings = new List<PrefabPoolSetting<T>>();

    [Tooltip("인스펙터에 없는 프리팹을 처음 쓸 때 만드는 풀의 최대 개수. 0이면 무제한")]
    [SerializeField, Min(0)] private int _defaultMaxCount = 0;

    private PrefabPoolTable<T> _table;

    private PrefabPoolTable<T> Table => _table ??= new PrefabPoolTable<T>(transform);

    protected virtual void Awake()
    {
        foreach (PrefabPoolSetting<T> setting in _settings)
        {
            Register(setting.Prefab);
        }
    }

    /// <summary>
    /// 프리팹의 풀을 미리 만들어 둔다. 이미 있으면 아무것도 하지 않음.
    /// 쓰기 전에 호출해 두면 첫 사용 때 생성 비용이 몰리지 않는다.
    /// </summary>
    public void Register(T prefab)
    {
        if (prefab == null || Table.IsRegistered(prefab))
        {
            return;
        }

        PrefabPoolSetting<T> setting = _settings.Find(s => s.Prefab == prefab);
        if (setting != null)
        {
            Table.Register(prefab, setting.MaxCount, setting.Preload);
        }
        else
        {
            Table.Register(prefab, _defaultMaxCount, false);
        }
    }

    protected T Spawn(T prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            GameLogger.LogError($"[{name}] 프리팹이 비어 있습니다.");
            return null;
        }

        Register(prefab);

        T clone = Table.Get(prefab, position, rotation);
        if (clone == null)
        {
            GameLogger.Info($"[{name}] {prefab.name} 풀이 가득 차서 꺼낼 수 없습니다. MaxCount를 늘려 주세요.");
        }

        return clone;
    }

    /// <summary>다 쓴 복제본을 풀에 돌려준다.</summary>
    public void Return(T clone)
    {
        switch (Table.Return(clone))
        {
            case PoolReturnResult.AlreadyReturned:
                GameLogger.Info($"[{name}] {clone.name}은(는) 이미 풀에 반납되었습니다.");
                break;
            case PoolReturnResult.NotFromPool:
                // 풀에서 꺼내지 않은 오브젝트(씬에 직접 배치 등)는 기존처럼 파괴
                if (clone != null)
                {
                    Destroy(clone.gameObject);
                }
                break;
        }
    }
}
