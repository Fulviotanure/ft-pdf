using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FtPdf.Dialogs;
using FtPdf.Models;
using FtPdf.Services;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
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
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = System.Windows.Controls.ComboBox;
using Orientation = System.Windows.Controls.Orientation;
using Image = System.Windows.Controls.Image;

namespace FtPdf
{
    public enum ActiveToolMode
    {
        None,
        InsertText,
        Highlight
    }

    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<PdfDocumentTab> _tabs = new();
        private PdfDocumentTab? _activeTab;
        private readonly PdfExtractionService _extractionService = new();
        private readonly PdfEditingService _editingService = new();
        private bool _isRawTextMode = false;
        private bool _isNotepadOpen = false;
        private ActiveToolMode _currentToolMode = ActiveToolMode.None;

        // Highlighter dragging state
        private bool _isHighlightDragging = false;
        private System.Windows.Point _highlightStartPoint;
        private System.Windows.Shapes.Rectangle? _currentHighlightRect;

        // In-place Text Box overlay
        private Border? _activeInPlaceBox;

        public MainWindow()
        {
            InitializeComponent();
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

        public async void OpenTab(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show(this, $"O arquivo não foi encontrado:\n{filePath}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // If already open in a tab, just activate it
                var existing = _tabs.FirstOrDefault(t => t.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    SetActiveTab(existing);
                    return;
                }

                var tab = new PdfDocumentTab
                {
                    FilePath = filePath,
                    Document = PdfiumViewer.PdfDocument.Load(filePath)
                };

                // Render pages
                await RenderAllPagesForTab(tab);

                _tabs.Add(tab);
                SetActiveTab(tab);

                // Run extraction & integrity analysis in background
                _ = Task.Run(() =>
                {
                    var result = _extractionService.ExtractAndAnalyze(filePath);
                    Dispatcher.Invoke(() =>
                    {
                        tab.Extraction = result;
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

        private async Task RenderAllPagesForTab(PdfDocumentTab tab)
        {
            if (tab.Document == null) return;
            tab.RenderedPages.Clear();

            int count = tab.TotalPages;
            for (int i = 0; i < count; i++)
            {
                int pageIndex = i;
                var bmpSource = await Task.Run(() => PdfEditingService.RenderPageToBitmapSource(tab.Document, pageIndex, 150));
                tab.RenderedPages.Add(bmpSource);
            }
        }

        private void SetActiveTab(PdfDocumentTab tab)
        {
            _activeTab = tab;
            UpdateTabsBar();

            TxtTitle.Text = tab.FileName;
            BadgeDocTitle.Visibility = Visibility.Visible;

            PanelEmptyState.Visibility = Visibility.Collapsed;
            PanelActiveContent.Visibility = Visibility.Visible;
            BorderTabs.Visibility = Visibility.Visible;
            BtnCloseFile.Visibility = Visibility.Visible;
            BtnToggleNotepad.Visibility = Visibility.Visible;
            BtnQuickSave.Visibility = Visibility.Visible;
            BtnQuickCopy.Visibility = Visibility.Visible;

            DisplayActivePages();
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
            TxtTitle.Text = "Nenhum documento aberto";
            BadgeDocTitle.Visibility = Visibility.Collapsed;
            PanelEmptyState.Visibility = Visibility.Visible;
            PanelActiveContent.Visibility = Visibility.Collapsed;
            BorderTabs.Visibility = Visibility.Collapsed;
            BtnCloseFile.Visibility = Visibility.Collapsed;
            BtnToggleNotepad.Visibility = Visibility.Collapsed;
            BtnQuickSave.Visibility = Visibility.Collapsed;
            BtnQuickCopy.Visibility = Visibility.Collapsed;
            BarActiveToolHint.Visibility = Visibility.Collapsed;
            PdfPagesContainer.Children.Clear();
            CloseNotepad();
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
                        ? (Color)ColorConverter.ConvertFromString("#2563EB") 
                        : (Color)ColorConverter.ConvertFromString("#1E293B")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 4, 8, 4),
                    Margin = new Thickness(0, 0, 6, 0),
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
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = new SolidColorBrush(isActive ? Colors.White : (Color)ColorConverter.ConvertFromString("#94A3B8")),
                    MaxWidth = 180,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var closeBtn = new Button
                {
                    Content = "✕",
                    FontSize = 10,
                    Width = 18,
                    Height = 18,
                    Margin = new Thickness(6, 0, 0, 0),
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
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

        #region Page Display & Interactive Canvas

        private void DisplayActivePages()
        {
            PdfPagesContainer.Children.Clear();
            RemoveInPlaceBox();

            if (_activeTab == null || _activeTab.RenderedPages.Count == 0) return;

            for (int i = 0; i < _activeTab.RenderedPages.Count; i++)
            {
                int pageIndex = i;
                var bmp = _activeTab.RenderedPages[i];

                double displayWidth = 820;
                double aspectRatio = (double)bmp.PixelHeight / bmp.PixelWidth;
                double displayHeight = displayWidth * aspectRatio;

                var pageGrid = new Grid
                {
                    Width = displayWidth,
                    Height = displayHeight,
                    Margin = new Thickness(0, 0, 0, 20),
                    Background = Brushes.White
                };

                // Drop shadow / border around page
                var border = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        Opacity = 0.35,
                        BlurRadius = 10,
                        ShadowDepth = 3
                    }
                };

                var img = new Image
                {
                    Source = bmp,
                    Width = displayWidth,
                    Height = displayHeight,
                    Stretch = Stretch.Uniform
                };

                // Interactive Overlay Canvas
                var canvas = new Canvas
                {
                    Width = displayWidth,
                    Height = displayHeight,
                    Background = Brushes.Transparent,
                    Tag = pageIndex // Stores the page index
                };

                // Update cursor based on tool
                UpdateCanvasCursor(canvas);

                // Mouse interaction on Canvas
                canvas.MouseLeftButtonDown += (s, e) => OnCanvasMouseDown(canvas, pageIndex, e);
                canvas.MouseMove += (s, e) => OnCanvasMouseMove(canvas, e);
                canvas.MouseLeftButtonUp += (s, e) => OnCanvasMouseUp(canvas, pageIndex, e);

                border.Child = img;
                pageGrid.Children.Add(border);
                pageGrid.Children.Add(canvas);

                PdfPagesContainer.Children.Add(pageGrid);
            }
        }

        private void UpdateCanvasCursor(Canvas canvas)
        {
            switch (_currentToolMode)
            {
                case ActiveToolMode.InsertText:
                    canvas.Cursor = Cursors.Cross;
                    break;
                case ActiveToolMode.Highlight:
                    canvas.Cursor = Cursors.Cross;
                    break;
                default:
                    canvas.Cursor = Cursors.IBeam;
                    break;
            }
        }

        private void RefreshAllCanvasCursors()
        {
            foreach (var child in PdfPagesContainer.Children)
            {
                if (child is Grid g)
                {
                    foreach (var elem in g.Children)
                    {
                        if (elem is Canvas c)
                        {
                            UpdateCanvasCursor(c);
                        }
                    }
                }
            }
        }

        #endregion

        #region Tool Modes (InsertText, Highlight)

        private void BtnToolText_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null) return;

            if (_currentToolMode == ActiveToolMode.InsertText)
            {
                ExitToolMode();
            }
            else
            {
                _currentToolMode = ActiveToolMode.InsertText;
                TxtToolHintIcon.Text = "✍️";
                TxtToolHint.Text = "Modo Inserir Texto: Clique em qualquer local da página para adicionar sua anotação.";
                BarActiveToolHint.Visibility = Visibility.Visible;
                BtnToolText.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                BtnToolHighlight.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                RefreshAllCanvasCursors();
            }
        }

        private void BtnToolHighlight_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null) return;

            if (_currentToolMode == ActiveToolMode.Highlight)
            {
                ExitToolMode();
            }
            else
            {
                _currentToolMode = ActiveToolMode.Highlight;
                TxtToolHintIcon.Text = "🖍️";
                TxtToolHint.Text = "Marcador Amarelo: Clique e arraste sobre o texto para grifar diretamente na página.";
                BarActiveToolHint.Visibility = Visibility.Visible;
                BtnToolHighlight.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAB308"));
                BtnToolText.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                RefreshAllCanvasCursors();
            }
        }

        private void BtnExitToolMode_Click(object sender, RoutedEventArgs e)
        {
            ExitToolMode();
        }

        private void ExitToolMode()
        {
            _currentToolMode = ActiveToolMode.None;
            BarActiveToolHint.Visibility = Visibility.Collapsed;
            BtnToolText.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            BtnToolHighlight.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            RemoveInPlaceBox();
            RefreshAllCanvasCursors();
        }

        #endregion

        #region In-Place Text Insertion Box

        private void OnCanvasMouseDown(Canvas canvas, int pageIndex, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(canvas);

            if (_currentToolMode == ActiveToolMode.InsertText)
            {
                SpawnInPlaceTextBox(canvas, pageIndex, pos);
            }
            else if (_currentToolMode == ActiveToolMode.Highlight)
            {
                _isHighlightDragging = true;
                _highlightStartPoint = pos;

                _currentHighlightRect = new System.Windows.Shapes.Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(100, 250, 204, 21)), // Yellow highlight #FACC15
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAB308")),
                    StrokeThickness = 1
                };

                Canvas.SetLeft(_currentHighlightRect, pos.X);
                Canvas.SetTop(_currentHighlightRect, pos.Y);
                canvas.Children.Add(_currentHighlightRect);
                canvas.CaptureMouse();
            }
        }

        private void OnCanvasMouseMove(Canvas canvas, System.Windows.Input.MouseEventArgs e)
        {
            if (_isHighlightDragging && _currentHighlightRect != null)
            {
                var cur = e.GetPosition(canvas);
                double x = Math.Min(_highlightStartPoint.X, cur.X);
                double y = Math.Min(_highlightStartPoint.Y, cur.Y);
                double w = Math.Abs(cur.X - _highlightStartPoint.X);
                double h = Math.Abs(cur.Y - _highlightStartPoint.Y);

                Canvas.SetLeft(_currentHighlightRect, x);
                Canvas.SetTop(_currentHighlightRect, y);
                _currentHighlightRect.Width = Math.Max(1, w);
                _currentHighlightRect.Height = Math.Max(1, h);
            }
        }

        private async void OnCanvasMouseUp(Canvas canvas, int pageIndex, MouseButtonEventArgs e)
        {
            if (_isHighlightDragging && _currentHighlightRect != null && _activeTab != null)
            {
                canvas.ReleaseMouseCapture();
                _isHighlightDragging = false;

                double x = Canvas.GetLeft(_currentHighlightRect);
                double y = Canvas.GetTop(_currentHighlightRect);
                double w = _currentHighlightRect.Width;
                double h = _currentHighlightRect.Height;

                canvas.Children.Remove(_currentHighlightRect);
                _currentHighlightRect = null;

                if (w > 5 && h > 4)
                {
                    // Convert canvas pixels to PDF points
                    using var pdfDoc = PdfReader.Open(_activeTab.FilePath, PdfDocumentOpenMode.Import);
                    var page = pdfDoc.Pages[pageIndex];
                    double scale = page.Width.Point / canvas.ActualWidth;

                    double pdfX = x * scale;
                    double pdfY = y * scale;
                    double pdfW = w * scale;
                    double pdfH = h * scale;

                    try
                    {
                        string tempOut = Path.Combine(Path.GetDirectoryName(_activeTab.FilePath)!,
                            Path.GetFileNameWithoutExtension(_activeTab.FilePath) + "_destaque.pdf");

                        _editingService.AddHighlight(
                            _activeTab.FilePath,
                            tempOut,
                            pageIndex + 1,
                            pdfX,
                            pdfY,
                            pdfW,
                            pdfH,
                            XColor.FromArgb(250, 204, 21) // Yellow
                        );

                        // Reload tab with modified file
                        _activeTab.Document?.Dispose();
                        _activeTab.FilePath = tempOut;
                        _activeTab.Document = PdfiumViewer.PdfDocument.Load(tempOut);
                        await RenderAllPagesForTab(_activeTab);
                        DisplayActivePages();
                        TxtTitle.Text = _activeTab.FileName;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Erro ao aplicar destaque:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SpawnInPlaceTextBox(Canvas canvas, int pageIndex, System.Windows.Point pos)
        {
            RemoveInPlaceBox();

            var container = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.5,
                    BlurRadius = 12,
                    ShadowDepth = 4
                }
            };

            var mainPanel = new StackPanel { Width = 280 };

            var txtInput = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 50,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                Padding = new Thickness(6),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Selected color state: Default is Black (0) or Red (1) or White (2)
            XColor selectedColor = XColors.Black;

            var toolbar = new Grid();
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Font size selector
            var comboSize = new ComboBox { SelectedIndex = 1, Height = 28, FontSize = 11.5, Margin = new Thickness(0, 0, 6, 0) };
            comboSize.Items.Add("10 pt");
            comboSize.Items.Add("14 pt");
            comboSize.Items.Add("18 pt");
            comboSize.Items.Add("24 pt");
            comboSize.Items.Add("32 pt");

            // Color buttons: White, Black, Red
            var colorPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var btnWhite = new Button
            {
                Content = "⚪",
                ToolTip = "Texto Branco",
                Width = 26,
                Height = 26,
                Margin = new Thickness(1, 0, 2, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnWhite.Click += (s, e) =>
            {
                selectedColor = XColors.White;
                txtInput.Foreground = Brushes.White;
            };

            var btnBlack = new Button
            {
                Content = "⚫",
                ToolTip = "Texto Preto",
                Width = 26,
                Height = 26,
                Margin = new Thickness(1, 0, 2, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnBlack.Click += (s, e) =>
            {
                selectedColor = XColors.Black;
                txtInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            };

            var btnRed = new Button
            {
                Content = "🔴",
                ToolTip = "Texto Vermelho",
                Width = 26,
                Height = 26,
                Margin = new Thickness(1, 0, 2, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnRed.Click += (s, e) =>
            {
                selectedColor = XColors.Red;
                txtInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            };

            colorPanel.Children.Add(btnWhite);
            colorPanel.Children.Add(btnBlack);
            colorPanel.Children.Add(btnRed);

            // Apply & Cancel buttons
            var actionPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var btnApply = new Button
            {
                Content = "✓",
                ToolTip = "Gravar Texto no PDF",
                Width = 28,
                Height = 28,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0)
            };

            var btnCancel = new Button
            {
                Content = "✕",
                ToolTip = "Cancelar",
                Width = 28,
                Height = 28,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            actionPanel.Children.Add(btnApply);
            actionPanel.Children.Add(btnCancel);

            Grid.SetColumn(comboSize, 0);
            Grid.SetColumn(colorPanel, 1);
            Grid.SetColumn(actionPanel, 2);

            toolbar.Children.Add(comboSize);
            toolbar.Children.Add(colorPanel);
            toolbar.Children.Add(actionPanel);

            mainPanel.Children.Add(txtInput);
            mainPanel.Children.Add(toolbar);
            container.Child = mainPanel;

            // Placement on canvas
            Canvas.SetLeft(container, Math.Min(pos.X, canvas.ActualWidth - 300));
            Canvas.SetTop(container, Math.Min(pos.Y, canvas.ActualHeight - 110));

            canvas.Children.Add(container);
            _activeInPlaceBox = container;
            txtInput.Focus();

            btnCancel.Click += (s, e) => RemoveInPlaceBox();

            btnApply.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtInput.Text) || _activeTab == null)
                {
                    RemoveInPlaceBox();
                    return;
                }

                double fontSize = comboSize.SelectedIndex switch
                {
                    0 => 10,
                    1 => 14,
                    2 => 18,
                    3 => 24,
                    4 => 32,
                    _ => 14
                };

                // Convert canvas coordinates to PDF points
                using var pdfDoc = PdfReader.Open(_activeTab.FilePath, PdfDocumentOpenMode.Import);
                var page = pdfDoc.Pages[pageIndex];
                double scale = page.Width.Point / canvas.ActualWidth;

                double pdfX = pos.X * scale;
                double pdfY = pos.Y * scale;

                try
                {
                    string tempOut = Path.Combine(Path.GetDirectoryName(_activeTab.FilePath)!,
                        Path.GetFileNameWithoutExtension(_activeTab.FilePath) + "_editado.pdf");

                    _editingService.InsertText(
                        _activeTab.FilePath,
                        tempOut,
                        pageIndex + 1,
                        txtInput.Text,
                        pdfX,
                        pdfY,
                        fontSize,
                        selectedColor
                    );

                    RemoveInPlaceBox();

                    // Reload tab with modified file
                    _activeTab.Document?.Dispose();
                    _activeTab.FilePath = tempOut;
                    _activeTab.Document = PdfiumViewer.PdfDocument.Load(tempOut);
                    await RenderAllPagesForTab(_activeTab);
                    DisplayActivePages();
                    TxtTitle.Text = _activeTab.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Erro ao inserir texto no PDF:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private void RemoveInPlaceBox()
        {
            if (_activeInPlaceBox != null && _activeInPlaceBox.Parent is Canvas c)
            {
                c.Children.Remove(_activeInPlaceBox);
                _activeInPlaceBox = null;
            }
        }

        #endregion

        #region Other Editing Tools Handlers

        private void BtnToolSignature_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null || _activeTab.Document == null)
            {
                MessageBox.Show(this, "Abra um documento PDF primeiro para assinar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (_activeTab == null || _activeTab.Document == null)
            {
                MessageBox.Show(this, "Abra um documento PDF primeiro para extrair páginas.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async void BtnToolRotate_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab == null || _activeTab.Document == null) return;

            try
            {
                string tempOut = Path.Combine(Path.GetDirectoryName(_activeTab.FilePath)!,
                    Path.GetFileNameWithoutExtension(_activeTab.FilePath) + "_girado.pdf");

                _editingService.RotatePages(_activeTab.FilePath, tempOut, Enumerable.Range(1, _activeTab.TotalPages), 90);

                _activeTab.Document.Dispose();
                _activeTab.FilePath = tempOut;
                _activeTab.Document = PdfiumViewer.PdfDocument.Load(tempOut);
                await RenderAllPagesForTab(_activeTab);
                DisplayActivePages();
                TxtTitle.Text = _activeTab.FileName;
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
            BtnToggleNotepad.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            BtnToggleNotepad.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
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

        private void TxtEditor_TextChanged(object sender, TextChangedEventArgs e) => UpdateEditorStats();

        private void UpdateEditorStats()
        {
            int chars = TxtEditor.Text.Length;
            int words = TxtEditor.Text.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            TxtEditorStats.Text = $"Caracteres: {chars:N0} | Palavras: {words:N0}";
        }

        private void BtnCloseFile_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTab != null) CloseTab(_activeTab);
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
