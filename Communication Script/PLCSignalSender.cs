using UnityEngine;
using System;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class PLCSignalSender : MonoBehaviour
{
    [Header("MQTT Broker Configuration")]
    [Tooltip("Alamat IP atau hostname dari MQTT broker Anda.")]
    public string brokerAddress = "broker.hivemq.com";
    [Tooltip("Port MQTT broker (biasanya 1883 untuk koneksi standar).")]
    public int brokerPort = 1883;
    [Tooltip("Gunakan koneksi terenkripsi (TLS/SSL).")]
    public bool encryptConnection = false;

    [Header("PLC Command & Status Settings")]
    [Tooltip("Topik MQTT default untuk mengirim perintah dan status.")]
    public string commandTopic = "unity/plc/write_commands";
    [Tooltip("Alamat di PLC yang akan menerima status koneksi Unity (misalnya, W51.00). Biarkan kosong jika tidak ingin mengirim status koneksi otomatis.")]
    public string connectionStatusAddress = "W51.00";

    private MqttClient commandClient;
    private bool isQuitting = false;
    private bool isConnecting = false;

    async void Start()
    {
        await ConnectToMqttBroker();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("F5 DITEKAN: Memulai koneksi ulang PLCSignalSender...");
            _ = AttemptReconnect();
        }
    }
    public async Task AttemptReconnect()
    {
        
        Debug.Log("PLCSignalSender: Mencoba melakukan koneksi ulang...");
        DisconnectFromMqttBroker();
        await Task.Delay(500);
        await ConnectToMqttBroker();
    }

    private async Task ConnectToMqttBroker()
    {
        if (commandClient != null && commandClient.IsConnected || isConnecting) return;

        isConnecting = true;
        Debug.Log($"PLCSignalSender: Mencoba menghubungkan ke {brokerAddress}:{brokerPort}...");
        
        try
        {
            commandClient = new MqttClient(brokerAddress, brokerPort, encryptConnection, null, null, encryptConnection ? MqttSslProtocols.TLSv1_2 : MqttSslProtocols.None);
            string clientId = "UnitySignalSender_" + Guid.NewGuid().ToString();

            await Task.Run(() => {
                commandClient.Connect(clientId);
            });

            if (commandClient.IsConnected)
            {
                Debug.Log($"PLCSignalSender: Berhasil terhubung ke MQTT Broker.");
                if (!string.IsNullOrEmpty(connectionStatusAddress))
                {
                    SendBooleanCommand(connectionStatusAddress, true);
                }
            }
            else
            {
                Debug.LogError($"PLCSignalSender: Gagal terhubung ke MQTT Broker.");
                commandClient = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"PLCSignalSender: Koneksi MQTT gagal: {e.ToString()}");
            commandClient = null; 
        }
        finally
        {
            isConnecting = false;
        }
    }
    
    public void SendBooleanCommand(string address, bool value)
    {
        if (commandClient == null || !commandClient.IsConnected)
        {
            Debug.LogWarning($"PLCSignalSender: Tidak terhubung, tidak bisa mengirim perintah ke {address}. Coba tekan F5 untuk koneksi ulang.");
            return;
        }

        if (string.IsNullOrEmpty(address))
        {
            Debug.LogError("PLCSignalSender: Alamat tidak boleh kosong saat mengirim perintah.");
            return;
        }

        string payloadValue = value.ToString().ToLowerInvariant();
        string payload = string.Format("{{\"{0}\": {1}}}", address, payloadValue);

        try
        {
            commandClient.Publish(commandTopic, 
                                 Encoding.UTF8.GetBytes(payload), 
                                 MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, 
                                 false); 
            }
        catch (Exception e)
        {
            Debug.LogError($"PLCSignalSender: Gagal mengirim perintah: {e.ToString()}");
        }
    }

    void OnApplicationQuit()
    {
        isQuitting = true;
        if (!string.IsNullOrEmpty(connectionStatusAddress))
        {
            Debug.Log("PLCSignalSender: Aplikasi berhenti. Mengirim sinyal OFF status koneksi.");
            SendBooleanCommand(connectionStatusAddress, false);
        }
        DisconnectFromMqttBroker();
    }

    void OnDestroy()
    {
        if (!isQuitting && (commandClient != null && commandClient.IsConnected))
        {
            if (!string.IsNullOrEmpty(connectionStatusAddress))
            {
                SendBooleanCommand(connectionStatusAddress, false);
            }
        }
        DisconnectFromMqttBroker();
    }

    private void DisconnectFromMqttBroker()
    {
        if (commandClient != null && commandClient.IsConnected)
        {
            commandClient.Disconnect();
        }
        commandClient = null;
    }
}