using UnityEngine;

using VersusFight;


public class VNetworkManager : MonoBehaviour
{
    private static VNetworkManager instance;
    public static VNetworkManager Instance
    {
        get { return instance; }
    }

    // 네트워크 클래스
    private VNetwork network;
    public int NetworkId { get; set; }

    // 설정
    [SerializeField] private Canvas startCanvas;
    [SerializeField] private Camera startCamera;

    // 프리팹
    [Space(10)]
    [SerializeField] private GameObject playerObj;
    [SerializeField] private GameObject enemyObj;

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
    }

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

    public void Disconnect()
    {
        if (network == null) return;
        
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


    // public methods
    public void SendPacket(PacketType packetType, byte[] data)
    {
        Packet p = new Packet()
        {
            packetType = packetType,
            data = data,
        };
        network.SendPacket(p);
    }
}