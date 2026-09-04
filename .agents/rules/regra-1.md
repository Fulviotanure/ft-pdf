# Regra 1: Diretrizes de Edição, Compilação e Verificação de Código

Esta regra define o protocolo obrigatório para operações de desenvolvimento, compilação e deploy neste projeto:

1. **Edição Exclusivamente Local**:
   - Sempre que for solicitada qualquer edição, alteração ou refatoração, o agente deve operar e modificar estritamente os arquivos locais contidos no repositório/workspace.
   - Não tentar aplicar alterações remotamente sem antes validar nos arquivos locais.

2. **Protocolo Obrigatório ao Compilar**:
   - Ao receber o comando ou solicitação para compilar:
     a) Realizar a compilação local dos projetos para assegurar integridade.
     b) Verificar e atualizar as GitHub Actions automáticas no Git garantindo as devidas permissões e autorizações (`permissions: contents: write`) para que releases e artefatos sejam gerados sem falhas de autenticação/segurança.

3. **Verificação Preventiva de Erros e Discrepâncias**:
   - Antes e durante as compilações ou publicações, inspecionar minuciosamente os arquivos e o código:
     - Checar se versões em `.csproj`, instaladores e telas de "Sobre/Atualizações" estão sincronizadas.
     - Identificar e sanar arquivos órfãos, caminhos incorretos ou referências quebradas.
     - Garantir que tags e gatilhos de CI/CD estejam consistentes com a versão vigente.
