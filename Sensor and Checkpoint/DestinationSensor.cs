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
        
        // --- PERBAIKAN UTAMA DI SINI ---
        // Langsung tentukan dan atur nilai final begitu objek terdeteksi.
        string finalDestination = boxType.ToString();
        objectData.FinalDestination_Unity = finalDestination;
        objectData.JenisBarang = finalDestination; // Jenis Barang juga ditentukan oleh tujuan akhir
        objectData.WaktuMasukBox = DateTime.Now;
        // ---------------------------------

        // Lakukan logging hanya jika mode tes aktif
        if (TestManager.Instance != null && TestManager.Instance.loggingMode == TestManager.LoggingMode.Aktif)
{
    if (MasterLogger.Instance != null)
    {
        // ... (logika hitung akurasi) ...
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