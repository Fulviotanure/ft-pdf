# 🎨 Guia de Estética, Cores e Design System: Landing Page de Download do FT PDF

Este documento mapeia todas as diretrizes visuais, paleta de cores, tipografia, tokens CSS, componentes e estrutura recomendada para a criação de uma página moderna, veloz e de altíssima conversão para download do **FT PDF** e **FT PDF Lite**.

---

## 1. Filosofia de Design & Identidade Visual

- **Conceito:** *Modern Dark Glassmorphism* (estilo Vercel, Linear, Stripe e Windows 11 Fluent).
- **Sensação do Usuário:** Velocidade instantânea, precisão cirúrgica, segurança e elegância corporativa/técnica.
- **Tema Base:** Fundo escuro profundo (*Deep Navy Slate*), contrastado com tons azuis elétricos e ciano inspirados no logotipo oficial.
- **Acabamento:** Bordas sutis de 1px com transparência, elevações suaves com drop-shadows esfumaçados, cantos arredondados modernos (8px a 16px) e micro-interações responsivas.

---

## 2. Paleta de Cores e Tokens CSS

### A. Cores de Superfície e Fundo (Backgrounds)
| Nome do Token | Hexadecimal | RGB | Aplicação |
| :--- | :--- | :--- | :--- |
| `--bg-canvas` | `#0B1120` | `11, 17, 32` | Fundo principal da página (Ultra Dark Slate) |
| `--bg-surface-1` | `#0F172A` | `15, 23, 42` | Fundo secundário / Seções alternadas |
| `--bg-surface-card`| `#1E293B` | `30, 41, 59` | Superfície de cards, modais e containers |
| `--bg-surface-hover`| `#334155`| `51, 65, 85` | Hover de cards e botões secundários |
| `--bg-glass` | `rgba(30, 41, 59, 0.7)` | — | Fundo translúcido com `backdrop-filter: blur(16px)` |

---

### B. Cores de Destaque e Marca (Brand Colors)
Cores extraídas diretamente da geometria do novo logotipo oficial:

| Nome do Token | Hexadecimal | RGB | Aplicação |
| :--- | :--- | :--- | :--- |
| `--brand-cyan-light`| `#54C5F8` | `84, 197, 248` | Asa superior do logo / Destaques em texto gradiente |
| `--brand-cyan-mid` | `#29B6F6` | `41, 182, 246` | Asa intermediária / Links ativos e ícones |
| `--brand-primary` | `#2563EB` | `37, 99, 235` | Botão principal de download (CTA Primary) |
| `--brand-primary-hover`| `#1D4ED8` | `29, 78, 216` | Estado hover do botão de download principal |
| `--brand-deep` | `#01579B` | `1, 87, 155` | Sombra geométrica do logo / Gradientes escuros |

---

### C. Cores Semânticas e Badges
| Categoria | Background | Border | Texto | Aplicação |
| :--- | :--- | :--- | :--- | :--- |
| **Destaque Lite (Esmeralda)** | `#1E3A2F` | `#10B981` | `#34D399` | Badge "Versão Ultraleve", "0 MB Runtime" |
| **Sucesso / Ativo (Verde)** | `rgba(16, 185, 129, 0.15)` | `#10B981` | `#34D399` | "Disponível para Windows 10/11", "Verificado" |
| **Atenção (Âmbar)** | `#3F2E18` | `#F59E0B` | `#FBBF24` | Avisos informativos, notas de compatibilidade |
| **Destaque Tecnológico (Azul)** | `rgba(56, 189, 248, 0.12)` | `#38BDF8` | `#38BDF8` | Badge de versão (ex: `Release 2.0.0`) |

---

### D. Hierarquia de Texto (Tipografia de Cores)
| Nome do Token | Hexadecimal | Uso |
| :--- | :--- | :--- |
| `--text-primary` | `#F8FAFC` | Títulos (H1, H2), botões e chamadas principais |
| `--text-secondary` | `#CBD5E1` | Subtítulos, cabeçalhos de cards |
| `--text-muted` | `#94A3B8` | Parágrafos explicativos, descrições secundárias |
| `--text-subtle` | `#64748B` | Rodapé, notas de versão, tamanho de arquivo |

---

### E. Bordas e Divisores
| Nome do Token | Valor CSS | Aplicação |
| :--- | :--- | :--- |
| `--border-subtle` | `1px solid rgba(255, 255, 255, 0.08)` | Divisores de seção, borda padrão de cards |
| `--border-strong` | `1px solid #334155` | Contorno de cards em foco, inputs |
| `--border-highlight`| `1px solid #38BDF8` | Card selecionado ou em destaque especial |

---

### F. Bloco de Variáveis CSS (Copiar & Colar)
```css
:root {
  /* Fundo e Superfícies */
  --bg-canvas: #0B1120;
  --bg-surface-1: #0F172A;
  --bg-surface-card: #1E293B;
  --bg-surface-hover: #334155;
  --bg-glass: rgba(30, 41, 59, 0.75);

  /* Identidade Visual / Azul e Ciano */
  --brand-cyan-light: #54C5F8;
  --brand-cyan-mid: #29B6F6;
  --brand-primary: #2563EB;
  --brand-primary-hover: #1D4ED8;
  --brand-deep: #01579B;

  /* Semânticas */
  --badge-lite-bg: #1E3A2F;
  --badge-lite-border: #10B981;
  --badge-lite-text: #34D399;

  /* Textos */
  --text-primary: #F8FAFC;
  --text-secondary: #CBD5E1;
  --text-muted: #94A3B8;
  --text-subtle: #64748B;

  /* Bordas */
  --border-subtle: 1px solid rgba(255, 255, 255, 0.08);
  --border-strong: 1px solid #334155;
  --border-highlight: 1px solid #38BDF8;

  /* Sombras e Iluminação (Glows) */
  --shadow-card: 0 10px 30px -10px rgba(0, 0, 0, 0.6);
  --shadow-card-hover: 0 20px 40px -15px rgba(37, 99, 235, 0.25);
  --glow-primary: 0 0 50px -10px rgba(56, 189, 248, 0.35);
  --glow-button: 0 4px 20px rgba(37, 99, 235, 0.4);

  /* Raios de Arredondamento */
  --radius-sm: 6px;
  --radius-md: 10px;
  --radius-lg: 16px;
  --radius-full: 9999px;
}
```

---

## 3. Tipografia e Fontes

1. **Fonte Principal:** `'Inter'`, `'Outfit'` ou `'Segoe UI'`, sans-serif.
   - Import recomendado do Google Fonts:
     ```html
     <link rel="preconnect" href="https://fonts.googleapis.com">
     <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
     <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">
     ```
2. **Escala Tipográfica:**
   - **Hero H1:** `48px` a `56px` | Peso: `800` (ExtraBold) | Letter-spacing: `-0.025em`
   - **H2 (Seções):** `32px` a `38px` | Peso: `700` (Bold) | Letter-spacing: `-0.02em`
   - **H3 (Cards):** `20px` a `24px` | Peso: `600` (SemiBold)
   - **Parágrafos (Body):** `15px` a `16px` | Peso: `400` (Regular) | Line-height: `1.6`
   - **Botões (CTA):** `14px` a `15px` | Peso: `600` (SemiBold)
   - **Badges / Tags:** `11px` a `12px` | Peso: `700` (Bold) | Caixa alta ou Capitalized

---

## 4. Estrutura e Blueprint da Landing Page

### 📍 Bloco 1: Header / Navbar Flutuante
- **Design:** Barra fixa no topo com efeito vidro (`backdrop-filter: blur(12px)`), borda inferior sutil de 1px.
- **Elementos:**
  - Lado esquerdo: Logotipo oficial (`ft pdf logo.png`) em 32x32 + texto "FT PDF" com badge ao lado `v2.0.0`.
  - Centro: Links de âncora ("Recursos", "Comparativo", "Requisitos", "GitHub").
  - Lado direito: Botão rápido de ação "Baixar Agora" (CTA secundário em estilo outline ou preenchido suave).

---

### 📍 Bloco 2: Hero Section (Apresentação Principal)
- **Fundo:** Efeito gradiente radial de iluminação suave (*radial-glow*) atrás do título com cor `rgba(56, 189, 248, 0.15)`.
- **Badge de Topo:** `🚀 Nova Versão 2.0 Lançada • Leitor Nativo e Ultraleve` (fundo translúcido com borda cyan).
- **Título (H1):**
  - *"O leitor e editor de PDF moderno, rápido e sem complicações para Windows."*
  - Com a palavra "rápido" ou "FT PDF" em gradiente de texto (`background: linear-gradient(135deg, #54C5F8, #2563EB); -webkit-background-clip: text; color: transparent;`).
- **Subtítulo:**
  - *"Zero telas travando. Renderização vetorial acelerada por hardware, ferramentas diretas de edição e versão Lite de inicialização instantânea. 100% gratuito e independente."*
- **Botões de Ação do Hero:**
  - **Botão 1 (Principal):** `⬇️ Baixar FT PDF Completo` (Azul `#2563EB`, glow sutil, peso de download recomendado).
  - **Botão 2 (Alternativo):** `⚡ Baixar FT PDF Lite (Ultraleve)` (Fundo `#1E293B`, borda `#334155`, ícone de raio).
- **Micro-aviso abaixo dos botões:**
  - `✓ Windows 10 e 11 nativo • Sem necessidade de instalar runtimes do .NET • Self-Contained`

---

### 📍 Bloco 3: Grid Comparativo de Versões (Cards Lado a Lado)
Seção central que ajuda o usuário a escolher a edição certa:

#### Card 1: FT PDF (Edição Completa) — ⭐ Recomendado
- **Destaque Visual:** Borda sutilmente iluminada com gradiente azul/ciano e badge `Recomendado`.
- **Tamanho:** ~76.4 MB (Single-File Self-Contained).
- **Lista de Recursos:**
  - ✅ Leitor Vetorial Nativo com aceleração gráfica.
  - ✅ Múltiplas abas simultâneas de documentos.
  - ✅ **Caixa de Texto Flutuante Interativa** (fundo transparente, bordas pontilhadas, quebra de linha ajustável).
  - ✅ Ferramentas de Assinatura Digital, Destaque e Edição.
  - ✅ Mesclar, dividir e girar páginas PDF.
  - ✅ Validador e diagnóstico de integridade de arquivo.
  - ✅ Auto-atualização transparente integrada.
- **Botão de Download Direto:**
  - URL: `https://github.com/Fulviotanure/ft-pdf/releases/download/v2.0.0/FtPdf.exe`

#### Card 2: FT PDF Lite (Edição Ultraleve) — ⚡ Foco em Leitura
- **Destaque Visual:** Badge verde esmeralda `Edição Ultraleve`.
- **Tamanho:** ~59.1 MB (Single-File Self-Contained).
- **Lista de Recursos:**
  - ⚡ Inicialização ultrarrápida (menos de 1 segundo).
  - ⚡ Visualizador vetorial puro e leve.
  - ⚡ Múltiplas abas sem sobrecarga de memória.
  - ⚡ Bloco de notas integrado para rascunhos.
  - ⚡ Validador de integridade PDF.
  - ⚡ Zero ferramentas pesadas de edição (apenas leitura rápida).
  - ⚡ Auto-atualização transparente integrada.
- **Botão de Download Direto:**
  - URL: `https://github.com/Fulviotanure/ft-pdf/releases/download/lite-v2.0.0/FtPdfLite.exe`

---

### 📍 Bloco 4: Destaques Técnicos (Feature Grid)
Quatro cards em grid 2x2 ou 4x1 com ícones vetoriais:
1. **🚀 100% Self-Contained:** Funciona imediatamente com dois cliques em qualquer máquina, sem exigir instalação prévia de runtimes .NET.
2. **🎨 Tipografia e Ícones Vetoriais Nítidos:** Suporte nativo a telas High DPI (125%, 150%, 2K e 4K) com fontes cravadas em ClearType e ícones sem perda de qualidade.
3. **🔄 Atualizações em 1 Clique:** O programa detecta automaticamente novas versões e atualiza o executável sem reinstalações manuais.
4. **🔒 Privacidade Total:** Todos os documentos são processados localmente na sua máquina. Nenhum dado é enviado para servidores em nuvem.

---

### 📍 Bloco 5: Requisitos e Informações Técnicas
Tabela simples e limpa:
- **Sistema Operacional:** Windows 10 (64-bit) ou Windows 11 (64-bit).
- **Arquitetura:** x64 nativo.
- **Licença:** Gratuito / Código Aberto no GitHub.
- **Última Versão:** `v2.0.0` (Lançamento Oficial).

---

### 📍 Bloco 6: Rodapé (Footer)
- Logotipo oficial miniaturizado.
- Links para o Repositório do GitHub, Releases e Licença.
- Mensagem de direitos/créditos: *"FT PDF Suite • Criado para máxima eficiência e velocidade."*

---

## 5. Assets Oficiais Mapeados para a Página

| Asset | Caminho Local no Repositório | Uso Recomendado na Web |
| :--- | :--- | :--- |
| **Logotipo Mestre (Alta Resolução)** | `ft-pdf/Assets/ft pdf logo.png` | Hero Banner, Favicon HQ, Imagens de prévia Social (OpenGraph) |
| **Logotipo Quadrado (1024x1024)** | `ft-pdf/Assets/logo.png` | Navbar, cabeçalhos de cards |
| **Ícone Windows (.ico)** | `ft-pdf/Assets/app.ico` | Favicon do navegador (`favicon.ico`) |

---

## 6. URLs de Download dos Executáveis (GitHub Releases)

### 🌟 Links Permanentes ("Evergreen" - Sempre Baixam a Versão Mais Recente)
> **Recomendado para o Site e Botões de Download:** Estes links são **fixos e eternos**. O GitHub redireciona automaticamente (HTTP 302) para o arquivo do release mais recente publicado. **Você nunca mais precisará alterar os links no HTML do site a cada nova versão lançada!**

- **FT PDF Completo (.exe Direto):**
  ```
  https://github.com/Fulviotanure/ft-pdf/releases/latest/download/FtPdf.exe
  ```
- **FT PDF Completo (Pacote .zip):**
  ```
  https://github.com/Fulviotanure/ft-pdf/releases/latest/download/FT-PDF-Windows-x64.zip
  ```

- **FT PDF Lite (.exe Direto Ultraleve):**
  ```
  https://github.com/Fulviotanure/ft-pdf/releases/latest/download/FtPdfLite.exe
  ```
- **FT PDF Lite (Pacote .zip):**
  ```
  https://github.com/Fulviotanure/ft-pdf/releases/latest/download/FT-PDF-Lite-Windows-x64.zip
  ```

---

### 📌 Links Específicos por Versão (Versão Atual v2.0.0)
Como na versão 2.0.0 os lançamentos foram gerados em tags separadas no GitHub:
- **FT PDF Completo (.exe):** `https://github.com/Fulviotanure/ft-pdf/releases/download/v2.0.0/FtPdf.exe`
- **FT PDF Completo (.zip):** `https://github.com/Fulviotanure/ft-pdf/releases/download/v2.0.0/FT-PDF-Windows-x64.zip`
- **FT PDF Lite (.exe Ultraleve):** `https://github.com/Fulviotanure/ft-pdf/releases/download/lite-v2.0.0/FtPdfLite.exe`
- **FT PDF Lite (.zip):** `https://github.com/Fulviotanure/ft-pdf/releases/download/lite-v2.0.0/FT-PDF-Lite-Windows-x64.zip`

> **Atenção:** Na tag `v2.0.0` está apenas o FT PDF Completo (`FtPdf.exe`). O executável do FT PDF Lite está na tag `lite-v2.0.0` (`.../download/lite-v2.0.0/FtPdfLite.exe`). Para que o link permanente `latest/download/FtPdfLite.exe` funcione, você também pode simplesmente arrastar o arquivo `FtPdfLite.exe` para dentro do release `v2.0.0` no GitHub.

---

### 💡 Como exibir dinamicamente a Versão Atual no Site (Script JavaScript Opcional)
Se você quiser que o botão do site mostre automaticamente o número da versão (ex: `Baixar FT PDF v2.0.0`) consultando a API pública do GitHub sem precisar editar o site:

```javascript
// Busca a versão mais recente direto do GitHub Releases
fetch('https://api.github.com/repos/Fulviotanure/ft-pdf/releases/latest')
  .then(res => res.json())
  .then(data => {
    const versao = data.tag_name; // Exemplo: "v2.0.0"
    // Atualiza os badges ou textos dos botões na landing page
    document.querySelectorAll('.badge-versao').forEach(el => el.textContent = versao);
  })
  .catch(err => console.log('Usando versão padrão local:', err));
```

