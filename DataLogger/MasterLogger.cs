using UnityEngine;
using System.IO;
using System;
using System.Globalization;

public class MasterLogger : MonoBehaviour
{
    public static MasterLogger Instance { get; private set; }

    private string accuracyLogPath;
    private string cycleTimeLogPath;
    private string analogLogPath;
    private string latencyLogPath;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this.gameObject); }
        else { Instance = this; DontDestroyOnLoad(this.gameObject); InitializeAllLogs(); }
    }

    private void InitializeAllLogs()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        accuracyLogPath = Path.Combine(Application.persistentDataPath, $"SortingLog_{timestamp}.csv");
        cycleTimeLogPath = Path.Combine(Application.persistentDataPath, $"TimelineLog_{timestamp}.csv");
        analogLogPath = Path.Combine(Application.persistentDataPath, $"CombinedAnalogLog_{timestamp}.csv");
        latencyLogPath = Path.Combine(Application.persistentDataPath, $"DetailedLatencyLog_{timestamp}.csv");

        try { File.WriteAllText(accuracyLogPath, "WaktuPencatatan;ID_Benda;JenisBenda;MaterialBerubah(Unity);PneumaticSortir(Unity);TujuanAkhir(Unity);HasilAkurasi\n"); }
        catch (Exception e) { Debug.LogError($"Gagal membuat SortingLog: {e.Message}"); }

        try { File.WriteAllText(cycleTimeLogPath, "JenisBarang;KecepatanMotorSaatSpawn;WaktuSpawn;WaktuSortir;WaktuMasukBox;DurasiSiklus(detik)\n"); }
        catch (Exception e) { Debug.LogError($"Gagal membuat TimelineLog: {e.Message}"); }
        
        try { File.WriteAllText(analogLogPath, "WaktuPencatatan;NilaiMentah_D100;Persentase_D100(%);NilaiPWM_D38;Persentase_PWM(%);NilaiConveyor_Unity;PersentaseConveyor_Unity(%)\n"); }
        catch (Exception e) { Debug.LogError($"Gagal membuat CombinedAnalogLog: {e.Message}"); }

        try { File.WriteAllText(latencyLogPath, "SumberEvent;WaktuNodeRED;WaktuDiterimaUnity;LatensiJaringan(ms);WaktuAksiScript;LatensiInternal(ms)\n"); }
        catch (Exception e) { Debug.LogError($"Gagal membuat DetailedLatencyLog: {e.Message}"); }
    }

    public void LogAccuracy(SortableObjectData data, string actualObjectType, string accuracyResult)
    {
        try
        {
            string line = string.Format(CultureInfo.InvariantCulture, "{0};{1};{2};{3};{4};{5};{6}\n",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                data.ObjectId,
                actualObjectType,
                data.MaterialChanged_Unity ? "Ya" : "Tidak",
                data.PneumaticSorted_Unity ? "Ya" : "Tidak",
                data.FinalDestination_Unity,
                accuracyResult
            );
            File.AppendAllText(accuracyLogPath, line);
        }
        catch (Exception e) { Debug.LogError($"Gagal menulis ke SortingLog: {e.Message}"); }
    }

    public void LogCycleTime(SortableObjectData data)
    {
        try
        {
            string waktuSortirStr = (data.WaktuSortir == DateTime.MinValue) ? "N/A" : data.WaktuSortir.ToString("HH:mm:ss.fff");
            double durasiDetik = (data.WaktuMasukBox - data.WaktuSpawn).TotalSeconds;

            string line = string.Format(CultureInfo.InvariantCulture, "{0};{1:F2};{1};{2};{3};{4:F3}\n",
                data.JenisBarang,
                data.KecepatanMotorSaatSpawn,
                data.WaktuSpawn.ToString("HH:mm:ss.fff"),
                waktuSortirStr,
                data.WaktuMasukBox.ToString("HH:mm:ss.fff"),
                durasiDetik
            );
            File.AppendAllText(cycleTimeLogPath, line);
        }
        catch (Exception e) { Debug.LogError($"Gagal menulis ke TimelineLog: {e.Message}"); }
    }

    public void LogAnalog(int d100_raw, float d100_percent, int d38_raw, float d38_percent, float conveyor_speed, float conveyor_percent)
    {
        try
        {
            string line = string.Format(CultureInfo.InvariantCulture, "{0};{1};{2:F2};{3};{4:F2};{5:F2};{6:F2}\n",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                d100_raw, d100_percent, d38_raw, d38_percent, conveyor_speed, conveyor_percent
            );
            File.AppendAllText(analogLogPath, line);
        }
        catch (Exception e) { Debug.LogError($"Gagal menulis ke CombinedAnalogLog: {e.Message}"); }
    }

    public void LogLatency(string eventSource, long nodeRedTimestamp, long unityReceiptTimestamp, long scriptActionTimestamp)
    {
        try
        {
            long networkLatency = unityReceiptTimestamp - nodeRedTimestamp;
            long internalLatency = scriptActionTimestamp - unityReceiptTimestamp;
            string nodeRedTimeStr = DateTimeOffset.FromUnixTimeMilliseconds(nodeRedTimestamp).LocalDateTime.ToString("HH:mm:ss.fff");
            string unityReceiptTimeStr = DateTimeOffset.FromUnixTimeMilliseconds(unityReceiptTimestamp).LocalDateTime.ToString("HH:mm:ss.fff");
            string scriptActionTimeStr = DateTimeOffset.FromUnixTimeMilliseconds(scriptActionTimestamp).LocalDateTime.ToString("HH:mm:ss.fff");

            string line = string.Format("{0};{1};{2};{3};{4};{5}\n",
                eventSource, nodeRedTimeStr, unityReceiptTimeStr, networkLatency, scriptActionTimeStr, internalLatency
            );
            File.AppendAllText(latencyLogPath, line);
        }
        catch (Exception e) { Debug.LogError($"Gagal menulis ke DetailedLatencyLog: {e.Message}"); }
    }
}