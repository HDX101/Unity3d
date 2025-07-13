// File: PneumaticActionSensor.cs (Perbaikan)
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PneumaticActionSensor : MonoBehaviour
{
    [Header("Komponen Terkait")]
    public PLCInputManager plcInputManager;
    public IntegratedPneumatic pneumatic; 

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PushableObject") || pneumatic == null) return;

        var objectData = other.GetComponent<SortableObjectData>();
        if (objectData == null) return;

        // Baris yang menyebabkan error telah dihapus.
        // objectData.PneumaticSorted_PLC = plcInputManager.GetBoolState(pneumatic.extendControlAddress); 

        // Baris ini tetap ada karena datanya masih kita pakai.
        objectData.PneumaticSorted_Unity = pneumatic.IsExtended; 
    }
}