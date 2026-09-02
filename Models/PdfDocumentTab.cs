using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using FtPdf.Services;
using PdfiumViewer;

namespace FtPdf.Models
{
    public class PdfDocumentTab : IDisposable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string FileName => Path.GetFileName(FilePath);
        public PdfDocument? Document { get; set; }
        public ExtractionResult? Extraction { get; set; }
        public List<BitmapSource> RenderedPages { get; set; } = new();
        public List<PageTextData> TextPages { get; set; } = new();
        public string CurrentlySelectedText { get; set; } = string.Empty;
        public int TotalPages => Document?.PageCount ?? 0;
        public int CurrentPage { get; set; } = 1;

        public void Dispose()
        {
            Document?.Dispose();
            Document = null;
            RenderedPages.Clear();
            TextPages.Clear();
        }
    }
}
