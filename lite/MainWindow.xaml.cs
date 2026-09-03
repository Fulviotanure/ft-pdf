using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using FtPdfLite.Models;
using FtPdfLite.Services;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Clipboard = System.Windows.Clipboard;
using Path = System.IO.Path;
using Cursors = System.Windows.Input.Cursors;
using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using StackPanel = System.Windows.Controls.StackPanel;
using TextBlock = System.Windows.Controls.TextBlock;
using Border = System.Windows.Controls.Border;

namespace FtPdfLite
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<PdfDocumentTab> _tabs = new();
        private PdfDocumentTab? _activeTab;
        private readonly PdfExtractionService _extractionService = new();
        private bool _isRawTextMode = false;
        private bool _isNotepadOpen = false;
        private bool _isWebViewInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            StateChanged += MainWindow_StateChanged;
            InitializeViewerAsync();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (BtnMaximize != null)
            {
                BtnMaximize.Content = WindowState == WindowState.Maximized ? "🗗" : "🗖";
                BtnMaximize.ToolTip = WindowState == WindowState.Maximized ? "Restaurar" : "Maximizar";
            }
        }

        #region Custom Integrated Window Controls (Min, Max, Close)

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        private async void InitializeViewerAsync()
        {
            try
            {
                PdfWebViewer.DefaultBackgroundColor = System.Drawing.Color.FromArgb(15, 23, 42);

                await PdfWebViewer.EnsureCoreWebView2Async();
                _isWebViewInitialized = true;
                PdfWebViewer.CoreWebView2.Settings.IsStatusBarEnabled = false;
                PdfWebViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
                PdfWebViewer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                PdfWebViewer.CoreWebView2.Settings.IsZoomControlEnabled = false;

                if (_activeTab != null && File.Exists(_activeTab.FilePath))
                {
                    NavigateToPdf(_activeTab.FilePath);
                }
            }
            catch
            {
            }
        }

        private void NavigateToPdf(string filePath)
        {
            string cleanUrl = $"{new Uri(filePath).AbsoluteUri}#toolbar=0&navpanes=0";
            if (_isWebViewInitialized && PdfWebViewer.CoreWebView2 != null)
            {
                PdfWebViewer.CoreWebView2.Navigate(cleanUrl);
            }
            else
            {
                PdfWebViewer.Source = new Uri(cleanUrl);
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWindow { Owner = this };
            settings.ShowDialog();
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Arquivos PDF (*.pdf)|*.pdf|Todos os Arquivos (*.*)|*.*",
                Multiselect = true,
                Title = "Selecionar Arquivos PDF"
            };

            if (dialog.ShowDialog(this) == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    OpenTab(fileName);
                }
            }
        }

        public void OpenTab(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show(this, $"O arquivo não foi encontrado:\n{filePath}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var existing = _tabs.FirstOrDefault(t => t.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    SetActiveTab(existing);
                    return;
                }

                var tab = new PdfDocumentTab
                {
                    FilePath = filePath
                };

                _tabs.Add(tab);
                SetActiveTab(tab);

                // Run extraction & integrity analysis in background
                _ = Task.Run(() =>
                {
                    var result = _extractionService.ExtractAndAnalyze(filePath);
                    Dispatcher.Invoke(() =>
                    {
                        tab.Extraction = result;
                        tab.TotalPages = result.Report.TotalPages;
                        if (_activeTab == tab)
                        {
                            UpdateNotepadView();
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Erro ao abrir o arquivo PDF:\n{ex.Message}", "Falha na Leitura", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetActiveTab(PdfDocumentTab tab)
        {
            _activeTab = tab;
            UpdateTabsBar();

            PanelEmptyState.Visibility = Visibility.Collapsed;
            PanelActiveContent.Visibility = Visibility.Visible;
            BtnQuickSave.Visibility = Visibility.Visible;
            BtnQuickCopy.Visibility = Visibility.Visible;
            BtnToggleNotepad.Visibility = Visibility.Visible;

            NavigateToPdf(tab.FilePath);
            UpdateNotepadView();
        }

        private void CloseTab(PdfDocumentTab tab)
        {
            int index = _tabs.IndexOf(tab);
            tab.Dispose();
            _tabs.Remove(tab);

            if (_activeTab == tab)
            {
                if (_tabs.Count > 0)
                {
                    int newIndex = Math.Clamp(index - 1, 0, _tabs.Count - 1);
                    SetActiveTab(_tabs[newIndex]);
                }
                else
                {
                    CloseAllDocuments();
                }
            }
            else
            {
                UpdateTabsBar();
            }
        }

        private void CloseAllDocuments()
        {
            _activeTab = null;
            PanelEmptyState.Visibility = Visibility.Visible;
            PanelActiveContent.Visibility = Visibility.Collapsed;
            BtnQuickSave.Visibility = Visibility.Collapsed;
            BtnQuickCopy.Visibility = Visibility.Collapsed;
            BtnToggleNotepad.Visibility = Visibility.Collapsed;
            CloseNotepad();
            UpdateTabsBar();
        }

        private void UpdateTabsBar()
        {
            PanelTabs.Children.Clear();

            foreach (var tab in _tabs)
            {
                bool isActive = (tab == _activeTab);

                var tabBorder = new Border
                {
                    Background = new SolidColorBrush(isActive 
                        ? (Color)ColorConverter.ConvertFromString("#1E293B") 
                        : Colors.Transparent),
                    BorderBrush = new SolidColorBrush(isActive 
                        ? (Color)ColorConverter.ConvertFromString("#334155") 
                        : Colors.Transparent),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 3, 6, 3),
                    Margin = new Thickness(0, 0, 4, 0),
                    Cursor = Cursors.Hand
                };

                var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                var icon = new TextBlock
                {
                    Text = "📄",
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var title = new TextBlock
                {
                    Text = tab.FileName,
                    FontSize = 11.5,
                    FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = new SolidColorBrush(isActive ? Colors.White : (Color)ColorConverter.ConvertFromString("#94A3B8")),
                    MaxWidth = 160,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var closeBtn = new Button
                {
                    Content = "✕",
                    FontSize = 9.5,
                    Width = 16,
                    Height = 16,
                    Margin = new Thickness(6, 0, 0, 0),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                closeBtn.Click += (s, e) =>
                {
                    e.Handled = true;
                    CloseTab(tab);
                };

                sp.Children.Add(icon);
                sp.Children.Add(title);
                sp.Children.Add(closeBtn);
                tabBorder.Child = sp;

                tabBorder.MouseLeftButtonDown += (s, e) => SetActiveTab(tab);

                PanelTabs.Children.Add(tabBorder);
            }
        }

        #region Validation & Notepad Panel

        private void UpdateNotepadView()
        {
            if (_activeTab?.Extraction == null) return;

            var report = _activeTab.Extraction.Report;
            var props = _activeTab.Extraction.Properties;

            TxtEditor.Text = _isRawTextMode ? _activeTab.Extraction.RawText : _activeTab.Extraction.FormattedText;
            TxtEditorMode.Text = _isRawTextMode ? "Modo: Texto Cru (Raw)" : "Modo: Layout Preservado";
            BtnToggleRawText.Content = _isRawTextMode ? "Exibir Texto Formatado" : "Exibir Texto Cru";

            TxtHeaderDocType.Text = report.DocumentType;
            TxtIntegrityScore.Text = $"{report.IntegrityScore:0.0}% Integridade";
            TxtIntegrityStatusText.Text = report.IntegrityStatus;

            if (report.IntegrityScore >= 90.0)
            {
                var greenColor = (Color)ColorConverter.ConvertFromString("#10B981");
                TxtHeaderDocType.Foreground = new SolidColorBrush(greenColor);
                BadgeIntegrity.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A2F"));
                BadgeIntegrity.BorderBrush = new SolidColorBrush(greenColor);
                TxtIntegrityScore.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
            }
            else if (report.IntegrityScore >= 65.0)
            {
                var yellowColor = (Color)ColorConverter.ConvertFromString("#F59E0B");
                TxtHeaderDocType.Foreground = new SolidColorBrush(yellowColor);
                BadgeIntegrity.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D3215"));
                BadgeIntegrity.BorderBrush = new SolidColorBrush(yellowColor);
                TxtIntegrityScore.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            }
            else if (report.IntegrityScore >= 30.0)
            {
                var orangeColor = (Color)ColorConverter.ConvertFromString("#F97316");
                TxtHeaderDocType.Foreground = new SolidColorBrush(orangeColor);
                BadgeIntegrity.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E2619"));
                BadgeIntegrity.BorderBrush = new SolidColorBrush(orangeColor);
                TxtIntegrityScore.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDBA74"));
            }
            else
            {
                var redColor = (Color)ColorConverter.ConvertFromString("#EF4444");
                TxtHeaderDocType.Foreground = new SolidColorBrush(redColor);
                BadgeIntegrity.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E1C1E"));
                BadgeIntegrity.BorderBrush = new SolidColorBrush(redColor);
                TxtIntegrityScore.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
            }

            TxtImportVerdict.Text = report.ImportVerdict;
            if (report.ImportVerdict == "O arquivo importa")
            {
                TxtImportVerdictIcon.Text = "✅";
                BorderImportVerdict.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A2F"));
                BorderImportVerdict.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                TxtImportVerdict.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
            }
            else if (report.ImportVerdict == "O arquivo pode importar com falha ou itens faltantes")
            {
                TxtImportVerdictIcon.Text = "⚠️";
                BorderImportVerdict.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D3215"));
                BorderImportVerdict.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                TxtImportVerdict.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            }
            else if (report.ImportVerdict == "Grandes chances de dar erro na importação")
            {
                TxtImportVerdictIcon.Text = "⚠️";
                BorderImportVerdict.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E2619"));
                BorderImportVerdict.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F97316"));
                TxtImportVerdict.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDBA74"));
            }
            else
            {
                TxtImportVerdictIcon.Text = "⛔";
                BorderImportVerdict.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E1C1E"));
                BorderImportVerdict.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                TxtImportVerdict.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
            }

            TxtDiagFormatting.Text = $"Formatação: {report.FormattingQuality}";
            TxtDiagStrangeChars.Text = $"Sinais Estranhos: {report.StrangeCharactersCount}";
            TxtDiagImages.Text = $"Imagens / Scans: {report.TotalImagesFound} ({report.ScannedPagesCount} pág. scan)";
            TxtDiagCharCount.Text = $"Total Caracteres: {report.TotalCharacters:N0}";

            TxtPropFileSize.Text = props.FileSize;
            TxtPropPdfVersion.Text = props.PdfVersion;
            TxtPropDimensions.Text = props.PageDimensions;
            TxtPropOrientation.Text = props.PageOrientation;
            TxtPropAuthorProducer.Text = $"{props.Author} / {props.Producer}";
            TxtPropCreationDate.Text = props.CreationDate;
            TxtPropSecurity.Text = props.Security;

            if (report.DiagnosticWarnings.Count > 0)
            {
                TxtDiagWarning.Text = string.Join(" • ", report.DiagnosticWarnings);
            }
            else
            {
                TxtDiagWarning.Text = "Texto bem estruturado e sem anomalias detectadas.";
            }

            UpdateEditorStats();
        }

        private void BtnToggleNotepad_Click(object sender, RoutedEventArgs e)
        {
            if (_isNotepadOpen) CloseNotepad(); else OpenNotepad();
        }

        private void OpenNotepad()
        {
            _isNotepadOpen = true;
            ColNotepad.Width = new GridLength(530);
            PanelNotepad.Visibility = Visibility.Visible;
            SplitterBar.Visibility = Visibility.Visible;
            BtnToggleNotepad.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            BtnToggleNotepad.Foreground = Brushes.White;
        }

        private void CloseNotepad()
        {
            _isNotepadOpen = false;
            ColNotepad.Width = new GridLength(0);
            PanelNotepad.Visibility = Visibility.Collapsed;
            SplitterBar.Visibility = Visibility.Collapsed;
            BtnToggleNotepad.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            BtnToggleNotepad.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FACC15"));
        }

        private void BtnCloseNotepad_Click(object sender, RoutedEventArgs e) => CloseNotepad();

        private void BtnToggleRawText_Click(object sender, RoutedEventArgs e)
        {
            _isRawTextMode = !_isRawTextMode;
            UpdateNotepadView();
        }

        private void BtnQuickSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_activeTab == null || !File.Exists(_activeTab.FilePath)) return;

                var dialog = new SaveFileDialog
                {
                    Filter = "Arquivo PDF (*.pdf)|*.pdf|Todos os Arquivos (*.*)|*.*",
                    DefaultExt = "pdf",
                    FileName = Path.GetFileNameWithoutExtension(_activeTab.FilePath) + "_copia.pdf"
                };

                if (dialog.ShowDialog(this) == true)
                {
                    File.Copy(_activeTab.FilePath, dialog.FileName, overwrite: true);
                    MessageBox.Show(this, "Arquivo PDF salvo com sucesso!", "Salvo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Erro ao salvar o arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnQuickCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string textToCopy = _activeTab?.Extraction != null ? _activeTab.Extraction.FormattedText : TxtEditor.Text;
                if (!string.IsNullOrEmpty(textToCopy))
                {
                    Clipboard.SetText(textToCopy);
                    MessageBox.Show(this, "Conteúdo do PDF copiado para a Área de Transferência com sucesso!", "Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(this, "Nenhum texto disponível para copiar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Não foi possível copiar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCopyText_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(TxtEditor.Text))
                {
                    Clipboard.SetText(TxtEditor.Text);
                    MessageBox.Show(this, "Texto copiado para a Área de Transferência com sucesso!", "Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Não foi possível copiar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSaveTxt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Arquivo de Texto (*.txt)|*.txt|Todos os Arquivos (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = Path.GetFileNameWithoutExtension(_activeTab?.FilePath ?? "Documento") + "_extraido.txt"
                };

                if (dialog.ShowDialog(this) == true)
                {
                    File.WriteAllText(dialog.FileName, TxtEditor.Text);
                    MessageBox.Show(this, "Arquivo salvo com sucesso!", "Salvo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Erro ao salvar o arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToggleWrap_Click(object sender, RoutedEventArgs e)
        {
            if (TxtEditor.TextWrapping == TextWrapping.Wrap)
            {
                TxtEditor.TextWrapping = TextWrapping.NoWrap;
                BtnToggleWrap.Content = "Quebrar Linha: OFF";
            }
            else
            {
                TxtEditor.TextWrapping = TextWrapping.Wrap;
                BtnToggleWrap.Content = "Quebrar Linha: ON";
            }
        }

        private void TxtEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateEditorStats();

        private void UpdateEditorStats()
        {
            int chars = TxtEditor.Text.Length;
            int words = TxtEditor.Text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            TxtEditorStats.Text = $"Caracteres: {chars:N0} | Palavras: {words:N0}";
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            foreach (var tab in _tabs) tab.Dispose();
            _tabs.Clear();
            base.OnClosed(e);
        }
    }
}
