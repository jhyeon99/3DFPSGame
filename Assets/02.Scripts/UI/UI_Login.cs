using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로그인/회원가입 화면. 계정은 학습용으로 PlayerPrefs에 저장한다.
/// (비밀번호는 평문이 아닌 해시로 저장하지만, 실서비스용 보안 수준은 아니다.)
/// </summary>
public class UI_Login : MonoBehaviour
{
    private const string AccountKeyPrefix = "Account_";
    private const string LastLoginIdKey = "LastLoginId";
    private const int MinIdLength = 3;
    private const int MinPasswordLength = 4;

    [Header("패널")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _registerPanel;

    [Header("로그인")]
    [SerializeField] private TMP_InputField _loginIdInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;
    [SerializeField] private TextMeshProUGUI _loginMessageText;

    [Header("회원가입")]
    [SerializeField] private TMP_InputField _registerIdInput;
    [SerializeField] private TMP_InputField _registerPasswordInput;
    [SerializeField] private TMP_InputField _registerConfirmInput;
    [SerializeField] private TextMeshProUGUI _registerMessageText;

    [Header("이동할 씬")]
    [SerializeField] private string _gameSceneName = "LoadingScene";  // 로그인 후 로딩 씬을 거쳐 게임으로 넘어간다.

    [Header("연출")]
    [SerializeField] private Image _fadeImage;
    [SerializeField] private float _fadeDuration = 0.9f;

    private bool _isLoading;

    private void Start()
    {
        // 게임 종료 후 돌아왔을 때도 커서가 보이도록 한다.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowLogin();
        _loginIdInput.text = PlayerPrefs.GetString(LastLoginIdKey, string.Empty);
    }

    public void ShowLogin()
    {
        _loginPanel.SetActive(true);
        _registerPanel.SetActive(false);
        _loginMessageText.text = string.Empty;
        _loginPasswordInput.text = string.Empty;
    }

    public void ShowRegister()
    {
        _loginPanel.SetActive(false);
        _registerPanel.SetActive(true);
        _registerIdInput.text = string.Empty;
        _registerPasswordInput.text = string.Empty;
        _registerConfirmInput.text = string.Empty;
        _registerMessageText.text = "아이디와 비밀번호를 입력해 주세요.";
    }

    public void OnLoginClicked()
    {
        string id = _loginIdInput.text.Trim();
        string password = _loginPasswordInput.text;

        if (id.Length == 0 || password.Length == 0)
        {
            ShowLoginError("아이디와 비밀번호를 입력해 주세요.");
            return;
        }

        string key = AccountKeyPrefix + id;
        if (!PlayerPrefs.HasKey(key) || PlayerPrefs.GetString(key) != HashPassword(password))
        {
            ShowLoginError("아이디 또는 비밀번호가 올바르지 않습니다.");
            return;
        }

        PlayerPrefs.SetString(LastLoginIdKey, id);
        PlayerPrefs.Save();
        StartCoroutine(LoadGame_Coroutine());
    }

    private IEnumerator LoadGame_Coroutine()
    {
        if (_isLoading) yield break;
        _isLoading = true;

        // 화면을 어둡게 덮으면서 입력도 막는다.
        _fadeImage.gameObject.SetActive(true);
        _fadeImage.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / _fadeDuration));
            yield return null;
        }

        SceneManager.LoadScene(_gameSceneName);
    }

    private IEnumerator Shake_Coroutine(RectTransform target)
    {
        const float duration = 0.3f;
        const float range = 14f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float damper = 1f - elapsed / duration;
            target.anchoredPosition = new Vector2(Mathf.Sin(elapsed * 80f) * range * damper, 0f);
            yield return null;
        }

        target.anchoredPosition = Vector2.zero;
    }

    private void ShowLoginError(string message)
    {
        _loginMessageText.text = message;
        StartCoroutine(Shake_Coroutine((RectTransform)_loginPanel.transform));
    }

    private void ShowRegisterError(string message)
    {
        _registerMessageText.text = message;
        StartCoroutine(Shake_Coroutine((RectTransform)_registerPanel.transform));
    }

    public void OnRegisterClicked()
    {
        string id = _registerIdInput.text.Trim();
        string password = _registerPasswordInput.text;

        if (id.Length < MinIdLength)
        {
            ShowRegisterError($"아이디는 {MinIdLength}자 이상이어야 합니다.");
            return;
        }

        if (password.Length < MinPasswordLength)
        {
            ShowRegisterError($"비밀번호는 {MinPasswordLength}자 이상이어야 합니다.");
            return;
        }

        if (password != _registerConfirmInput.text)
        {
            ShowRegisterError("비밀번호가 일치하지 않습니다.");
            return;
        }

        string key = AccountKeyPrefix + id;
        if (PlayerPrefs.HasKey(key))
        {
            ShowRegisterError("이미 존재하는 아이디입니다.");
            return;
        }

        PlayerPrefs.SetString(key, HashPassword(password));
        PlayerPrefs.SetString(LastLoginIdKey, id);
        PlayerPrefs.Save();

        ShowLogin();
        _loginIdInput.text = id;
        _loginMessageText.text = "가입이 완료되었습니다. 로그인해 주세요.";
    }

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return System.BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}
