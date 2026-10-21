using System;
using TMPro;
using UnityEngine;

using VersusFight;


public class VNetworkManager : MonoBehaviour
{
    private static VNetworkManager instance;
    public static VNetworkManager Instance
    {
        get { return instance; }
    }

    // 상호작용 오브젝트 저장
    [SerializeField] private GameObject[] interactableObjects;
    public GameObject[] InteractableObjects => interactableObjects;

    // 네트워크 클래스
    [SerializeField] private VNetwork network;
    public int NetworkId { get; set; }
    private bool isConnect = false;
    public bool IsConnect => isConnect;
    private bool isServer = false;
    public bool IsServer => isServer;

    // 설정
    [SerializeField] private Canvas startCanvas;
    [SerializeField] private Camera startCamera;

    // 프리팹
    [Space(10)]
    [SerializeField] private GameObject playerObj;
    [SerializeField] private GameObject enemyObj;

    // 테스트
    [Space(10)]
    [SerializeField] private TextMeshProUGUI infoText;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
            transform.parent = null;
            DontDestroyOnLoad(this);
            
            // 상호작용 오브젝트 초기화
            var objects = FindObjectsByType<InteractableObject>();

            Array.Sort(objects, (a, b) =>
                StringComparer.Ordinal.Compare(a.SceneId, b.SceneId));

            interactableObjects = new GameObject[Setting.MAX_OBJECTS + 1];

            for (int i = 0; i < objects.Length; ++i)
            {
                interactableObjects[i] = objects[i].gameObject;
            }
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
        if (network == null) network = gameObject.AddComponent<VNetworkServer>();
        else network = gameObject.GetComponent<VNetworkServer>();

        isServer = true;

        if (infoText != null) infoText.text = "Server";
        Connect();
    }

    public void ConnectClient()
    {
        if (network == null) network = gameObject.AddComponent<VNetworkClient>();
        else network = gameObject.GetComponent<VNetworkClient>();

        isServer = false;
        
        if (infoText != null) infoText.text = "Client";
        Connect();
    }

    private void Connect()
    {
        if (network.Connect(playerObj, enemyObj, ConnectAction))
        {
            // 연결 성공
            // _ = network.ReceiveLoop();
            isConnect = true;
        }
    }

    public void Disconnect()
    {
        if (network == null) return;
        
        // Disconnect 패킷을 보냄
        // DisconnectPacket packet = new DisconnectPacket()
        // {
        //     networkId = NetworkId
        // };

        Packet disconnectPacket = new Packet()
        {
            packetType = PacketType.Disconnect, // 1
            entityType = EntityType.PLAYABLE,   // 1
            networkId = NetworkId,              // 4
            // data = PacketSerializer.SerializeDiconnect(packet),
            data = new byte[1]                  // 1
        };
        network.SendPacket(disconnectPacket);

        if (network.Disconnect())
        {
            // 연결 끊기 성공
            network = null;
            isConnect = false;
        }
    }

    private void ConnectAction()
    {
        startCanvas.gameObject.SetActive(false);
        startCamera.gameObject.SetActive(false);

        // 상호작용 오브젝트 초기화
        network.InitInteractableObjects();
    }


    // public methods
    public void SendPacket(PacketType packetType, EntityType entityType, byte[] data)
    {
        if (network == null)
        {
            Debug.Log("you can't send packet because network is null");
            return;
        }

        Packet p = new Packet()
        {
            packetType = packetType,
            networkId = NetworkId,
            entityType = entityType,
            data = data,
        };
        network.SendPacket(p);
    }

    public void SendPacket(Packet p)
    {
        if (network == null)
        {
            Debug.Log("you can't send packet because network is null");
            return;
        }

        network.SendPacket(p);
    }
}