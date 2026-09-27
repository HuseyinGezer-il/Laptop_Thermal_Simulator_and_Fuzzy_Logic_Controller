# Laptop_Thermal_Simulator_and_Fuzzy_Logic_Controller
# Laptop Thermal Simulator and Fuzzy Logic Controller 💻🌡️

Bu proje, bir dizüstü bilgisayarın termal davranışını modelleyen ve işlemci (CPU) sıcaklığını optimize etmek için **Bulanık Mantık (Fuzzy Logic)** kullanan bir simülasyon ve kontrolcü sistemidir. 

Klasik "Aç/Kapat" (Bang-Bang) fan kontrolcüleri ani sıcaklık dalgalanmalarına ve gürültüye neden olurken, bu projedeki bulanık mantık kontrolcüsü sıcaklık ve sıcaklık değişim hızını analiz ederek çok daha pürüzsüz, proaktif ve sessiz bir fan yönetimi sağlar.

## 🚀 Özellikler

* **Termal Dinamik Simülasyonu:** İşlemci yüküne (load) göre ısı üretimini ve soğutma kapasitesini hesaplayan fiziksel modelleme.
* **Bulanık Mantık Kontrolcüsü:** Sadece anlık sıcaklığa değil, ısınma/soğuma trendine (Delta T) göre de karar veren akıllı algoritma.
* **Proaktif Soğutma:** Sıcaklık henüz kritik seviyeye gelmeden, hızlı artış trendi saptandığında fan devrini önceden artırma.
* **Görselleştirme:** Simülasyon boyunca sıcaklık, işlemci yükü ve fan hızının zaman içindeki değişimini gösteren grafikler.

## 🧠 Bulanık Mantık Mimarisi

Sistem, iki temel girdi alarak tek bir çıktı üretir:

### Girdiler (Inputs)
1. **Sıcaklık (Temperature - °C):** 
   * *Kümeler:* Düşük (Low), Normal (Normal), Yüksek (High), Kritik (Critical)
2. **Sıcaklık Değişim Hızı (Delta T - °C/s):** 
   * *Kümeler:* Düşüyor (Falling), Sabit (Steady), Artıyor (Rising), Hızlı Artıyor (Fast Rising)

### Çıktı (Output)
1. **Fan Hızı (Fan Speed - %):**
   * *Kümeler:* Kapalı (Off), Yavaş (Slow), Orta (Medium), Hızlı (Fast), Maksimum (Maximum)

### Örnek Kurallar
* *EĞER* Sıcaklık **Normal** *VE* Delta T **Sabit** *İSE* Fan Hızı **Yavaş** olsun.
* *EĞER* Sıcaklık **Yüksek** *VE* Delta T **Hızlı Artıyor** *İSE* Fan Hızı **Maksimum** olsun. (Proaktif müdahale)
