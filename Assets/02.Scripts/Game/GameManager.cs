using System;
using System.Collections;
using TMPro;
using UnityEngine;

public enum GameState
{
    Ready,
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;
    
    [SerializeField] private TextMeshProUGUI _stateText;
    
    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }
    
    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";
        yield return new WaitForSeconds(1.5f);
        _stateText.text = "시작!";
        yield return new WaitForSeconds(0.5f);
        _stateText.gameObject.SetActive(false);
        _state = GameState.Playing;
    }

    public event Action OnGameOver;

    public void GameOver()
    {
        _state = GameState.GameOver;

        _stateText.gameObject.SetActive(true);
        _stateText.text = "게임 오버";
        _stateText.color = Color.red;

        ShowCursor();
        OnGameOver?.Invoke();
    }

    // 일시정지: 게임 시간을 멈추고 커서를 보여준다.
    public void Pause()
    {
        if (_state != GameState.Playing) return;

        _state = GameState.Paused;
        Time.timeScale = 0f;
        ShowCursor();
    }

    // 재개: 게임 시간을 되돌리고 커서를 잠근다.
    public void Resume()
    {
        if (_state != GameState.Paused) return;

        _state = GameState.Playing;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 씬 전환 전에 멈춘 시간을 원래대로 돌린다.
    public void RestoreTimeScale()
    {
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
