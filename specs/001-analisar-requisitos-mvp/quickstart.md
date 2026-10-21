# Guia futuro de preparação e validação — Requisita+

Data: 2026-10-18. **Não executado.** Este guia será aplicável somente depois de aprovação do plano, geração/aprovação de tarefas e implementação autorizada. Os caminhos de projetos abaixo ainda são propostos; os comandos são instruções futuras, não evidência de arquivos existentes ou resultados. Nenhum procedimento abaixo deve ser acionado automaticamente por este planejamento.

## Pré-requisitos a conferir posteriormente

1. Windows e PowerShell; SDK .NET 10 e patches compatíveis; EF Core/SQL provider/Identity da linha compatível com net10.0, ferramentas de EF e xUnit registradas no projeto futuro. Registrar versões efetivas, sem presumir instalação.
2. SQL Server Express local, nome real da instância, acesso da conta e conexão SQL TCP/IP quando configurada conforme arquitetura acadêmica. Não presumir `(localdb)` nem substituir Express por outro banco. Definir instância/porta local e política de certificado da conexão na preparação.
3. Ollama local dedicado à aplicação, modelo `qwen2.5:3b` previamente disponível. Conferir versão/modelo/digest e contrato `/api/generate`, raw/schema, contadores e término; nada deste guia autoriza baixar modelo ou instalar dependências agora.
4. Memória disponível para Windows, SQL Express, aplicação Web e modelo simultâneos. Fechar processos desnecessários; nenhuma inferência concorrente por terminal ou outro cliente. Não instalar Docker nem GPU/runtime extra como requisito.
5. HTTPS local configurado para o navegador e segredos fora do Git. Chrome, Firefox e Edge para validação futura, com versões registradas.
6. Banco de testes separado, contas de dois analistas, diretório protegido de backup separado dos arquivos ativos e conta operacional definida. Não usar registros pessoais sensíveis nos ensaios.

## Configuração futura

Usar a hierarquia própria do ASP.NET Core: appsettings, arquivo por ambiente, variáveis de ambiente (por exemplo `Ollama__Model`) e User Secrets no desenvolvimento para valores sensíveis. ConnectionStrings:DefaultConnection deve apontar à instância Express escolhida; preferir autenticação integrada quando viável, sem versionar credenciais. Não fornecer connection string real nos relatórios.

Definir inicialmente Ollama: BaseUrl `http://127.0.0.1:11434`, Model `qwen2.5:3b`, ContextTokens `4096`, MaxOutputTokens `1024`, OperationTimeoutSeconds `60`, KeepAlive `5m`, MaxProblems `5`. Conferir validador de destino exato, proxy/redirects desativados e ausência de retry/fallback. Uma sobreposição de ambiente não pode habilitar servidor externo ou ampliar limites sem aprovação. Ver [contrato de IA](contracts/ollama.md).

Executar validação offline da guarda de contexto sobre o prompt raw final e o tokenizer local compatível, incluindo tokens especiais e amostras Unicode/limites. Registrar a fundamentação de que bytes UTF-8 mais sobrecarga são limite superior, ou resolver contador exato local antes de habilitar IA. Não executar inferência de sondagem para descobrir se cabia. Se a capacidade não for demonstrada, mostrar IA indisponível e manter funções independentes.

Conferir o diário de operações no startup. Se houver Enviada/Incerta sem término comprovado, seguir recuperação antes de novas inferências. Startup da Web não deve depender de Ollama respondendo nem criar migration automaticamente.

## Comandos futuros, após implementação e autorização

Executar na raiz do repositório futuro. `restore`, migrations e testes reais podem escrever/alterar o ambiente e só pertencem à fase autorizada correspondente.

```powershell
dotnet --info
dotnet restore .\RequisitaPlus.slnx
dotnet build .\RequisitaPlus.slnx --no-restore
dotnet ef migrations list --project .\src\RequisitaPlus.Web --startup-project .\src\RequisitaPlus.Web
dotnet ef migrations script --idempotent --project .\src\RequisitaPlus.Web --startup-project .\src\RequisitaPlus.Web
```

Revisar migrations e SQL gerado, integridade de dados e estratégia de recuperação. Confirmar alvo de banco e backup antes de qualquer atualização; nunca aplicar no banco principal como teste. Só então, em execução futura expressamente autorizada:

```powershell
dotnet ef database update --project .\src\RequisitaPlus.Web --startup-project .\src\RequisitaPlus.Web
dotnet test .\tests\RequisitaPlus.Tests --filter "Category!=SqlServer&Category!=Ollama"
dotnet run --project .\src\RequisitaPlus.Web
```

As categorias deverão ser implementadas de modo que integração seja opt-in, inclusive dentro de dotnet test. O processo Web é único; não executar duas instâncias IIS Express/Kestrel em paralelo. Não usar dotnet watch reiniciando a aplicação durante inferência. Abrir o endereço HTTPS efetivo exibido pela Web, sem presumir porta. Inicializar SQL/Ollama pelos mecanismos já aprovados/disponíveis do ambiente; não iniciar inferência para aquecer o modelo automaticamente.

Integrações separadas, somente depois de configurar banco descartável e autorizar os serviços reais:

```powershell
dotnet test .\tests\RequisitaPlus.Tests --filter "Category=SqlServer"
dotnet test .\tests\RequisitaPlus.Tests --filter "Category=Ollama"
```

SQL tests nunca devem escolher a connection string principal por fallback. Ollama tests devem usar modelo e limites aprovados, uma inferência ativa e confirmação/autorização de execução no relatório; simulações não habilitam essas chamadas implicitamente.

## Cenários por incremento — resultados esperados

| Incremento / cenário | Resultado que deverá ser observado | Origem |
| --- | --- | --- |
| 1: cadastrar/login/logout; campos e senha inválidos | Conta válida utiliza Identity; dados inválidos negados; senha não legível; páginas protegidas | US1; SEC-001 |
| 1/2: dois usuários e identificadores diretos | Nenhuma leitura/alteração de requisito, análise, sugestão ou operação alheia | US3.4; SC-002 |
| 2: título/descrição/espaços/limites/prioridade ausente | Recusar inválidos no servidor, sem default de prioridade ou status manipulável | US2/US4; RF004 |
| 2: mudar texto e só prioridade | Texto incrementa versão e retorna Rascunho; prioridade sozinha conserva versão/ativa/status | US4; R1 |
| 2/4: A → B → A | Terceira versão não reativa análise da primeira | R1; SC-004 |
| 3: cancelar confirmação e contexto insuficiente | Zero chamadas/alterações; requisito completo nunca cortado | US5.6/US5.8 |
| 3: candidatos presentes/ausentes, resposta válida | Uma chamada; resultado somente após commit; candidatos não viram diagnósticos sem IA | US5/US6 |
| 3: JSON inválido, incompleto, truncado, categoria/quantidade/explicação inválida | Rejeitar integralmente, sem correção/retry ou análise parcial | Q5; SC-005 |
| 3: explicação suficiente sem exemplo e proposta ausente | Aceitar contrato; mostrar “Sugestão não disponível” | US6.1/US6.6 |
| 3: mesmo ID/duplo clique; segundo usuário dispara outra análise | Uma chamada por ID; outra operação recebe ocupado imediato, nunca execução futura automática | US5.5/US5.7 |
| 3: timeout com inferência ainda ativa / conexão perdida | Erro ao usuário em 60 s; bloqueio continua; CRUD/histórico independentes funcionam | SEC-005; PEND-002.1 |
| 3: queda entre intenção/envio/commit e restart | Diário impede reenvio e bloqueia situação incerta; commit único reconhecido por ID | US5; D7/D8 |
| 3: falha ao inserir uma sugestão/status; regravação | Rollback completo; ativa anterior preservada; retry de gravação sem inferência | US5.9; SC-004 |
| 3/4: editar durante inferência | Snapshot corresponde ao enviado; resultado antigo fica histórico sem promover nova versão | US5.10 |
| 4: A1 ativa Revisado; A2 sucesso/falha mesma versão | Sucesso promove A2/Analisado com novas Pendentes; falha preserva A1/status/decisões | R7; US5.11–US5.12 |
| 4: aplicar/ajustar/descartar em casos independentes | TextoAplicado exato ou null; origem IA intacta; decisões definitivas | US8; SC-006 |
| 4: corrida nova ativa x revisão; duas abas | Uma mutação consistente, outra conflito com rollback; sem aplicar sugestão substituída | R7; RN004 |
| 4: zero problemas ou todos descartados | Analisado até confirmação explícita da conclusão; reabrir resultado não desfaz revisão | US6.3; US9 |
| 4: abrir histórico vazio, depois duas análises próprias | Mensagem explícita e dados verdadeiros em ordem/fuso corretos, sem reinferir | US7 |
| 5: fluxo P0/P1 integrado e real | Autenticar → cadastrar → analisar ao vivo → ler → decidir → consultar histórico, com evidências | SC-001–SC-006 |
| Continuidade P2: arquivar com/sem histórico | Exclusão lógica e consulta separada; bloqueia edição/análise/revisão/restauração | US10; RN004 |
| Continuidade P2: filtros e datas | Interseção opcional; dia final integral; intervalo inválido negado; dados próprios | US11 |
| Continuidade P2: ajuda | Exemplos bem/mal escritos; consulta não analisa nem altera | US12 |

Falhas e concorrência devem ser reproduzidas primeiro por simuladores/barreiras controladas, sem exigir inferências reais repetidas. Transações/FKs/rowversion devem ser verificadas com SQL Express real em banco separado. Relatório de simulação jamais afirma qualidade real do modelo.

## Medição real antes de 22/10

Depois de autorização para executar integração, registrar máquina, versões, modelo/digest, serviços simultâneos, RAM total/livre/pico, paginação, primeira chamada/fria e chamadas posteriores autorizadas, latência, status, tokens conhecidos e custo de API zero. Usar requisitos curtos representativos dos quatro tipos e um caso sem indicadores, sem exigir detecção perfeita. Registrar avaliação humana de cada explicação/proposta e problemas não encontrados. Não confundir JSON válido com qualidade satisfatória.

Se houver OOM/timeout, não aumentar limites nem trocar modelo silenciosamente. Reduzir processos dispensáveis, revisar medições e relatar impedimento; qualquer mudança de modelo/limite exige aprovação. Não presumir GPU útil ou quantidade de memória do modelo. O keep_alive de cinco minutos não é prazo de liberação do controle. Se não houver tempo seguro para nova tentativa confirmada, usar somente histórico real explicitamente identificado como contingência.

## Recuperação de execução incerta

1. Bloquear novas análises; manter gestão/histórico quando SQL/Web disponíveis. Registrar estado, horário e causa sem conteúdo sensível.
2. Se a chamada original ainda é observável, aguardar prova terminal sem iniciar outra inferência; timeout já informado não volta a sucesso silencioso.
3. Se não houver prova, operador encerra efetivamente a instância dedicada Ollama e seus processos de inferência; verifica término antes de reiniciar. Reiniciar apenas a Web ou consultar modelos carregados não basta.
4. Em manutenção com Web parada, reconciliar o diário por procedimento restrito revisado futuramente, registrando a evidência de encerramento. Não apagar operação, análise, sugestões ou decisões. Reabrir IA só após esse gate; dúvida mantém bloqueio.
5. A operação incerta não é reenviada. Nova análise é nova confirmação/ID. Se um commit anterior já existe, retornar a análise persistida; se apenas o payload em memória se perdeu, informar ausência de conclusão e preservar o diário.

## Backup, restauração e disponibilidade futura

Preparar Agendador de Tarefas Windows para backup FULL diário da instância/banco escolhidos, via ferramenta SQL disponível/aprovada, sem SQL Agent como dependência presumida. Destino protegido separado do banco ativo, ACL restrita e retenção de cópias dos últimos sete dias. Registrar sucesso/erro e execução perdida; agendar execução após horário perdido quando o computador voltar. Falha impede limpeza da última cópia válida e gera aviso; dia sem cópia não é declarado cumprido.

Em ensaio separado: verificar a cópia, restaurar em outro banco e outros arquivos, conferir integridade SQL, usuários, requisitos, análises, sugestões, decisões, TextoAplicado, operações e relacionamentos, além de amostras e contagens. Verificar que o principal não foi alterado. Não usar WITH REPLACE no principal nem cópia de MDF aberto. Uma verificação de arquivo sem restauração não comprova recuperabilidade. Possível perda desde a última cópia válida deve constar no relatório.

Validar acesso fora de 8h–18h e em fim de semana sem restrição por relógio; com Ollama indisponível, verificar cadastro/edição/consulta/histórico independentes. Horário comercial futuro é referência operacional, não bloqueio de login ou garantia contínua do computador local.

## Evidências e encerramento de incremento

Registrar: incremento e versão do código futuro, cenário/RF/RN/UC/SC, ambiente, dados sem exposição pessoal, tipo de teste (simulado/SQL/Ollama/manual), esperado, observado, aprovado/reprovado, falhas, não executados e pendências. Não avançar para o próximo incremento enquanto controles essenciais do anterior estiverem sem evidência ou decisão humana. Este guia contém critérios, sem resultados preenchidos.

22/10/2026: demonstração local do MVP em desenvolvimento, sem declarar implantação formal/homologação. Contingência deve mostrar análise real já armazenada como histórica e identificar os passos ao vivo não executados. A única implantação acadêmica formal em testes ocorrerá depois, mediante preparação, validação e autorização próprias. Não condicionar a demonstração à implementação de P2 e não declarar o TCC completo por demonstrar P0/P1.
