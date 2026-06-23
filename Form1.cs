using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ISIControl
{
    // Arayüz, Motor ve Fizik Modeli arasındaki güncellenmiş iletişim köprüsü
    public static class SimState
    {
        // Dashboard (Canlı Simülasyon) Değerleri
        public static volatile float CpuLoad = 10.0f;
        public static volatile CoolingMode TargetMode = CoolingMode.Auto;

        // Sistemin Doğrudan Termodinamik Parametreleri
        public static volatile float ThermalResistance = 0.2f;   // Toplam Isı Direnci (K/W)
        public static volatile float R_max = 5.0f;           // Pasif Taşınım Direnci (K/W) - Doğal Işıma/Taşınım
        public static volatile float Sogutma_K = 0.1f;       // Radyatör Etkinlik Katsayısı
        public static volatile float HeatCapacity = 57.7f;       // Toplam Isı Sığası (J/K)
        public static volatile float AmbientTemperature = 25.0f; // Ortam Sıcaklığı (°C)
        public static volatile float FanMaxCfm = 50.0f;          // Fan Hava Akışı (CFM)

        public static volatile float CurrentTemperature = 25.0f;   // Fizik motoru yazar, kontrolcü okur
        public static volatile float CurrentFanPwm = 0.0f;         // Kontrolcü yazar, fizik motoru okur
        public static volatile int CurrentThrottlingLevel = 0;     // Kontrolcü yazar, fizik motoru okur
        public static volatile float CurrentBakirTemperature = 25.0f; // Fizik motoru yazar, arayüz okur
    }

    public partial class Form1 : Form
    {
        private CancellationTokenSource _cts;

        // --- SAĞ PANEL (DASHBOARD) UI ELEMANLARI ---
        private ProgressBar pbTemp;
        private ProgressBar pbFan;
        private Label lblBakirTemp; // YENİ EKLENDİ
        private Label lblTemp;
        private Label lblFan;
        private Label lblMod;
        private Label lblThrottle;
        private Label lblECpuLoad;
        private TrackBar tbCpuLoad;
        private Label lblCpuLoad;

        // --- SOL PANEL (SİSTEM TASARIMI) UI ELEMANLARI ---
        private NumericUpDown nudResistance;
        private NumericUpDown nudCapacity;
        private NumericUpDown nudAmbient;
        private NumericUpDown nudFanCfm;
        private NumericUpDown nudR_max;
        private NumericUpDown nudSogutmaK;

        public Form1()
        {
            InitializeComponent();
            GelistirilmisArayuzuCiz();
            this.Load += Form1_Load;
        }

        private void GelistirilmisArayuzuCiz()
        {
            // Form Ayarları
            this.Text = "Termal Dijital İkiz ve Bulanık Mantık Kontrolcüsü";
            this.Size = new Size(850, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // --- 1. SOL PANEL: SİSTEM MODELİ ATÖLYESİ ---
            Panel pnlDesign = new Panel { Location = new Point(10, 10), Size = new Size(350, 440), BorderStyle = BorderStyle.FixedSingle };

            Label lblDesignBaslik = new Label { Text = "1. Sistem Parametreleri", Location = new Point(10, 10), Font = new Font("Arial", 12, FontStyle.Bold), AutoSize = true };

            // Isı Direnci (R_th)
            Label lblRes = new Label { Text = "Toplam Isı Direnci (K/W):", Location = new Point(10, 50), AutoSize = true };
            nudResistance = new NumericUpDown { Location = new Point(185, 47), Size = new Size(120, 25), DecimalPlaces = 2, Increment = 0.1M, Minimum = 0.1M, Maximum = 10.0M, Value = 0.2M };

            Label lblRmax = new Label { Text = "Pasif R_max (K/W):", Location = new Point(10, 90), AutoSize = true };
            nudR_max = new NumericUpDown { Location = new Point(185, 87), Size = new Size(120, 25), DecimalPlaces = 1, Increment = 0.5M, Minimum = 1.0M, Maximum = 20.0M, Value = 5.0M };

            Label lblSogutma = new Label { Text = "Soğutma Katsayısı (K):", Location = new Point(10, 130), AutoSize = true };
            nudSogutmaK = new NumericUpDown { Location = new Point(185, 127), Size = new Size(120, 25), DecimalPlaces = 2, Increment = 0.05M, Minimum = 0.01M, Maximum = 2.0M, Value = 0.1M };

            // Isı Sığası (C_th)
            Label lblCap = new Label { Text = "Toplam Isı Sığası (J/K):", Location = new Point(10, 170), AutoSize = true };
            nudCapacity = new NumericUpDown { Location = new Point(185, 167), Size = new Size(120, 25), DecimalPlaces = 1, Increment = 1.0M, Minimum = 5.0M, Maximum = 1000.0M, Value = 57.7M };

            // Ortam Sıcaklığı
            Label lblAmb = new Label { Text = "Ortam Sıcaklığı (°C):", Location = new Point(10, 210), AutoSize = true };
            nudAmbient = new NumericUpDown { Location = new Point(185, 207), Size = new Size(120, 25), DecimalPlaces = 1, Increment = 0.5M, Minimum = -20.0M, Maximum = 60.0M, Value = 25.0M };

            // Fan Gücü (CFM)
            Label lblFanPwr = new Label { Text = "Fan Kapasitesi (CFM):", Location = new Point(10, 250), AutoSize = true };
            nudFanCfm = new NumericUpDown { Location = new Point(185, 247), Size = new Size(120, 25), DecimalPlaces = 1, Increment = 1.0M, Minimum = 10.0M, Maximum = 200.0M, Value = 50.0M };

            // Uygula Butonu
            Button btnUygula = new Button { Text = "Modeli Güncelle", Location = new Point(10, 290), Size = new Size(290, 40), BackColor = Color.LightSteelBlue, Font = new Font("Arial", 10, FontStyle.Bold) };
            btnUygula.Click += BtnUygula_Click;

            Label lblInfo = new Label { Text = "Not: Bu parametreler sistemin transfer\nfonksiyonunu doğrudan etkileyecektir.", Location = new Point(10, 330), AutoSize = true, ForeColor = Color.Gray };

            pnlDesign.Controls.AddRange(new Control[] { 
                lblDesignBaslik, 
                lblRes, nudResistance, 
                lblRmax, nudR_max, 
                lblSogutma, nudSogutmaK, 
                lblCap, nudCapacity, 
                lblAmb, nudAmbient, 
                lblFanPwr, nudFanCfm, 
                btnUygula, 
                lblInfo });

            // --- 2. SAĞ PANEL: CANLI SİMÜLASYON (DASHBOARD) ---
            Panel pnlDashboard = new Panel { Location = new Point(370, 10), Size = new Size(450, 440), BorderStyle = BorderStyle.FixedSingle };

            Label lblDashBaslik = new Label { Text = "2. Canlı Kontrol Paneli", Location = new Point(10, 10), Font = new Font("Arial", 12, FontStyle.Bold), AutoSize = true };

            // Göstergeler
            lblTemp = new Label { Text = "Sıcaklık: 0 °C", Location = new Point(10, 50), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) };
            pbTemp = new ProgressBar { Location = new Point(10, 75), Size = new Size(420, 25), Maximum = 120 };

            lblBakirTemp = new Label { Text = "Radyatör Sıcaklığı: 0 °C", Location = new Point(10, 32), AutoSize = true, Font = new Font("Arial", 9, FontStyle.Regular), ForeColor = Color.DimGray };

            lblFan = new Label { Text = "Fan Hızı: %0", Location = new Point(10, 110), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) };
            pbFan = new ProgressBar { Location = new Point(10, 135), Size = new Size(420, 25), Maximum = 100 };

            lblThrottle = new Label { Text = "Durum: Normal", Location = new Point(10, 175), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.Green };

            lblECpuLoad = new Label { Text = "Etkin CPU Yükü: 0W", Location = new Point(10, 195), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) };

            // Kontrol Modları
            lblMod = new Label { Text = "Aktif Mod: AUTO", Location = new Point(10, 220), AutoSize = true, Font = new Font("Arial", 10) };

            Button btnSessiz = new Button { Text = "Sessiz", Location = new Point(10, 245), Size = new Size(80, 35), BackColor = Color.LightBlue };
            btnSessiz.Click += (s, e) => { SimState.TargetMode = CoolingMode.Quiet; };

            Button btnAuto = new Button { Text = "Auto", Location = new Point(100, 245), Size = new Size(80, 35), BackColor = Color.LightGray };
            btnAuto.Click += (s, e) => { SimState.TargetMode = CoolingMode.Auto; };

            Button btnPerf = new Button { Text = "Turbo", Location = new Point(190, 245), Size = new Size(80, 35), BackColor = Color.LightCoral };
            btnPerf.Click += (s, e) => { SimState.TargetMode = CoolingMode.Performance; };

            // CPU Stres Testi
            Label lblStresBaslik = new Label { Text = "İşlemci Stres Testi (Isı Üretimi)", Location = new Point(10, 310), Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };
            lblCpuLoad = new Label { Text = "CPU Yükü: 10W", Location = new Point(10, 335), AutoSize = true, Font = new Font("Arial", 10) };

            tbCpuLoad = new TrackBar { Location = new Point(10, 360), Size = new Size(420, 45), Minimum = 0, Maximum = 100, Value = 10, TickFrequency = 10 };
            tbCpuLoad.Scroll += (s, e) =>
            {
                lblCpuLoad.Text = $"CPU Yükü: {tbCpuLoad.Value}W";
                SimState.CpuLoad = tbCpuLoad.Value;
            };

            pnlDashboard.Controls.AddRange(new Control[] {
                lblDashBaslik, lblTemp, pbTemp, lblBakirTemp, lblFan, pbFan, lblThrottle, lblECpuLoad,
                lblMod, btnSessiz, btnAuto, btnPerf, lblStresBaslik, lblCpuLoad, tbCpuLoad
            });

            this.Controls.Add(pnlDesign);
            this.Controls.Add(pnlDashboard);
        }

        private void BtnUygula_Click(object sender, EventArgs e)
        {
            // Kullanıcı modeli güncellediğinde SimState'e yazıyoruz.
            SimState.ThermalResistance = (float)nudResistance.Value;
            SimState.R_max = (float)nudR_max.Value;
            SimState.Sogutma_K = (float)nudSogutmaK.Value;
            SimState.HeatCapacity = (float)nudCapacity.Value;
            SimState.AmbientTemperature = (float)nudAmbient.Value;
            SimState.FanMaxCfm = (float)nudFanCfm.Value;

            MessageBox.Show($"Termal Model Güncellendi!\n" +
                            $"Isı Direnci: {SimState.ThermalResistance} K/W\n" +
                            $"Pasif R_max: {SimState.R_max} K/W\n" +
                            $"Soğutma Katsayısı: {SimState.Sogutma_K}\n" +
                            $"Isı Sığası: {SimState.HeatCapacity} J/K\n" +
                            $"Ortam: {SimState.AmbientTemperature} °C\n" +
                            $"Fan: {SimState.FanMaxCfm} CFM",
                            "Sistem Modeli", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var progressBridge = new Progress<ThermalUpdateState>(UpdateUI);
            _cts = new CancellationTokenSource();
            Task.Run(() => ThermalEngine.StartEngineAsync(0.1f, progressBridge, _cts.Token));


        }

        private void UpdateUI(ThermalUpdateState state)
        {
            lblTemp.Text = $"Sıcaklık: {state.CurrentTemperature:F1} °C";
            lblBakirTemp.Text = $"Radyatör Sıcaklığı: {state.BakirTemperature:F1} °C";
            lblFan.Text = $"Fan Hızı: %{state.FanPwm:F1}";
            lblMod.Text = $"Aktif Mod: {state.CurrentMode}";

            pbTemp.Value = Math.Min(pbTemp.Maximum, Math.Max(0, (int)state.CurrentTemperature));
            pbFan.Value = Math.Min(pbFan.Maximum, Math.Max(0, (int)state.FanPwm));

            if (state.ThrottlingLevel > 0)
            {
                lblThrottle.Text = $"DURUM: THERMAL THROTTLING! (Seviye {state.ThrottlingLevel})";
                lblThrottle.ForeColor = Color.Red;
            }
            else
            {
                lblThrottle.Text = "Durum: Normal (Güvenli)";
                lblThrottle.ForeColor = Color.Green;
            }

            lblECpuLoad.Text = $"Etkin CPU Yükü: {state.EffectiveCpuLoad:F1}W";

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
        }
    }
}