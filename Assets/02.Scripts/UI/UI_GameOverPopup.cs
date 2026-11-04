using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 오버 시 잠시 뒤 나타나는 팝업. 다시 하기 / 로그인 화면으로 돌아가기를 제공한다.
/// 이 스크립트는 항상 활성 상태인 오브젝트에 두고, 실제 팝업 화면은 _panel로 켜고 끈다.
/// </summary>
public class UI_GameOverPopup : MonoBehaviour
{
    private const string LoginSceneName = "LoginScene";

    [SerializeField] private GameObject _panel;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private float _showDelay = 1.0f;
    [SerializeField] private float _fadeDuration = 0.5f;

    private void Start()
    {
        _restartButton.onClick.AddListener(Restart);
        _quitButton.onClick.AddListener(GoToLogin);

        _panel.SetActive(false);
        GameManager.Instance.OnGameOver += Show;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= Show;
        }
    }

    private void Show()
    {
        StartCoroutine(Show_Coroutine());
    }

    private IEnumerator Show_Coroutine()
    {
        yield return new WaitForSeconds(_showDelay);

        _panel.SetActive(true);
        _canvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToLogin()
    {
        SceneManager.LoadScene(LoginSceneName);
    }
}
