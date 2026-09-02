# FT PDF - Leitor, Editor e Validador de Documentos PDF

Um aplicativo nativo, leve e moderno para Windows desenvolvido em **C# (.NET 10)** e **WPF**, focado em leitura rápida, integridade de dados e ferramentas de edição de PDF 100% offline.

---

## 🚀 Funcionalidades Principais

- **📄 Visualizador Nativo Rápido**:
  - Renderização fluida de páginas com rolagem suave.
  - Suporte completo a **Múltiplas Abas** para trabalhar com vários documentos PDF ao mesmo tempo.

- **🔍 Análise de Integridade e Validação**:
  - **Classificação do Documento**: Identifica automaticamente textos nativos, documentos escaneados (imagens) e documentos com fontes criptografadas ou caracteres corrompidos.
  - **Veredito de Importação**: Informa se o arquivo importa perfeitamente, com ressalvas ou se não é importável.
  - **Bloco de Notas Integrado**: Extração de texto preservando layout ou texto cru (raw glyphs), com cópia rápida e exportação para `.txt`.
  - **Propriedades do Documento**: Metadados completos (autor, produtor, versão do PDF, tamanho, dimensões e segurança).

- **✍️ Suíte de Edição Interativa**:
  - **Inserção de Texto no Clique**: Clique em qualquer local da página para abrir uma caixa de texto flutuante com escolha de tamanho (10 a 32 pt) e cores (Branco, Preto, Vermelho).
  - **Marcador Amarelo Direto**: Arraste o mouse sobre qualquer trecho para grifar com marca-texto amarelo semitransparente.
  - **Assinatura Digital / Rubrica**: Desenhe sua assinatura com mouse/caneta ou carregue uma imagem para estampar nas páginas.
  - **Dividir e Extrair Páginas**: Separe intervalos de páginas selecionadas em novos arquivos PDF.
  - **Mesclar PDFs**: Una múltiplos documentos PDF em uma ordem personalizada.
  - **Girar Páginas**: Rotação rápida em 90° e 180°.

---

## 🛠️ Tecnologias Utilizadas

- **Linguagem**: C# 13 / .NET 10 (Windows Desktop)
- **Interface**: WPF (Windows Presentation Foundation) com tema escuro moderno
- **Renderização e Extração**: PdfiumViewer, UglyToad.PdfPig
- **Manipulação e Edição**: PDFsharp 6.x
- **CI/CD**: GitHub Actions para compilação e empacotamento automatizado

---

## 📦 Como Executar

1. Certifique-se de ter o [.NET 10 SDK](https://dotnet.microsoft.com/download) instalado.
2. Clone o repositório:
   ```bash
   git clone https://github.com/Fulviotanure/ft-pdf.git
   cd ft-pdf
   ```
3. Compile e execute:
   ```bash
   dotnet run
   ```
