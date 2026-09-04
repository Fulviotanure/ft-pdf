# FT PDF Suite

Repositório unificado contendo as duas edições oficiais do **FT PDF** desenvolvidas em **C# (.NET 10 WPF)**:

---

## 📁 Estrutura do Projeto

```text
ft-pdf/
├── FtPdf.slnx                       # Solution unificada (.NET 10)
├── ft-pdf/                          # Edição Completa (Leitura, Validação e Ferramentas de Edição)
│   ├── FtPdf.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / .cs
│   ├── Dialogs/
│   ├── Services/
│   ├── Models/
│   └── Assets/
├── ft-pdf-lite/                     # Edição Ultraleve (Leitura e Validação, sem ferramentas de edição)
│   ├── FtPdfLite.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / .cs
│   ├── SettingsWindow.xaml / .cs
│   ├── Services/
│   ├── Models/
│   ├── installer/
│   └── Assets/
├── Iniciar FT PDF.bat               # Inicializador rápido da edição completa
├── Iniciar FT PDF Lite.bat          # Inicializador rápido da edição lite
└── .github/workflows/
    ├── release-ft-pdf.yml           # CI/CD & Releases da Edição Completa
    └── release-ft-pdf-lite.yml      # CI/CD & Releases da Edição Lite
```

---

## 🚀 Como Acionar os Releases no GitHub Actions

### 1. Release do FT PDF (Edição Completa)
- **Tag:** `v2.0.0`, `v2.1.0` (ou qualquer tag `vX.Y.Z` ou `ft-pdf-v*`)
- **Manual:** Na aba **Actions** do GitHub, selecione o workflow **"Release FT PDF"** > **Run workflow**.
- **Artefatos:** `FtPdf.exe` e `FT-PDF-Windows-x64.zip`.

### 2. Release do FT PDF Lite (Edição Ultraleve)
- **Tag:** `lite-v2.0.0`, `v2.0.0-lite` (ou qualquer tag `lite-v*` ou `v*-lite`)
- **Manual:** Na aba **Actions** do GitHub, selecione o workflow **"Release FT PDF Lite"** > **Run workflow**.
- **Artefatos:** `FtPdfLite.exe` e `FT-PDF-Lite-Windows-x64.zip`.
