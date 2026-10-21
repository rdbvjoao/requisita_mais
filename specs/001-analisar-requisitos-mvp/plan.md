# Implementation Plan: Requisita+ — planejamento técnico consolidado

**Branch real**: `main`, preservada | **Date**: 2026-10-18 | **Spec**: [spec.md](spec.md)

**Input**: especificação funcional aprovada e instrução T1–T10 desta etapa. Estado: desenho técnico proposto, sujeito à revisão humana; nenhuma implementação ou validação operacional realizada.

## Summary

Aplicação Web ASP.NET Core MVC monolítica e modular em .NET 10/C#, Razor Views e Identity, com SQL Server Express local e EF Core Code First. Um único executável Web contém interface, regras, persistência e integração HTTP com Ollama. Um projeto xUnit separado cobre comportamentos previsíveis. Todo P0/P1/P2 permanece planejado; a demonstração local de 22/10/2026 prioriza US1–US9, sem reclassificar prioridades ou exigir P2 previamente.

Fontes obrigatórias lidas: [Constituição](../../.specify/memory/constitution.md), [especificação](spec.md), [checklist](checklists/requirements.md) e [DOCX acadêmico](../../docs/Documentacao_Requisita+.docx), versão 1.1. Os refinamentos aprovados prevalecem nos pontos explicitados pela spec. Os avisos antigos de “sem autorização para planejamento” descrevem a etapa anterior; o pedido atual aprova a especificação e autoriza exclusivamente este planejamento. Não houve alteração dessas fontes.

## Technical Context

| Aspecto | Decisão |
| --- | --- |
| Language/Version | C#, alvo `net10.0`; versão exata do SDK e patches será registrada na preparação futura |
| Primary Dependencies | ASP.NET Core MVC, Razor, Identity UI, EF Core 10 com provider SQL Server e ferramentas de migrations compatíveis; HttpClient; System.Text.Json; xUnit |
| Storage | Instância local SQL Server Express, quatro entidades de domínio, tabelas auxiliares Identity e registro técnico de operações; sem banco alternativo |
| Testing | Um projeto xUnit; testes unitários/contrato com simuladores; integração SQL Express e Ollama real em grupos explicitamente separados |
| Target Platform | Windows/PowerShell, navegador desktop/notebook por HTTPS; Ollama somente em loopback |
| Project Type | Monólito MVC modular, uma instância e um processo Web; sem SPA separada |
| Performance Goals | Uma inferência ativa; aquisição imediata sem fila; prazo de resposta de análise de 60 s, sem garantir encerramento remoto nesse prazo |
| Constraints | i5-1235U, aproximadamente 8 GB RAM e Iris Xe; contexto 4.096, saída 1.024, até cinco apontamentos; custo monetário de API zero |
| Scale/Scope | Uso acadêmico local; usuários isolados; P0 US1–US6, P1 US7–US9, P2 US10–US12; sem meta de usuários simultâneos não aprovada |

Não se presumem versões instaladas, serviços disponíveis, RAM livre, desempenho ou compatibilidade da versão local do Ollama. São verificações futuras do [quickstart](quickstart.md). Não há decisão funcional em aberto necessária ao desenho. O contrato documenta os bloqueios técnicos antes de habilitar inferência real.

## Constitution Check

Gate documental antes da pesquisa e reavaliado após o desenho; “atende” refere-se ao plano, nunca à aplicação.

| Princípio | Antes da fase 0 | Após fase 1 / evidência |
| --- | --- | --- |
| I — Fidelidade | Atende: fontes disponíveis e refinamentos aprovados | Matriz abaixo preserva RF/RN/UC; fontes intocadas |
| II — Simplicidade | Atende: stack constitucional T1–T3 | Dois projetos futuros, sem serviços Web adicionais, repositório genérico, CQRS ou infraestrutura distribuída |
| III — Incremental | Atende: autorização só de planejamento | Cinco incrementos propostos; nenhuma tarefa ou implementação criada; próxima transição depende de autorização |
| IV — Integridade | Atende: RN004 e refinamentos conhecidos | Transações, snapshots, rowversion, versionamento, decisões definitivas e backup em data-model/research |
| V — Segurança | Atende: Identity e processamento local | Autorização por proprietário, antiforgery, HTTPS, hash Identity, destino restrito e logs mínimos |
| VI — Qualidade | Atende: aceite da spec | Matriz de testes e validação futura; nenhuma alegação de testes realizados |
| VII — Supervisão | Atende: IA somente apoio | Confirmação por análise e por decisão; texto gerado imutável; sem alteração automática |
| VIII — Mudanças | Atende: T1–T10 autorizam desenho | Modelo técnico proposto para revisão, sem aplicação de esquema; pendências humanas explicitadas |
| IX — Custos | Atende: Q9/T5 delimitam modelo e orçamento | Uma chamada por operação, deduplicação, registro de tokens/custo zero; sem fallback ou chamadas nesta etapa |

**Resultado**: sem violação constitucional identificada no desenho. As adaptações do modelo acadêmico estão fundamentadas nos refinamentos Q1/Q4/Q6/R1/R2/R7 e detalhadas em data-model.md; não são emenda da Constituição nem alteração do DOCX.

## Project Structure

### Documentation (this feature)

```text
specs/001-analisar-requisitos-mvp/
├── spec.md                         (preservado)
├── checklists/requirements.md      (preservado)
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    ├── mvc.md
    └── ollama.md
```

### Source Code (repository root) — somente organização futura

```text
RequisitaPlus.slnx
src/RequisitaPlus.Web/
  Controllers/
  Models/                          (domínio e ApplicationUser)
  ViewModels/
  Views/
  Areas/Identity/                  (UI padrão; ajustes mínimos de cadastro)
  Services/
    Requisitos/                    (edição, versão e status)
    Analises/                      (orquestração, candidatos e validação)
    Revisao/                       (decisões e conclusão)
    IA/                            (cliente HTTP e supervisor singleton)
  Data/                            (DbContext, mapeamentos e Migrations)
  wwwroot/                         (HTML/CSS/JavaScript de apoio)
tests/RequisitaPlus.Tests/
  Unit/
  Contracts/
  Integration/SqlServer/
  Integration/Ollama/
```

**Structure Decision**: nenhum desses projetos/diretórios de código foi criado. Controllers recebem ViewModels, validam ModelState/autorização e delegam; serviços executam regras e transações; DbContext acessa SQL Server diretamente. Views usam codificação Razor e exibem estados, sem decidir autorização. `HttpClient` tipado fica atrás de interface substituível nos testes. O supervisor usa escopos próprios de DI para DbContext; nunca retém um DbContext scoped dentro de singleton.

## Dados, revisão e persistência

O [modelo](data-model.md) conserva Usuário → Requisito → Análise → Sugestão. Cada requisito inicia `VersaoTextual=1`; alteração efetiva de título/descrição incrementa; só prioridade não incrementa. Cada análise guarda os dois textos exatos enviados e sua versão. A ativa é calculada por requisito, versão atual e maior `OrdemPersistencia`; não há flag manual ou seleção por conteúdo idêntico. Só resultados válidos entram em Analise. Falhas ficam no registro técnico e não substituem a ativa.

Análise/sugestões/ordem/status/confirmação de persistência da operação são uma transação curta. Decisão/TextoAplicado/descrição/versão/status são outra transação curta. Não manter transação SQL aberta durante HTTP. `rowversion` no requisito serializa logicamente edição, persistência e revisão; conflitos rejeitam a mutação e exigem atualização da tela ou regravação explícita do mesmo resultado, sem reinferir. A definição exata está no modelo.

## Integração local e controle de execução

Configuração por `appsettings.json`, `appsettings.{Environment}.json`, variáveis de ambiente e User Secrets para segredos locais. Options validadas antes de habilitar análise: endereço exato autorizado, modelo/limites aprovados e ausência de redirecionamentos/proxy/fallback. Configuração inválida desabilita IA com mensagem, preservando funções Web/SQL independentes; falhas de configuração de banco/Identity são distintas.

Fluxo: confirmar processamento local → autorizar e capturar versão/textos → indicadores candidatos → conferir orçamento de contexto → deduplicar → tentar `SemaphoreSlim.Wait(0)` → registrar envio → uma chamada HTTP → validar integralmente → persistir → exibir. A ausência de candidatos não elimina a chamada. O [contrato Ollama](contracts/ollama.md) define o JSON, a guarda conservadora de contexto e a recuperação.

Um supervisor singleton, dentro do mesmo executável, mantém o semáforo e a tarefa em curso separados da conexão do navegador. Timeout de 60 s encerra a espera útil e marca a tentativa como falha; não libera a capacidade enquanto faltar prova de término. Uma resposta tardia é descartada para conclusão funcional, mas pode comprovar término. Perda de comunicação coloca IA em recuperação bloqueada. Reinício da aplicação também bloqueia IA se o registro durável indicar envio sem encerramento comprovado. Sem fila, retry HTTP ou inferência de diagnóstico. Ollama deve ser dedicado à aplicação durante o uso; clientes externos não são coordenados por SemaphoreSlim.

## Estratégia de testes futura

| Grupo | Verificações prioritárias | Evidência prevista |
| --- | --- | --- |
| Unitário xUnit | Campos/limites/espaços, prioridade explícita, versão por texto e retorno a redação antiga, status e confirmação | Entradas válidas/inválidas e transições esperadas |
| Revisão/histórico | Ativa R7, zero problemas, nova ativa após Revisado, decisões definitivas, sugestões substituídas e TextoAplicado | US4–US9 e SC-004/SC-006 |
| Contrato simulado | Categorias, 0–5 itens, explicação até 500 sem exigir exemplo, proposta opcional, JSON inválido, truncado, stop por limite | Validador rejeita integralmente; zero persistência parcial |
| Execução simulada | Um HTTP por operação, duplo POST, ocupado imediato, timeout, desconexão, reinício incerto, resposta tardia e regravação | Contadores de chamadas e barreiras controladas; nenhum teste depende de tempo aleatório |
| HTTP/autorização | Dois usuários, IDs diretos e forjados, overposting, antiforgery, endpoints de operação e recuperação | Negação sem exposição de conteúdo |
| SQL Express real | Rollback nos dois pontos atômicos, FK/restrições/índices únicos, rowversion e concorrência de abas, migrations revisadas, backup/restauração separados | Banco descartável específico; não usar EF InMemory para provar comportamento SQL |
| Ollama real | Modelo/versão/digest, parâmetros, contexto sem corte, sinal de término, indisponibilidade/timeout e desempenho com serviços simultâneos | Registro separado de latência, RAM, tokens, apontamentos e avaliação humana |
| Manual integrado | SC-001–SC-007 conforme incremento, Chrome/Firefox/Edge, horário livre e operação sem IA | Relatório de resultados, falhas, não executados e pendências |

Simulações não comprovam a qualidade ou integração real do modelo. Integrações reais devem ser opt-in, sem execução por padrão em cada rodada xUnit. O futuro relatório deve distinguir testes unitários, SQL, Ollama e demonstração.

## Incrementos e cronograma condicionado

As janelas são orientação para quatro dias disponíveis, não compromisso de conclusão. Após aprovação do plano ainda haverá geração/aprovação de tarefas antes de implementar. Cada incremento depende da evidência de saída do anterior; nenhuma verificação abaixo foi executada.

| Incremento | Prioridade / conteúdo | Condição de saída futura | Janela indicativa |
| --- | --- | --- | --- |
| 1 — Base | P0 transversal: solução, MVC, EF/SQL Express, modelo, migrations revisáveis, Identity e testes básicos | Cadastro/login/logout e isolamento inicial; esquema revisado; conexão local | 18–19/10, após autorizações |
| 2 — Requisitos | P0 US2–US4: cadastro, consulta, edição, prioridade, status e versão | Validações, edição concorrente e histórico preservado | 19/10 |
| 3 — IA local | P0 US5–US6: confirmação, candidatos, HTTP, contexto, JSON, dedup, falhas, transação | Simulações essenciais e primeira integração real autorizada; recuperação segura | 20/10; maior risco |
| 4 — Revisão/histórico | P1 US7–US9: decisões, TextoAplicado, ativa R7, status e histórico básico | SC-004/SC-006, rollback e acesso por proprietário | 21/10, condicionado ao 3 |
| 5 — Demonstração | Validar fluxo P0/P1, testes essenciais e Ollama real sob carga local | Evidências e contingência de histórico verdadeiro; falhas declaradas | 21–22/10; reservar preparação |
| Continuidade | P2 US10–US12: arquivamento lógico/consulta separada/bloqueios, filtros e ajuda; validar operação e preparar implantação final | SC-007 e critérios operacionais completos | Após demonstração, sem data imposta |

Se 3/4 não couberem, registrar entrega parcial e riscos ao responsável; não reduzir controles de integridade/segurança, não apresentar simulação como integração e não declarar concluído o fluxo inteiro. P2 não é pré-condição da apresentação. Backup é obrigação operacional transversal: definir procedimento mínimo na preparação e completar automação/evidências na continuidade conforme aprovação; qualquer ausência na demonstração deve constar como pendência operacional.

## Operação, riscos e decisões humanas

Disponibilidade futura de referência: segunda a sexta, 8h–18h em São Paulo; sem bloqueio por horário, inclusive aulas noturnas. Web/SQL mantêm cadastro, edição, consulta e histórico quando Ollama falhar. Backup completo diário, últimos sete dias, cópia protegida separada do banco ativo, restauração de teste separada e sem sobrescrita do principal; procedimento em research/quickstart. Demonstração local não equivale a implantação ou homologação; implantação acadêmica formal única será posterior, em testes.

| Risco | Tratamento e comprovação futura |
| --- | --- |
| 8 GB compartilhados entre Windows, IDE, SQL e Ollama | Reduzir abas/processos; uma inferência; medir memória total/paginação junto dos serviços; ajustar teto SQL somente após medição e revisão |
| Modelo lento, frio ou sem RAM | Não prometer cumprir 60 s; medir primeira execução e aquecidas, evitar inferência automática de aquecimento; usar histórico verdadeiro identificado na contingência |
| SQL Express ausente, permissões/instância/TCP/certificado incorretos | Preparação explícita e separada; conexão testada no futuro, segredos fora do Git; sem instalação/migration automática na inicialização |
| Timeout/conexão perdida | Quarentena de IA até prova de término; nunca liberar por relógio, consulta de modelos carregados ou simples reinício Web |
| Versão Ollama sem contrato/capacidade verificável | Bloquear análise até verificar contrato, raw/contexto/término; sem adaptação silenciosa para APIs externas |
| Contexto pequeno | Guarda conservadora pode recusar textos cadastralmente válidos; explicar limite sem cortar nem alterar os 5.000 caracteres permitidos |
| Falha ou desligamento no horário de backup | Agendamento na próxima inicialização disponível e alerta de atraso; não declarar backup diário cumprido em dia sem cópia |
| Prazo curto | Portões por incremento e validação antecipada do caminho mais arriscado; sem promessa de P2 ou implantação formal em 22/10 |

Para revisão humana: aprovar estes artefatos e suas adaptações técnicas do modelo; autorizar separadamente tarefas e, depois, implementação; escolher instância/versão instalada do SQL Express, diretório protegido de backup e conta operacional na preparação; definir data/ambiente da implantação final e amostra de avaliação da IA. Ampliação de limites, provedor externo ou mudança funcional exigem nova aprovação. A verificação técnica de tokenizer/sinal de término não é decisão funcional pendente: é gate de habilitação futura descrito no contrato.

PEND-004 recebeu desenho para seus nove temas em research/data-model/contracts/quickstart; não se declara resolvida operacionalmente nem se modifica seu registro na spec.

## Rastreabilidade de cobertura

| Origem | Desenho / incremento | Verificação futura |
| --- | --- | --- |
| RF001, RF004; RN001; UC001/UC002 | Validação ViewModels/serviços; incrementos 1–3 | US2/US4/US5; SC-002 |
| RF002; RN002/RN004; UC001/UC004 | Versão textual e transação de revisão; 2/4 | US4/US8; SC-004 |
| RF003; RN004; UC001 | Arquivamento sempre lógico; continuidade P2 | US10; SC-007 |
| RF005, RF006; RN001/RN004; UC002 | Candidatos + Ollama e validação; 3 | US5/US6; SC-003/SC-005 |
| RF007, RF008; RN002/RN004; UC002/UC004 | Resultado e propostas opcionais; 3/4 | US6.1–US6.6; SC-003 |
| RF009; RN004; UC002/UC003 | Snapshots, transação e reabertura; 3/4 | US5/US6/US7; SC-004 |
| RF010; RN004; UC003 | Histórico próprio sem edição, inclusive vazio; 4 | US7; SC-006 |
| RF011; RN004; UC003 | Filtros combináveis, datas inclusivas; P2 | US11; SC-007 |
| RF012, RF013; RN002/RN003/RN004; UC004 | Decisão definitiva/TextoAplicado/análise ativa; 4 | US8/US9; SC-006 |
| RF014; RN003; UC004 | Status e conclusão humana da ativa; 4 | US9; SC-006 |
| RF015; UC001/UC002 | Lista e consulta próprias; 2 | US3; SC-001/SC-002 |
| Segurança de acesso e Constituição V | Identity/isolamento/HTTPS; 1 e todos | US1/US3.4, SEC-001–SEC-003 |
| Apreensibilidade | Ajuda com exemplos; P2 | US12; SC-007 |
| Disponibilidade/Recuperabilidade/Implantação | Operação e continuidade | Critérios PEND-002/PEND-003 da spec |

## Complexity Tracking

Sem exceção constitucional. O registro técnico durável de operações justifica-se pela deduplicação após reinício, evidência de envio e recuperação segura; não cria funcionalidade de domínio ou serviço distribuído. As tabelas auxiliares Identity são infraestrutura do mecanismo aprovado.

## Registro da execução documental

Leituras e revisão local, sem pesquisa por APIs externas. `setup-plan.ps1 -Json` foi tentado e bloqueado por ExecutionPolicy; nenhuma política foi alterada. Aplicada preparação documental equivalente usando o template lido: FEATURE_SPEC=`specs/001-analisar-requisitos-mvp/spec.md`, IMPL_PLAN=`specs/001-analisar-requisitos-mvp/plan.md`, FEATURE_DIR=`specs/001-analisar-requisitos-mvp`. O campo BRANCH do resolver pode usar o nome da feature como fallback, mas a branch Git real observada é `main`. `.specify/extensions.yml` ausente nas verificações anterior e posterior: nenhum hook a executar. Nenhum tasks.md, código, projeto, instalação, banco, migration, build, teste da aplicação, inferência, commit, push ou mudança de branch nesta etapa.

Verificação documental realizada: seis artefatos produzidos, referências locais conferidas sem links quebrados e sem placeholders pendentes do template. SHA-256 antes/depois confirmou Constitution, spec, checklist e DOCX idênticos. O DOCX também coincide com o hash acadêmico registrado na spec. A árvore já continha arquivos não rastreados antes desta execução; nenhum deles foi adicionado ao índice Git. Essas verificações são de documentos e escopo, não testes da aplicação.
