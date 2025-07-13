using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class DestinationSensor : MonoBehaviour
{
    public enum DestinationType { Metal, NonMetal }

    [Header("Konfigurasi Sensor")]
    public DestinationType boxType;
    [SerializeField] private float deactivateDelay = 1.0f;

    [Header("Tampilan Counter")]
    public TMP_Text countTextDisplay;

    private int objectCountInThisBox = 0;
    private Dictionary<GameObject, Coroutine> runningCoroutines = new Dictionary<GameObject, Coroutine>();

    void Start()
    {
        if (countTextDisplay == null) { Debug.LogError($"DestinationSensor pada '{gameObject.name}': 'Count Text Display' belum diisi!", this); }
        GetComponent<Collider>().isTrigger = true;
        UpdateCountText();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PushableObject")) return;

        var objectData = other.GetComponentInParent<SortableObjectData>();
        if (objectData == null || objectData.WaktuMasukBox != DateTime.MinValue) return;

        CancelDeactivation(other.gameObject);
        
        objectCountInThisBox++;
        UpdateCountText();
        
        string finalDestination = boxType.ToString();
        objectData.FinalDestination_Unity = finalDestination;
        objectData.JenisBarang = finalDestination; 
        objectData.WaktuMasukBox = DateTime.Now;
        if (TestManager.Instance != null && TestManager.Instance.loggingMode == TestManager.LoggingMode.Aktif)
{
    if (MasterLogger.Instance != null)
    {
        string accuracyResult = "Error";
        if (objectData.JenisBarang == "Metal")
        {
            if (objectData.MaterialChanged_Unity && objectData.PneumaticSorted_Unity) accuracyResult = "Aman";
        }
        else
        {
            if (!objectData.MaterialChanged_Unity && !objectData.PneumaticSorted_Unity) accuracyResult = "Aman";
        }
        MasterLogger.Instance.LogAccuracy(objectData, objectData.JenisBarang, accuracyResult);
        MasterLogger.Instance.LogCycleTime(objectData);
    }
}
        StartCoroutine(DeactivateObjectAfterDelay(other.gameObject));
    }

    private IEnumerator DeactivateObjectAfterDelay(GameObject objectToDeactivate)
    {
        yield return new WaitForSeconds(deactivateDelay);
        if (runningCoroutines.ContainsKey(objectToDeactivate))
        {
            runningCoroutines.Remove(objectToDeactivate);
        }
        if (objectToDeactivate != null && objectToDeactivate.activeInHierarchy)
        {
            objectToDeactivate.SetActive(false);
        }
    }

    private void UpdateCountText()
    {
        if (countTextDisplay != null) { countTextDisplay.text = objectCountInThisBox.ToString(); }
    }
    
    public void CancelDeactivation(GameObject objectToCancel)
    {
        if (runningCoroutines.TryGetValue(objectToCancel, out Coroutine coroutineToStop))
        {
            StopCoroutine(coroutineToStop);
            runningCoroutines.Remove(objectToCancel);
        }
    }
}