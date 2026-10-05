using UnityEngine;

using VersusFight;


public class VNetworkManager : MonoBehaviour
{
    // 네트워크 클래스
    private VNetwork network;

    // 설정
    [SerializeField] private Canvas startCanvas;
    [SerializeField] private Camera startCamera;

    // 프리팹
    [Space(10)]
    [SerializeField] private GameObject playerObj;
    [SerializeField] private GameObject enemyObj;

    void OnDestroy()
    {
        Disconnect();
    }

    public void ConnectServer()
    {
        network = gameObject.AddComponent<VNetworkServer>();
        Connect();
    }

    public void ConnectClient()
    {
        network = gameObject.AddComponent<VNetworkClient>();
        Connect();
    }

    private void Connect()
    {
        if (network.Connect(playerObj, enemyObj, ConnectAction))
        {
            // 연결 성공
            // _ = network.ReceiveLoop();
        }
    }

    private void Disconnect()
    {
        if (network.Disconnect())
        {
            // 연결 끊기 성공

        }
    }

    private void ConnectAction()
    {
        startCanvas.gameObject.SetActive(false);
        startCamera.gameObject.SetActive(false);
    }
}