using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(MeshRenderer))]
public class IntegratedConveyor : MonoBehaviour
{
    public enum ControlMode { Kontrol_PLC, Kecepatan_Manual_Hybrid }

    [Header("Mode Kontrol")]
    public ControlMode controlMode = ControlMode.Kontrol_PLC;

    [Header("Pengaturan Manual (Mode Hybrid)")]
    public string manualEnableAddress = "W50.09";
    [Range(0f, 40f)]
    public float kecepatanManual = 18.5f;

    [Header("Koneksi & Kontrol PLC")]
    public PLCInputManager plcInputManager;
    public string conveyorEnableAddress = "CIO101.01";
    public string variableValueAddress = "D100_val";

    [Header("Konfigurasi Penskalaan Nilai PLC")]
    public int minInputValue = 0;
    public int maxInputValue = 5200;

    [Header("Konfigurasi Rentang Kecepatan & Display")]
    public float minConveyorSpeed = 17.0f;
    public float maxConveyorSpeed = 20.6f;
    [Range(0, 100)] public float minDisplayPercentage = 30.0f;
    [Range(0, 100)] public float maxDisplayPercentage = 100.0f;

    [Header("Pengaturan Fisik & Tampilan")]
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    public TMP_Text statusDisplayText;

    public bool IsRunning { get; private set; }
    public float CurrentSpeed { get; private set; }
    public float CurrentDisplayPercentage { get; private set; }
    
    private Material material;
    private List<GameObject> onBelt = new List<GameObject>();

    void Start()
    {
        material = GetComponent<MeshRenderer>().material;
        if (plcInputManager == null) { Debug.LogError("IntegratedConveyor: PLCInputManager belum di-assign!", this); enabled = false; }
    }

    void Update()
    {
        if (plcInputManager == null) return;

        if (controlMode == ControlMode.Kontrol_PLC) { HandlePLCControl(); }
        else { HandleManualHybridControl(); }

        if (IsRunning)
        {
            material.mainTextureOffset += new Vector2(moveDirection.x, moveDirection.z).normalized * CurrentSpeed * Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (!IsRunning) return;
        for (int i = onBelt.Count - 1; i >= 0; i--)
        {
            GameObject go = onBelt[i];
            if (go == null) { onBelt.RemoveAt(i); continue; }
            Rigidbody objRb = go.GetComponent<Rigidbody>();
            if (objRb != null)
            {
                objRb.MovePosition(objRb.position + moveDirection.normalized * CurrentSpeed * Time.fixedDeltaTime);
            }
        }
    }

    private void HandlePLCControl()
{
    bool plcIsRunning = plcInputManager.GetBoolState(conveyorEnableAddress);

    if (plcIsRunning != IsRunning)
    {
        long scriptActionTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        long nodeRedTimestamp = plcInputManager.GetLongValue("timestamp_origin", 0);
        
        // --- PERBAIKAN: Gunakan 'conveyorEnableAddress' bukan 'extendControlAddress' ---
        PLCDataPacket? packet = plcInputManager.GetPacket(conveyorEnableAddress);

        if (nodeRedTimestamp > 0 && packet.HasValue && MasterLogger.Instance != null)
        {
            // --- PERBAIKAN: Gunakan 'conveyorEnableAddress' di sini juga ---
            MasterLogger.Instance.LogLatency(conveyorEnableAddress, nodeRedTimestamp, packet.Value.Timestamp, scriptActionTimestamp);
        }
    }

    IsRunning = plcIsRunning;
    int plcRawValue = plcInputManager.GetIntValue(variableValueAddress, 0);
    CurrentSpeed = ScaleValueToSpeed(plcRawValue);
    CurrentDisplayPercentage = ScaleValueToDisplayPercentage(plcRawValue);
    UpdateDisplayText(true);
}

    private void HandleManualHybridControl()
    {
        IsRunning = plcInputManager.GetBoolState(manualEnableAddress);
        CurrentSpeed = kecepatanManual;
        UpdateDisplayText(false);
    }

    private float ScaleValueToSpeed(int rawValue)
    {
        if (maxInputValue <= minInputValue) return minConveyorSpeed;
        float normalizedValue = Mathf.Clamp01((float)(rawValue - minInputValue) / (maxInputValue - minInputValue));
        return Mathf.Lerp(minConveyorSpeed, maxConveyorSpeed, normalizedValue);
    }

    private float ScaleValueToDisplayPercentage(int rawValue)
    {
        if (maxInputValue <= minInputValue) return minDisplayPercentage;
        float normalizedValue = Mathf.Clamp01((float)(rawValue - minInputValue) / (maxInputValue - minInputValue));
        return Mathf.Lerp(minDisplayPercentage, maxDisplayPercentage, normalizedValue);
    }
    
    private void UpdateDisplayText(bool usePercentage)
    {
        if (statusDisplayText != null)
        {
            if (!IsRunning) { statusDisplayText.text = "OFF"; }
            else { statusDisplayText.text = usePercentage ? CurrentDisplayPercentage.ToString("F0") + "%" : CurrentSpeed.ToString("F1") + " m/s"; }
        }
    }
    
    private void OnCollisionEnter(Collision collision) { if (!onBelt.Contains(collision.gameObject)) onBelt.Add(collision.gameObject); }
    private void OnCollisionExit(Collision collision) { onBelt.Remove(collision.gameObject); }
}