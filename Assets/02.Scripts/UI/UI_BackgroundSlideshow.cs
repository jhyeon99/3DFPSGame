using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 여러 배경 이미지를 일정 시간마다 크로스페이드로 번갈아 보여준다.
/// _back 이미지가 _front 위에 그려지며, 페이드가 끝나면 _front에 새 이미지를 넣고 _back을 투명하게 되돌린다.
/// </summary>
public class UI_BackgroundSlideshow : MonoBehaviour
{
    [SerializeField] private Image _front;
    [SerializeField] private Image _back;
    [SerializeField] private Sprite[] _sprites;
    [SerializeField] private float _holdTime = 6f;
    [SerializeField] private float _fadeTime = 1.5f;
    [SerializeField] private bool _randomStart = true;

    private int _index;

    private void Start()
    {
        if (_sprites == null || _sprites.Length == 0) return;

        _index = _randomStart ? Random.Range(0, _sprites.Length) : 0;
        _front.sprite = _sprites[_index];
        SetAlpha(_back, 0f);

        if (_sprites.Length > 1)
        {
            StartCoroutine(Slideshow_Coroutine());
        }
    }

    private IEnumerator Slideshow_Coroutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(_holdTime);

            _index = (_index + 1) % _sprites.Length;
            _back.sprite = _sprites[_index];

            float elapsed = 0f;
            while (elapsed < _fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(_back, Mathf.SmoothStep(0f, 1f, elapsed / _fadeTime));
                yield return null;
            }

            _front.sprite = _sprites[_index];
            SetAlpha(_back, 0f);
        }
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
