using TMPro;
using UnityEngine;

// 남은 좀비 수 / 최대 좀비 수를 화면 위쪽에 표시한다.
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private ZombieSpawner _spawner;

    private void Start()
    {
        Refresh();
        _spawner.OnAliveCountChanged += Refresh;
    }

    private void OnDestroy()
    {
        if (_spawner != null)
        {
            _spawner.OnAliveCountChanged -= Refresh;
        }
    }

    // 좀비 수가 바뀔 때만 텍스트를 갱신한다.
    private void Refresh()
    {
        _countText.text = $"좀비 {_spawner.AliveCount} / {_spawner.MaxAlive}";
    }
}
