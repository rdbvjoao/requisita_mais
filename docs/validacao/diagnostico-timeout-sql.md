# Diagnóstico offline do timeout SQL

> Atualização: o resultado posterior confirmado foi `SQL_FAILURE code=-2; stage=LeaseOpen`.
> A abertura inicial é a etapa que falha; a hipótese anterior sobre comandos abaixo
> não explica esse resultado. A proposta anterior de teste real foi substituída pelo
> [diagnóstico isolado de conexão](conexao-sql-isolada.md), preparado sem execução real.
> O restante deste documento registra a análise anterior à confirmação da etapa.

## Conclusão e limite da evidência

O `-2` é um timeout do SqlClient; a mensagem anterior perdia a etapa ao sanitizar a exceção. A duração total de 37–46 segundos é compatível com um comando que chega ao timeout padrão de 30 segundos, somado à preparação do teste. A hipótese prioritária é um timeout de comando nas verificações iniciais (identidade/permissões ou estado) ou no catálogo. A duração total sozinha não identifica a operação e não exclui abertura de conexão.

A validação de identidade consulta `sys.login_token`, funções de associação a papéis e várias permissões efetivas. É uma candidata a investigar, mas não há evidência offline que permita atribuir a demora à conta Windows, NTLM ou resolução de grupos. A conexão bem-sucedida do sqlcmd não exercita esse mesmo lote nem as reaberturas do SqlClient. A ausência de requests em uma amostra posterior não exclui uma requisição que já expirou.

## Caminho do teste de catálogo

1. Abre uma conexão de lease, sem pooling/retry (`LeaseOpen`).
2. Valida identidade e permissões (`LeaseIdentity`).
3. Adquire applock exclusivo de sessão (`LeaseLock`).
4. Consulta tabelas, histórico e objetos/tipos inesperados (`LeaseState`).
5. Abre uma segunda conexão via EF (`ChildOpen`). O interceptor consulta o lock na lease (`SessionLock`) e valida identidade/estado da conexão filha (`ChildIdentity`, `ChildState`).
6. Verifica colunas, PKs, índices, CHECKs e FKs (`CatalogQuery`), depois fecha essa conexão.
7. O corpo do teste abre uma terceira conexão física e repete as verificações de sessão e de catálogo.

Os comandos de identidade/estado/catálogo incluem tanto a execução quanto a leitura assíncrona dos resultados no escopo protegido. Todos usam o timeout padrão de comando de 30 segundos. A abertura usa o ConnectTimeout validado pelo gate (1–15 segundos). Os limites são por operação, não pelo teste completo; a repetição aumenta o trabalho, mas não acumula um único orçamento de 30 segundos.

## Bloqueio e concorrência

- As duas classes SQL pertencem à coleção `SqlServer`, com paralelismo desabilitado. Isso não coordena processos de teste externos.
- A lease mantém applock de sessão até o descarte. As conexões filhas apenas consultam seu estado; não tentam adquirir o mesmo lock. Não há transação no teste de catálogo nem reader mantido aberto entre comandos. Não foi encontrado ciclo de espera produzido por esse caminho.
- `sp_getapplock` usa `@LockTimeout=0`: disputa pelo recurso retorna falha imediatamente. Seus retornos negativos são rejeitados como `SQL_EXCLUSIVE_LOCK_UNAVAILABLE`; um retorno numérico negativo do procedimento não é `SqlException.Number=-2`. A execução do próprio comando ainda pode sofrer timeout do cliente.
- `leaseGuard` serializa apenas a consulta `APPLOCK_MODE` e é liberado em `finally`. A espera no semáforo não produz código SQL `-2`.
- O teste de cadastro tem concorrência intencional entre duas conexões e uma barreira de 20 segundos. Timeout da barreira é uma exceção .NET, não SQL `-2`, e esse caminho não participa do teste de catálogo.
- Os demais testes fazem DML em transações: comandos brutos, SaveChanges e consultas LINQ podem sofrer `-2`, inclusive por bloqueio de dados. Aberturas implícitas e operações de transação (início, commit, rollback e descarte com rollback pendente) também envolvem I/O SQL. O caminho de migration contém início de transação, comandos DDL de 30 segundos, commit/rollback; ele não é chamado pelo teste de catálogo e não foi executado nesta investigação.
- Parsing, construção do modelo, guards de configuração/evidência e `CheckExpression.Canonical` não geram SqlException `-2`.

## Instrumentação e preservação das proteções

Foram adicionados identificadores enum fixos, sem estado global ou ambiente mutável de diagnóstico. Falhas SQL nesses escopos exibem somente `SQL_FAILURE code=<n>; stage=<etapa>`, sem mensagem original, inner exception, nomes de objetos ou dados de conexão. Guards externos preservam a etapa interna. A sanitização anterior de nomes aprovados continua disponível para os testes de restrições que já dependem dela.

Nenhum endpoint, timeout, retry, pooling, identidade, TLS, privilégio, applock ou condição fail-closed foi alterado. As mudanças pré-existentes no gate, parser e testes associados foram preservadas.

## Único teste real proposto (não executado)

Em sessão já preparada sob a identidade restrita e a configuração descartável autorizada, operação `test`, sem outro processo SQL/Web de teste concorrente e com Ollama desabilitado, executar uma única vez:

```powershell
dotnet test .\tests\RequisitaPlus.Tests\RequisitaPlus.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName=RequisitaPlus.Tests.Integration.SqlServer.SchemaTests.CatalogContainsAllMappedColumnsChecksIndexesAndForeignKeys'
```

Esse teste apenas abre/valida sessões e lê metadados; não aplica migrations nem faz DML. Se reproduzir, `LeaseOpen`/`ChildOpen` identifica abertura, `LeaseIdentity`/`ChildIdentity` identidade, `LeaseLock` aquisição, `SessionLock` confirmação de posse, `LeaseState`/`ChildState` estado e `CatalogQuery` consultas posteriores. Isso localiza a operação que expirou, sem provar sozinho o motivo interno da espera no servidor. Se passar, valida o parser contra o catálogo real naquele momento, mas não confirma a causa do timeout intermitente. Não aumentar limites a partir desse resultado sem investigar a etapa identificada.

## Validação realizada em 2026-10-20

- `dotnet build RequisitaPlus.slnx --no-restore`: sucesso, zero avisos e zero erros.
- `dotnet test tests/RequisitaPlus.Tests/RequisitaPlus.Tests.csproj --no-build --no-restore --filter 'Category!=SqlServer&Category!=Ollama'`: 554 aprovados, zero falhas, zero ignorados, 45 segundos.
- Ambos executados com `REQUISITAPLUS_TEST_SQL_ENABLED=false` e `REQUISITAPLUS_TEST_OLLAMA_ENABLED=false` no processo filho. Nenhum acesso a SQL Server/Ollama, migration, DDL/DML, commit ou push foi executado.
- Os 12 casos adicionais cobrem as nove etapas, preservação da etapa interna, manutenção da rejeição de política e isolamento dos diagnósticos concorrentes. Os timeouts são simulados com falhas já sanitizadas, sem conexão real; não comprovam a causa no servidor.
