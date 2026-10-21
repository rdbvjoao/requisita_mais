# CRUD web de requisitos — 20/10/2026

## Entrega e limites

Implementadas US2–US4 e a parte de CRUD da US10 conforme spec.md, plan.md,
data-model.md e contracts/mvc.md. Foram reaproveitados Requisito,
ApplicationDbContext, mapeamentos SQL, Identity, AcessoProprietarioService,
RelogioLocal, TimeProvider e o layout Bootstrap/Razor existentes. Não havia
serviços de cadastro/consulta/edição/arquivamento a duplicar nem interfaces
dessas operações na base inspecionada.

- GET/POST `/Requisitos/Criar`: título obrigatório até 150, descrição obrigatória
  até 5.000 unidades UTF-16 e prioridade explícita Alta/Média/Baixa, sem default.
  Preserva Unicode e espaços finais. Proprietário, datas UTC, Rascunho, versão 1
  e contador zero são atribuídos pelo servidor. Sucesso somente após gravação.
- GET `/Requisitos`: somente requisitos próprios não arquivados, ordenados por
  cadastro/ID decrescentes, estado vazio e navegação para detalhes.
- GET `/Requisitos/Detalhes/{id}`: conteúdo codificado pelo Razor, prioridade,
  status, versão e datas apresentadas em São Paulo.
- GET/POST `/Requisitos/Editar/{id}`: validações e bloqueio de arquivados;
  comparação ordinal exata; título/descrição alterados incrementam uma única
  versão e retornam a Rascunho. Somente prioridade conserva versão/status;
  edição idêntica não grava nem altera datas. A→B→A produz versão 3.
- POST `/Requisitos/Arquivar/{id}`: exclusão exclusivamente lógica, confirmação
  explícita e rowversion; preserva texto, versão, contador e histórico. GET
  `/Requisitos/Arquivados` oferece consulta própria separada somente leitura.
  Não existem ações de exclusão física ou restauração.
- Conflitos de rowversion/versão textual e `DbUpdateConcurrencyException` exigem
  recarga explícita, sem reaplicar automaticamente. Gravação usa o rowversion
  confirmado como valor original no EF. Cada mutação usa um SaveChangesAsync;
  não há transação aberta durante interação com o usuário.
- Autorização Identity e consultas do serviço de propriedade existentes em
  todas as operações; recurso alheio e inexistente têm a mesma resposta 404.
  Antiforgery global existente preservado; ViewModels específicos e rejeição de
  campos controlados pelo servidor; ID de recurso é obtido da rota.
- Views responsivas com tabela rolável, formulários, ajuda dos campos, erros,
  confirmação de arquivamento e mensagens de sucesso via POST/redirect/GET.
  Validação de texto não branco também no cliente, sem normalizar a redação.

A pesquisa/filtros previstos são os de **análises** (US11), não de requisitos;
não foram acrescentados filtros ou endpoints fora deste CRUD. Não foi
implementado Ollama, análise ativa, revisão ou navegação de histórico de
análises. Registros históricos existentes são preservados pelos serviços; as
telas desses fluxos permanecem para suas histórias próprias.

## Verificações executadas

```powershell
dotnet build RequisitaPlus.slnx --no-restore
dotnet test RequisitaPlus.slnx --no-build --no-restore --filter "Category!=SqlServer&Category!=Ollama" --logger "console;verbosity=minimal"
node --check src/RequisitaPlus.Web/wwwroot/js/requisitos-validacao.js
node --test tests/RequisitaPlus.Tests/Client/requisitos-validacao.test.cjs
```

Resultado final: build com **0 avisos e 0 erros**; **492 testes .NET aprovados**,
nenhuma falha (51 novos casos de CRUD); **9 testes JavaScript aprovados**.
Integrações SQL/Ollama foram excluídas explicitamente pelo filtro, não
habilitadas. Não houve conexão a banco, migrations, DDL, instalação, commit
ou push nesta execução.

Os testes HTTP executam o pipeline MVC/Identity/Razor, com cookies e tokens
antiforgery reais do framework, via o suporte HTTP já existente. Somente o
armazenamento é simulado em testes; a aplicação continua usando SQL Server.
SimulatedRequisitoContext é um dublê exclusivo de testes: usa metadados EF do
SQL Server, consultas LINQ locais e snapshots por contexto; nunca abre conexão
e não é um provider de banco alternativo. Não é prova de comportamento SQL.

Cobertura: CRUD com reabertura; campos/limites/Unicode/espaços finais;
prioridade sem default; validação também em POST direto; status/proprietário
forjados; visitante, dois usuários, listas principal/arquivados e IDs diretos;
antiforgery ausente/adulterado; texto HTML codificado; versão e prioridade;
tokens inválidos e duas abas; conflito simulado entre leitura e commit;
confirmação e repetição do arquivamento; ausência de exclusão física/restore;
falha simulada de gravação sem persistência, sucesso falso ou mensagem SQL
exposta. Testes com análise/sugestões carregadas verificam todos os valores
históricos e decisões Pendente/Ajustada preservados após edição/arquivamento.
Testes JS verificam o método de validação e seu adaptador; não substituem teste
visual/interativo no navegador.

## Tarefas e pendências

Concluídas T026–T030, T032–T035, T037 e T039–T041. Os testes correspondentes
estão reunidos em `Contracts/RequisitoCrudHttpTests.cs`,
`Unit/RequisitoCrudTests.cs` e `Client/requisitos-validacao.test.cjs`, em vez de
separados por história. Essa organização não muda os contratos funcionais.

T031, T036, T038 e T042 continuam pendentes: exigem preparação/execução SQL
e validação do incremento que não foram autorizadas nesta execução. Não foi
declarado concluído nenhum gate de integração real.

T103–T106 têm implementação/testes locais de CRUD, mas ficam sem marcação de
conclusão integral: T103 inclui testes SQL e bloqueios dos fluxos de IA/revisão;
T104 inclui a corrida com inferência em andamento; T105 envolve acesso ao
histórico de análises; T106 depende desse conjunto. As rotas, formulário e
consulta de arquivados do CRUD estão disponíveis. T107–T108 continuam
pendentes. Nenhuma tarefa de IA, filtros de análises ou operação foi marcada.

Para executar com banco real:

1. Disponibilizar a instância SQL Server e um banco previamente provisionado
   com o esquema da migration inicial já aplicado. Se isso ainda não estiver
   pronto, a preparação/aplicação deve ocorrer em execução própria autorizada,
   pelos mecanismos de segurança e executor existentes.
2. Configurar `ConnectionStrings:DefaultConnection` por User Secrets ou
   configuração externa, conforme a segurança existente; usar o HTTPS local
   já configurado e iniciar `dotnet run --project src/RequisitaPlus.Web`.
3. Autenticar e acessar `/Requisitos`. Validar persistência entre acessos,
   concorrência real com duas abas, rollback, FKs e preservação histórica em
   ambiente autorizado. Fazer a verificação visual desktop/móvel no navegador.

Nada desse procedimento de banco ou validação manual foi executado aqui.
Nenhum esquema, migration, executor controlado, configuração de segurança SQL
ou configuração/fixture dos testes SQL foi alterado nesta entrega.

## Inventário desta execução

Criados:

- `src/RequisitaPlus.Web/Controllers/RequisitosController.cs`
- `src/RequisitaPlus.Web/Services/Requisitos/CadastroRequisitoService.cs`
- `src/RequisitaPlus.Web/Services/Requisitos/ConsultaRequisitoService.cs`
- `src/RequisitaPlus.Web/Services/Requisitos/EdicaoRequisitoService.cs`
- `src/RequisitaPlus.Web/Services/Requisitos/ArquivamentoRequisitoService.cs`
- `src/RequisitaPlus.Web/ViewModels/Requisitos/CriarRequisitoViewModel.cs`
- `src/RequisitaPlus.Web/ViewModels/Requisitos/EditarRequisitoViewModel.cs`
- `src/RequisitaPlus.Web/ViewModels/Requisitos/DetalhesRequisitoViewModel.cs`
- `src/RequisitaPlus.Web/Views/Requisitos/Criar.cshtml`
- `src/RequisitaPlus.Web/Views/Requisitos/Index.cshtml`
- `src/RequisitaPlus.Web/Views/Requisitos/Detalhes.cshtml`
- `src/RequisitaPlus.Web/Views/Requisitos/Editar.cshtml`
- `src/RequisitaPlus.Web/Views/Requisitos/Arquivados.cshtml`
- `src/RequisitaPlus.Web/Views/Requisitos/_Campos.cshtml`
- `src/RequisitaPlus.Web/Views/Requisitos/_Lista.cshtml`
- `src/RequisitaPlus.Web/wwwroot/js/requisitos-validacao.js`
- `tests/RequisitaPlus.Tests/Contracts/RequisitoCrudHttpTests.cs`
- `tests/RequisitaPlus.Tests/Unit/RequisitoCrudTests.cs`
- `tests/RequisitaPlus.Tests/Support/SimulatedRequisitoContext.cs`
- `tests/RequisitaPlus.Tests/Client/requisitos-validacao.test.cjs`
- `docs/validacao/crud-requisitos.md`

Alterados:

- `src/RequisitaPlus.Web/Program.cs`: registro dos quatro serviços scoped.
- `src/RequisitaPlus.Web/Services/Requisitos/AcessoProprietarioService.cs`:
  expõe a resolução existente do usuário para cadastro, sem duplicar regra.
- `src/RequisitaPlus.Web/Views/Shared/_Layout.cshtml`: navegação, idioma e
  apresentação das mensagens de sucesso.
- `tests/RequisitaPlus.Tests/Support/IdentityHttpApplication.cs`: callback
  opcional de DI para reaproveitar o pipeline existente nos testes de CRUD.
- `specs/001-analisar-requisitos-mvp/tasks.md`: somente tarefas verificadas e
  registro do recorte entregue.

Alterações preexistentes em `docs/validacao/base.md`,
`docs/validacao/migrations.md`, `Integration/SqlServer/SqlServerFixture.cs`,
`RequisitaPlus.Tests.csproj`, `Operations/` e
`Unit/ApprovedMigrationExecutorTests.cs` foram preservadas e não fazem parte
desta implementação.
