using MessageBox = System.Windows.MessageBox;
using System;
using System.Diagnostics;
using System.Windows;

namespace FtPdf
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void BtnSetDefaultApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:defaultapps",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
                    $"Não foi possível abrir as configurações do Windows:\n{ex.Message}\n\nVocê pode definir manualmente em: Configurações do Windows > Aplicativos > Aplicativos Padrão.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnOpenRepo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/Fulviotanure/ft-pdf",
                    UseShellExecute = true
                });
            }
            catch {}
        }

        private void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, 
                "FT PDF v1.0.0 (Release 1)\n\nVocê está utilizando a versão mais recente! As compilações e novidades são publicadas automaticamente via GitHub Actions.",
                "Atualizações", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
