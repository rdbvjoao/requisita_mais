# Migration inicial — T017 / Fase A

Data: 2026-10-19. Artefatos gerados e revisados offline; **nenhuma migration aplicada e nenhum banco acessado**. Fontes: Constituição IV/V/VI/VIII, tasks T017–T019, data-model.md e research D2/D3/D9.

Arquivos:

- `src/RequisitaPlus.Web/Data/ApplicationDbContextFactory.cs`
- `src/RequisitaPlus.Web/Data/Migrations/20261019220059_InitialCreate.cs`
- `src/RequisitaPlus.Web/Data/Migrations/20261019220059_InitialCreate.Designer.cs`
- `src/RequisitaPlus.Web/Data/Migrations/ApplicationDbContextModelSnapshot.cs`
- `docs/validacao/esquema-inicial.sql`

A ferramenta local aprovada é dotnet-ef **10.0.12**, sem restore, instalação ou atualização de dependências. A factory usa somente configuração vazia e um destino fictício `design-time.invalid`, reaproveitando os registros de Identity de Program para reproduzir as opções do modelo. Não lê appsettings, User Secrets ou connection strings de ambiente. Um interceptor recusa aberturas síncronas e assíncronas de conexão antes do provider acessar rede. Nenhum host é iniciado.

Geração realizada após build com `--no-restore`:

```powershell
dotnet ef migrations add InitialCreate --project src/RequisitaPlus.Web --output-dir Data/Migrations --no-build --verbose
dotnet ef migrations script 0 InitialCreate --project src/RequisitaPlus.Web --no-build --output docs/validacao/esquema-inicial.sql
dotnet ef migrations has-pending-model-changes --project src/RequisitaPlus.Web --no-build
```

O log confirmou seleção da factory. O último comando não encontrou mudanças pendentes. O script é DDL para revisão, sem CREATE DATABASE ou execução automática; inclui a tabela de histórico do EF. Não foi usado database update, migrations remove, script reverso ou opção de execução. A primeira geração foi substituída offline durante a revisão: a factory inicialmente omitira as opções de comprimento de chaves auxiliares do Identity UI. A geração final incorpora os limites de 128 caracteres do runtime; somente uma migration inicial permanece, sem mudanças nas entidades ou configurações aprovadas.

Revisão estrutural do código e SQL:

| Item | Evidência offline |
| --- | --- |
| Identity | Sete tabelas padrão; IDs int; Nome 100; Email/NormalizedEmail/UserName/NormalizedUserName 150; hash sem limite artificial |
| Domínio | Requisito, OperacaoAnalise, Analise e Sugestao; FKs obrigatórias, com RESTRICT/NO ACTION |
| Índices únicos | EmailIndex, UserNameIndex, IdOperacao de Analise, (IdRequisito, OrdemPersistencia), (IdAnalise, OrdemNoResultado) |
| Consultas | Índices de listagem própria e ativa, incluindo OrdemPersistencia descendente |
| Integridade | CHECKs aprovados; DATALENGTH para espaços finais/UTF-16; domínio exato com BIN2; nulabilidade e tipos Unicode |
| Concorrência | Três rowversion: Requisito, Sugestao e OperacaoAnalise |
| Datas | datetime2; conversão UTC mantida no modelo, sem presumir que datetime2 armazene fuso |

As cascatas auxiliares normais do Identity permanecem; não há cascata física no domínio. Os testes locais comparam o modelo relacional da factory ao obtido com os registros reais de Program, inspecionam operações da migration, geram SQL em memória e confirmam conexão fechada. Isso não prova que o SQL foi aceito pelo Express nem substitui T019.

## Preservação e recuperação antes de aplicar

1. Revisão humana do SQL, migration e snapshot; confirmar instância, banco descartável e permissão específica da Fase B. Nunca usar o principal para teste, nem LocalDB ou seleção automática.
2. Caso o alvo tenha dados, interromper antes da aplicação inicial até revisar seu esquema e histórico de migrations. Não marcar migration como aplicada manualmente, sobrescrever tabelas ou presumir banco vazio.
3. Antes de mudanças em banco com dados: backup FULL em diretório protegido separado do ativo, conta autorizada, retenção dos últimos sete dias e preservação da última cópia válida se houver falha. Conferir a cópia e ensaiar restauração em outro banco/arquivos, sem sobrescrever o principal. Recuperabilidade só estará comprovada após ensaio real autorizado e revisão de integridade/conteúdo.
4. Registrar migration anterior/pretendida, impacto e janela, autorização e identificação do alvo sem segredos. Parar a aplicação durante manutenção conforme o procedimento aprovado; não manter transação de negócio aberta em execução externa.
5. Em falha, interromper novas alterações e preservar evidências mínimas sem dados privados. Avaliar estado efetivo e transação antes de decidir recuperação. Preferir correção revisada que preserve dados; restauração exige alvo separado e autorização própria, com perda possível desde a cópia explicitada.

O método Down padrão gerado pelo EF contém remoção de tabelas e **não é um procedimento autorizado de recuperação**. Não há rotina de rollback destrutivo, limpeza de banco ou migration no startup. Nenhum Down, DROP ou restauração foi executado. A fixture futura aplica somente a migration explicitamente aprovada ao alvo descartável configurado; não exclui bancos ou dados automaticamente.

## Correções preparatórias da Fase B — nenhuma execução SQL

Criação do banco, aplicação e testes são operações distintas. O operador deve criar previamente um banco novo, com nome explicitamente aprovado no padrão RequisitaPlus_Test_..., após autorização própria. Se o nome já existir, não reaproveitar ou excluir: interromper e revisar o destino. O código de teste/aplicação não provisiona banco e não abre conexão com master. Eventuais atos administrativos do operador, inclusive acesso a bancos de sistema, exigem autorização separada.

A entrada administrativa `SqlServerFixture.ApplyApprovedMigrationAsync` exige opt-in, operação `migrate` e aprovação exata da migration. Não é invocada por testes. Não usa IMigrator nem seu caminho de criação implícita/regressão. Renderiza em memória apenas as operações Up da migration já existente, admitindo somente CreateTable/CreateIndex transacionais, após conferir ID único e coerência de modelo/snapshot. Os arquivos de migration, snapshot e esquema-inicial.sql permanecem intactos. Down, SQL arbitrário, migrations extras e operações não transacionais são recusados.

Antes de qualquer DDL, a conexão direta deve provar identidade local/Express e permissões limitadas, adquirir o lock exclusivo de sessão e demonstrar banco existente vazio, sem histórico ou objetos de usuário inesperados. Ver detalhes, variáveis e critérios fail-closed em [base.md](base.md). A criação do histórico, operações Up e registro da migration compartilham uma única transação explícita. Falha provoca rollback transacional, não execução de Down; ao terminar, a sessão fecha e libera o lock. Falha de conexão/commit deixa a confirmação dependente de inspeção humana, sem retry, limpeza ou recriação automática.

O lock SQL é `sp_getapplock`, principal public (compatível com conta sem db_owner), proprietário Session, modo Exclusive, timeout zero e recurso fixo RequisitaPlus.SqlExclusive.v1. Está no próprio banco de teste e é comum a aplicação e testes, inclusive entre processos cooperantes. Resultado negativo/desconhecido bloqueia. Não executar ferramenta/DDL manual concorrente que ignore esse protocolo; a exclusividade operacional é uma condição da autorização.

Após aplicação aprovada: fechar a sessão administrativa, retirar a aprovação da migration, retirar permissões de DDL/referência da conta de testes e configurar operação `test`. Cada teste abre somente uma sessão de validação; exige histórico exatamente 20261019220059_InitialCreate / 10.0.12 e esquema esperado. Histórico desconhecido, futuro, já aplicado em modo migrate, incompleto ou esquema alterado resulta em interrupção, sem regressão ou reparo. O executor preparado abaixo é separado de dotnet test; sua execução real exige autorização específica. Não remover a guarda offline da factory para usar database update.

Não houve criação/regeneração/aplicação de migration ou script nesta etapa. Aplicação, identidade efetiva, conta/permissões, certificado/transporte local, exclusão entre processos no SQL, aceitação pelo Express e constraints reais continuam pendentes. Backup/restauração permanecem sujeitos ao procedimento e às autorizações próprias já documentados.

## Executor operacional preparado offline — 2026-10-19

`tests/RequisitaPlus.Tests/Operations/ApprovedMigrationExecutor.cs` contém uma entrada dedicada. A compilação normal mantém a entrada gerada do xUnit. A propriedade abaixo seleciona `MigrationExecutorProgram.Main`, com assembly e diretórios de saída/intermediários separados, reutilizando dependências já restauradas, sem pacote novo. O executável contém a infraestrutura de testes, mas sua entrada não chama o runner, testes de esquema/concorrência, startup Web ou Ollama. Não há fallback para testes.

Preparação offline do artefato:

```powershell
$env:REQUISITAPLUS_TEST_SQL_ENABLED = 'false'
$env:REQUISITAPLUS_TEST_OLLAMA_ENABLED = 'false'
dotnet build .\tests\RequisitaPlus.Tests\RequisitaPlus.Tests.csproj --no-restore -p:RequisitaPlusMigrationExecutor=true
```

**Invocação futura, não executada:** somente após autorização humana, revisão do artefato e configuração protegida de todas as variáveis de base.md, incluindo operação `migrate` e `REQUISITAPLUS_TEST_SQL_APPROVED_MIGRATION` com o ID exato:

```powershell
dotnet .\tests\RequisitaPlus.Tests\bin\MigrationExecutor\Debug\net10.0\RequisitaPlus.MigrationExecutor.dll --apply-approved-migration 20261019220059_InitialCreate
```

Argumentos extras, SQL, arquivos de script, Down, force, outro ID e modo `test` são recusados antes da construção da sessão. A validação offline da configuração também precede essa construção. A entrada real instancia exclusivamente um adaptador da fixture; a interface interna injetável serve aos testes offline e não é opção de CLI. A fixture revalida configuração/aprovação e evidências SQL antes de DDL. Cada instância consome uma tentativa, inclusive recusada/falhada; não há loop/retry. Novo processo não autoriza repetição: exige revisão humana; banco já migrado é recusado em `migrate`.

O adaptador é descartado com `await using`. Permanecem o lock exclusivo de sessão e a transação única de histórico + Up + registro. A transação é descartada antes da conexão; falha ao iniciar a transação também fecha a sessão/libera o lock. Rollback transacional não executa Down. Falha de commit, rollback ou descarte não permite presumir o estado efetivo do banco.

Saída: uma mensagem fixa, sem exceção bruta, stack trace, connection string ou valores de argumentos/ambiente. Exit code 0: `SQL_MIGRATION_EXECUTOR_COMPLETED`; 2: autorização/configuração recusada ou instância já usada; 1: `SQL_MIGRATION_EXECUTOR_FAILED_REVIEW_REQUIRED`, inclusive falhas de construção/aplicação/descarte. Em falha, interromper e revisar, sem reenvio automático. Exit code 0 não comprova T019/T024. As flags e o argumento são confirmações técnicas, não prova de autorização humana.

### Procedimento futuro separado — não executado

1. **Criar identidade Windows**, mediante autorização: conta comum, sem Administradores nem grupos que herdem administração SQL. Uma conta com DDL temporário é suficiente; duas contas separadas também são viáveis. A conta administrativa local fica reservada aos atos administrativos autorizados, nunca à execução da fixture.
2. **Configurar login e usuário**, mediante autorização: login Windows sem papel de servidor além de public; usuário somente no banco descartável previamente confirmado pelo responsável (nome local omitido), schema padrão dbo, sem propriedade do banco/schema. Esse banco existente foi confirmado vazio pelo responsável; o contexto atual não autoriza executar nele nem recriá-lo. Nenhuma etapa da fixture abre master; atos administrativos do operador exigem autorização própria. O nome do principal não foi confirmado: manter `REQUISITAPLUS_MAIN_SQL_DATABASE` pendente, sem suposição.
3. **Verificar identidade e permissões efetivas**, em atividade SQL de leitura separadamente autorizada: conta Windows do processo, login SQL efetivo, grupos, papéis, grants/denies, propriedade e acesso a outros bancos. Conferir alvo, vazio, transporte/certificado, sem DDL. Usar `.\REQUISITAPLUS` ou `lpc:.\REQUISITAPLUS` e conferir o nome real da máquina esperada; a grafia explícita `MAQUINA-EXEMPLO\REQUISITAPLUS` (nome ilustrativo) é recusada pelo gate mesmo quando o nome real identifica a instância confirmada.
4. **Conceder temporariamente privilégios de migration**, mediante autorização: direitos da tabela abaixo somente no descartável, sem grant option, controle de instância/banco ou papéis administrativos. Reservar exclusividade operacional: o applock não impede um administrador que ignore o protocolo.
5. **Executar exclusivamente a migration aprovada**, mediante autorização: novo PowerShell sob a conta comum, variáveis de base.md no processo e aprovação exata, artefato revisado e comando acima. Ollama desabilitado. Não usar database update, Web, SQL arbitrário ou startup hook. Qualquer falha exige revisão humana, sem retry, recriação, Down ou restauração automática.
6. **Revogar privilégios de estrutura**: encerrar o processo aplicador, retirar a variável de aprovação e revogar CREATE TABLE, ALTER dbo e REFERENCES dbo em todas as origens de concessão. Conferir ausência efetiva também nos objetos, grupos/papéis e propriedade. Rever a proposta de proteção do histórico abaixo antes de habilitar testes.
7. **Executar integrações autorizadas**: novo PowerShell sob identidade restrita, operação `test`, opt-in SQL consciente e Ollama desabilitado. Com binários normais de teste preparados, usar `dotnet test .\tests\RequisitaPlus.Tests\RequisitaPlus.Tests.csproj --no-build --no-restore --filter "Category=SqlServer"`. Não passar a propriedade de build do executor a dotnet test. Ignorados não contam como prova SQL; preservar a concorrência interna do cadastro sem iniciar outros processos SQL/Web/Ollama em paralelo.
8. **Preservar evidências**: autorização, alvo, artefato, identidade, permissões e resultados sanitizados, distinguindo simulado/local de SQL real. O cadastro concorrente preserva uma conta sintética. Não excluir automaticamente dados/banco. T018/T019/T024 continuam pendentes até execução/revisão real e aceite humano.

| Escopo / condição efetiva | migrate | test |
| --- | --- | --- |
| Login Windows, CONNECT SQL e CONNECT no descartável | Necessários | Necessários |
| Schema padrão dbo, sem propriedade | Necessário | Necessário |
| SELECT, INSERT, UPDATE, DELETE em SCHEMA::dbo | Exigidos pelo gate atual | Exigidos pelo gate atual |
| VIEW DEFINITION no banco descartável | Exigido | Exigido |
| CREATE TABLE no banco descartável | Exigido | Ausente |
| ALTER e REFERENCES em SCHEMA::dbo | Exigidos | Ausentes |

Os direitos de conexão podem decorrer das concessões padrão; conferir permissões efetivas e DENY. Não usar db_datareader/db_datawriter como substitutos da checagem explícita de schema. O DML em migrate é mais amplo que o necessário ao Up inicial, mas removê-lo mudaria a política atual. Ambos os modos recusam papéis de servidor além de public e db_owner/db_securityadmin/db_ddladmin/db_accessadmin/db_backupoperator; recusam CONTROL SERVER, CREATE ANY DATABASE, ALTER ANY DATABASE, ALTER ANY LOGIN, ALTER ANY SERVER ROLE, IMPERSONATE ANY LOGIN e, no banco, CONTROL, ALTER, TAKE OWNERSHIP, ALTER ANY USER e ALTER ANY ROLE. ALTER de schema difere de ALTER de banco. sp_getapplock usa public, sem exigir db_owner. VIEW DEFINITION é do banco; essas consultas não justificam VIEW SERVER STATE.

### PowerShell, VS Code e memória

O terminal comum herda a identidade do VS Code. Janela não elevada da conta administrativa local continua sysadmin no SQL. Abrir PowerShell separado por Executar como outro usuário, runas com /user e perfil carregado, ou Start-Process -Credential -LoadUserProfile; senha apenas no prompt, sem /savecred. Não usar /netonly para Shared memory. Confirmar identidade antes da operação, sem elevar a conta dedicada. Fornecer variáveis dentro do novo processo, sem setx, gravação no perfil, conexão nos argumentos ou persistência no VS Code. Encerrar a sessão ao terminar; não alterar ExecutionPolicy para contornar bloqueios.

Conferir SDK/runtime e acesso a pacotes/binários: ferramentas globais e cache NuGet da conta administrativa local não são automaticamente da nova conta. Preferir pasta de execução com artefatos revisados e ACLs específicas, leitura de dependências e escrita limitada a perfil/temporários/resultados, sem acesso amplo ao diretório de perfil da conta administrativa local. ACLs/cópias/perfis exigem autorização própria; nenhum foi preparado. A invocação da DLL dispensa ferramenta EF global. Outro VS Code, se necessário, deve ter identidade e diretório de dados próprios; não é necessário neste procedimento.

Com 8 GB: um PowerShell leve, build separado dos testes, sem IDE adicional/Web/Ollama/inferência simultâneos; não modificar limites de memória SQL nesta preparação. Duas contas não exigem processos simultâneos nem aumentam a RAM por si só.

### Histórico e limites de privilégio mínimo pendentes

Os testes SQL atuais apenas leem `dbo.__EFMigrationsHistory`; esquema/cadastro não precisam modificar esse histórico. O DML no schema, porém, abrange a tabela. Proposta para aprovação administrativa posterior: após migration e revogação de DDL, negar INSERT, UPDATE e DELETE no objeto dbo.__EFMigrationsHistory à identidade de testes, preservando SELECT. REVOKE somente no objeto não remove GRANT herdado do schema. Não aplicar a negação antes da migration se a mesma conta precisa inserir o registro inicial.

A proposta preserva DML de domínio/Identity e a exigência no schema; sua compatibilidade efetiva com HAS_PERMS_BY_NAME e os testes requer confirmação SQL autorizada. Nenhum DENY/GRANT/REVOKE foi executado e o código não passou a exigir essa política. Se for necessário mudar o contrato de permissões ou comprometer testes, interromper e obter aprovação antes da mudança. Não alegar que o histórico já está protegido.

O gate não fixa SID/nome do login, não audita outros bancos nem enumera todas as permissões de servidor/objeto. Ausência de ALTER/REFERENCES no schema não exclui concessões diretas nas tabelas. Grants por coluna, grupos, papéis, propriedade e public precisam de revisão administrativa; retirar só o grant direto não basta. A fixture não prova isolamento completo da identidade. Testes offline não resolvem essas pendências nem comprovam autenticação Windows real.
