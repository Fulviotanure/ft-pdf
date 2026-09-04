using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FtPdf.Dialogs;
using FtPdf.Models;
using FtPdf.Services;
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
using DockPanel = System.Windows.Controls.DockPanel;
using Dock = System.Windows.Controls.Dock;
using TextBlock = System.Windows.Controls.TextBlock;
using Border = System.Windows.Controls.Border;
using ComboBoxItem = System.Windows.Controls.ComboBoxItem;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Vector = System.Windows.Vector;

namespace FtPdf
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<PdfDocumentTab> _tabs = new();
        private PdfDocumentTab? _activeTab;
        private readonly PdfExtractionService _extractionService = new();
        private readonly PdfEditingService _editingService = new();
        private bool _isRawTextMode = false;
        private bool _isNotepadOpen = false;
        private bool _isWebViewInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            StateChanged += MainWindow_StateChanged;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            InitializeViewerAsync();
            CheckCommandLineArgs();
            Loaded += async (s, e) => await UpdateService.AutoCheckOnStartupAsync(isLite: false, this);
            Loaded += (s, e) => CheckDefaultAppBanner();
        }

        private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
            {
                if (e.Key == System.Windows.Input.Key.O || e.Key == System.Windows.Input.Key.T)
                {
                    e.Handled = true;
                    BtnOpenFile_Click(this, new RoutedEventArgs());
                }
                else if (e.Key == System.Windows.Input.Key.W)
                {
                    if (_activeTab != null)
                    {
                        e.Handled = true;
                        CloseTab(_activeTab);
                    }
                }
                else if (e.Key == System.Windows.Input.Key.S)
                {
                    e.Handled = true;
                    BtnQuickSave_Click(this, new RoutedEventArgs());
                }
                else if (e.Key == System.Windows.Input.Key.Tab)
                {
                    if (_tabs.Count > 1 && _activeTab != null)
                    {
                        e.Handled = true;
                        int step = System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift) ? -1 : 1;
                        int idx = (_tabs.IndexOf(_activeTab) + step + _tabs.Count) % _tabs.Count;
                        SetActiveTab(_tabs[idx]);
                    }
                }
            }
        }

        private void CheckCommandLineArgs()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        string path = args[i].Trim('"', ' ');
                        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                        {
                            OpenTab(path);
                        }
                    }
                }
            }
            catch {}
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    foreach (var file in files)
                    {
                        if (File.Exists(file) && file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            OpenTab(file);
                        }
                    }
                }
            }
            catch {}
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (BtnMaximize != null)
            {
                BtnMaximize.Content = WindowState == WindowState.Maximized ? "🗗" : "🗖";
                BtnMaximize.ToolTip = WindowState == WindowState.Maximized ? "Restaurar" : "Maximizar";
            }
        }

        #region Custom Integrated Window Chrome Controls (Min, Max, Close)

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

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                DependencyObject? current = dep;
                while (current != null && current != sender)
                {
                    if (current is System.Windows.Controls.Button)
                        return;
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            if (e.ClickCount == 2)
            {
                BtnMaximize_Click(sender, e);
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch { }
            }
        }

        private void ScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep)
            {
                DependencyObject? current = dep;
                while (current != null && current != sender)
                {
                    if (current is System.Windows.Controls.Button)
                        return;
                    if (current is Border b && b.Parent == PanelTabs)
                        return;
                    current = VisualTreeHelper.GetParent(current);
                }
            }

            TitleBar_MouseLeftButtonDown(sender, e);
        }

        #endregion

        private async void InitializeViewerAsync()
        {
            try
            {
                // Set background color to match app background (#0F172A)
                PdfWebViewer.DefaultBackgroundColor = System.Drawing.Color.FromArgb(15, 23, 42);

                var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FtPdf", "WebView2");
                Directory.CreateDirectory(userDataFolder);
                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await PdfWebViewer.EnsureCoreWebView2Async(env);
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
                // Fallback gracefully
            }
        }

        private void NavigateToPdf(string filePath)
        {
            // #toolbar=0&navpanes=0 completely hides the native browser toolbar,
            // zoom buttons, print, save and the 3-dots settings menu
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
            CheckDefaultAppBanner();
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
                if (string.IsNullOrWhiteSpace(filePath))
                    return;

                try { filePath = Path.GetFullPath(filePath); } catch { }

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
            Title = $"{tab.FileName} - FT PDF";
            UpdateTabsBar();

            PanelEmptyState.Visibility = Visibility.Collapsed;
            PanelActiveContent.Visibility = Visibility.Visible;
            BtnQuickSave.Visibility = Visibility.Visible;
            BtnQuickCopy.Visibility = Visibility.Visible;
            BtnToggleNotepad.Visibility = Visibility.Visible;

            // Load the original vector PDF file cleanly without native browser toolbars
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
            Title = "FT PDF";
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
                    MinWidth = 160,
                    MaxWidth = 280,
                    Padding = new Thickness(12, 4, 8, 4),
                    Margin = new Thickness(0, 0, 5, 0),
                    Cursor = Cursors.Hand
                };

                var dp = new DockPanel { LastChildFill = true, VerticalAlignment = VerticalAlignment.Center };

                var icon = new TextBlock
                {
                    Text = "📄",
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(icon, Dock.Left);

                var closeBtn = new Button
                {
                    Content = "✕",
                    FontSize = 9.5,
                    Width = 18,
                    Height = 18,
                    Margin = new Thickness(8, 0, 0, 0),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Fechar aba"
                };
                closeBtn.Click += (s, e) =>
                {
                    e.Handled = true;
                    CloseTab(tab);
                };
                DockPanel.SetDock(closeBtn, Dock.Right);

                var title = new TextBlock
                {
                    Text = tab.FileName,
                    FontSize = 11.5,
                    FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = new SolidColorBrush(isActive ? Colors.White : (Color)ColorConverter.ConvertFromString("#94A3B8")),
                    MaxWidth = 210,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                dp.Children.Add(icon);
                dp.Children.Add(closeBtn);
                dp.Children.Add(title);
                tabBorder.Child = dp;

                tabBorder.MouseDown += (s, e) =>
                {
                    if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
                    {
                        e.Handled = true;
                        CloseTab(tab);
                    }
                };

                tabBorder.MouseLeftButtonDown += (s, e) =>
                {
                    SetActiveTab(tab);
                    e.Handled = true;
                };

                PanelTabs.Children.Add(tabBorder);
            }
        }

        #region Editing Tools Handlers

        private Point _dragStartPoint;
        private double _startHOffset;
        private double _startVOffset;
        private bool _isDraggingPopup = false;
        private string _selectedTextColorHex = "#000000";

        private void BtnToolText_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null)
            {
                MessageBox.Show(this, "Abra um arquivo PDF primeiro para inserir caixas de texto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Populate Page dropdown
            CmbFloatingPage.Items.Clear();
            for (int i = 1; i <= _activeTab.TotalPages; i++)
            {
                CmbFloatingPage.Items.Add(new ComboBoxItem { Content = i.ToString(), IsSelected = (i == 1) });
            }

            // Center initial position
            PopupFloatingTextBox.HorizontalOffset = 0;
            PopupFloatingTextBox.VerticalOffset = 0;

            // Reset text and open
            TxtFloatingInput.Text = "Digite seu texto aqui...";
            PopupFloatingTextBox.IsOpen = true;

            // Set focus to the text box and select all so user can immediately type
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtFloatingInput.Focus();
                TxtFloatingInput.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        #region Floating Text Box Interactivity Handlers

        private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingPopup = true;
            _dragStartPoint = e.GetPosition(this);
            _startHOffset = PopupFloatingTextBox.HorizontalOffset;
            _startVOffset = PopupFloatingTextBox.VerticalOffset;
            ((UIElement)sender).CaptureMouse();
        }

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingPopup)
            {
                Point current = e.GetPosition(this);
                Vector diff = current - _dragStartPoint;
                PopupFloatingTextBox.HorizontalOffset = _startHOffset + diff.X;
                PopupFloatingTextBox.VerticalOffset = _startVOffset + diff.Y;
            }
        }

        private void DragHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingPopup)
            {
                _isDraggingPopup = false;
                ((UIElement)sender).ReleaseMouseCapture();
            }
        }

        private void ResizeThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double newWidth = Math.Max(180, BoxBorderContainer.Width + e.HorizontalChange);
            double newHeight = Math.Max(50, BoxBorderContainer.Height + e.VerticalChange);
            BoxBorderContainer.Width = newWidth;
            BoxBorderContainer.Height = newHeight;
        }

        private void CmbFloatingFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFloatingFontSize != null && CmbFloatingFontSize.SelectedItem is ComboBoxItem item &&
                double.TryParse(item.Content.ToString(), out double sz) &&
                TxtFloatingInput != null)
            {
                TxtFloatingInput.FontSize = sz;
            }
        }

        private void BtnColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                _selectedTextColorHex = hex;
                var color = (Color)ColorConverter.ConvertFromString(hex);
                TxtFloatingInput.Foreground = new SolidColorBrush(color);

                // Update visual indication on color buttons
                Button[] colorBtns = { BtnColorBlack, BtnColorWhite, BtnColorRed, BtnColorBlue, BtnColorYellow };
                foreach (var b in colorBtns)
                {
                    if (b == null) continue;
                    bool isThis = (b == btn);
                    b.BorderBrush = isThis ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                    b.BorderThickness = isThis ? new Thickness(2) : new Thickness(1);
                }
            }
        }

        private void BtnFloatingStyle_Click(object sender, RoutedEventArgs e)
        {
            if (TxtFloatingInput == null) return;
            TxtFloatingInput.FontWeight = (BtnFloatingBold.IsChecked == true) ? FontWeights.Bold : FontWeights.Normal;
            TxtFloatingInput.FontStyle = (BtnFloatingItalic.IsChecked == true) ? FontStyles.Italic : FontStyles.Normal;
        }

        private void BtnCloseFloatingText_Click(object sender, RoutedEventArgs e)
        {
            PopupFloatingTextBox.IsOpen = false;
        }

        private void BtnApplyFloatingText_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null || !File.Exists(_activeTab.FilePath)) return;

            string text = TxtFloatingInput.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show(this, "Por favor, digite algum texto antes de aplicar no PDF.", "Texto Vazio", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int pageNumber = 1;
            if (CmbFloatingPage.SelectedItem is ComboBoxItem pageItem && int.TryParse(pageItem.Content.ToString(), out int p))
            {
                pageNumber = p;
            }

            double fontSize = TxtFloatingInput.FontSize;
            bool isBold = BtnFloatingBold.IsChecked == true;
            bool isItalic = BtnFloatingItalic.IsChecked == true;

            var wpfColor = (Color)ColorConverter.ConvertFromString(_selectedTextColorHex);
            var xColor = PdfSharp.Drawing.XColor.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);

            // Compute PDF coordinates based on container size
            double viewerW = Math.Max(CenterDocumentContainer.ActualWidth, 400);
            double viewerH = Math.Max(CenterDocumentContainer.ActualHeight, 400);

            double relX = (viewerW / 2.0) + PopupFloatingTextBox.HorizontalOffset - (BoxBorderContainer.Width / 2.0);
            double relY = (viewerH / 2.0) + PopupFloatingTextBox.VerticalOffset - (BoxBorderContainer.Height / 2.0);

            double normX = Math.Clamp(relX / viewerW, 0.05, 0.90);
            double normY = Math.Clamp(relY / viewerH, 0.05, 0.90);

            double pdfPageW = 595;
            double pdfPageH = 842;
            try
            {
                using var doc = PdfSharp.Pdf.IO.PdfReader.Open(_activeTab.FilePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                if (pageNumber <= doc.PageCount)
                {
                    pdfPageW = doc.Pages[pageNumber - 1].Width.Point;
                    pdfPageH = doc.Pages[pageNumber - 1].Height.Point;
                }
            }
            catch {}

            double pdfX = normX * pdfPageW;
            double pdfY = normY * pdfPageH;
            double pdfBoxWidth = (BoxBorderContainer.Width / viewerW) * pdfPageW;

            var saveDialog = new SaveFileDialog
            {
                Filter = "Arquivo PDF (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                FileName = Path.GetFileNameWithoutExtension(_activeTab.FilePath) + "_texto.pdf",
                Title = "Salvar PDF com Caixa de Texto"
            };

            if (saveDialog.ShowDialog(this) == true)
            {
                try
                {
                    _editingService.InsertFormattedTextBox(
                        _activeTab.FilePath,
                        saveDialog.FileName,
                        pageNumber,
                        text,
                        pdfX,
                        pdfY,
                        pdfBoxWidth,
                        fontSize,
                        xColor,
                        isBold,
                        isItalic
                    );

                    PopupFloatingTextBox.IsOpen = false;
                    OpenTab(saveDialog.FileName);
                    MessageBox.Show(this, "Texto inserido com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Erro ao gravar texto no PDF:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        private void BtnToolHighlight_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null)
            {
                MessageBox.Show(this, "Abra um arquivo PDF primeiro para adicionar destaques.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new HighlightDialog(_activeTab.TotalPages, 1) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "Arquivo PDF (*.pdf)|*.pdf",
                        DefaultExt = "pdf",
                        FileName = Path.GetFileNameWithoutExtension(_activeTab.FilePath) + "_destacado.pdf",
                        Title = "Salvar PDF com Destaque"
                    };

                    if (saveDialog.ShowDialog(this) == true)
                    {
                        _editingService.AddHighlight(
                            _activeTab.FilePath,
                            saveDialog.FileName,
                            dialog.PageNumber,
                            dialog.PosX,
                            dialog.PosY,
                            dialog.RectWidth,
                            dialog.RectHeight,
                            dialog.HighlightColor
                        );

                        OpenTab(saveDialog.FileName);
                        MessageBox.Show(this, "Destaque aplicado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Erro ao aplicar destaque no PDF:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnToolSignature_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null)
            {
                MessageBox.Show(this, "Abra um arquivo PDF primeiro para assinar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SignatureDialog(_activeTab.TotalPages, 1) { Owner = this };

            if (dialog.ShowDialog() == true && dialog.SignatureImageBytes != null)
            {
                try
                {
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "Arquivo PDF (*.pdf)|*.pdf",
                        DefaultExt = "pdf",
                        FileName = Path.GetFileNameWithoutExtension(_activeTab.FilePath) + "_assinado.pdf",
                        Title = "Salvar PDF com Assinatura"
                    };

                    if (saveDialog.ShowDialog(this) == true)
                    {
                        _editingService.InsertSignature(
                            _activeTab.FilePath,
                            saveDialog.FileName,
                            dialog.PageNumber,
                            dialog.SignatureImageBytes,
                            dialog.PosX,
                            dialog.PosY,
                            dialog.SigWidth,
                            dialog.SigHeight
                        );

                        OpenTab(saveDialog.FileName);
                        MessageBox.Show(this, "Assinatura digital aplicada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Erro ao aplicar assinatura no PDF:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnToolSplit_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null)
            {
                MessageBox.Show(this, "Abra um arquivo PDF primeiro para extrair páginas.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SplitPdfDialog(_activeTab.TotalPages, 1) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _editingService.ExtractPages(_activeTab.FilePath, dialog.OutputFilePath, dialog.SelectedPages);
                    MessageBox.Show(this, $"Páginas extraídas com sucesso para:\n{dialog.OutputFilePath}", "Extração Concluída", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Erro ao extrair páginas do PDF:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnToolMerge_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MergePdfDialog(_activeTab?.FilePath) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _editingService.MergePdfs(dialog.FilesToMerge, dialog.OutputFilePath);
                    var result = MessageBox.Show(this, $"PDFs mesclados com sucesso em:\n{dialog.OutputFilePath}\n\nDeseja abrir o documento mesclado agora?", "Mesclagem Concluída", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        OpenTab(dialog.OutputFilePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Erro ao mesclar arquivos PDF:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnToolRotate_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null)
            {
                MessageBox.Show(this, "Abra um arquivo PDF primeiro para girar páginas.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string tempOut = Path.Combine(Path.GetDirectoryName(_activeTab.FilePath)!,
                    Path.GetFileNameWithoutExtension(_activeTab.FilePath) + "_girado.pdf");

                _editingService.RotatePages(_activeTab.FilePath, tempOut, Enumerable.Range(1, _activeTab.TotalPages), 90);

                OpenTab(tempOut);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Erro ao girar documento:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

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

            if (report.IntegrityScore >= 100.0)
            {
                var greenColor = (Color)ColorConverter.ConvertFromString("#10B981");
                TxtHeaderDocType.Foreground = new SolidColorBrush(greenColor);
                BadgeIntegrity.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A2F"));
                BadgeIntegrity.BorderBrush = new SolidColorBrush(greenColor);
                TxtIntegrityScore.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
            }
            else if (report.IntegrityScore >= 70.0)
            {
                var yellowColor = (Color)ColorConverter.ConvertFromString("#F59E0B");
                TxtHeaderDocType.Foreground = new SolidColorBrush(yellowColor);
                BadgeIntegrity.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D3215"));
                BadgeIntegrity.BorderBrush = new SolidColorBrush(yellowColor);
                TxtIntegrityScore.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            }
            else if (report.IntegrityScore >= 35.0)
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
            else if (report.ImportVerdict.StartsWith("Atenção") ||
                     report.ImportVerdict.Contains("pode importar com falha") ||
                     report.ImportVerdict.Contains("pode importar com erros"))
            {
                TxtImportVerdictIcon.Text = "⚠️";
                BorderImportVerdict.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D3215"));
                BorderImportVerdict.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                TxtImportVerdict.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            }
            else if (report.ImportVerdict.Contains("grandes chances", StringComparison.OrdinalIgnoreCase) ||
                     report.ImportVerdict.Contains("Grandes chances"))
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

        #region Default App Banner

        private void CheckDefaultAppBanner()
        {
            try
            {
                bool isDef = DefaultAppService.IsDefaultPdfReader(isLite: false);
                bool dismissed = DefaultAppService.IsDismissed(isLite: false);
                BannerDefaultApp.Visibility = (!isDef && !dismissed) ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                BannerDefaultApp.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSetDefaultBanner_Click(object sender, RoutedEventArgs e)
        {
            BannerDefaultApp.Visibility = Visibility.Collapsed;
            DefaultAppService.RegisterAndSetDefault(isLite: false);
        }

        private void BtnDismissDefaultBanner_Click(object sender, RoutedEventArgs e)
        {
            BannerDefaultApp.Visibility = Visibility.Collapsed;
            DefaultAppService.DismissPrompt(isLite: false);
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
