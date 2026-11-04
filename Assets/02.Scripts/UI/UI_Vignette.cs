using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 가장자리를 어둡게 만드는 비네트 이미지를 런타임에 생성한다. (별도 텍스처 에셋 불필요)
/// </summary>
[RequireComponent(typeof(Image))]
public class UI_Vignette : MonoBehaviour
{
    private const int TextureSize = 256;

    [SerializeField, Range(0f, 1f)] private float _innerRadius = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.9f;

    private void Awake()
    {
        var image = GetComponent<Image>();
        image.sprite = CreateSprite();
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private Sprite CreateSprite()
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (TextureSize - 1) * 0.5f;
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                // 중심(0) ~ 모서리(1 이상)까지의 정규화된 거리
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / 1.2f;
                float alpha = Mathf.SmoothStep(0f, _maxAlpha, Mathf.InverseLerp(_innerRadius, 1f, distance));
                texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f));
    }
}
