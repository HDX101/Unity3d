using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class IntegratedPneumatic : MonoBehaviour
{
    [Header("Koneksi PLC")]
    public PLCInputManager plcInputManager;
    public string extendControlAddress = "CIO100.06";

    [Header("Parameter Gerakan")]
    public float moveSpeed = 15f;
    public float targetWorldZ = -8f;
    
    public bool IsExtended { get; private set; }
    
    private Rigidbody rb;
    private Vector3 worldOriginPosition;
    private Vector3 worldExtendedPosition;
    private Vector3 currentTargetPosition;
    private bool lastPlcState = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        if (plcInputManager == null) { Debug.LogError("IntegratedPneumatic: PLCInputManager belum di-assign.", this); enabled = false; return; }
        
        worldOriginPosition = transform.position;
        worldExtendedPosition = new Vector3(worldOriginPosition.x, worldOriginPosition.y, targetWorldZ);
        
        bool initialPlcState = plcInputManager.GetBoolState(extendControlAddress, false);
        currentTargetPosition = initialPlcState ? worldExtendedPosition : worldOriginPosition;
        IsExtended = initialPlcState;
        lastPlcState = initialPlcState;
        rb.position = currentTargetPosition;
    }

    void Update()
    {
        if (plcInputManager == null) return;
        
        bool plcShouldExtend = plcInputManager.GetBoolState(extendControlAddress);
        
        if (plcShouldExtend != lastPlcState)
        {
            long scriptActionTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            long nodeRedTimestamp = plcInputManager.GetLongValue("timestamp_origin", 0);
            PLCDataPacket? packet = plcInputManager.GetPacket(extendControlAddress);

            if (nodeRedTimestamp > 0 && packet.HasValue && MasterLogger.Instance != null)
{
    long unityReceiptTimestamp = packet.Value.Timestamp;
    MasterLogger.Instance.LogLatency(extendControlAddress, nodeRedTimestamp, unityReceiptTimestamp, scriptActionTimestamp);
}
            
            currentTargetPosition = plcShouldExtend ? worldExtendedPosition : worldOriginPosition;
            IsExtended = plcShouldExtend; 
            lastPlcState = plcShouldExtend;
        }
    }

    void FixedUpdate()
    {
        if (Vector3.Distance(rb.position, currentTargetPosition) > 0.01f)
        {
            Vector3 nextPosition = Vector3.MoveTowards(rb.position, currentTargetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(nextPosition);
        }
    }
}