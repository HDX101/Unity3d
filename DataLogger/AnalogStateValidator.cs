using UnityEngine;
using System.Collections;

public class AnalogStateValidator : MonoBehaviour
{
    [Header("Komponen untuk Divalidasi")]
    public IntegratedConveyor conveyor;
    public PWMSystem pwmSystem;

    [Header("Pengaturan Interval")]
    public float validationInterval = 1.0f;

    void Start()
    {
        if (conveyor == null || pwmSystem == null)
        {
            Debug.LogError("AnalogStateValidator: Conveyor atau PWMSystem belum di-assign!", this);
            enabled = false;
            return;
        }
        StartCoroutine(AnalogValidationRoutine());
    }

    private IEnumerator AnalogValidationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(validationInterval);
            ValidateAndLogCombinedData();
        }
    }

    private void ValidateAndLogCombinedData()
    {
        if (MasterLogger.Instance == null || conveyor.plcInputManager == null) return;

        // --- 1. Ambil & Hitung Data D100 ---
        int d100_plc_raw = conveyor.plcInputManager.GetIntValue(conveyor.variableValueAddress, 0);
        float d100_plc_percent = 0f;
        if (conveyor.maxInputValue > conveyor.minInputValue)
        {
            d100_plc_percent = ((float)(d100_plc_raw - conveyor.minInputValue) / (conveyor.maxInputValue - conveyor.minInputValue)) * 100f;
        }

        // --- 2. Ambil & Hitung Data D38 ---
        int d38_plc_raw = (int)pwmSystem.CurrentValue_PLC;
        float d38_plc_percent = pwmSystem.CurrentPercentage_PLC;

        // --- 3. Ambil Data Aktual dari Unity ---
        float conveyor_unity_speed = conveyor.CurrentSpeed;
        
        // =========================================================================
        // ===            INILAH PERHITUNGAN PERSENTASE YANG BENAR               ===
        // =========================================================================
        // Menghitung persentase dari RENTANG KECEPATAN AKTUAL (0-100%), BUKAN dari tampilan UI.
        float conveyor_actual_percent = 0f;
        if (conveyor.maxConveyorSpeed > conveyor.minConveyorSpeed)
        {
            conveyor_actual_percent = ((conveyor.CurrentSpeed - conveyor.minConveyorSpeed) / (conveyor.maxConveyorSpeed - conveyor.minConveyorSpeed)) * 100f;
        }
        // =========================================================================

        // Kirim semua data ke Logger. Perhatikan kita menggunakan 'conveyor_actual_percent'.
        MasterLogger.Instance.LogAnalog(
    d100_plc_raw,
    d100_plc_percent,
    d38_plc_raw,
    d38_plc_percent,
    conveyor_unity_speed,
    conveyor_actual_percent
);
    }
}