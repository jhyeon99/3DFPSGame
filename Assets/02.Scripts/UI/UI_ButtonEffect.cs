using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼 hover/클릭 피드백: 크기와 배경색이 부드럽게 변한다.
/// </summary>
[RequireComponent(typeof(Image))]
public class UI_ButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float _hoverScale = 1.05f;
    [SerializeField] private float _pressScale = 0.96f;
    [SerializeField] private float _speed = 14f;
    [SerializeField] private Color _normalColor = new Color(0.05f, 0.05f, 0.05f, 0.7f);
    [SerializeField] private Color _hoverColor = new Color(0.8f, 0.18f, 0.1f, 0.95f);

    private Image _image;
    private bool _isHover;
    private bool _isPressed;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.color = _normalColor;
    }

    private void OnDisable()
    {
        _isHover = false;
        _isPressed = false;
        transform.localScale = Vector3.one;
        _image.color = _normalColor;
    }

    private void Update()
    {
        float targetScale = _isPressed ? _pressScale : (_isHover ? _hoverScale : 1f);
        Color targetColor = _isHover ? _hoverColor : _normalColor;
        float t = Time.unscaledDeltaTime * _speed;

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, t);
        _image.color = Color.Lerp(_image.color, targetColor, t);
    }

    public void OnPointerEnter(PointerEventData eventData) => _isHover = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHover = false;
        _isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData) => _isPressed = true;

    public void OnPointerUp(PointerEventData eventData) => _isPressed = false;
}
