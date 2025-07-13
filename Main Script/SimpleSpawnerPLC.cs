using UnityEngine;
using System;

public class SimpleSpawnerPLC : MonoBehaviour
{
    [Header("Konfigurasi Spawner")]
    public GameObject objectPrefab;

    [Header("Kontrol PLC")]
    public PLCInputManager plcInputManager;
    public string spawnTriggerAddress = "W50.01";

    [Header("Referensi Sistem")]
    public IntegratedConveyor conveyor;

    private bool plcSpawnTriggerWasActiveLastFrame = false;
    private static int objectIdCounter = 1;

    void Start()
    {
        if (objectPrefab == null || plcInputManager == null || conveyor == null)
        {
            Debug.LogError("SimpleSpawnerPLC: Referensi belum di-assign!", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (plcInputManager == null) return;
        bool plcIsOn = plcInputManager.GetBoolState(spawnTriggerAddress);

        if (plcIsOn && !plcSpawnTriggerWasActiveLastFrame)
        {
            SpawnObject();
        }
        plcSpawnTriggerWasActiveLastFrame = plcIsOn;
    }

    private void SpawnObject()
    {
        if (objectPrefab == null) return;

        GameObject newObject = Instantiate(objectPrefab, transform.position, transform.rotation);
        var data = newObject.GetComponent<SortableObjectData>();

        if (data != null)
        {
            data.Initialize();
            
            data.ObjectId = objectIdCounter++;

            bool isMetal = (UnityEngine.Random.Range(0, 2) == 0);
            data.JenisBarang = isMetal ? "Metal" : "Non-Metal";
            data.KecepatanMotorSaatSpawn = conveyor.CurrentSpeed;
            data.WaktuSpawn = DateTime.Now;
        }
    }
}