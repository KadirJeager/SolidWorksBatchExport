using System;
using System.IO;
using System.Windows;
// using Microsoft.Win32; // for OpenFileDialog
// For FolderBrowserDialog in WPF without external libraries in .NET 8,
// we can use the WinForms one by referencing System.Windows.Forms or use the newer Vista-style dialogs.
// To keep it simple without adding winforms reference right now, I will use a basic implementation or prompt.

namespace SolidWorksBatchExport
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Log("Uygulama başlatıldı. Lütfen bir hedef klasör seçin.");
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            // .NET 8 WPF artık yerleşik OpenFolderDialog destekliyor! (Hack'e gerek yok)
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Lütfen Sac Parçaların Bulunduğu Klasörü Seçin"
            };

            if (dialog.ShowDialog() == true)
            {
                txtFolderPath.Text = dialog.FolderName;
                Log($"Klasör seçildi: {txtFolderPath.Text}");
            }
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolderPath.Text) || !Directory.Exists(txtFolderPath.Text))
            {
                MessageBox.Show("Lütfen geçerli bir klasör seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string radiusText = txtNotchRadius.Text.Replace(",", ".");
            if (!double.TryParse(radiusText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double notchRadius))
            {
                MessageBox.Show("Lütfen geçerli bir çentik yarıçapı girin (Örn: 1.5 veya 1,5).", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnStart.IsEnabled = false;
            Log("İşlem başlatılıyor...");

            try
            {
                // Progress reporting interface to update UI safely from background thread
                var progress = new Progress<string>(msg => Log(msg));

                var processor = new Core.BatchProcessor();
                
                string folderPath = txtFolderPath.Text;
                bool enableNotch = chkEnableNotch.IsChecked == true;
                bool exportBendLines = chkExportBendLines.IsChecked == true;

                // Run heavy SolidWorks processing on a background thread so UI doesn't freeze
                await Task.Run(() => processor.ProcessFolderAsync(folderPath, enableNotch, notchRadius, exportBendLines, progress));
            }
            catch (Exception ex)
            {
                Log($"Hata oluştu: {ex.Message}");
            }
            finally
            {
                btnStart.IsEnabled = true;
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                txtLog.ScrollToEnd();
            });
        }
    }
}
