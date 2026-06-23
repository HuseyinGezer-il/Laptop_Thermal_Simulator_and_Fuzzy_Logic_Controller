using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ISIControl
{

    public static class ThermalEngine
    {

        // Bu metot, fizik motoru ve kontrolcüyü sırayla çalıştırır.
        /*public static async Task StartEngineAsync(float kontrolSikligi, IProgress<ThermalUpdateState> progress, CancellationToken cancellationToken)
        {
            Controller termalKontrol = new Controller();
            PhysicsEngine fizikMotoru = new PhysicsEngine();
            //TempSensor tempSensor = new TempSensor();
            //MechanicFans mechanicFan = new MechanicFans();

            float lastFanPwm = 0;
            float effectiveCpuLoad = 10;

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(kontrolSikligi));
            try
            {
                // CancellationToken ile döngüyü dışarıdan durdurulabilir yapıyoruz
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    // Döngü her döndüğünde arayüzden gelen son modu kontrolcüye aktar
                    termalKontrol.Mode = SimState.TargetMode;

                    // Fizik Motoruna mevcut fan hızını ve geçen zamanı verip yeni sıcaklığı alıyoruz
                    float currentTemp = fizikMotoru.CalculateNextTemperature(lastFanPwm, kontrolSikligi, effectiveCpuLoad);

                    // Bulanık Mantık Kontrolcüsü Karar Veriyor
                    ControlResult result = termalKontrol.Update(currentTemp, kontrolSikligi);

                    //mechanicFan.SetFanPwm(result.FanPwm);
                    lastFanPwm = result.FanPwm;
                    float maxAllowedLoad = 100.0f - (SimState.CurrentThrottlingLevel * 10.0f);
                    effectiveCpuLoad = Math.Min(SimState.CpuLoad, maxAllowedLoad);

                    // UI'a gönderilecek paketi hazırla
                    if (progress != null)
                    {
                        var state = new ThermalUpdateState
                        {
                            CurrentTemperature = currentTemp,
                            FanPwm = result.FanPwm,
                            ThrottlingLevel = result.ThrottlingLevel,
                            CurrentMode = termalKontrol.Mode,
                            EffectiveCpuLoad = effectiveCpuLoad,
                        };

                        // Bu metot, veriyi UI Thread'ine güvenle fırlatır
                        progress.Report(state);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Motor dışarıdan durduruldu (Normal kapanış)
            }
        }*/


        // Bu metot, fizik motoru ve kontrolcüyü paralel çalıştırır. Ama Windows Zamanlayı Tuzağı nedeniyle hatalıdır.
        /*public static async Task StartEngineAsync(float kontrolSikligi, IProgress<ThermalUpdateState> progress, CancellationToken cancellationToken)
        {
            Controller termalKontrol = new Controller();
            PhysicsEngine fizikMotoru = new PhysicsEngine();

            // Fizik motorunun çözünürlüğü: 1 milisaniye (0.001s). Doğaya çok yakın.
            float fizikAdimiSaniye = 0.001f;

            // 1. GÖREV: FİZİK MOTORU DÖNGÜSÜ (Çok hızlı döner)
            var physicsTask = Task.Run(async () =>
            {
                using var physTimer = new PeriodicTimer(TimeSpan.FromSeconds(fizikAdimiSaniye));
                try
                {
                    while (await physTimer.WaitForNextTickAsync(cancellationToken))
                    {
                        // Kontrolcünün belirlediği kısıtlamayı (Throttling) al
                        float maxAllowedLoad = 100.0f - (SimState.CurrentThrottlingLevel * 10.0f);
                        float effectiveLoad = Math.Min(SimState.CpuLoad, maxAllowedLoad);

                        // Fizik motorunu 1 milisaniye işlet ve evrenin yeni sıcaklığını kaydet
                        SimState.CurrentTemperature = fizikMotoru.CalculateNextTemperature(SimState.CurrentFanPwm, fizikAdimiSaniye, effectiveLoad);
                    }
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);

            // 2. GÖREV: KONTROLCÜ VE ARAYÜZ DÖNGÜSÜ (100ms de bir uyanır)
            var controlTask = Task.Run(async () =>
            {
                using var ctrlTimer = new PeriodicTimer(TimeSpan.FromSeconds(kontrolSikligi));
                try
                {
                    while (await ctrlTimer.WaitForNextTickAsync(cancellationToken))
                    {
                        termalKontrol.Mode = SimState.TargetMode;

                        // Fizik motorunun hesapladığı anlık sıcaklığı oku
                        float okunanSicaklik = SimState.CurrentTemperature;

                        // Bulanık mantık kararı veriyor
                        ControlResult result = termalKontrol.Update(okunanSicaklik, kontrolSikligi);

                        // Yeni kararları fizik motorunun ve donanımın görebileceği hafızaya yaz
                        SimState.CurrentFanPwm = result.FanPwm;
                        SimState.CurrentThrottlingLevel = result.ThrottlingLevel;

                        // Arayüze paketi yolla
                        if (progress != null)
                        {
                            var state = new ThermalUpdateState
                            {
                                CurrentTemperature = okunanSicaklik,
                                FanPwm = result.FanPwm,
                                ThrottlingLevel = result.ThrottlingLevel,
                                CurrentMode = termalKontrol.Mode,
                                EffectiveCpuLoad = Math.Min(SimState.CpuLoad, 100.0f - (result.ThrottlingLevel * 10.0f))
                            };
                            progress.Report(state);
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);

            // İki motorun da paralel çalışmasını ve kapanma emri geldiğinde güvenle durmasını sağla
            await Task.WhenAll(physicsTask, controlTask);
        }*/


        // Bu metot, fizik motoru ve kontrolcüyü paralel çalıştırır. Windows Zamanlayıcı tuzağına karşı gerçek zamanlı bir kronometre kullanır.
        public static async Task StartEngineAsync(float kontrolSikligi, IProgress<ThermalUpdateState> progress, CancellationToken cancellationToken)
        {
            Controller termalKontrol = new Controller();
            PhysicsEngine fizikMotoru = new PhysicsEngine();

            // Fizik döngüsünü Windows'un rahat nefes alacağı 10ms (saniyede 100 kez) seviyesine çekiyoruz.
            using var physTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));
            using var ctrlTimer = new PeriodicTimer(TimeSpan.FromSeconds(kontrolSikligi));

            // Donanımsal, yüksek çözünürlüklü gerçek zaman kronometresi
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // 1. GÖREV: FİZİK MOTORU DÖNGÜSÜ (Gerçek Zaman Senkronizasyonlu)
            var physicsTask = Task.Run(async () =>
            {
                try
                {
                    while (await physTimer.WaitForNextTickAsync(cancellationToken))
                    {
                        // İki döngü arasında GERÇEKTE geçen süreyi saniye cinsinden ölçüyoruz
                        float gecenGercekSure = (float)stopwatch.Elapsed.TotalSeconds;
                        stopwatch.Restart();

                        // İşletim sistemi anlık donarsa devasa bir adım atıp simülasyonu patlatmasın diye üst limit
                        if (gecenGercekSure > 0.1f) gecenGercekSure = 0.1f;

                        float maxAllowedLoad = 100.0f - (SimState.CurrentThrottlingLevel * 10.0f);
                        float effectiveLoad = Math.Min(SimState.CpuLoad, maxAllowedLoad);

                        // Sabit 0.001f yerine, akan GERÇEK zamanı diferansiyel denkleme gönderiyoruz
                        SimState.CurrentTemperature = fizikMotoru.CalculateNextTemperature(SimState.CurrentFanPwm, gecenGercekSure, effectiveLoad);
                    }
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);

            // 2. GÖREV: KONTROLCÜ VE ARAYÜZ DÖNGÜSÜ (100ms)
            var controlTask = Task.Run(async () =>
            {
                try
                {
                    while (await ctrlTimer.WaitForNextTickAsync(cancellationToken))
                    {
                        termalKontrol.Mode = SimState.TargetMode;
                        float okunanSicaklik = SimState.CurrentTemperature;

                        ControlResult result = termalKontrol.Update(okunanSicaklik, kontrolSikligi);

                        SimState.CurrentFanPwm = result.FanPwm;
                        SimState.CurrentThrottlingLevel = result.ThrottlingLevel;

                        if (progress != null)
                        {
                            var state = new ThermalUpdateState
                            {
                                CurrentTemperature = okunanSicaklik,
                                BakirTemperature = SimState.CurrentBakirTemperature, // Bakır sıcaklığı da UI'a gönderiliyor
                                FanPwm = result.FanPwm,
                                ThrottlingLevel = result.ThrottlingLevel,
                                CurrentMode = termalKontrol.Mode,
                                EffectiveCpuLoad = Math.Min(SimState.CpuLoad, 100.0f - (result.ThrottlingLevel * 10.0f))
                            };
                            progress.Report(state);
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);

            await Task.WhenAll(physicsTask, controlTask);
        }


    }

}
