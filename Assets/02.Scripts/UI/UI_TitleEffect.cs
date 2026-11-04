using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 타이틀 로고 연출: 크게 시작해 쿵 하고 안착 → 붉은색으로 고정되며, 가끔 글리치처럼 흔들린다.
/// </summary>
public class UI_TitleEffect : MonoBehaviour
{
    private const float GlitchDuration = 0.14f;
    private const float GlitchShakeRange = 14f;

    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private float _introDuration = 0.7f;
    [SerializeField] private float _introStartScale = 2.5f;
    [SerializeField] private Color _titleColor = new Color(0.85f, 0.1f, 0.07f, 1f);
    [SerializeField] private Color _outlineColor = new Color(0.05f, 0f, 0f, 1f);
    [SerializeField] private float _outlineWidth = 0.22f;
    [SerializeField] private Vector2 _glitchInterval = new Vector2(3f, 6f);

    private RectTransform _rect;
    private Vector2 _basePosition;
    private bool _introDone;

    private void Awake()
    {
        _rect = _title.rectTransform;
        _basePosition = _rect.anchoredPosition;

        // 붉은색으로 고정하고, 어두운 외곽선으로 배경 위에서 글자가 묵직하게 보이게 한다.
        _title.color = _titleColor;
        _title.outlineColor = _outlineColor;
        _title.outlineWidth = _outlineWidth;
    }

    private void OnEnable()
    {
        StartCoroutine(Intro_Coroutine());
        StartCoroutine(Glitch_Coroutine());
    }


    private IEnumerator Intro_Coroutine()
    {
        _introDone = false;
        float elapsed = 0f;
        while (elapsed < _introDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _introDuration);
            // easeOutCubic
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float scale = Mathf.Lerp(_introStartScale, 1f, eased);
            _rect.localScale = new Vector3(scale, scale, 1f);
            _title.alpha = Mathf.Clamp01(t * 2f);
            yield return null;
        }

        _rect.localScale = Vector3.one;
        _title.alpha = 1f;
        _introDone = true;
    }

    private IEnumerator Glitch_Coroutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(_glitchInterval.x, _glitchInterval.y));
            if (!_introDone) continue;

            float elapsed = 0f;
            while (elapsed < GlitchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _rect.anchoredPosition = _basePosition + new Vector2(Random.Range(-GlitchShakeRange, GlitchShakeRange), Random.Range(-3f, 3f));
                yield return null;
            }

            _rect.anchoredPosition = _basePosition;
        }
    }
}
