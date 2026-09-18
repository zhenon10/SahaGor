namespace SahaGor.Infrastructure.Gorevler;

/// <summary>appsettings.json'dan okunan SLA alarm tarama yapilandirmasi (SG-410, SG-411).</summary>
public sealed class SlaAlarmAyarlari
{
    public const string BolumAdi = "SlaAlarm";

    /// <summary>SLA suresinin yuzde kaci tuketildiginde "yaklasiyor" alarminin tetiklenecegi esik (0-100).</summary>
    public double YaklasmaEsikYuzdesi { get; set; } = 80;

    /// <summary>Arka plan tarama servisinin iki tarama arasinda bekleyecegi sure (saniye).</summary>
    public int TaramaAraligiSaniye { get; set; } = 60;

    public void Dogrula()
    {
        if (YaklasmaEsikYuzdesi is <= 0 or >= 100)
        {
            throw new InvalidOperationException(
                $"'{BolumAdi}:YaklasmaEsikYuzdesi' 0 ile 100 arasinda (haric) bir deger olmalidir.");
        }

        if (TaramaAraligiSaniye <= 0)
        {
            throw new InvalidOperationException($"'{BolumAdi}:TaramaAraligiSaniye' pozitif bir deger olmalidir.");
        }
    }
}
