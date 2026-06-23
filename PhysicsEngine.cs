namespace ISIControl
{
    /*public class PhysicsEngine
    {
        // İşlemci Güç Sınırları (Watt)
        private const float P_IDLE = 5.0f;
        private const float P_MAX = 115.0f;

        // Fiziksel evrenin anlık sıcaklığı (Ground Truth)
        private float _currentTemperature = SimState.AmbientTemperature;

        public float CalculateNextTemperature(float currentFanPwm, float deltaTime, float CpuLoad)
        {
            float t_amb = SimState.AmbientTemperature;

            if (_currentTemperature < t_amb) _currentTemperature = t_amb;

            // 1. Üretilen Isı (Q_in)
            float q_in = P_IDLE + (CpuLoad / 100.0f) * (P_MAX - P_IDLE);

            // 2. Termal Devre (Seri Dirençler Modeli)
            float r_iletim = SimState.ThermalResistance; // Macun + Bakır (Arayüzden gelen)

            // Alternatif olarak, taşınım direnci hesaplanmasında üssel bir azalma modeli de kullanılabilir:
            //float r_tasinim = SimState.r_Min - (SimState.r_Max - SimState.r_Min) / (Math.Exp(SimState.sogutma_K * (currentFanPwm / 100.0f) * SimState.FanMaxCfm));

            //r_Max, fan kapalıyken ve işlemci sabit düşük bir yükte iken sıcaklığın sabitlendiği noktada
            //(((işlemci sıcaklığı - ortam sıcaklığı) / işlemci güç tüketimi) - r_iletim) olarak tanımlanabilir.
            //r_Min ise fan tam hızda ve işlemci sabit yüksek bir yükte iken sıcaklığın sabitlendiği noktada yine aynı formül kullanılarak gözlemlenen dirençtir.

            // Karma Taşınım Direnci Modeli: Fan kapalıyken yüksek, tam hızda düşük bir direnç verir
            float r_tasinim = 1 / ((1 / SimState.R_max) + (SimState.Sogutma_K * (float)Math.Pow((currentFanPwm / 100.0f) * SimState.FanMaxCfm, 0.8f)));

            // Toplam Isı Direnci (K/W)
            float r_toplam = r_iletim + r_tasinim;

            // 3. Havaya Atılan Isı (Q_out)
            float q_out = (_currentTemperature - t_amb) / r_toplam;

            // 4. Termodinamik Diferansiyel Denklem (Delta T)
            float c_th = SimState.HeatCapacity;
            float deltaT = ((q_in - q_out) / c_th) * deltaTime;

            // 5. Saf Fiziksel Sıcaklığı Güncelle
            _currentTemperature += deltaT;

            // Madde sınırları
            if (_currentTemperature > 110.0f) _currentTemperature = 110.0f;
            if (_currentTemperature < t_amb) _currentTemperature = t_amb;


            return _currentTemperature;
        }
    }*/


    public class PhysicsEngine
    {
        // İşlemci Güç Sınırları (Watt)
        private const float P_IDLE = 5.0f;
        private const float P_MAX = 115.0f;

        // --- 2-DÜĞÜM (NODE) FİZİKSEL SABİTLERİ ---
        // Silikon çekirdeğin (Die) ısı sığası. 
        // Sadece birkaç gram olduğu için çok küçüktür (2 Joule/Kelvin).
        // Bu küçük değer, sıcaklığın ani fırlamasını (spike) sağlayacak.
        private const float C_SILICON = 2.0f;

        // --- ANLIK DURUM DEĞİŞKENLERİ (Sistem Hafızası) ---
        private float _currentSiliconTemp;
        private float _currentBakirTemp;
        private bool _isInitialized = false;

        public float CalculateNextTemperature(float currentFanPwm, float deltaTime, float effectiveCpuLoad)
        {
            float t_amb = SimState.AmbientTemperature;

            // İlk çalıştırmada sıcaklıkları ortam sıcaklığına eşitle
            if (!_isInitialized)
            {
                _currentSiliconTemp = t_amb;
                _currentBakirTemp = t_amb;
                _isInitialized = true;
            }

            // Ortam sıcaklığı arayüzden aniden düşürülürse taban sınırını koru
            if (_currentSiliconTemp < t_amb) _currentSiliconTemp = t_amb;
            if (_currentBakirTemp < t_amb) _currentBakirTemp = t_amb;

            // ----------------------------------------------------------------
            // 1. ADIM: İŞLEMCİDE ÜRETİLEN ISI (Q_in -> Doğrudan Silikona girer)
            // ----------------------------------------------------------------
            float q_in = P_IDLE + (effectiveCpuLoad / 100.0f) * (P_MAX - P_IDLE);

            // ----------------------------------------------------------------
            // 2. ADIM: SİLİKONDAN BAKIRA GEÇEN ISI (Q_silikon_to_bakir)
            // İki blok arasındaki iletim direncini (Termal Macun) kullanır.
            // ----------------------------------------------------------------
            float r_iletim = SimState.ThermalResistance; // Arayüzdeki "Toplam Isı Direnci" kutusu
            float q_silikon_to_bakir = (_currentSiliconTemp - _currentBakirTemp) / r_iletim;

            // ----------------------------------------------------------------
            // 3. ADIM: BAKIRDAN HAVAYA ATILAN ISI (Q_bakir_to_air)
            // Fan hızına bağlı dinamik taşınım direncini kullanır (Senin formülün).
            // ----------------------------------------------------------------
            float mevcutCfm = (currentFanPwm / 100.0f) * SimState.FanMaxCfm;
            float r_tasinim = 1.0f / ((1.0f / SimState.R_max) + (SimState.Sogutma_K * (float)Math.Pow(mevcutCfm, 0.8f)));

            float q_bakir_to_air = (_currentBakirTemp - t_amb) / r_tasinim;

            // ----------------------------------------------------------------
            // 4. ADIM: 1. DÜĞÜM (SİLİKON) DİFERANSİYEL DENKLEMİ
            // Enerji dengesi: Üretilen ısı eksi bakıra aktarılan ısı
            // ----------------------------------------------------------------
            float deltaT_silikon = ((q_in - q_silikon_to_bakir) / C_SILICON) * deltaTime;
            _currentSiliconTemp += deltaT_silikon;

            // ----------------------------------------------------------------
            // 5. ADIM: 2. DÜĞÜM (BAKIR BLOK) DİFERANSİYEL DENKLEMİ
            // Enerji dengesi: Silikondan gelen ısı eksi havaya fırlatılan ısı
            // ----------------------------------------------------------------
            float c_bakir = SimState.HeatCapacity; // Arayüzdeki "Toplam Isı Sığası" kutusu
            float deltaT_bakir = ((q_silikon_to_bakir - q_bakir_to_air) / c_bakir) * deltaTime;
            _currentBakirTemp += deltaT_bakir;

            // ----------------------------------------------------------------
            // 6. ADIM: GÜVENLİK SINIRLARI VE ÇIKTI
            // ----------------------------------------------------------------
            if (_currentSiliconTemp > 125.0f) _currentSiliconTemp = 125.0f;
            if (_currentSiliconTemp < t_amb) _currentSiliconTemp = t_amb;
            if (_currentBakirTemp > 125.0f) _currentBakirTemp = 125.0f;
            if (_currentBakirTemp < t_amb) _currentBakirTemp = t_amb;

            SimState.CurrentBakirTemperature = _currentBakirTemp;

            // Kontrolcüye ve Arayüze "Silikon Çekirdek" sıcaklığını raporluyoruz
            return _currentSiliconTemp;
        }
    }
}