using UnityEngine;

public class TestManager : MonoBehaviour
{
    // Enum untuk pilihan mode logging
    public enum LoggingMode { Nonaktif, Aktif }

    public static TestManager Instance { get; private set; }

    [Header("Mode Pencatatan Data")]
    [Tooltip("Pilih mode untuk mengaktifkan atau menonaktifkan semua logger.")]
    public LoggingMode loggingMode = LoggingMode.Aktif;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
}