using UnityEngine;
using System;
using System.Collections.Generic;

public struct PLCDataPacket
{
    public object Value;
    public long Timestamp;
}

public class PLCInputManager : MonoBehaviour
{
    [Header("MQTT Subscriber Reference")]
    public MQTTSubscriber mqttSubscriber;

    public Dictionary<string, PLCDataPacket> PlcDataStates { get; private set; } = new Dictionary<string, PLCDataPacket>();

    void Start()
    {
        if (mqttSubscriber == null) { Debug.LogError("PLCInputManager: MQTTSubscriber belum di-assign!"); enabled = false; return; }
        mqttSubscriber.onMessageReceivedAndProcessed.AddListener(HandleMqttMessage);
    }

    void OnDestroy()
    {
        if (mqttSubscriber != null) { mqttSubscriber.onMessageReceivedAndProcessed.RemoveListener(HandleMqttMessage); }
    }

    private void HandleMqttMessage(string jsonMessage)
    {
        ParseAndStorePlcJsonData(jsonMessage);
    }

    private void ParseAndStorePlcJsonData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            long unityReceiptTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            string cleanedJson = json.Trim().TrimStart('{').TrimEnd('}');
            string[] pairs = cleanedJson.Split(',');

            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split(new char[] {':'}, 2); 
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim().Trim('"'); 
                    string valueString = keyValue[1].Trim();
                    object parsedValue = null;

                    if (valueString.Equals("true", StringComparison.OrdinalIgnoreCase)) { parsedValue = true; }
                    else if (valueString.Equals("false", StringComparison.OrdinalIgnoreCase)) { parsedValue = false; }
                    else if (long.TryParse(valueString, out long longValue)) { parsedValue = longValue; }
                    else if (int.TryParse(valueString, out int intValue)) { parsedValue = intValue; }
                    else if (float.TryParse(valueString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float floatValue)) { parsedValue = floatValue; }
                    
                    if (parsedValue != null)
                    {
                        PlcDataStates[key] = new PLCDataPacket { Value = parsedValue, Timestamp = unityReceiptTimestamp };
                    }
                }
            }
        }
        catch (Exception e) { Debug.LogError($"PLCInputManager: Error saat parsing JSON: {e.Message}\nJSON: {json}"); }
    }

    public bool GetBoolState(string address, bool defaultValue = false)
    {
        if (PlcDataStates.TryGetValue(address, out PLCDataPacket packet) && packet.Value is bool boolValue)
        {
            return boolValue;
        }
        return defaultValue;
    }

    public PLCDataPacket? GetPacket(string address)
    {
        if (PlcDataStates.TryGetValue(address, out PLCDataPacket packet))
        {
            return packet;
        }
        return null;
    }

    public long GetLongValue(string address, long defaultValue = 0)
    {
        if (PlcDataStates.TryGetValue(address, out PLCDataPacket packet) && packet.Value != null)
        {
            if (long.TryParse(packet.Value.ToString(), out long parsedLong)) return parsedLong;
        }
        return defaultValue;
    }

    public int GetIntValue(string address, int defaultValue = 0)
    {
        if (PlcDataStates.TryGetValue(address, out PLCDataPacket packet) && packet.Value != null)
        {
            if (int.TryParse(packet.Value.ToString(), out int parsedInt)) return parsedInt;
        }
        return defaultValue;
    }
}