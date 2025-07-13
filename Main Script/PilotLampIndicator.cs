using UnityEngine;
using System;

[RequireComponent(typeof(Renderer))]
public class PilotLampIndicator : MonoBehaviour
{
    [Header("PLC Input Configuration")]
    public PLCInputManager plcInputManager;
    public string statusAddress = "W50.03";

    [Header("Visual State Materials")]
    public Material onMaterial;
    public Material offMaterial;

    private Renderer objectRenderer;
    private bool lastKnownState = false;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (plcInputManager == null) { Debug.LogError("PilotLampIndicator: PLCInputManager belum di-assign.", this); enabled = false; return; }

        bool initialState = plcInputManager.GetBoolState(statusAddress, false);
        SetIndicatorMaterial(initialState);
        lastKnownState = initialState;
    }

    void Update()
    {
        if (plcInputManager == null) return;

        bool currentState = plcInputManager.GetBoolState(statusAddress);

        if (currentState != lastKnownState)
        {
            long scriptActionTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            long nodeRedTimestamp = plcInputManager.GetLongValue("timestamp_origin", 0);
            
            PLCDataPacket? packet = plcInputManager.GetPacket(statusAddress);

            if (nodeRedTimestamp > 0 && packet.HasValue && MasterLogger.Instance != null)
            {
                MasterLogger.Instance.LogLatency(statusAddress, nodeRedTimestamp, packet.Value.Timestamp, scriptActionTimestamp);
            }
            
            SetIndicatorMaterial(currentState);
            lastKnownState = currentState;
        }
    }

    private void SetIndicatorMaterial(bool isStateOn)
    {
        if (isStateOn) 
        { 
            if (onMaterial != null) objectRenderer.material = onMaterial; 
        }
        else 
        { 
            if (offMaterial != null) objectRenderer.material = offMaterial; 
        }
    }
}