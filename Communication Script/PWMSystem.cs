using UnityEngine;

public class PWMSystem : MonoBehaviour
{
    [Header("Koneksi & Kontrol PLC")]
    public PLCInputManager plcInputManager;
    public string pwmAddress = "D38_val";

    [Header("Rentang Nilai PWM")]
    public int minInputValue = 310;
    public int maxInputValue = 1024;
    
    public float CurrentValue_PLC { get; private set; }
    public float CurrentPercentage_PLC { get; private set; }

    void Update()
    {
        if (plcInputManager == null) return;

        CurrentValue_PLC = plcInputManager.GetIntValue(pwmAddress, minInputValue);

        if (maxInputValue > minInputValue)
        {
            CurrentPercentage_PLC = ((CurrentValue_PLC - minInputValue) / (maxInputValue - minInputValue)) * 100f;
            CurrentPercentage_PLC = Mathf.Clamp(CurrentPercentage_PLC, 0f, 100f);
        }
    }
}