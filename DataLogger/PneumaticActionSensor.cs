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
        objectData.PneumaticSorted_Unity = pneumatic.IsExtended; 
    }
}