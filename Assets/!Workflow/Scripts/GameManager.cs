using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    [SerializeField] private Canvas startCanvas;
    [SerializeField] private Canvas pauseCanvas;

    [SerializeField] InputActionReference pauseAction;

    private bool isPause = false;
    public bool IsPause => isPause;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(this);
        }
        pauseAction.action.started += Pause;
    }

    void OnDisable()
    {
        pauseAction.action.started -= Pause;
    }

    private void Pause(InputAction.CallbackContext context)
    {
        if (isPause)
        {
            // 일시정지일 때 일시정지 해제
            // Time.timeScale = 1;
            // 일시정지 캔버스 끄기
            pauseCanvas.gameObject.SetActive(false);
            // 커서 고정
            Cursor.lockState = CursorLockMode.Locked;
            // 커서 숨김
            Cursor.visible = false;
        }
        else
        {
            // 일시정지 아닐 때 일시정지
            // Time.timeScale = 0;
            // 일시정지 캔버스 켜기
            pauseCanvas.gameObject.SetActive(true);
            // 커서 고정 해제
            Cursor.lockState = CursorLockMode.None;
            // 커서 숨김 해제
            Cursor.visible = true;
        }
        isPause = !isPause;
    }

    public void Resume()
    {
        // 일시정지 해제
        // Time.timeScale = 1;
        // 일시정지 캔버스 끄기
        pauseCanvas.gameObject.SetActive(false);
        // 커서 고정
        Cursor.lockState = CursorLockMode.Locked;
        // 커서 숨김
        Cursor.visible = false;
        isPause = false;
    }

    public void Disconnect()
    {
        // VNetworkManager의 Disconnect 호출
        VNetworkManager.Instance.Disconnect();
        // 일시정지 캔버스 끄기
        pauseCanvas.gameObject.SetActive(false);
        // 일시정지 캔버스 켜기
        startCanvas.gameObject.SetActive(true);
    }
}