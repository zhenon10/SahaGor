namespace SahaGor.Domain.Enumlar;

/// <summary>SLA arka plan tarama servisinin (SG-410/411) urettigi alarmin turu.</summary>
public enum SlaAlarmTuru
{
    /// <summary>SLA suresi henuz asilmadi ama tuketim esik degerine (orn. %80) ulasti.</summary>
    Yaklasiyor = 1,

    /// <summary>SLA hedef zamani asildi; gorev hala acik.</summary>
    Ihlal = 2
}
