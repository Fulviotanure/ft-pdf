using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using FtPdf.Services;

namespace FtPdf
{
    public partial class SettingsWindow : Window
    {
        private UpdateCheckResult? _cachedResult;
        private bool _isDownloading = false;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtCurrentVersion.Text = $"v{UpdateService.CurrentVersionString}";
            TxtBadgeVersion.Text = $"Release {UpdateService.CurrentVersionString}";
            await CheckUpdatesAsync();
        }

        private async Task CheckUpdatesAsync()
        {
            TxtStatus.Text = "Verificando atualizações...";
            TxtLatestVersion.Text = "Verificando...";
            PbStatus.Visibility = Visibility.Visible;
            BtnAction.IsEnabled = false;

            var result = await UpdateService.CheckForUpdatesAsync(isLite: false);
            _cachedResult = result;

            PbStatus.Visibility = Visibility.Collapsed;
            BtnAction.IsEnabled = true;

            if (result.HasUpdate)
            {
                TxtLatestVersion.Text = $"v{result.LatestVersion}";
                TxtStatus.Text = $"Nova versão v{result.LatestVersion} disponível para instalação!";
                BtnAction.Content = "⬇️ Atualizar Agora";
                BtnAction.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            }
            else if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                TxtLatestVersion.Text = "Indisponível";
                TxtStatus.Text = "Não foi possível conectar ao canal de atualizações.";
                BtnAction.Content = "🔄 Tentar Novamente";
                BtnAction.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            }
            else
            {
                TxtLatestVersion.Text = $"v{UpdateService.CurrentVersionString}";
                TxtStatus.Text = "Você já está utilizando a versão mais recente.";
                BtnAction.Content = "🔄 Verificar Novamente";
                BtnAction.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            }
        }

        private async void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading) return;

            if (_cachedResult != null && _cachedResult.HasUpdate && !string.IsNullOrEmpty(_cachedResult.DownloadUrl))
            {
                _isDownloading = true;
                BtnAction.IsEnabled = false;
                PbStatus.Visibility = Visibility.Visible;
                TxtStatus.Text = "Baixando nova versão e substituindo executável antigo...";

                await UpdateService.DownloadAndApplyUpdateAsync(_cachedResult.DownloadUrl, "FtPdf.exe");

                _isDownloading = false;
                BtnAction.IsEnabled = true;
                PbStatus.Visibility = Visibility.Collapsed;
            }
            else
            {
                await CheckUpdatesAsync();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
