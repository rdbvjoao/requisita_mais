# Base e identidade — Fase A e correções preparatórias da Fase B

Data: 2026-10-19. **Validação SQL real pendente.** Não há evidência de criação/aplicação de esquema, constraints ou concorrência no SQL Server nesta fase. Nenhum acesso SQL/Ollama foi realizado. T018, T019 e T024 não têm aceite integral enquanto dependerem da Fase B.

## Infraestrutura preparada

`SqlServerFixture` não conecta ao ser construída. Revalida o gate antes das operações e dos contextos; um contexto utilizável exige uma sessão já verificada no SQL. Não descobre instâncias, bancos ou conexões da aplicação, e não contém credenciais. O descarte apenas fecha a sessão aberta futuramente, liberando seu lock; não limpa dados. Exige simultaneamente:

| Variável | Condição, somente em futura execução autorizada |
| --- | --- |
| REQUISITAPLUS_TEST_SQL_ENABLED | Valor literal `true`; nesta fase, `false` |
| REQUISITAPLUS_TEST_SQL_CONNECTION | Connection string exclusiva do alvo descartável, fornecida apenas no ambiente protegido do processo; não versionar ou imprimir |
| REQUISITAPLUS_TEST_SQL_DATABASE | Nome explícito no padrão RequisitaPlus_Test_ seguido de letras ASCII, números ou sublinhado |
| REQUISITAPLUS_MAIN_SQL_DATABASE | Nome explícito do principal, diferente do descartável, inclusive sem distinguir caixa |
| REQUISITAPLUS_TEST_SQL_OPERATION | `migrate` exclusivamente na aplicação separada; `test` exclusivamente na execução dos testes |
| REQUISITAPLUS_TEST_SQL_EXPECTED_MACHINE | Nome NetBIOS local explicitamente aprovado, igual a Environment.MachineName; não substitui evidência SQL |
| REQUISITAPLUS_TEST_SQL_APPROVED_MIGRATION | ID exato `20261019220059_InitialCreate`, somente no modo `migrate`, depois da autorização |

O nome na connection string deve coincidir exatamente com o descartável. Só são aceitos `Server=.\REQUISITAPLUS` e `Server=lpc:.\REQUISITAPLUS`, sem distinguir caixa. A conexão é fixada em shared memory (`lpc:`); localhost, IP, aliases, portas, outros protocolos, servidores remotos e LocalDB são recusados. Uma lista fechada de opções recusa alternativas de transporte, redirecionamento, arquivos anexados, credenciais SQL e opções extras. Exige autenticação integrada, Encrypt=Mandatory, TrustServerCertificate=false e timeout de 1–15 segundos. A string canônica desabilita pooling, reconexão, enlistment e MARS. Falha de transporte/certificado interrompe; não habilitar TrustServerCertificate ou trocar protocolo automaticamente.

A configuração é lida uma vez por validação, evitando devolver uma segunda leitura de segredo não validada. Mudança do destino canônico durante a sessão bloqueia a operação. Nenhuma configuração real foi lida ou persistida nesta etapa. Opt-in técnico não substitui autorização humana nem comprova a identidade do servidor.

Os atributos SqlServerFact/Theory ignoram testes sem o opt-in seguro e o modo `test`. A fixture também bloqueia chamadas diretas sem as condições. **Os testes SQL não aplicam migrations.** Testes unitários chamam a entrada apenas em condições que devem bloquear antes de conexão, ou usam sessão simulada. `OpenForTestsAsync` exige esquema existente e histórico exato; `ApplyApprovedMigrationAsync` é uma entrada administrativa separada, sem startup hook, teste aplicador ou comando automático. Não usa IMigrator, Database.Migrate, EnsureCreated, criador de banco ou conexão com master. Os comandos progressivos são renderizados apenas em memória a partir da migration existente; nenhum arquivo de migration ou script aprovado é regenerado.

Antes de qualquer DDL futuro: conectar diretamente ao banco já existente, confirmar via SQL a máquina física, InstanceName=REQUISITAPLUS, EngineEdition=4 (Express), transporte Shared memory, ausência de cluster/HADR/AG, banco não sistêmico, online, gravável, sem snapshot, MULTI_USER e schema padrão dbo. Evidência nula, insuficiente ou divergente bloqueia. Conferir permissões efetivas: sem papéis administrativos de servidor, db_owner/db_securityadmin/db_ddladmin/db_accessadmin/db_backupoperator, controle/alteração de servidor/banco, criação de bancos ou administração de identidades; exigir DML em dbo e VIEW DEFINITION. Na aplicação, exigir CREATE TABLE, ALTER e REFERENCES em dbo; nos testes essas permissões de DDL/referência devem estar ausentes. São condições verificadas no alvo, sem concessão automática. Permissões e possível necessidade de uma conta Windows distinta da conta administradora continuam pendentes de validação real.

Depois da identidade, `sp_getapplock` adquire lock Exclusive, proprietário Session, principal public, recurso fixo `RequisitaPlus.SqlExclusive.v1`, escopo do banco e timeout zero. Falha ou retorno desconhecido aborta. A mesma chave é usada em aplicação e testes, excluindo processos cooperantes entre si. A sessão de teste mantém o lock até o descarte; contextos filhos revalidam configuração, lock, identidade e histórico ao abrir. A coleção SQL também impede paralelismo entre classes; apenas os dois cadastros do teste de corrida concorrem dentro da sessão autorizada. Isso não impede DDL manual de administrador que ignore o protocolo: exigir exclusividade operacional e impedir outros operadores/processos no alvo.

No modo `migrate`, banco existente deve estar vazio de tabelas e objetos de usuário; qualquer histórico prévio, inclusive a própria migration inicial já aplicada, é recusado. No modo `test`, exigir exatamente a migration inicial, ProductVersion=10.0.12 e as 12 tabelas esperadas em dbo, sem objetos executáveis/tipos de usuário inesperados. Antes de devolver uma sessão de testes utilizável, também executar a comparação completa do catálogo com o modelo, recusando drift antes de DML. Históricos futuros, desconhecidos, incompletos ou versões diferentes bloqueiam, sem downgrade, reconciliação ou reparo. A criação do banco continua responsabilidade de operador em etapa separada e autorizada. Nenhum teste usa EF InMemory como prova SQL.

## Cobertura preparada para Fase B, não executada

| Teste | O que ainda precisa ser comprovado no Express |
| --- | --- |
| SchemaTests.CatalogContainsAllMappedColumnsChecksIndexesAndForeignKeys | Tipos, nulabilidade, comprimentos, precisão/escala, identity/colunas computadas, PKs, índices/colunas/ordem/filtros/unicidade, FKs/destinos/ações/ordem/trust e definições CHECK |
| SchemaTests.SqlRejectsInvalidWritesAndPhysicalCascade | FK inválida, nulo obrigatório, texto branco/Unicode/espaços finais, limites UTF-16, enums exatos, contadores, ordem de sugestão, decisão/texto/data e exclusão física bloqueada |
| SchemaTests.UniqueIndexesRejectDuplicateValues | Email normalizado, operação única, ordem de persistência por requisito e ordem por análise |
| SchemaTests.RowVersionRejectsStaleWriteAndPreservesCommittedText | Token gerado pelo servidor, alteração do token e rejeição otimista de escrita obsoleta no requisito |
| SchemaTests.SuggestionAndOperationRowVersionsAlsoRejectStaleWrites | Tokens gerados e rejeição de escrita obsoleta também na sugestão e no diário de operação |
| SchemaTests.ValidMinimumAndMaximumBoundariesRoundTripWithoutTruncation | Limites válidos de nome/e-mail/título/descrição/explicação/TextoAplicado, UTF-16 e espaços finais, nulos opcionais e proposta sem limite funcional extra |
| SchemaTests.SupplementaryUnicodeAndTrailingSpaceCannotBypassDescriptionLimit | 5.001 unidades UTF-16 rejeitadas pela constraint exata |
| SchemaTests.EmptyOperationIdAndEachParentDeletionFailOnTheirSpecificConstraint | GUID vazio e exclusões isoladas para identificar precisamente o CHECK/FK que rejeita a operação |
| IdentityUniquenessTests | Dois handlers em contextos independentes após barreira: uma conta persistida, um sucesso, nenhum cookie/autenticação para o perdedor, mensagem pública fixa sem detalhes internos e unicidade de e-mail/username normalizados |

Os ensaios de esquema usam transações próprias e rollback de dados sintéticos no descartável. Cada cenário negativo exige código e objeto específicos; índices únicos exigem 2601 e o índice esperado. Truncamentos exigem 2628 e a coluna esperada, não aceitam 8152 como prova indistinta: incompatibilidade de diagnóstico deve ser investigada, sem modificar configuração SQL automaticamente. CHECKs são comparados por AST conservadora: preserva precedência, literais e collation, admite formatação e expansões equivalentes IN/OR e BETWEEN/comparações; sintaxe desconhecida falha. Normalização e capacidades reais ainda precisam de ensaio no Express.

O teste concorrente preserva sua conta sintética com GUID. Exerce os handlers reais de cadastro/Identity contra SQL, mas não transporta duas requisições HTTP completas nem renderiza o HTML da mensagem. Asserções inspecionam ModelState e autenticação/cookies; os contratos HTTP locais separados continuam usando store simulado. Nenhuma alegação de fluxo HTTP completo com SQL é feita.

EF logging sensível/detalhado continua desabilitado. A fixture usa NullLoggerFactory e o host do teste concorrente remove providers. `SqlSafety.ProtectAsync` transforma exceções SQL, inclusive aninhadas em falhas de asserção, em falhas sem InnerException, mantendo apenas código e nomes de objetos/colunas da migration aprovada. Falha EF sem código SQL usa marcador -1, não um resultado de servidor inventado. Na aplicação, logs brutos do EF são filtrados; falhas SQL recebem eventos próprios só com código/categoria. O cadastro mantém erro público fixo. Erros inesperados continuam falhando, sem sucesso falso. Não incluir mensagens brutas, senhas, hashes ou connection strings em evidências.

## Evidência local e limites

Os contratos HTTP executam RequestDelegate com roteamento, autenticação, autorização e MVC/Razor em processo. Não iniciam Kestrel, servidor externo ou socket. O armazenamento de usuários é simulado; hash, normalização, validação, SignInManager, cookies e filtros antiforgery são do ASP.NET Core Identity. Há requisições aos formulários reais, POSTs com/sem tokens e tokens inválidos, cadastro válido/inválido/duplicado, login correto/incorreto/inexistente, logout via POST, GET sem encerrar sessão, navegação, retorno local seguro e desafio em acesso direto protegido antes/depois de logout. Não se trata apenas de inspeção de metadados.

Os testes unitários cobrem nome 100/101, e-mail 150/151, obrigatórios brancos, Unicode/UTF-16, e-mails inválidos, confirmação, todas as classes de senha e duplicidade normalizada pelo UserManager. Os testes da fixture exercitam configuração ausente/insegura sem conectar; os da migration verificam equivalência com o runtime e geração offline.

Esses resultados demonstram o contrato local com dependências isoladas. Persistência de contas, colisão concorrente e schema SQL real permanecem sem validação. Não há declaração de autenticação integrada com SQL comprovada. T025, validação manual de UI em navegadores, implantação e tarefas posteriores não foram iniciadas.

## Resultado consolidado da Fase A

Os números e a relação abaixo são o registro histórico da Fase A. As correções preparatórias da Fase B estão descritas acima e seu resultado local está ao final; não constituem validação SQL.

Executado com `REQUISITAPLUS_TEST_SQL_ENABLED=false` e `REQUISITAPLUS_TEST_OLLAMA_ENABLED=false`:

```powershell
dotnet build RequisitaPlus.slnx --no-restore
dotnet test RequisitaPlus.slnx --no-build --no-restore
```

Build aprovado, **zero avisos e zero erros**. Rodada final: **373 aprovados, zero falhados, seis ignorados, 379 no total**. São 58 aprovações adicionais sobre a referência de 315 testes. Os seis registros ignorados são os métodos/grupos SQL; teorias desabilitadas não enumeram cada caso de dados nesse total. As falhas intermediárias de revisão do modelo de design-time foram corrigidas antes desta rodada; nenhuma falha ficou pendente na suíte local.

Também aprovados: `dotnet format whitespace --verify-no-changes --no-restore` limitado aos arquivos C# alterados, `git diff --check`, geração SQL offline e `dotnet ef migrations has-pending-model-changes --no-build` sem diferenças. Arquivos gerados do EF preservam o formato da ferramenta. O resultado bruto TRX foi mantido como artefato temporário fora do repositório, para não incluir metadados locais de execução na entrega.

| Tarefa | Implementação e arquivos | Critérios atendidos | Pendência / situação |
| --- | --- | --- | --- |
| T017 | Factory offline, três arquivos em Data/Migrations, esquema-inicial.sql, migrations.md, InitialMigrationTests.cs | Estrutura revisada, equivalência com runtime, SQL offline, preservação/recuperação documentadas | Pronta para revisão; aplicação futura depende de autorização separada |
| T018 | SqlServerFixture.cs, IntegrationGate.cs, SqlServerFixtureTests.cs | Gates fail-closed, alvo descartável explícito, sem fallback/credenciais, aprovação exata da migration | Parcialmente pendente: configuração real protegida e aplicação autorizada no descartável |
| T019 | SchemaTests.cs, base.md | Verificações SQL preparadas e bloqueadas sem opt-in | Parcialmente pendente: executar e registrar integridade SQL real |
| T020 | IdentityValidationTests.cs, SimulatedIdentityStore.cs | Matriz de limites, obrigatórios, senha, normalização e duplicidade local aprovada | Pronta para revisão |
| T021 | IdentityHttpTests.cs, IdentityHttpApplication.cs, SimulatedIdentityStore.cs | Fluxos HTTP em processo e filtros reais aprovados com store isolado | Pronta para revisão do contrato local; integração SQL permanece separada |
| T022 | Register.cshtml e Register.cshtml.cs | Nome/e-mail/senha/confirmação, cliente/servidor, Identity/hash, cadastro sem SMTP exercitado localmente | Pronta para revisão |
| T023 | Login e Logout (.cshtml/.cshtml.cs), _LoginPartial.cshtml, _Layout.cshtml | E-mail/senha, cookie, logout POST/antiforgery, GET sem logout, navegação e acesso protegido exercitados localmente | Pronta para revisão |
| T024 | Register.cshtml.cs, IdentityUniquenessTests.cs | Colisão de EmailIndex/UserNameIndex tratada sem sign-in/sucesso após INSERT rejeitado; corrida SQL preparada | Parcialmente pendente: comprovar a corrida real no Express |

A alteração de _Layout é a inserção do partial de navegação da T023. Program.cs, modelos, configurações EF, pacotes, versões, Constituição, especificações, contratos e tasks.md foram preservados. Não foi necessário instalar infraestrutura HTTP: o pipeline do framework existente foi suficiente. A discrepância inicial da factory foi corrigida dentro do escopo, sem alterar estrutura de entidades. Não há conflito documental ou dependência nova pendente.

Nenhum acesso SQL/Ollama, criação/alteração/exclusão de banco, aplicação de migration, commit ou push ocorreu. Nenhuma caixa do tasks.md foi alterada. Não foram iniciadas T025 ou posteriores. Aguardar revisão humana da Fase A; a Fase B exige autorização específica.

## Resultado local das correções preparatórias da Fase B

Build `dotnet build RequisitaPlus.slnx --no-restore`: aprovado, zero avisos/erros. Testes com SQL_ENABLED=false e OLLAMA_ENABLED=false: **415 aprovados, zero falhados e nove grupos SQL ignorados, total 424**. São 42 aprovações adicionais sobre a referência de 373. Os grupos SQL incluem teorias cujos casos não são enumerados quando desabilitadas; não representam apenas nove cenários de integração.

Adicionados/ampliados: segurança da configuração, snapshot de leitura do segredo, lista de instâncias/opções permitidas, evidências simuladas de identidade/permissões, distinção entre banco vazio e esquema aplicado, recusa de históricos futuros/incompatíveis, comandos progressivos apenas em memória, gramática dos CHECKs e sanitização de diagnósticos. Uma rodada intermediária detectou interferência no cache de modelo de contextos antigos de metadados sem opções DI do Identity; isolado o cache desses contextos em ApplicationDbContextModelTests/PilotOwnershipTests. Nenhum modelo, mapeamento, contexto/factory de produção ou artefato de migration foi alterado para resolver a discrepância.

Arquivos desta correção:

- Aplicação: Program.cs (filtro de logs e códigos seguros), Register.cshtml.cs (diagnóstico seguro da colisão), Services/SqlErrorDiagnostics.cs (novo).
- Support: IntegrationGate.cs e IntegrationFactAttributes.cs.
- Integration/SqlServer: SqlServerFixture.cs, SchemaTests.cs, IdentityUniquenessTests.cs; novos SqlSafety.cs, SqlSchemaAssertions.cs e CheckExpression.cs.
- Unit: IntegrationGateTests.cs, SqlServerFixtureTests.cs, novo SqlSafetyDiagnosticsTests.cs; isolamento dos contextos em ApplicationDbContextModelTests.cs e PilotOwnershipTests.cs.
- Documentação: somente base.md e migrations.md.

Formatação de whitespace dos arquivos C# alterados e consistência textual verificadas. SHA-256 de 32 arquivos protegidos confirmou preservação das especificações/tasks.md, Constituição, entidades/mapeamentos, ApplicationDbContext/factory, migration/Designer/snapshot, script SQL e definições de versões/pacotes. Alterações preexistentes de outras etapas não foram revertidas.

**T018, T019 e T024 continuam pendentes.** Identidade/permissões reais, transporte/certificado, bloqueio de processos concorrentes no SQL, aplicação progressiva e todos os comportamentos de esquema/cadastro no Express ainda não foram executados. O parser do catálogo e os diagnósticos 2628/2601 também exigem confirmação real; divergência aborta, sem flexibilização automática. Os locks protegem apenas operações que respeitem o mesmo protocolo; manutenção manual concorrente continua proibida operacionalmente.

Nenhuma conexão SQL (incluindo master), acesso Ollama, criação/exclusão de banco, execução/regeneração de migration, restore, instalação de pacote, execução de servidor Web, commit ou push ocorreu nesta correção. tasks.md não foi alterado e T025/posteriores não foram iniciadas. Aguardar revisão humana e autorização específica da integração real.

## Preparação offline do executor da Fase B — 2026-10-19

Preparado `Operations/ApprovedMigrationExecutor.cs` no projeto de testes, com ponto de entrada operacional separado por compilação explícita. O build normal continua usando xUnit. `RequisitaPlusMigrationExecutor=true` seleciona outro Main, outro assembly e diretórios próprios, sem novos pacotes/dependências. A entrada operacional só adapta `SqlServerFixture.ApplyApprovedMigrationAsync`, nunca inicia testes de esquema/cadastro, Web ou Ollama. Procedimento, permissões, invocação futura e códigos de saída estão em [migrations.md](migrations.md#executor-operacional-preparado-offline--2026-10-19).

Exige exatamente `--apply-approved-migration 20261019220059_InitialCreate`, operação migrate, aprovação exata em ambiente e gate completo antes de construir a sessão. Não aceita SQL, arquivos, parâmetros extras, force, Down ou outro ID. O adaptador real mantém as verificações SQL existentes, inclusive recusa de sysadmin. Instância de executor permite uma tentativa, sem retry/fallback; erro de construção, aplicação ou descarte resulta em falha fixa sanitizada e revisão humana obrigatória. O descarte usa await using. Ajustado o escopo da transação da fixture para descartá-la antes da conexão e fechar a sessão também se BeginTransaction falhar; não houve mudança nas permissões, no SQL de evidência, migration ou modelo.

Adicionados 26 casos offline em `Unit/ApprovedMigrationExecutorTests.cs`: configuração ausente/insegura, modo test, aprovação ausente/divergente, argumentos fechados/SQL arbitrário, uma aplicação simulada, descarte, falhas de construção/aplicação/descarte, consumo de tentativa e concorrência de invocações. A recusa de sysadmin é exercitada por evidência simulada Restricted=false, que representa a rejeição da consulta existente; não é uma consulta a login real. Chamadas negativas à fixture real bloqueiam em test ou com ID incorreto antes de qualquer I/O SQL. A interface interna de sessão simulada não é uma opção exposta pelo executável.

Validação local com `REQUISITAPLUS_TEST_SQL_ENABLED=false` e `REQUISITAPLUS_TEST_OLLAMA_ENABLED=false` em todos os builds/testes:

```powershell
dotnet build .\tests\RequisitaPlus.Tests\RequisitaPlus.Tests.csproj --no-restore -p:RequisitaPlusMigrationExecutor=true
dotnet build RequisitaPlus.slnx --no-restore
dotnet test RequisitaPlus.slnx --no-build --no-restore
```

Build operacional e build normal aprovados, zero avisos/erros nas validações finais. Suíte: **441 aprovados, zero falhados, nove grupos SQL ignorados, total 450**; 26 aprovações adicionais à referência 415. Um aviso de nulabilidade na primeira compilação foi corrigido antes das validações finais. Os ignorados incluem teorias cujos casos não são enumerados com integração desabilitada. O executável operacional foi compilado, não executado. Simulações não comprovam login Windows, permissões reais, transação, liberação de locks, aceitação de DDL ou comportamento no Express.

Contexto fornecido pelo responsável, com identificadores locais omitidos: instância local nomeada REQUISITAPLUS, somente Windows; a conta administrativa local é sysadmin; banco existente descartável confirmado vazio; migration ainda não aplicada. Não se repetiu essa inspeção no servidor. O nome do principal continua sem confirmação e não foi inventado/persistido em configuração. Os nomes usados nos testes são sentinelas sintéticas offline.

**Privilégio mínimo ainda pendente:** criar/configurar conta comum e login/usuário mediante autorizações próprias; verificar identidade/grupos/permissões efetivas, inclusive objetos e outros bancos; retirar DDL antes dos testes; validar transporte/certificado. DML atual em dbo alcança __EFMigrationsHistory. Documentada proposta de negar INSERT/UPDATE/DELETE apenas nesse objeto depois da migration, preservando SELECT e DML de domínio/Identity; nenhuma negação foi aplicada nem passou a ser exigida no código. Sua compatibilidade real precisa de validação autorizada; mudança no contrato de permissões exige aprovação antes de implementação. O gate não fixa SID/login nem comprova isolamento completo.

**T018, T019 e T024 permanecem pendentes.** Nenhuma conexão SQL/master, aplicação/remoção/regeneração de migration, criação de conta/login/usuário, concessão/revogação de direitos, configuração real, Web/Ollama, instalação/restore, commit ou push ocorreu. Modelos/mapeamentos/artefatos da migration, tasks.md e tarefas T025/posteriores foram preservados. Aguardar revisão humana antes de qualquer etapa operacional real.
