using System;
using System.Diagnostics;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

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
                // Open Windows Default Apps Settings
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:defaultapps",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Não foi possível abrir as configurações do Windows:\n{ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "Você está utilizando a versão mais recente do FT PDF (v1.0.0).\n\nO canal de atualização contínua via GitHub Actions verificará novos lançamentos automaticamente nas próximas versões.", "Verificação de Atualizações", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
