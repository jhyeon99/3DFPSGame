using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 파티클 이펙트를 담는 풀.
/// Play로 꺼내 재생하고, 파티클이 모두 사라지면 자동으로 풀에 반납한다.
/// 씬에 배치하지 않아도 처음 Instance에 접근할 때 자동으로 만들어진다.
/// 반복(Looping) 이펙트는 끝나지 않으므로 직접 Stop해야 반납된다.
/// </summary>
public class EffectPool : PrefabPoolBehaviour<ParticleSystem>
{
    private static EffectPool _instance;

    public static EffectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EffectPool>();
            }

            if (_instance == null)
            {
                _instance = new GameObject(nameof(EffectPool)).AddComponent<EffectPool>();
            }

            return _instance;
        }
    }

    // 재생 중인 이펙트. 매 프레임 끝났는지 확인해 반납 (코루틴을 쓰지 않아 재생마다 생기는 할당이 없음)
    private readonly List<ParticleSystem> _playingEffects = new List<ParticleSystem>();

    // Stop Action을 정리해 둔 복제본
    private readonly HashSet<ParticleSystem> _preparedEffects = new HashSet<ParticleSystem>();

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

    private void Update()
    {
        for (int i = _playingEffects.Count - 1; i >= 0; i--)
        {
            ParticleSystem effect = _playingEffects[i];

            if (effect != null && effect.IsAlive(true))
            {
                continue;
            }

            _playingEffects.RemoveAt(i);

            if (effect != null)
            {
                Return(effect);
            }
        }
    }

    /// <summary>
    /// 이펙트를 꺼내 지정 위치·회전에서 재생한다. 풀이 가득 찼으면 재생하지 않고 null.
    /// </summary>
    public ParticleSystem Play(ParticleSystem prefab, Vector3 position, Quaternion rotation)
    {
        ParticleSystem effect = Spawn(prefab, position, rotation);
        if (effect == null)
        {
            return null;
        }

        if (_preparedEffects.Add(effect))
        {
            DisableSelfDestroy(effect);
        }

        // 이전 재생에서 남은 입자를 지우고 처음부터 재생
        effect.Clear(true);
        effect.Play(true);
        _playingEffects.Add(effect);

        return effect;
    }

    /// <summary>
    /// 이펙트 프리팹의 Stop Action이 Destroy / Disable이면 재생이 끝날 때 스스로 사라져 풀이 깨진다.
    /// 복제본에 한해 None으로 바꿔 두고, 반납은 풀이 맡는다.
    /// </summary>
    private static void DisableSelfDestroy(ParticleSystem effect)
    {
        foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = system.main;
            if (main.stopAction == ParticleSystemStopAction.Destroy
                || main.stopAction == ParticleSystemStopAction.Disable)
            {
                main.stopAction = ParticleSystemStopAction.None;
            }
        }
    }
}
