using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ESC로 여닫는 옵션 팝업. 열려 있는 동안 게임은 일시정지된다.
/// 이 스크립트는 항상 활성 상태인 오브젝트에 두고, 실제 팝업 화면은 _panel로 켜고 끈다.
/// </summary>
public class UI_OptionPopup : MonoBehaviour
{
    private const string LoginSceneName = "LoginScene";

    [SerializeField] private GameObject _panel;
    [SerializeField] private Slider _volumeSlider;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;

    private void Start()
    {
        _volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        _resumeButton.onClick.AddListener(Close);
        _restartButton.onClick.AddListener(Restart);
        _quitButton.onClick.AddListener(GoToLogin);

        _panel.SetActive(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (GameManager.Instance.State == GameState.Playing)
        {
            Open();
        }
        else if (GameManager.Instance.State == GameState.Paused)
        {
            Close();
        }
    }

    private void Open()
    {
        GameManager.Instance.Pause();
        _panel.SetActive(true);
    }

    private void Close()
    {
        _panel.SetActive(false);
        GameManager.Instance.Resume();
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    private void Restart()
    {
        GameManager.Instance.RestoreTimeScale();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToLogin()
    {
        GameManager.Instance.RestoreTimeScale();
        SceneManager.LoadScene(LoginSceneName);
    }
}
