using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace FtPdf.Services
{
    public class PdfEditingService
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public static System.Windows.Media.Imaging.BitmapSource RenderPageToBitmapSource(PdfiumViewer.PdfDocument doc, int pageIndex, int dpi = 150)
        {
            using var img = doc.Render(pageIndex, dpi, dpi, true);
            using var bmp = new System.Drawing.Bitmap(img);
            var hBitmap = bmp.GetHbitmap();
            try
            {
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        public void InsertText(string sourcePath, string outputPath, int pageNumber, string text, double x, double y, double fontSize, XColor color)
        {
            using var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
            if (pageNumber < 1 || pageNumber > document.PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Número de página inválido.");

            var page = document.Pages[pageNumber - 1];
            using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            var font = new XFont("Arial", fontSize, XFontStyleEx.Regular);
            var brush = new XSolidBrush(color);

            gfx.DrawString(text, font, brush, new XPoint(x, y), XStringFormats.TopLeft);
            document.Save(outputPath);
        }

        public void InsertSignature(string sourcePath, string outputPath, int pageNumber, byte[] imageBytes, double x, double y, double width, double height)
        {
            using var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
            if (pageNumber < 1 || pageNumber > document.PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Número de página inválido.");

            var page = document.Pages[pageNumber - 1];
            using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            using var stream = new MemoryStream(imageBytes);
            using var image = XImage.FromStream(stream);

            gfx.DrawImage(image, x, y, width, height);
            document.Save(outputPath);
        }

        public void AddHighlight(string sourcePath, string outputPath, int pageNumber, double x, double y, double width, double height, XColor highlightColor)
        {
            using var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
            if (pageNumber < 1 || pageNumber > document.PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Número de página inválido.");

            var page = document.Pages[pageNumber - 1];
            using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            
            // Semi-transparent highlight brush
            var colorWithAlpha = XColor.FromArgb(120, highlightColor.R, highlightColor.G, highlightColor.B);
            var brush = new XSolidBrush(colorWithAlpha);

            gfx.DrawRectangle(brush, x, y, width, height);
            document.Save(outputPath);
        }

        public void MergePdfs(IEnumerable<string> filePaths, string outputPath)
        {
            using var outputDocument = new PdfDocument();

            foreach (var file in filePaths)
            {
                if (!File.Exists(file)) continue;

                using var inputDocument = PdfReader.Open(file, PdfDocumentOpenMode.Import);
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    outputDocument.AddPage(inputDocument.Pages[i]);
                }
            }

            outputDocument.Save(outputPath);
        }

        public void ExtractPages(string sourcePath, string outputPath, IEnumerable<int> pageNumbers)
        {
            using var inputDocument = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            var uniquePages = pageNumbers.Distinct().OrderBy(p => p);
            foreach (var pageNumber in uniquePages)
            {
                if (pageNumber >= 1 && pageNumber <= inputDocument.PageCount)
                {
                    outputDocument.AddPage(inputDocument.Pages[pageNumber - 1]);
                }
            }

            outputDocument.Save(outputPath);
        }

        public void RotatePages(string sourcePath, string outputPath, IEnumerable<int> pageNumbers, int angleDegrees)
        {
            using var document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify);
            var targetPages = new HashSet<int>(pageNumbers);

            for (int i = 0; i < document.PageCount; i++)
            {
                int pageNum = i + 1;
                if (targetPages.Contains(pageNum) || !targetPages.Any())
                {
                    var page = document.Pages[i];
                    page.Rotate = (page.Rotate + angleDegrees) % 360;
                }
            }

            document.Save(outputPath);
        }
    }
}
