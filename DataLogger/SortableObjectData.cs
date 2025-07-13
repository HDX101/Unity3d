using UnityEngine;
using System;

public class SortableObjectData : MonoBehaviour
{
    // Variabel untuk Akurasi & ID
    public int ObjectId { get; set; } = 0;
    public bool MaterialChanged_Unity { get; set; } = false;
    public bool PneumaticSorted_Unity { get; set; } = false;
    public string FinalDestination_Unity { get; set; } = "N/A";

    // Variabel untuk Timeline Log
    public string JenisBarang { get; set; }
    public float KecepatanMotorSaatSpawn { get; set; }
    public DateTime WaktuSpawn { get; set; }
    public DateTime WaktuSortir { get; set; }
    public DateTime WaktuMasukBox { get; set; }

    // Variabel yang dibutuhkan oleh DestinationSensor
    public DateTime WaktuMulaiSiklus { get; set; }


    public void Initialize()
    {
        // Reset semua data ke nilai awal
        ObjectId = 0;
        MaterialChanged_Unity = false;
        PneumaticSorted_Unity = false;
        FinalDestination_Unity = "N/A";
        JenisBarang = "Non-Metal";
        KecepatanMotorSaatSpawn = 0f;
        WaktuSpawn = DateTime.MinValue;
        WaktuSortir = DateTime.MinValue;
        WaktuMasukBox = DateTime.MinValue;
        WaktuMulaiSiklus = DateTime.MinValue;
    }
}