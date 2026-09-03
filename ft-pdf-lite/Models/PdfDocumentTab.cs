using System;
using System.IO;
using FtPdfLite.Services;

namespace FtPdfLite.Models
{
    public class PdfDocumentTab : IDisposable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string FileName => Path.GetFileName(FilePath);
        public int TotalPages { get; set; } = 1;
        public ExtractionResult? Extraction { get; set; }

        public void Dispose()
        {
        }
    }
}
