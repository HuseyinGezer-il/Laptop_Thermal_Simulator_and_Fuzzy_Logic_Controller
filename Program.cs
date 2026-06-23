using System;
using System.Windows.Forms;

namespace ISIControl
{
    internal static class Program
    {

        [STAThread]
        static void Main()
        {
            // WinForms standart başlatma kodları
            ApplicationConfiguration.Initialize();

            // Form1 nesnesini oluşturuyoruz ama hemen Run etmiyoruz
            Form1 anaForm = new Form1();

            // Uygulamayı ana form üzerinden başlatıyoruz. 
            // Form1_Load tetiklenecek ve motoru o arka planda başlatacak.
            Application.Run(anaForm);

        }



    }
}