using UnityEngine;
using System;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine.Events;
using System.Collections.Generic;

public class MQTTSubscriber : MonoBehaviour
{
    [Header("MQTT Broker Configuration")]
    public string brokerAddress = "broker.hivemq.com";
    public int brokerPort = 1883;
    public bool encrypt = false;

    [Header("MQTT Topic")]
    [Tooltip("Topik MQTT yang akan di-subscribe.")]
    public string topicToSubscribe = "unity/plc/bits_status";

    [Header("Processing & Timeout")]
    [Tooltip("Interval dalam detik untuk memproses antrian pesan MQTT.")]
    public float refreshIntervalSeconds = 0.5f;
    [Tooltip("Waktu dalam detik sebelum menampilkan peringatan jika tidak ada pesan diterima.")]
    public float messageTimeoutSeconds = 3.0f;
    private float timeSinceLastMessage = 0f;
    private bool isTimeoutWarningActive = false;

    private float currentActiveInterval = -1f;

    [Header("Events")]
    [Tooltip("Event yang dipicu ketika pesan baru diterima dan diproses.")]
    public StringEvent onMessageReceivedAndProcessed;

    private MqttClient client;
    private string _lastReceivedMessage;
    private readonly object messageLock = new object();
    private bool _newMessageAvailable = false;

    [System.Serializable]
    public class StringEvent : UnityEvent<string> { }

    void Start()
    {
        MainThreadDispatcher.Init(); 
        ConnectToMqtt();
        UpdateInvokeRepeatingStatus();
    }

    void Update()
    {
        // --- TAMBAHAN: Deteksi F5 untuk koneksi ulang ---
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("F5 DITEKAN: Memulai koneksi ulang MQTT Subscriber...");
            AttemptReconnect();
        }
        // --- AKHIR TAMBAHAN ---

        if (Math.Abs(currentActiveInterval - refreshIntervalSeconds) > 0.001f)
        {
            UpdateInvokeRepeatingStatus();
        }

        if (client != null && client.IsConnected)
        {
            timeSinceLastMessage += Time.deltaTime;
            if (timeSinceLastMessage > messageTimeoutSeconds)
            {
                if (!isTimeoutWarningActive)
                {
                    Debug.LogWarning($"MQTTSubscriber: Tidak ada pesan diterima dari topik '{topicToSubscribe}' selama lebih dari {messageTimeoutSeconds} detik. Periksa koneksi PLC ke Node-RED.");
                    isTimeoutWarningActive = true;
                }
            }
        }
    }

    public void AttemptReconnect()
    {
        Debug.Log("MQTTSubscriber: Mencoba melakukan koneksi ulang...");
    
        CleanUp();
        ConnectToMqtt();
        UpdateInvokeRepeatingStatus();
    }

    void UpdateInvokeRepeatingStatus()
    {
        CancelInvoke("ProcessMqttQueue");
        currentActiveInterval = refreshIntervalSeconds;
        if (currentActiveInterval > 0)
        {
            InvokeRepeating("ProcessMqttQueue", currentActiveInterval, currentActiveInterval);
        }
    }

    void ConnectToMqtt()
    {
        try
        {
            client = new MqttClient(brokerAddress, brokerPort, encrypt, null, null, encrypt ? MqttSslProtocols.TLSv1_2 : MqttSslProtocols.None);
            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            client.ConnectionClosed += OnMqttConnectionClosed;

            string clientId = "UnityClient_" + Guid.NewGuid().ToString();
            client.Connect(clientId);

            if (client.IsConnected)
            {
                Debug.Log($"Berhasil terhubung ke MQTT Broker: {brokerAddress} Client ID: {clientId}");
                SubscribeToTopic(topicToSubscribe);
                isTimeoutWarningActive = false; 
                timeSinceLastMessage = 0f;     
            }
            else
            {
                Debug.LogError($"Gagal terhubung ke MQTT Broker: {brokerAddress}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Koneksi MQTT gagal: {e.ToString()}");
        }
    }

    void SubscribeToTopic(string topic)
    {
        if (client != null && client.IsConnected)
        {
            try
            {
                client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                Debug.Log($"Berhasil subscribe ke topik: {topic}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Gagal subscribe ke topik {topic}: {e.ToString()}");
            }
        }
        else
        {
            Debug.LogWarning("Tidak bisa subscribe, MQTT client tidak terhubung.");
        }
    }

    void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        timeSinceLastMessage = 0f;
        if (isTimeoutWarningActive)
        {
            Debug.Log($"MQTTSubscriber: Koneksi data dari topik '{topicToSubscribe}' telah pulih.");
            isTimeoutWarningActive = false;
        }

        string receivedMessage = Encoding.UTF8.GetString(e.Message);
        lock (messageLock)
        {
            _lastReceivedMessage = receivedMessage;
            _newMessageAvailable = true;
        }
    }
    
    public void ProcessMqttQueue()
    {
        string messageToProcess = null;
        bool processThisTick = false;

        lock (messageLock)
        {
            if (_newMessageAvailable)
            {
                messageToProcess = _lastReceivedMessage;
                _newMessageAvailable = false;
                processThisTick = true;
            }
        }

        if (processThisTick && messageToProcess != null)
        {
            if (onMessageReceivedAndProcessed != null)
            {
                if (MainThreadDispatcher.Exists())
                {
                    MainThreadDispatcher.Enqueue(() => onMessageReceivedAndProcessed.Invoke(messageToProcess));
                }
                else
                {
                    Debug.LogError("MQTTSubscriber (ProcessMqttQueue): MainThreadDispatcher tidak ditemukan.");
                }
            }
        }
    }

    void OnMqttConnectionClosed(object sender, EventArgs e)
    {
        Debug.LogWarning("Koneksi MQTT terputus.");
        isTimeoutWarningActive = true; 
    }

    void OnDisable()
    {
        CancelInvoke("ProcessMqttQueue");
        currentActiveInterval = -1f;
    }

    void OnEnable()
    {
        if (client != null && client.IsConnected)
        {
             UpdateInvokeRepeatingStatus();
        }
    }

    void OnApplicationQuit()
    {
        CleanUp();
    }

    void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        CancelInvoke("ProcessMqttQueue");
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
        }
        client = null; // Pastikan client di-null kan
    }
}


public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static MainThreadDispatcher _instance = null;

    public static void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if(_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
    
    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

   
    public static bool Exists()
    {
        return _instance != null;
    }

    public static void Init()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("MainThreadDispatcher_Instance");
            _instance = go.AddComponent<MainThreadDispatcher>();
        }
    }
}