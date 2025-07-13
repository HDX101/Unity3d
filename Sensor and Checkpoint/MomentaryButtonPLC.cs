// File: MomentaryButtonPLC.cs
// Menggabungkan logika momentary button dengan umpan balik visual yang interaktif.
// Script ini mengirimkan sinyal TRUE saat tombol ditekan dan ditahan,
// dan mengirimkan sinyal FALSE saat tombol dilepas.

using UnityEngine;

[RequireComponent(typeof(Collider))] // Memastikan objek ini selalu memiliki Collider
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

    // Variabel privat untuk fungsionalitas
    private Renderer objectRenderer;
    private Material originalMaterial; // Mengganti Color dengan Material
    private Vector3 originalPosition;
    private Vector3 pressedPosition;

    void Start()
    {
        // --- Inisialisasi Komponen & Posisi ---
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material; // Simpan material asli
        }

        originalPosition = transform.localPosition;
        pressedPosition = new Vector3(originalPosition.x, originalPosition.y - pressDepth, originalPosition.z);

        // --- Validasi Koneksi ---
        if (plcSignalSender == null)
        {
            Debug.LogError($"MomentaryButtonPLC: PLCSignalSender belum di-assign pada tombol '{gameObject.name}'. Tombol tidak akan mengirim sinyal.", this);
            enabled = false;
        }
    }

    // Dipanggil SAAT tombol mouse DITEKAN di atas collider
    private void OnMouseDown()
    {
        // Beri umpan balik visual
        transform.localPosition = pressedPosition;
        if (objectRenderer != null && pressedMaterial != null)
        {
            objectRenderer.material = pressedMaterial;
        }

        // Kirim sinyal ON (true) ke PLC
        Debug.Log($"Tombol '{gameObject.name}' DITEKAN. Mengirim TRUE ke {plcAddress}");
        plcSignalSender.SendBooleanCommand(plcAddress, true);
    }

    // Dipanggil SAAT tombol mouse DILEPAS, di mana pun kursor berada
    private void OnMouseUp()
    {
        // Kembalikan posisi dan material tombol
        transform.localPosition = originalPosition;
        if (objectRenderer != null)
        {
            objectRenderer.material = originalMaterial;
        }

        // Kirim sinyal OFF (false) ke PLC
        Debug.Log($"Tombol '{gameObject.name}' DILEPAS. Mengirim FALSE ke {plcAddress}");
        plcSignalSender.SendBooleanCommand(plcAddress, false);
    }
}
