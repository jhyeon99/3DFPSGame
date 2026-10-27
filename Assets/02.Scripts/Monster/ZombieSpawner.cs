using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 지정한 위치(들)에 일정 간격으로 좀비를 스폰한다.
public class ZombieSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private Transform[] _spawnPoints;          // 비어 있으면 스포너 자신의 위치에 스폰
    [SerializeField] private float _spawnInterval = 5f;         // 스폰 간격
    [SerializeField] private int _maxAlive = 5;                 // 동시에 존재할 수 있는 최대 좀비 수
    [SerializeField] private float _navMeshSampleRange = 2f;    // NavMesh 위로 보정할 때 탐색하는 반경

    private readonly List<Zombie> _spawned = new List<Zombie>();
    private float _timer = 0f;

    public int AliveCount => _spawned.Count;
    public int MaxAlive => _maxAlive;

    // 살아 있는 좀비 수가 변할 때 구독자(UI 등)에게 알린다.
    public event Action OnAliveCountChanged;

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        _timer += Time.deltaTime;
        if (_timer < _spawnInterval) return;

        if (_spawned.Count >= _maxAlive) return;

        _timer = 0f;
        Spawn();
    }

    private void Spawn()
    {
        Vector3 position = GetSpawnPosition();

        // NavMesh 위가 아니면 NavMeshAgent가 동작하지 않으므로 가장 가까운 NavMesh 위치로 보정한다.
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, _navMeshSampleRange, NavMesh.AllAreas))
        {
            position = hit.position;
        }
        else
        {
            Debug.LogWarning($"{name}: {position} 근처에 NavMesh가 없어 스폰을 건너뜁니다.");
            return;
        }

        Zombie zombie = Instantiate(_zombiePrefab, position, Quaternion.identity);
        zombie.OnDied += HandleZombieDied;
        _spawned.Add(zombie);
        OnAliveCountChanged?.Invoke();
    }

    private void HandleZombieDied(Zombie zombie)
    {
        zombie.OnDied -= HandleZombieDied;
        _spawned.Remove(zombie);
        OnAliveCountChanged?.Invoke();
    }

    private Vector3 GetSpawnPosition()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            return transform.position;
        }

        return _spawnPoints[UnityEngine.Random.Range(0, _spawnPoints.Length)].position;
    }
}
