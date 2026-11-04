using System.Collections;
using UnityEngine;

/// <summary>
/// 패널이 켜질 때 자식 UI들이 위에서 아래로 순서대로 페이드 인하며 내려온다.
/// </summary>
public class UI_PanelIntro : MonoBehaviour
{
    [SerializeField] private float _stagger = 0.08f;
    [SerializeField] private float _duration = 0.35f;
    [SerializeField] private float _slideDistance = 40f;

    private void OnEnable()
    {
        StartCoroutine(Intro_Coroutine());
    }

    private IEnumerator Intro_Coroutine()
    {
        int count = transform.childCount;
        var groups = new CanvasGroup[count];
        var rects = new RectTransform[count];
        var basePositions = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            rects[i] = (RectTransform)transform.GetChild(i);
            groups[i] = rects[i].GetComponent<CanvasGroup>();
            if (groups[i] == null)
            {
                groups[i] = rects[i].gameObject.AddComponent<CanvasGroup>();
            }

            basePositions[i] = rects[i].anchoredPosition;
            groups[i].alpha = 0f;
        }

        float totalTime = _duration + _stagger * count;
        float elapsed = 0f;
        while (elapsed < totalTime)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < count; i++)
            {
                float t = Mathf.Clamp01((elapsed - _stagger * i) / _duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                groups[i].alpha = eased;
                rects[i].anchoredPosition = basePositions[i] + new Vector2(0f, _slideDistance * (1f - eased));
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            groups[i].alpha = 1f;
            rects[i].anchoredPosition = basePositions[i];
        }
    }
}
