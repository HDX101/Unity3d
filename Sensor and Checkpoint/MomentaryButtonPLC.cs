using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MomentaryButtonPLC : MonoBehaviour
{
    [Header("Koneksi PLC")]
    [Tooltip("Seret GameObject yang memiliki script PLCSignalSender ke sini.")]
    [SerializeField] private PLCSignalSender plcSignalSender;

    [Tooltip("Alamat bit di PLC yang akan dikontrol oleh tombol ini. Contoh: W51.01")]
    [SerializeField] private string plcAddress = "W51.01";

    [Header("Visual & Fisik")]
    [Tooltip("Material yang digunakan saat tombol sedang ditekan.")]
    [SerializeField] private Material pressedMaterial;

    [Tooltip("Seberapa 'dalam' tombol akan bergerak pada sumbu lokal Y saat ditekan.")]
    [SerializeField] private float pressDepth = 0.05f;

    private Renderer objectRenderer;
    private Material originalMaterial; 
    private Vector3 originalPosition;
    private Vector3 pressedPosition;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        originalPosition = transform.localPosition;
        pressedPosition = new Vector3(originalPosition.x, originalPosition.y - pressDepth, originalPosition.z);

        if (plcSignalSender == null)
        {
            Debug.LogError($"MomentaryButtonPLC: PLCSignalSender belum di-assign pada tombol '{gameObject.name}'. Tombol tidak akan mengirim sinyal.", this);
            enabled = false;
        }
    }

    private void OnMouseDown()
    {
        transform.localPosition = pressedPosition;
        if (objectRenderer != null && pressedMaterial != null)
        {
            objectRenderer.material = pressedMaterial;
        }

        Debug.Log($"Tombol '{gameObject.name}' DITEKAN. Mengirim TRUE ke {plcAddress}");
        plcSignalSender.SendBooleanCommand(plcAddress, true);
    }

    private void OnMouseUp()
    {
        transform.localPosition = originalPosition;
        if (objectRenderer != null)
        {
            objectRenderer.material = originalMaterial;
        }

        Debug.Log($"Tombol '{gameObject.name}' DILEPAS. Mengirim FALSE ke {plcAddress}");
        plcSignalSender.SendBooleanCommand(plcAddress, false);
    }
}
