# Requisita+ — Modernização visual v1

## Escopo e preservação

Modernização de apresentação na branch `feat/integracao-ollama`. O estado inicial continha alterações e arquivos não rastreados da integração com Ollama. Esses trabalhos foram preservados; nenhum comando de staging, commit, stash, reset, clean, merge ou push foi executado.

Não foram alterados controllers, serviços, ViewModels, entidades, migrations, Identity, configuração SQL, prompts ou mecanismos de execução/recuperação da IA. `Program.cs` permaneceu byte a byte igual ao início desta tarefa (SHA-256 `D303537141CB00B8E96BB3B6F134D084743CF6BB5E5E3A7A9FFA3A5530F313B5`).

## Arquivos desta entrega

Todos os caminhos de aplicação abaixo são relativos a `src/RequisitaPlus.Web/`:

- `wwwroot/css/site.css`: tokens de identidade, tipografia local, cartões, formulários, mensagens, foco visível e regras responsivas.
- `wwwroot/images/requisita-logo.svg`: logotipo geométrico original e local.
- `Views/Shared/_Layout.cshtml`: marca, navegação responsiva, seção ativa, atalho para o conteúdo e rodapé.
- `Views/Shared/_Layout.cshtml.css`: retirada dos estilos demonstrativos que conflitavam com a identidade central.
- `Views/Shared/_LoginPartial.cshtml`: apresentação da identificação do usuário e ações de autenticação, preservando o POST de saída.
- `Views/Home/Index.cshtml`: apresentação, dois acessos funcionais, painel explicativo e três cartões de funcionalidades.
- `Views/Requisitos/Index.cshtml`: painel de gestão e contagem real de ativos a partir do modelo já isolado por usuário.
- `Views/Requisitos/_Lista.cshtml`: tabela com status persistido, região rolável por teclado e mensagem de lista vazia.
- `Views/Requisitos/Criar.cshtml` e `Editar.cshtml`: formulários com superfície e estilos compartilhados.
- `Views/Requisitos/Detalhes.cshtml`: metadados, descrição, ações e área de arquivamento. Preservados os links anteriores para IA/histórico e todos os ramos condicionais.
- `Views/Requisitos/Arquivados.cshtml`: apresentação consistente da consulta somente leitura.
- `Views/Analises/Confirmar.cshtml`: texto original separado da confirmação de processamento local.
- `Views/Analises/Operacao.cshtml`: estado operacional em painel, sem progresso fictício ou atualização automática.
- `Views/Analises/Detalhes.cshtml`: snapshot, cartões de problemas, reescrita e decisão humana. Mantida a mensagem existente quando não há texto sugerido.
- `Views/Analises/Historico.cshtml`: lista de análises com versão, data e navegação existente.

O parcial `Views/Requisitos/_Campos.cshtml` não precisou ser alterado: recebe a aparência compartilhada pelo CSS. As páginas de Identity também recebem esses estilos sem alterações em seus arquivos.

## Indicadores

Somente **Requisitos ativos** é exibido em Meus requisitos. A contagem usa `Model.Count`, proveniente da consulta existente, sem nova consulta ou inferência a partir do status de análise.

As contagens de requisitos com análise, com sugestões pendentes e arquivados não estão disponíveis nesse modelo. Sua inclusão requer uma proposta separada de consulta agregada com isolamento por proprietário. Nenhum zero, percentual ou estado fictício foi usado para substituí-las.

## Verificação executada

- `dotnet build --no-restore`: compilação da solução e das Razor Views, sem avisos ou erros.
- Testes seguros: **650 aprovados, 0 falhas, 0 ignorados**. As categorias SQL Server e Ollama foram excluídas explicitamente; ambas as variáveis de opt-in foram definidas como `false` no processo de teste.
- Comparação das 11 Views/partials de requisitos e análises com uma cópia anterior à edição: preservados atributos de binding, ações/controllers/IDs de rotas, métodos, campos ocultos, valores, validações, limites, confirmações obrigatórias e condições existentes.
- Os testes HTTP existentes usam Identity, requisitos e cliente de IA simulados em memória; verificam CSRF, autorização, isolamento, concorrência, CRUD, arquivamento, histórico e escape do conteúdo da IA.
- O hash de `Program.cs` foi conferido após a edição.
- Links revisados contra as ações existentes. A rota Privacy continua disponível, sem link na navegação principal. Não foi criado um link global de análises.
- Revisão estática das regras responsivas: Home em uma coluna abaixo de 992 px, navegação recolhível, ações flexíveis, metadados em duas colunas abaixo de 576 px e rolagem horizontal restrita à tabela.

Não houve consulta SQL real, inferência real ou execução de migrations. Nenhum navegador está conectado às ferramentas desta sessão; não foi possível validar screenshots, dimensões renderizadas, contraste calculado ou interação responsiva em um navegador. A inspeção visual final continua necessária.

Para repetir os testes seguros em PowerShell:

```powershell
$env:REQUISITAPLUS_TEST_SQL_ENABLED = 'false'
$env:REQUISITAPLUS_TEST_OLLAMA_ENABLED = 'false'
dotnet test --no-restore --filter 'Category!=SqlServer&Category!=Ollama' --logger 'console;verbosity=minimal'
```

## Personalização

Edite os tokens de `:root` em `wwwroot/css/site.css` para trocar cores, fonte, raios e sombra. Não há fontes remotas nem dependências novas. Para trocar a marca, substitua `wwwroot/images/requisita-logo.svg`, preservando a proporção ou ajustando as dimensões dos dois elementos `img`. As cores internas do SVG estão no próprio arquivo.

## Publicação e conferência manual

Para gerar o pacote local, com o SDK .NET 10 e as dependências disponíveis:

```powershell
dotnet publish src/RequisitaPlus.Web/RequisitaPlus.Web.csproj -c Release -o artifacts/publish
```

Esse comando é uma instrução; publicação em servidor não foi executada. Utilize o procedimento de hospedagem já adotado pelo projeto, incluindo runtime .NET 10, HTTPS, configuração de ambiente, segredos e conectividade existentes. Inclua todo o pacote publicado, especialmente `wwwroot` e os assets gerados. Esta entrega não exige atualização de esquema.

Para abrir a aplicação local pelo perfil existente:

```powershell
dotnet run --project src/RequisitaPlus.Web --launch-profile https
```

Acesse `https://localhost:7234`. A Home mantém a autorização existente: entre com uma conta já cadastrada. Abrir áreas autenticadas pode consultar o banco; execute a conferência abaixo no ambiente de demonstração já autorizado, sem criar novas inferências apenas para validar aparência.

1. Confira Home, marca, navegação, identificação do usuário e saída. Verifique os dois botões de entrada e os três cartões.
2. Em larguras de 360, 768 e 1366 px, confira o menu recolhido/expandido, quebra dos botões, textos longos, formulário e rodapé. A página não deve ganhar rolagem lateral; a tabela pode rolar em sua própria região.
3. Navegue por Tab/Shift+Tab. Confira o atalho para conteúdo, foco visível, menu móvel, campos, botões e região da tabela.
4. Confira a contagem de ativos contra a lista do próprio usuário. Acesse também uma lista vazia e os arquivados.
5. Abra cadastro e edição e confira validações. Revise título, prioridade, status, versão, datas, descrição e botões em detalhes, inclusive para um requisito arquivado.
6. Abra a confirmação de IA sem enviar. Confira texto, versão, caixa obrigatória e cancelamento.
7. Use resultados e histórico já persistidos para conferir snapshots, sugestões sem reescrita, decisão humana e aviso de revisão. Não é necessário disparar uma nova análise.
8. Valide alterações de dados, conflitos de concorrência e arquivamento apenas com registros destinados a esse teste e no ambiente autorizado; o arquivamento existente não possui restauração.
