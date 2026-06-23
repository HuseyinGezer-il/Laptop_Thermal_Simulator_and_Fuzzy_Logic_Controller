using System;

namespace ISIControl
{

    // UI'a gönderilecek verilerin paketi
    public class ThermalUpdateState
    {
        public float CurrentTemperature { get; set; }
        public float FanPwm { get; set; }
        public int ThrottlingLevel { get; set; }
        public CoolingMode CurrentMode { get; set; }
        public float EffectiveCpuLoad { get; set; }
        public float BakirTemperature { get; set; }
    }
}
