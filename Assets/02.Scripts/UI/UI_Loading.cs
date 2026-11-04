using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로딩 씬: 다음 씬을 비동기로 불러오며 슬라이더와 퍼센트 텍스트를 점점 채운다.
/// 로딩이 너무 빨라도 로딩 화면이 보이도록 최소 표시 시간을 둔다.
/// </summary>
public class UI_Loading : MonoBehaviour
{
    // AsyncOperation.progress는 allowSceneActivation이 false이면 0.9에서 멈춘다.
    private const float AsyncProgressMax = 0.9f;

    [SerializeField] private string _nextSceneName = "GameScene";
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private float _minDuration = 2f;

    private void Start()
    {
        SetProgress(0f);
        StartCoroutine(Load_Coroutine());
    }

    private IEnumerator Load_Coroutine()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(_nextSceneName);
        operation.allowSceneActivation = false;

        float displayed = 0f;
        while (displayed < 1f)
        {
            float target = Mathf.Clamp01(operation.progress / AsyncProgressMax);

            // 실제 진행도를 따라가되, 최소 표시 시간 동안은 일정 속도로만 차오른다.
            displayed = Mathf.MoveTowards(displayed, target, Time.unscaledDeltaTime / _minDuration);
            SetProgress(displayed);
            yield return null;
        }

        operation.allowSceneActivation = true;
    }

    private void SetProgress(float value)
    {
        _progressBar.value = value;
        _progressText.text = $"불러오는 중 {Mathf.RoundToInt(value * 100f)}%";
    }
}
