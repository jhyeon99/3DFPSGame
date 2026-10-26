using System.Collections;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.2f;
    [SerializeField] private float _fadeTime = 0.3f;
    
    private Image _image;
    
    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.enabled = false;
    }

    private void Start()
    {
        PlayerHealth playerHealth = GameObject.FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDamaged += OnPlayerDamaged;
        }
    }

    private void OnPlayerDamaged()
    {
        Fade(0.7f);
    }

    public void Flash(float alpha)
    {
        SetAlpha(alpha);

        StartCoroutine(Flash_Coroutine());
    }

    private IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;
        yield return new WaitForSeconds(_flashTime);
        _image.enabled = false;
    }

    public void Fade(float alpha)
    {
        SetAlpha(alpha);

        StartCoroutine(Fade_Coroutine());
    }

    private IEnumerator Fade_Coroutine()
    {
        _image.enabled = true;
        
        float elapsedTime = 0f;
        float alpha = _image.color.a;
        while (elapsedTime < _fadeTime)
        {
            float a = Mathf.Lerp(alpha, 0f, elapsedTime / _fadeTime);
            
            SetAlpha(a);

            elapsedTime += Time.deltaTime;
            yield return null; // 한 프레임씩 쉰다.
        }
        
        _image.enabled = false;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }
}