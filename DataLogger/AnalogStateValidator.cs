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

        int d100_plc_raw = conveyor.plcInputManager.GetIntValue(conveyor.variableValueAddress, 0);
        float d100_plc_percent = 0f;
        if (conveyor.maxInputValue > conveyor.minInputValue)
        {
            d100_plc_percent = ((float)(d100_plc_raw - conveyor.minInputValue) / (conveyor.maxInputValue - conveyor.minInputValue)) * 100f;
        }

        int d38_plc_raw = (int)pwmSystem.CurrentValue_PLC;
        float d38_plc_percent = pwmSystem.CurrentPercentage_PLC;

        float conveyor_unity_speed = conveyor.CurrentSpeed;
        
        float conveyor_actual_percent = 0f;
        if (conveyor.maxConveyorSpeed > conveyor.minConveyorSpeed)
        {
            conveyor_actual_percent = ((conveyor.CurrentSpeed - conveyor.minConveyorSpeed) / (conveyor.maxConveyorSpeed - conveyor.minConveyorSpeed)) * 100f;
        }
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