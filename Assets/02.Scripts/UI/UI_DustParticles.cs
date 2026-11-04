using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 위로 천천히 떠다니는 먼지·재·불씨 입자를 UI 이미지로 표현한다.
/// 입자 수가 고정이라 런타임에 추가 할당이 없다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UI_DustParticles : MonoBehaviour
{
    private const int SpriteSize = 32;
    private const float EmberRatio = 0.3f;

    [SerializeField] private int _count = 60;
    [SerializeField] private Vector2 _sizeRange = new Vector2(4f, 14f);
    [SerializeField] private Vector2 _lifeRange = new Vector2(6f, 12f);
    [SerializeField] private Vector2 _riseSpeedRange = new Vector2(10f, 35f);
    [SerializeField] private float _driftSpeed = 18f;
    [SerializeField] private float _wobbleAmplitude = 22f;
    [SerializeField] private Vector2 _alphaRange = new Vector2(0.25f, 0.7f);
    [SerializeField] private Color _ashColor = new Color(0.8f, 0.8f, 0.78f, 1f);
    [SerializeField] private Color _emberColor = new Color(1f, 0.45f, 0.1f, 1f);

    private RectTransform _area;
    private RectTransform[] _rects;
    private Image[] _images;
    private Vector2[] _basePositions;
    private Vector2[] _velocities;
    private float[] _lives;
    private float[] _ages;
    private float[] _maxAlphas;
    private float[] _phases;
    private bool[] _isEmber;

    private void Awake()
    {
        _area = (RectTransform)transform;
        Sprite sprite = CreateSoftCircleSprite();

        _rects = new RectTransform[_count];
        _images = new Image[_count];
        _basePositions = new Vector2[_count];
        _velocities = new Vector2[_count];
        _lives = new float[_count];
        _ages = new float[_count];
        _maxAlphas = new float[_count];
        _phases = new float[_count];
        _isEmber = new bool[_count];

        for (int i = 0; i < _count; i++)
        {
            var go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            _rects[i] = (RectTransform)go.transform;
            _rects[i].anchorMin = _rects[i].anchorMax = new Vector2(0.5f, 0.5f);

            _images[i] = go.GetComponent<Image>();
            _images[i].sprite = sprite;
            _images[i].raycastTarget = false;

            Respawn(i, true);
        }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < _count; i++)
        {
            _ages[i] += dt;
            if (_ages[i] >= _lives[i])
            {
                Respawn(i, false);
            }

            _basePositions[i] += _velocities[i] * dt;

            // 좌우로 살랑거리는 움직임
            float wobble = Mathf.Sin(_ages[i] * 0.8f + _phases[i]) * _wobbleAmplitude;
            _rects[i].anchoredPosition = _basePositions[i] + new Vector2(wobble, 0f);

            // 수명 동안 서서히 나타났다가 사라진다.
            float lifeRatio = _ages[i] / _lives[i];
            Color color = _images[i].color;
            color.a = Mathf.Sin(lifeRatio * Mathf.PI) * _maxAlphas[i];
            _images[i].color = color;
        }
    }

    private void Respawn(int i, bool scatterAnywhere)
    {
        Rect rect = _area.rect;
        float x = Random.Range(rect.xMin, rect.xMax);
        // 처음에는 화면 전체에 흩뿌리고, 이후에는 아래쪽에서 올라오게 한다.
        float y = scatterAnywhere ? Random.Range(rect.yMin, rect.yMax) : rect.yMin - 10f;

        _isEmber[i] = Random.value < EmberRatio;
        float size = Random.Range(_sizeRange.x, _sizeRange.y) * (_isEmber[i] ? 0.8f : 1f);
        float rise = Random.Range(_riseSpeedRange.x, _riseSpeedRange.y) * (_isEmber[i] ? 1.6f : 1f);

        _rects[i].sizeDelta = new Vector2(size, size);
        _basePositions[i] = new Vector2(x, y);
        _velocities[i] = new Vector2(Random.Range(-_driftSpeed, _driftSpeed), rise);
        _lives[i] = Random.Range(_lifeRange.x, _lifeRange.y);
        _ages[i] = scatterAnywhere ? Random.Range(0f, _lives[i]) : 0f;
        _maxAlphas[i] = Random.Range(_alphaRange.x, _alphaRange.y) * (_isEmber[i] ? 1.4f : 1f);
        _phases[i] = Random.Range(0f, Mathf.PI * 2f);

        Color baseColor = _isEmber[i] ? _emberColor : _ashColor;
        baseColor.a = 0f;
        _images[i].color = baseColor;
    }

    private static Sprite CreateSoftCircleSprite()
    {
        var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (SpriteSize - 1) * 0.5f;
        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                float alpha = Mathf.Clamp01(1f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, SpriteSize, SpriteSize), new Vector2(0.5f, 0.5f));
    }
}
