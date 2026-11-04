using UnityEngine;

/// <summary>
/// 배경 이미지를 천천히 흔들고 숨 쉬게 하며, 마우스 위치에 따라 살짝 반대로 움직인다(시차 효과).
/// 화면 밖으로 빈 공간이 보이지 않도록 배경을 기본 배율보다 크게 유지한다.
/// </summary>
public class UI_BackgroundMotion : MonoBehaviour
{
    private const float ParallaxFollowSpeed = 3f;
    private const float SwayVerticalRatio = 0.5f;
    private const float SwayVerticalSpeedRatio = 0.7f;

    [SerializeField] private RectTransform _target;
    [SerializeField] private float _baseScale = 1.08f;
    [SerializeField] private float _breatheScale = 0.01f;
    [SerializeField] private float _swayAmplitude = 18f;
    [SerializeField] private float _swaySpeed = 0.25f;
    [SerializeField] private float _parallax = 28f;

    private Vector2 _parallaxCurrent;

    private void Update()
    {
        float time = Time.unscaledTime * _swaySpeed;
        var sway = new Vector2(
            Mathf.Sin(time) * _swayAmplitude,
            Mathf.Cos(time * SwayVerticalSpeedRatio) * _swayAmplitude * SwayVerticalRatio);

        // 마우스 위치를 -1~1로 정규화
        var mouse = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        Vector2 normalized = (mouse - new Vector2(0.5f, 0.5f)) * 2f;
        normalized = Vector2.ClampMagnitude(normalized, 1f);

        Vector2 parallaxTarget = -normalized * _parallax;
        _parallaxCurrent = Vector2.Lerp(_parallaxCurrent, parallaxTarget, Time.unscaledDeltaTime * ParallaxFollowSpeed);

        _target.anchoredPosition = sway + _parallaxCurrent;
        float scale = _baseScale + Mathf.Sin(time * 0.8f) * _breatheScale;
        _target.localScale = new Vector3(scale, scale, 1f);
    }
}
