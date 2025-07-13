using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider))]
public class MaterialChanger : MonoBehaviour
{
    [Header("Pengaturan Material")]
    [Tooltip("Material baru yang akan diterapkan pada objek.")]
    [SerializeField]
    private Material newMaterial;

    private readonly List<GameObject> objectsReadyToChange = new List<GameObject>();

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PushableObject"))
        {
            if (!objectsReadyToChange.Contains(other.gameObject))
            {
                objectsReadyToChange.Add(other.gameObject);
            }
        }
        else if (other.CompareTag("PneumaticCylinder"))
        {
            foreach (GameObject objToChange in objectsReadyToChange.ToList())
            {
                if (objToChange != null)
                {
                    ApplyMaterialChange(objToChange);
                }
            }
            objectsReadyToChange.Clear();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PushableObject"))
        {
            objectsReadyToChange.Remove(other.gameObject);
        }
    }

    private void ApplyMaterialChange(GameObject target)
{
    if (target == null || newMaterial == null) return;

    Renderer objectRenderer = target.GetComponent<Renderer>();
    if (objectRenderer != null)
    {
        objectRenderer.material = newMaterial;
        var objectData = target.GetComponentInParent<SortableObjectData>();
        if (objectData != null)
        {
            objectData.MaterialChanged_Unity = true;
            // --- BARIS INI DIHAPUS ---
            // objectData.WaktuSortir = System.DateTime.Now; 
        }
    }
}
}