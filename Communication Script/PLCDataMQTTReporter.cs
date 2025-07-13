using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Globalization;

public class PLCDataMQTTReporter : MonoBehaviour
{
    [Header("MQTT Broker Configuration (for Publishing)")]
    public string brokerAddress = "broker.hivemq.com";
    public int brokerPort = 1883;
    public bool encryptConnection = false;
    public string mqttUsername = null;
    public string mqttPassword = null;

    [Header("Publish Settings")]
    public string publishTopic = "unity/plc/send";
    [Tooltip("Interval dalam detik untuk mempublikasikan data. Atur ke 0 untuk publish manual.")]
    public float publishIntervalSeconds = 1.0f;

    [Header("Data Source")]
    public PLCInputManager plcInputManager;

    private MqttClient publisherClient;
    private float timeSinceLastPublish = 0f;

    void Start()
    {
        if (plcInputManager == null)
        {
            Debug.LogError("PLCDataMQTTReporter: PLCInputManager belum di-assign! Reporter tidak akan berfungsi.");
            enabled = false;
            return;
        }
        ConnectToMqttBroker();
    }

    void ConnectToMqttBroker()
    {
        try
        {
            publisherClient = new MqttClient(brokerAddress, brokerPort, encryptConnection, null, null, encryptConnection ? MqttSslProtocols.TLSv1_2 : MqttSslProtocols.None);
            string clientId = "UnityReporterClient_" + Guid.NewGuid().ToString();
            
            if (!string.IsNullOrEmpty(mqttUsername)) { publisherClient.Connect(clientId, mqttUsername, mqttPassword); }
            else { publisherClient.Connect(clientId); }

            if (publisherClient.IsConnected) { Debug.Log($"PLCDataMQTTReporter: Berhasil terhubung ke MQTT Broker untuk publish: {brokerAddress}"); }
            else { Debug.LogError($"PLCDataMQTTReporter: Gagal terhubung ke MQTT Broker: {brokerAddress}"); }
        }
        catch (Exception e) { Debug.LogError($"PLCDataMQTTReporter: Koneksi MQTT untuk publish gagal: {e.ToString()}"); publisherClient = null; }
    }

    void Update()
    {
        if (publisherClient == null || !publisherClient.IsConnected || plcInputManager == null) return;
        if (publishIntervalSeconds > 0)
        {
            timeSinceLastPublish += Time.deltaTime;
            if (timeSinceLastPublish >= publishIntervalSeconds)
            {
                PublishPlcDataNow();
                timeSinceLastPublish = 0f;
            }
        }
    }

    public void PublishPlcDataNow()
    {
        if (publisherClient == null || !publisherClient.IsConnected) return;
        if (plcInputManager.PlcDataStates == null || plcInputManager.PlcDataStates.Count == 0) return;
        
        string jsonPayload = SerializePlcDataToJson(plcInputManager.PlcDataStates);

        if (!string.IsNullOrEmpty(jsonPayload) && jsonPayload != "{}")
        {
            try
            {
                publisherClient.Publish(publishTopic, Encoding.UTF8.GetBytes(jsonPayload), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
            }
            catch (Exception e) { Debug.LogError($"PLCDataMQTTReporter: Gagal publish data: {e.ToString()}"); }
        }
    }
    
    private string SerializePlcDataToJson(Dictionary<string, PLCDataPacket> plcData)
    {
        if (plcData == null || plcData.Count == 0) return "{}";

        StringBuilder sb = new StringBuilder();
        sb.Append("{");

        bool firstEntry = true;
        foreach (KeyValuePair<string, PLCDataPacket> entry in plcData)
        {
            if (entry.Key == "timestamp_origin") continue;
            if (!firstEntry) { sb.Append(","); }

            sb.AppendFormat("\"{0}\":", entry.Key); 
            object value = entry.Value.Value; 

            if (value is bool boolValue) { sb.Append(boolValue.ToString().ToLowerInvariant()); }
            else if (value is long longValue) { sb.Append(longValue.ToString(CultureInfo.InvariantCulture)); }
            else if (value is int intValue) { sb.Append(intValue.ToString(CultureInfo.InvariantCulture)); }
            else if (value is float floatValue) { sb.Append(floatValue.ToString("R", CultureInfo.InvariantCulture)); }
            else if (value is double doubleValue) { sb.Append(doubleValue.ToString("R", CultureInfo.InvariantCulture)); }
            else if (value is string stringValue) { sb.AppendFormat("\"{0}\"", EscapeJsonString(stringValue)); }
            else if (value != null) { sb.AppendFormat("\"{0}\"", EscapeJsonString(value.ToString())); }
            else { sb.Append("null"); }
            
            firstEntry = false;
        }
        sb.Append("}");
        return sb.ToString();
    }
    private string EscapeJsonString(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        
        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ')
                    {
                        string t = "000" + ((int)c).ToString("X");
                        sb.Append("\\u" + t.Substring(t.Length - 4));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
    void OnApplicationQuit() { DisconnectFromMqttBroker(); }
    void OnDestroy() { DisconnectFromMqttBroker(); }
    private void DisconnectFromMqttBroker()
    {
        if (publisherClient != null && publisherClient.IsConnected) { publisherClient.Disconnect(); }
        publisherClient = null;
    }
}