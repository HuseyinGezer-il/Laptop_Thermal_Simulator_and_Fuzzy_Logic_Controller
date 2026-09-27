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

# English --------------------------------

This project is a simulation and controller system that models the thermal behavior of a laptop and uses **Fuzzy Logic** to optimize the processor (CPU) temperature. 

While classic "On/Off" (Bang-Bang) fan controllers cause sudden temperature fluctuations and noise, the fuzzy logic controller in this project analyzes both the temperature and its rate of change to provide a much smoother, proactive, and quiet fan management.

## 🚀 Features

* **Thermal Dynamics Simulation:** Physical modeling that calculates heat generation and cooling capacity based on the CPU load.
* **Fuzzy Logic Controller:** A smart algorithm that makes decisions based not only on the current temperature but also on the heating/cooling trend (Delta T).
* **Proactive Cooling:** Preemptively increases fan speed when a rapid heating trend is detected, before the temperature reaches critical levels.
* **Visualization:** Graphs showing the variation of temperature, CPU load, and fan speed over time throughout the simulation.

## 🧠 Fuzzy Logic Architecture

The system takes two main inputs and produces a single output:

### Inputs
1. **Temperature (°C):** 
   * *Sets:* Low, Normal, High, Critical
2. **Rate of Temperature Change (Delta T - °C/s):** 
   * *Sets:* Falling, Steady, Rising, Fast Rising

### Output
1. **Fan Speed (%):**
   * *Sets:* Off, Slow, Medium, Fast, Maximum

### Sample Rules
* *IF* Temperature is **Normal** *AND* Delta T is **Steady** *THEN* Fan Speed is **Slow**.
* *IF* Temperature is **High** *AND* Delta T is **Fast Rising** *THEN* Fan Speed is **Maximum**. (Proactive intervention)
