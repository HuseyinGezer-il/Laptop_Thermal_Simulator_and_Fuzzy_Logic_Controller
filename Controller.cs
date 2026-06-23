using System;

namespace ISIControl
{
    public enum CoolingMode
    {
        Auto,
        Quiet,
        Performance
    }

    public struct ControlResult
    {
        public float FanPwm;
        public int ThrottlingLevel;
    }

    public class Controller
    {
        // --- SİSTEM AYARLARI ---
        private const float FAN_DEADBAND = 5.0f;
        private const float FAN_DECAY_STEP = 2.0f;
        private const float DERIVATIVE_TIME_CONSTANT = 0.5f;

        // Acil durum için sabitler (Modlardan etkilenmez)
        private const int THROTTLE_EMERGENCY = 3;
        private const float FAN_MAX_EMERGENCY = 100.0f;

        // --- DURUM DEĞİŞKENLERİ ---
        private float _previousTemperature = -1.0f;
        private float _currentFanPwm = 30.0f;
        private float _filteredDerivative = 0.0f;
        private int _currentThrottlingLevel = 0;

        // Mod Seçimi (Dışarıdan anlık olarak değiştirilebilir)
        public CoolingMode Mode { get; set; } = CoolingMode.Auto;

        public ControlResult Update(float currentTemperature, float deltaTimeSeconds)
        {
            if (_previousTemperature < 0.0f)
            {
                _previousTemperature = currentTemperature;
            }

            // 1. Türev Filtreleme (EMA Filtresi)
            float raw_dT_dt = 0.0f;
            if (deltaTimeSeconds > 0.001f)
            {
                raw_dT_dt = (currentTemperature - _previousTemperature) / deltaTimeSeconds;
            }
            float alpha = deltaTimeSeconds / (DERIVATIVE_TIME_CONSTANT + deltaTimeSeconds);
            _filteredDerivative = (alpha * raw_dT_dt) + ((1.0f - alpha) * _filteredDerivative);

            // 2. Bulanıklaştırma (Fuzzification)
            float mu_T_serin = LeftShoulderMembership(currentTemperature, 40f, 50f);
            float mu_T_ilik = TriangularMembership(currentTemperature, 45f, 60f, 75f);
            float mu_T_sicak = TriangularMembership(currentTemperature, 70f, 80f, 90f);
            //float mu_T_kritik = (currentTemperature >= 85f) ? 1.0f : 0.0f;
            float mu_T_kritik = 0.0f;
            if (currentTemperature > 80f && currentTemperature < 85f)
            {
                mu_T_kritik = (currentTemperature - 80f) / 5.0f;
            }
            else if (currentTemperature >= 85f)
            {
                mu_T_kritik = 1.0f;
            }

            float mu_dT_sabit = TriangularMembership(_filteredDerivative, -5.0f, 0.0f, 1.5f);
            float mu_dT_hizli = TriangularMembership(_filteredDerivative, 1.0f, 2.0f, 5.0f);

            // --- 3. MODA GÖRE ÇIKIŞ SABİTLERİNİN (SINGLETONS) AYARLANMASI ---
            float fanStop = 0.0f, fanLow = 30.0f, fanMed = 65.0f, fanMax = 100.0f;
            int throtNone = 0, throtLight = 1, throtHard = 2;

            switch (Mode)
            {
                case CoolingMode.Quiet:
                    fanLow = 20.0f;  // Fan daha yavaş başlar
                    fanMed = 45.0f;  // Orta seviyede daha az ses
                    fanMax = 70.0f;  // MAKSİMUM fan hızına %70 limiti
                    throtLight = 2;  // Fan yavaş olacağı için daha erken sert kısma yap
                    throtHard = 3;   // Sıcaklık artarsa hemen acil kısmaya geç
                    break;

                case CoolingMode.Performance:
                    fanLow = 45.0f;  // Fan hiç durmaz/yavaşlamaz, hep hazır bekler
                    fanMed = 80.0f;  // Orta sıcaklıkta bile fırtına koparır
                    fanMax = 100.0f; // Tam hız
                    throtLight = 0;  // Sıcaklık artınca gücü kısmayı reddet (Performans düşmesin)
                    throtHard = 1;   // Sadece zorunlu kalırsa çok hafif kıs
                    break;

                case CoolingMode.Auto:
                default:
                    // Sınıf başındaki varsayılan dengeli ayarlar korunur
                    break;
            }

            // --- 4. KURAL TABANI VE ÇIKARIM ---

            float w_Serin = FuzzyAnd(mu_T_serin, mu_dT_sabit);
            float f_Serin = fanStop;
            int t_Serin = throtNone;

            float w_IlikSabit = FuzzyAnd(mu_T_ilik, mu_dT_sabit);
            float f_IlikSabit = fanLow;
            int t_IlikSabit = throtNone;

            float w_IlikHizli = FuzzyAnd(mu_T_ilik, mu_dT_hizli);
            float f_IlikHizli = fanMed;
            int t_IlikHizli = throtNone;

            float w_SicakSabit = FuzzyAnd(mu_T_sicak, mu_dT_sabit);
            float f_SicakSabit = fanMed;
            int t_SicakSabit = throtLight;

            float w_SicakHizli = FuzzyAnd(mu_T_sicak, mu_dT_hizli);
            float f_SicakHizli = fanMax;
            int t_SicakHizli = throtHard;

            float w_Kritik = mu_T_kritik;
            float f_Kritik = FAN_MAX_EMERGENCY;
            int t_Kritik = THROTTLE_EMERGENCY; // 85 Derece üstünde mod dinlenmez, işlemci kurtarılır

            // 5. Durulaştırma (Defuzzification)
            float totalWeight = w_Serin + w_IlikSabit + w_IlikHizli + w_SicakSabit + w_SicakHizli + w_Kritik;
            float targetFanPwm = fanLow;
            float targetThrot = throtNone;

            if (totalWeight > 0.001f)
            {
                targetFanPwm = (w_Serin * f_Serin +
                                w_IlikSabit * f_IlikSabit +
                                w_IlikHizli * f_IlikHizli +
                                w_SicakSabit * f_SicakSabit +
                                w_SicakHizli * f_SicakHizli +
                                w_Kritik * f_Kritik) / totalWeight;

                targetThrot = (w_Serin * t_Serin +
                               w_IlikSabit * t_IlikSabit +
                               w_IlikHizli * t_IlikHizli +
                               w_SicakSabit * t_SicakSabit +
                               w_SicakHizli * t_SicakHizli +
                               w_Kritik * t_Kritik) / totalWeight;
            }

            // 6. Asimetrik Filtre (Dalgalanma Önleyici)
            float finalFanPwm = ApplyAsymmetricFilter(targetFanPwm, _currentFanPwm);

            // YENİ EKLENEN THROTTLING HİSTEREZİS (GECİKME) KİLİDİ
            int newThrottleTarget = (int)Math.Round(targetThrot);

            if (newThrottleTarget > _currentThrottlingLevel)
            {
                // Sistem ısınıyor: Gücü kısmak için bekleme, hemen uygula (Donanımı koru)
                _currentThrottlingLevel = newThrottleTarget;
            }
            else if (newThrottleTarget < _currentThrottlingLevel)
            {
                // Sistem soğuyor: Gücü geri vermek için sıcaklığın 80C'nin altına düşmesini bekle
                if (currentTemperature < 80.0f)
                {
                    _currentThrottlingLevel = newThrottleTarget;
                }
                // 80'in altına düşmediyse, kısma seviyesini (Throttle) korumaya devam et!
            }

            // 7. Durumları Güncelle
            _previousTemperature = currentTemperature;
            _currentFanPwm = finalFanPwm;

            return new ControlResult
            {
                FanPwm = (float)Math.Round(finalFanPwm, 1),
                //ThrottlingLevel = (int)Math.Round(targetThrot)
                ThrottlingLevel = _currentThrottlingLevel // Kilitli seviyeyi gönder
            };
        }

        private float ApplyAsymmetricFilter(float target, float current)
        {
            if (target > (current + FAN_DEADBAND))
            {
                return target;
            }
            else if (target < (current - FAN_DEADBAND))
            {
                float newSpeed = current - FAN_DECAY_STEP;
                return (newSpeed < target) ? target : newSpeed;
            }
            return current;
        }

        private float LeftShoulderMembership(float x, float a, float b)
        {
            if (x <= a) return 1.0f;
            if (x >= b) return 0.0f;
            return (b - x) / (b - a);
        }

        private float TriangularMembership(float x, float a, float b, float c)
        {
            if (x <= a || x >= c) return 0.0f;
            if (a < x && x <= b) return (x - a) / (b - a);
            if (b < x && x < c) return (c - x) / (c - b);
            return 0.0f;
        }

        private float FuzzyAnd(float a, float b)
        {
            return (a <= b) ? a : b;
        }
    }
}