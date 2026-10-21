# Feature Specification: Requisita+ — MVP de análise e controle de requisitos

**Feature Branch**: `main` (existente; nenhuma branch criada)

**Created**: 2026-10-17

**Status**: Consolidação documental de 2026-10-18 concluída com decisões Q1–Q10, revisões R1–R7 e decisões finais sobre US6.1/PEND-002–PEND-004 incorporadas. PEND-002 e PEND-003 resolvidas documentalmente; PEND-004 encaminhada ao planejamento técnico, sem resolução técnica antecipada. Aguardando revisão e aprovação humanas; sem autorização para planejamento.

**Input**: Especificação existente refinada pelo prompt consolidado aprovado em 2026-10-18: Q1–Q10, R1 (versões), R2 (TextoAplicado) e R6 (privacidade), além das regras complementares transcritas no registro de decisões. Entrega prioritária em 22/10/2026 com demonstração do fluxo completo, IA local e revisão humana; demais requisitos acadêmicos preservados. O prompt não individualiza enunciados R3–R5; não são inventadas decisões sob esses rótulos.

**Complemento aprovado R7**: a análise ativa é exclusivamente a análise válida, concluída e persistida mais recentemente para a versão atual do título e da descrição; o registro R7 abaixo resolve PEND-001 sem alterar as demais decisões.

**Consolidação final aprovada**: explicações com exemplos opcionais; disponibilidade operacional de referência sem restrição de horário de acesso; política de backup diário com retenção de sete dias; demonstração local distinta da implantação formal; detalhes técnicos encaminhados ao planejamento futuro autorizado. Esses refinamentos são registrados abaixo sem modificar as fontes originais.

## User Scenarios & Testing *(mandatory)*

### Fontes e delimitação da entrega

Foram lidos a [Constituição v1.0.0](../../.specify/memory/constitution.md) e o [documento acadêmico original](../../docs/Documentacao_Requisita+.docx), incluindo casos de uso, RF, RN, qualidade, modelo de dados, arquitetura e glossário. O controle de versão do original declara versão 1.1, de 17/09/2026. As referências usam os títulos de seção, sem presumir paginação.

O documento acadêmico permanece inalterado. SHA-256 de referência: `BF2299FDD0202D23823443421A743C9CECB81DB7B57AB1EAD4D88F57EA3005C2`.

| Recorte | Conteúdo | Condição de aceite |
| --- | --- | --- |
| Demonstração de 22/10/2026 | P0: US1–US6 e controles transversais; Q10 inclui US7–US9 P1 no fluxo demonstrado | Autenticar, cadastrar título/descrição/prioridade, analisar localmente ao vivo, visualizar, decidir e consultar histórico. Contingência somente com análises reais armazenadas, identificadas como históricas. |
| Incremento P1 | US7–US9, histórico básico e revisão humana | Prioridade P1 preservada; Q10 aprova sua participação na demonstração completa, sem reclassificação para P0. |
| Continuidade do TCC | P2: US10–US12, exclusão, filtros e ajuda; implementação e validação dos requisitos operacionais ainda não realizadas | Permanecem no escopo acadêmico, sem exclusão definitiva. |
| Escopo completo | P0 + P1 + P2; RF001–RF015, RN001–RN004, UC001–UC004 e requisitos não funcionais aplicáveis | Aprovação dos artefatos e validação integral futura. Demonstrar P0 não equivale a concluir o TCC. |

P0/P1/P2 expressam a sequência das histórias, sem modificar Alta/Média/Baixa dos RF originais nem a prioridade obrigatória do requisito cadastrado. A revisão funcional completa é P1 e integra a demonstração por Q10; a proibição de alterar texto pela IA sem confirmação vale desde P0. Reabrir resultado é P0; navegar pelo histórico é P1; filtrar por requisito/período é P2.

Cada história pode ser testada com dados preparados, mesmo quando depende de outras capacidades. O fluxo integrado depende de autenticação, requisito cadastrado e confirmação explícita antes de cada análise local. Os cenários abaixo são critérios para validação futura, ainda não executados. Pendências residuais estão registradas ao final e não autorizam escolher comportamentos.

### User Story 1 - Acessar com identidade autenticada (Priority: P0)

Como Analista, quero cadastrar minha identificação e entrar com e-mail e senha para acessar as funcionalidades protegidas.

**Why this priority**: A autenticação é pré-condição de UC001. O cadastro por tela com nome, e-mail e senha está expresso em “Descrição da Arquitetura”.

**Rastreabilidade**: Sem RF específico; qualidade “Segurança de acesso”, arquitetura e pré-condição UC001; sem RN de credenciais; Constituição V.

**Independent Test**: Usar uma conta preparada para autenticação e testar o cadastro separadamente, sem análises.

**Acceptance Scenarios**:

1. **Given** nome obrigatório de até 100 caracteres, e-mail obrigatório válido e único de até 150 e senha de pelo menos oito caracteres contendo maiúscula, minúscula, número e caractere especial, **When** confirma cadastro, **Then** cria conta utilizável para autenticação com ASP.NET Core Identity e senha protegida por hash com salt, nunca em texto puro.
2. **Given** conta existente, **When** informa credenciais corretas, **Then** obtém acesso; com senha incorreta, o acesso é negado sem expor senha ou segredos.
3. **Given** campo obrigatório vazio ou só com espaços, e-mail inválido/duplicado, nome acima de 100 caracteres, e-mail acima de 150 ou senha fora da política, **When** confirma, **Then** cliente e servidor validam os dados e impedem o cadastro inválido, inclusive por acesso direto ao servidor.
4. **Given** visitante não autenticado, **When** tenta consultar ou modificar requisitos, análises ou decisões, **Then** o acesso é negado e nenhum dado é modificado.

### User Story 2 - Cadastrar requisito válido (Priority: P0)

Como Analista autenticado, quero cadastrar título, descrição e prioridade para centralizar um requisito.

**Why this priority**: É a entrada do fluxo de análise.

**Rastreabilidade**: RF001, RF004; RN001 (mesmos campos exigidos na análise); UC001; “Estrutura de Dados — Requisito”.

**Independent Test**: Cadastrar com conta preparada e reabrir o registro sem analisá-lo.

**Acceptance Scenarios**:

1. **Given** título obrigatório de até 150 caracteres, descrição obrigatória de até 5.000 e prioridade escolhida explicitamente entre Alta, Média e Baixa, **When** confirma inclusão, **Then** armazena requisito vinculado ao criador, com data de cadastro e status inicial Rascunho, disponível na lista.
2. **Given** título/descrição vazio ou só com espaços, limites excedidos ou prioridade ausente/fora do domínio, **When** confirma, **Then** cliente e servidor recusam a operação, identificam o campo inválido e não criam registro incompleto.
3. **Given** dados válidos e falha de gravação, **When** confirma, **Then** informa erro ao salvar e não anuncia sucesso.

Q1 aprova prioridade obrigatória sem seleção automática, editável posteriormente. Status é controlado exclusivamente pelo fluxo: Rascunho, Analisado, Revisado ou Arquivado; tentativas de edição manual são rejeitadas também no servidor.

### User Story 3 - Listar e consultar requisitos (Priority: P0)

Como Analista autenticado, quero consultar a lista e o conteúdo de um requisito para escolher o que editar ou analisar.

**Why this priority**: Permite localizar registros e iniciar os fluxos principais.

**Rastreabilidade**: RF015; UC001 e seleção de UC002; sem RN específica de listagem.

**Independent Test**: Listar e abrir registros preparados sem gerar análises.

**Acceptance Scenarios**:

1. **Given** registros próprios não arquivados, **When** consulta a lista e seleciona um, **Then** consulta somente seus requisitos, com título, descrição, prioridade e status armazenados.
2. **Given** nenhum requisito acessível, **When** consulta, **Then** vê indicação de ausência de requisitos, sem dados fictícios.
3. **Given** identificador inexistente, **When** tenta consultar, **Then** recebe “Requisito não encontrado”, sem criar registro.

4. **Given** identificador de requisito, análise ou sugestão de outro usuário, **When** tenta consultar ou modificar, inclusive diretamente no servidor, **Then** a autorização nega acesso sem revelar conteúdo nem permitir alterações. Não há compartilhamento no MVP (Q3/R6).

### User Story 4 - Editar preservando análises (Priority: P0)

Como Analista autenticado, quero corrigir título, descrição e prioridade sem perder avaliações anteriores.

**Why this priority**: Permite manutenção com integridade histórica.

**Rastreabilidade**: RF002, RF004, RF009; RN001, RN004; UC001; seção 1.6, preservação de TextoAnalisado.

**Independent Test**: Editar registro com análise preparada e comparar o histórico antes/depois.

**Acceptance Scenarios**:

1. **Given** requisito próprio não arquivado, **When** confirma alteração válida no título ou na descrição, **Then** atualiza campos/data, retorna a Rascunho e distingue a nova versão textual; texto analisado, data da análise, explicações, propostas e decisões anteriores permanecem preservados.
2. **Given** título/descrição vazio ou só com espaços, título acima de 150, descrição acima de 5.000 ou prioridade inválida, **When** tenta salvar, **Then** cliente e servidor recusam e mantêm os dados persistidos anteriores.
3. **Given** requisito inexistente ou erro de gravação, **When** confirma edição, **Then** informa respectivamente não encontrado ou erro ao salvar, sem anunciar atualização concluída.

4. **Given** análise da versão anterior com sugestões Pendentes, **When** altera o texto, **Then** elas permanecem visíveis e Pendentes no histórico, mas sua aplicação à nova versão é bloqueada; não há descarte automático. Uma nova análise é necessária para concluir nova revisão (Q2/R1).
5. **Given** prioridade previamente cadastrada, **When** confirma outra opção válida, **Then** a prioridade é atualizada; a associação da análise considera a versão do título e da descrição, não a prioridade.

### User Story 5 - Solicitar análise real de IA (Priority: P0)

Como Analista autenticado, quero submeter um requisito cadastrado para avaliar a qualidade de sua escrita.

**Why this priority**: É o valor central do MVP e exige integração real autorizada.

**Rastreabilidade**: RF005, RF006, RF009; RN001, RN002, RN004; UC002; Constituição VII/IX.

**Independent Test**: Usar requisito próprio e confirmação preparados; validar integração local separadamente das respostas simuladas usadas para testar falhas. Nenhuma inferência é executada nesta etapa documental.

**Acceptance Scenarios**:

1. **Given** requisito próprio não arquivado com campos válidos, contexto suficiente e serviço local livre, **When** confirma explicitamente o processamento local de título, descrição e candidatos derivados, **Then** executa uma chamada ao Ollama, valida o resultado e somente após persistir atomicamente análise e sugestões informa conclusão, preservando título/descrição analisados e associação verificável à versão textual.
2. **Given** campos obrigatórios vazios, só com espaços ou inválidos, **When** tenta analisar, **Then** informa a pendência e não inicia inferência.
3. **Given** requisito inexistente, **When** solicita análise, **Then** informa “Requisito não encontrado” e não chama o provedor.
4. **Given** Ollama indisponível, resposta inválida/incompleta/truncada/incompatível ou tempo limite de 60 segundos excedido, **When** a operação falha, **Then** informa a causa e “Não foi possível concluir a análise”, preserva requisito/histórico e não corrige silenciosamente nem repete a chamada automaticamente.
5. **Given** acionamentos repetidos da mesma operação, **When** chegam pela interface ou diretamente ao servidor, **Then** a identificação da operação permite reconhecê-los sem duplicar inferência ou persistência. Nova análise intencional não é automaticamente duplicata, mas exige nova confirmação.
6. **Given** confirmação cancelada, **When** encerra o diálogo, **Then** não cria análise, não modifica requisito e não inicia inferência.
7. **Given** uma análise local em execução, **When** solicita outra ou a interface atinge o tempo limite, **Then** não inicia nova análise nem cria fila automática; o controle no servidor permanece enquanto a execução local ainda estiver ativa.
8. **Given** título, descrição, instruções, candidatos e reserva de saída que excedam o contexto configurado, **When** verifica antes da inferência, **Then** informa a limitação sem cortar o requisito nem chamar o modelo.
9. **Given** resultado válido recebido e falha na persistência, **When** tenta concluir, **Then** nenhuma análise/sugestão parcial é confirmada; preserva temporariamente o resultado quando possível e permite nova tentativa explícita de gravação sem outra inferência, sem anunciar análise concluída antes do sucesso.
10. **Given** edição textual durante uma análise, **When** o resultado é persistido, **Then** continua associado à versão efetivamente enviada; se ela já não for atual, não pode orientar a revisão nem tornar a nova versão Analisado/Revisado.
11. **Given** análise ativa A da versão atual, **When** uma nova análise B dessa mesma versão é validada, concluída e persistida com sucesso, **Then** B substitui A como única análise ativa; suas sugestões começam Pendente, quando houver, sem transferir decisões de A. A permanece no histórico com resultados e decisões originais (R7).
12. **Given** análise ativa A, inclusive com requisito Revisado, **When** uma nova análise falha, é inválida ou não é persistida com sucesso, **Then** A permanece ativa e a tentativa não altera o status nem os resultados e decisões anteriores. Resultado apenas recebido ou temporariamente preservado não substitui A (R7).

Contrato JSON, limites e fluxo híbrido são detalhados em “Integração de IA local e contrato aprovado” (Q5/Q9).

### User Story 6 - Ler e reabrir resultado armazenado (Priority: P0)

Como Analista autenticado, quero visualizar possíveis problemas, explicações e sugestões e reabrir o resultado para compreender a avaliação.

**Why this priority**: Torna a análise útil e comprova o armazenamento na demonstração.

**Rastreabilidade**: RF006–RF009; RN002, RN004; UC002 e entrada de UC004; usabilidade “Inteligibilidade”.

**Independent Test**: Abrir análise armazenada, inclusive após novo acesso, sem chamar novamente a IA.

**Acceptance Scenarios**:

1. **Given** análise com apontamentos, **When** abre o resultado, **Then** vê requisito, data, título/descrição analisados e cada problema com tipo obrigatório, explicação obrigatória, clara e compreensível de até 500 caracteres e proposta quando retornada; exemplos podem integrar a explicação quando úteis, mas não são obrigatórios. Sem texto sugerido, exibe “Sugestão não disponível”, distinguindo conteúdo original, sugerido e efetivamente aplicado.
2. **Given** resultado armazenado, **When** reabre em novo acesso, **Then** recupera o mesmo conteúdo gerado sem nova chamada de análise.
3. **Given** lista vazia de problemas na análise ativa válida recém-persistida da versão atual ainda não revisada, **When** consulta, **Then** informa que não foram identificados problemas, mantém Analisado e exige confirmação explícita do analista para Revisado; ausência de apontamentos não certifica qualidade. Reabrir um resultado não desfaz revisão já confirmada; concluir e persistir uma nova análise ativa segue R7.
4. **Given** nenhuma análise para o requisito, **When** consulta resultado, **Then** vê indicação de ausência de análise, sem resultado simulado.
5. **Given** sugestão exibida, **When** apenas abre ou fecha o resultado, **Then** não altera o texto nem registra decisão humana automaticamente.
6. **Given** apontamento com tipo permitido, explicação válida e suficiente de até 500 caracteres e texto sugerido opcional ou nulo, **When** valida e exibe o resultado, **Then** a ausência de exemplo não invalida o apontamento nem exige propriedade adicional obrigatória no JSON.

Q5 resolve a aparente inconsistência de RF008: texto sugerido é opcional e pode ser nulo, conforme o modelo acadêmico. A análise pode não ser exaustiva; revisão humana é indispensável, inclusive quando não há apontamentos.

### User Story 7 - Consultar histórico básico (Priority: P1)

Como Analista autenticado, quero consultar avaliações anteriores.

**Why this priority**: Amplia a consulta individual P0 para navegação histórica sem filtros.

**Rastreabilidade**: RF010, RF009; RN004; UC003, recorte sem filtros.

**Independent Test**: Consultar duas análises preparadas do mesmo requisito e uma de outro.

**Acceptance Scenarios**:

1. **Given** análises próprias armazenadas, **When** abre “Consultar Análises” e seleciona uma, **Then** vê a lista da mais recente para a mais antiga e consulta requisito, versão textual, data, problemas e sugestões daquela análise, sem substituir resultados históricos.
2. **Given** nenhuma análise própria, **When** acessa a página, **Then** o acesso é permitido e exibe “Nenhuma análise encontrada” (refinamento aprovado Q8 da pré-condição de UC003).
3. **Given** histórico existente, **When** tenta editar/excluir análise ou conteúdo gerado, inclusive por acesso direto, **Then** a operação é negada; somente decisão humana e TextoAplicado podem ser registrados uma única vez conforme US8, preservando a origem da IA.
4. **Given** instante de análise armazenado em UTC, **When** consulta o histórico, **Then** exibe a data/hora no fuso America/Sao_Paulo e restringe resultados ao usuário autenticado, incluindo consulta preservada dos próprios arquivados.
5. **Given** análise A substituída por B como ativa na mesma versão textual, **When** consulta A no histórico, **Then** recupera seus resultados, sugestões e decisões originais, sem transferência de decisões para B nem alteração ou exclusão do conteúdo de A (R7; RN004).

### User Story 8 - Decidir sobre uma sugestão (Priority: P1)

Como Analista autenticado, quero aplicar, ajustar manualmente ou descartar uma sugestão, confirmando a decisão para manter controle sobre o requisito.

**Why this priority**: Completa a revisão humana sem automatizar alterações.

**Rastreabilidade**: RF012, RF013, RF002; RN002, RN004; UC004.

**Independent Test**: Confirmar aplicação, ajuste e descarte em três sugestões preparadas e reabrir as decisões.

**Acceptance Scenarios**:

1. **Given** sugestão Pendente com texto sugerido da análise ativa da versão atual de requisito próprio não arquivado, **When** confirma explicitamente a aplicação da redação válida, **Then** atualiza a descrição, registra Aplicada e TextoAplicado com a redação efetivamente adotada na mesma transação; se mudar o texto, retorna a Rascunho.
2. **Given** sugestão Pendente da análise ativa da versão atual, inclusive sem texto sugerido, **When** prepara redação ajustada válida e confirma, **Then** atualiza descrição, TextoAplicado, decisão Ajustada e status atomicamente; se mudar o texto, retorna a Rascunho, preservando TextoSugerido e explicação originais.
3. **Given** sugestão Pendente da análise ativa da versão atual, **When** confirma descarte, **Then** registra Descartada, mantém TextoAplicado nulo e preserva a descrição; eventual conclusão da revisão depende de US9.
4. **Given** alteração apenas preparada, **When** não há confirmação explícita, **Then** nenhuma mudança é aplicada nem decisão é registrada como concluída.
5. **Given** falha em qualquer parte da gravação de descrição, TextoAplicado, decisão ou status, **When** confirma, **Then** não confirma mudanças parciais e informa erro, mantendo todos os dados persistidos anteriores.
6. **Given** decisão já confirmada, **When** tenta modificá-la, **Then** a operação é negada também no servidor; decisões são definitivas. Toda sugestão começa Pendente.
7. **Given** alteração textual por aplicação/ajuste, **When** tenta aplicar as demais sugestões da mesma análise, **Then** bloqueia sua aplicação à nova versão e as conserva no histórico com suas decisões anteriores, sem descartar automaticamente as Pendentes.
8. **Given** proposta com marcador como [TEMPO A DEFINIR], **When** prepara a decisão, **Then** a redação final deve ser revisada e confirmada pelo analista; o marcador não é presumido como valor de negócio aprovado.
9. **Given** sugestão de análise anterior substituída por nova análise ativa, mesmo sem mudança no título/descrição, **When** tenta aplicar ou ajustar essa sugestão ao requisito atual, inclusive diretamente no servidor, **Then** a aplicação é bloqueada e o resultado e a decisão originais permanecem preservados no histórico (R7).

Q6/R2 aprovam TextoAplicado nullable como refinamento do modelo acadêmico: preenchido em Aplicada/Ajustada quando houver texto efetivamente adotado; nulo em Pendente/Descartada. Não reescreve conteúdo gerado. Sem proposta, não há aplicação direta; o analista pode redigir ajuste ou descartar. R7 resolve PEND-001: somente sugestões da análise ativa da versão atual podem orientar a revisão; análises anteriores preservam seus resultados e decisões originais.

### User Story 9 - Acompanhar conclusão da revisão (Priority: P1)

Como Analista autenticado, quero ver o status refletir as decisões para acompanhar pendências.

**Why this priority**: Dá significado verificável a Revisado.

**Rastreabilidade**: RF014, RF013; RN003, RN004; UC004; glossário de status.

**Independent Test**: Conferir análise sem apontamentos, descarte de todas as sugestões e alteração textual por aplicação/ajuste, distinguindo Analisado, Revisado e Rascunho.

**Acceptance Scenarios**:

1. **Given** ao menos uma sugestão Pendente na análise ativa da versão atual, **When** decide sobre parte delas sem mudar o texto, **Then** não passa a Revisado por conclusão dessa análise; decisões de análises anteriores não satisfazem as pendências da ativa.
2. **Given** todas as sugestões da análise ativa da versão atual decididas sem alteração textual, inclusive se todas foram descartadas, **When** o analista confirma explicitamente a conclusão da revisão, **Then** passa a Revisado utilizando somente a análise ativa; registrar a última decisão isoladamente não presume essa confirmação.
3. **Given** falha de registro de decisão, **When** confere o status, **Then** a falha não conta como decisão concluída para atingir Revisado.
4. **Given** nova análise válida da versão atual sem apontamentos, **When** conclui e persiste o resultado, **Then** ela se torna a análise ativa e o requisito fica Analisado até confirmação explícita do analista para concluir a revisão, inclusive se antes estava Revisado; isso não certifica qualidade.
5. **Given** aplicação/ajuste ou edição que alterou título/descrição, **When** tenta concluir revisão com análise anterior, **Then** permanece Rascunho e exige nova análise da nova versão, mesmo que todas as decisões anteriores tenham sido registradas.
6. **Given** versão atual em Rascunho ainda sem análise concluída, **When** persiste sua primeira análise válida, com ou sem apontamentos, **Then** passa a Analisado; falha ou resposta não persistida não promove o status.
7. **Given** requisito Revisado sem alteração textual, **When** uma nova análise válida da versão atual é concluída e persistida, **Then** ela se torna ativa e o status retorna a Analisado; suas sugestões começam Pendente, quando houver, sem herdar decisões anteriores. Nova conclusão exige tratar as sugestões da ativa e confirmação humana explícita (R7).
8. **Given** nova análise ativa com sugestões Pendentes e análise anterior com todas as decisões concluídas, **When** tenta concluir revisão com base na análise anterior, **Then** não passa a Revisado; somente as decisões pertinentes à análise ativa contam, seguidas da confirmação explícita do analista (R7).

### User Story 10 - Excluir preservando histórico (Priority: P2)

Como Analista autenticado, quero excluir um requisito sem apagar seu histórico.

**Why this priority**: Permanece obrigação acadêmica, sem ser essencial à demonstração inicial.

**Rastreabilidade**: RF003; RN004; UC001; seção 1.6; Constituição IV.

**Independent Test**: Excluir requisito com análise preparada e conferir preservação dos vínculos.

**Acceptance Scenarios**:

1. **Given** requisito próprio com ou sem análises, **When** confirma exclusão, **Then** recebe status Arquivado por exclusão sempre lógica, preservando registros, análises, sugestões e vínculos, sem referências órfãs.
2. **Given** requisito inexistente ou erro de gravação, **When** solicita exclusão, **Then** informa a falha correspondente sem anunciar sucesso.
3. **Given** requisito arquivado, **When** consulta a listagem principal, **Then** não o vê nessa lista e pode consultá-lo em seção própria de arquivados com histórico preservado.
4. **Given** requisito arquivado, **When** tenta editar, analisar, revisar ou restaurar, inclusive diretamente no servidor, **Then** a operação é bloqueada. Não há exclusão física disponível na aplicação (Q7).

### User Story 11 - Filtrar análises (Priority: P2)

Como Analista autenticado, quero filtrar análises por requisito ou período para localizar resultados.

**Why this priority**: Refina a consulta P1; RF011 permanece integralmente no TCC.

**Rastreabilidade**: RF011, RF010; RN004; UC003.

**Independent Test**: Conferir conjuntos retornados com análises em requisitos e datas diferentes.

**Acceptance Scenarios**:

1. **Given** análises de dois requisitos, **When** filtra por um requisito acessível, **Then** aparecem apenas análises vinculadas a ele.
2. **Given** período válido, **When** filtra, **Then** interpreta as datas em America/Sao_Paulo e inclui integralmente as datas inicial e final, comparando corretamente com instantes armazenados em UTC.
3. **Given** filtro sem correspondências, **When** consulta, **Then** vê “Nenhuma análise encontrada”.
4. **Given** período invertido ou data inválida, **When** consulta, **Then** rejeita e informa filtro inválido, sem apresentar consulta irrestrita como filtrada (Q8 aprovado).
5. **Given** filtros opcionais por requisito e período, **When** combina ambos, usa apenas um ou não informa nenhum, **Then** aplica a interseção dos filtros informados, sempre restrita ao usuário autenticado e ordenada da mais recente para a mais antiga.

### User Story 12 - Consultar ajuda sobre escrita (Priority: P2)

Como Analista, quero exemplos de requisitos bem e mal escritos para compreender os critérios avaliados.

**Why this priority**: Apoia aprendizado e permanece no TCC sem bloquear o fluxo central.

**Rastreabilidade**: Sem RF/RN/UC dedicado; usabilidade “Apreensibilidade”; apoio a RF006–RF008 e UC002/UC004.

**Independent Test**: Consultar exemplos na central de ajuda sem executar análise.

**Acceptance Scenarios**:

1. **Given** central disponível, **When** consulta critérios, **Then** encontra exemplos de requisitos bem e mal escritos e explicações compreensíveis dos problemas de escrita.
2. **Given** consulta à ajuda, **When** lê um exemplo, **Then** não altera requisito nem dispara chamada de IA apenas por consultá-lo.

### Edge Cases

| Caso | Comportamento exigido ou pendência |
| --- | --- |
| Campos vazios, só espaços ou acima dos limites / prioridade ausente | Recusar no cliente e servidor conforme US1–US5; sem prioridade automática. |
| Sem apontamentos x sem análise x falha | Resultado válido vazio fica Analisado; ausência é informada; falha não conta como conclusão. Revisado exige confirmação explícita. |
| Resposta recebida sem gravação | Persistência atômica; preservar temporariamente quando possível; nova tentativa de gravação sem inferência; sem sucesso falso. |
| Edição durante análise / sugestão de versão antiga | Preservar título/descrição analisados e vínculo à versão; impedir revisão da nova versão com resultado antigo. |
| Várias análises da mesma versão | R7: somente a válida, concluída e persistida mais recentemente para a versão atual é ativa. Falha/invalidez/ausência de persistência mantém a ativa anterior. Substituídas permanecem históricas e suas sugestões não podem ser aplicadas, mesmo sem mudança textual. |
| Nova análise após Revisado / nova análise sem apontamentos | Nova análise válida concluída e persistida da versão atual torna-se ativa e retorna a Analisado, inclusive com zero apontamentos; nova revisão exige confirmação explícita, sem herdar decisões anteriores. |
| Sugestões sobrepostas / aplicação que muda o texto | A primeira alteração textual invalida aplicação das demais à nova versão, sem descarte automático. |
| Sem proposta / explicação acima de 500 / mais de cinco problemas | Proposta nula é válida e tem mensagem própria; excesso de explicação ou quantidade é rejeitado, sem truncamento. |
| Alteração/exclusão de análise por acesso direto | Negar, conforme RN004, inclusive fora da interface visual. |
| Duplo acionamento / ocupado / tempo limite com execução ativa | Deduplicação por identificação de operação; uma análise por vez, sem fila; não liberar controle enquanto a execução local persistir. |
| Contexto insuficiente / confirmação cancelada | Não inferir, cortar texto, criar análise ou modificar requisito; informar a limitação quando aplicável. |
| Arquivamento com ou sem histórico | Sempre lógico; consulta separada; edição, análise, revisão e restauração bloqueadas no servidor. |

## Requirements *(mandatory)*

### Functional Requirements

FR são identificadores locais preservados; RF/RN/UC são os acadêmicos. Q1–Q10 e refinamentos aprovados estão no registro ao final; R7 encerra PEND-001 e define a análise ativa sem criar nova regra de negócio acadêmica.

| ID | Comportamento verificável | Origem | Aceite |
| --- | --- | --- | --- |
| FR-001 | Cadastrar/autenticar com política Q4 e Identity; isolar dados próprios também por autorização no servidor. | Segurança de acesso; arquitetura; UC001; Q3/Q4/R6 | US1.1–US1.4; US3.4 |
| FR-002 | Cadastrar título/descrição válidos e prioridade obrigatória explícita, responsável, data e Rascunho. | RF001/RF004; UC001; Q1/Q4 | US2.1–US2.3 |
| FR-003 | Listar/consultar somente requisitos próprios, omitindo arquivados da lista principal. | RF015; UC001/UC002; Q3/Q7 | US3.1–US3.4; US10.3 |
| FR-004 | Editar título/descrição/prioridade válidos; alteração textual retorna a Rascunho e preserva histórico. | RF002/RF004/RF009; RN004; UC001; Q2/R1 | US4.1–US4.5 |
| FR-005 | Analisar localmente com confirmação, contexto suficiente, validação JSON, persistência atômica e controle de operação; somente nova análise válida concluída e persistida da versão atual substitui a ativa. | RF005/RF006/RF009; RN001/RN004; UC002; Q3/Q5/Q9/R6/R7 | US5.1–US5.12; contrato local |
| FR-006 | Mostrar problemas com explicações obrigatórias, claras e compreensíveis de até 500 caracteres, exemplos opcionais dentro da explicação e propostas opcionais, mensagem sem proposta e limitação de cobertura. | RF006–RF008; Inteligibilidade; UC002; Q5; decisão final US6.1 | US6.1–US6.6 |
| FR-007 | Armazenar cada resultado com requisito, data, texto analisado e sugestões; reabrir sem nova chamada. | RF009; RN004; UC002/UC003; seção 1.6 | US4.1; US6.1–US6.4; US7.1 |
| FR-008 | Preservar análise/conteúdo gerado imutáveis; registrar uma única decisão definitiva e TextoAplicado; substituição da ativa preserva resultados e decisões anteriores sem transferi-los. | RN004; seção 1.6; refinamento Q6/R2/R7 | US7.3/US7.5; US8.1–US8.9 |
| FR-009 | Consultar histórico próprio mesmo vazio, ordenado por data decrescente e exibido no fuso aprovado, preservando análises substituídas e suas decisões. | RF009/RF010; RN004; UC003; Q8/R7 | US7.1–US7.5 |
| FR-010 | Aplicar/ajustar/descartar com confirmação somente na análise ativa da versão atual e transação de descrição/TextoAplicado/decisão/status; impedir aplicação de sugestões de análises substituídas mesmo da mesma versão textual. | RF012/RF013; RN002/RN004; UC004; Q2/Q6/R1/R2/R7 | US8.1–US8.9 |
| FR-011 | Nova análise válida concluída e persistida da versão atual torna-se ativa e leva a Analisado, inclusive após Revisado ou sem apontamentos; nova conclusão exige decisões somente da ativa e confirmação explícita. Alteração textual exige nova análise. | RF014; RN003/RN004; UC002/UC004; Q2/R1/R7 | US5.11–US5.12; US9.1–US9.8 |
| FR-012 | Sempre arquivar logicamente; consulta separada e bloqueio de edição/análise/revisão/restauração. | RF003; RN004; UC001; Constituição IV; Q7 | US10.1–US10.4 |
| FR-013 | Filtrar opcionalmente por requisito/período combináveis, com datas integrais no fuso aprovado e rejeição de intervalo inválido. | RF011; UC003; Q8 | US11.1–US11.5 |
| FR-014 | Disponibilizar ajuda com exemplos bem e mal escritos. | Apreensibilidade | US12.1–US12.2 |
| FR-015 | Não alterar texto por geração/exibição de análise nem anunciar sucesso de operação que falhou. | RN002; UC001/UC002/UC004; Constituição IV/VII | US2.3, US4.3, US5.4, US6.5, US8.4–US8.5 |

### Rastreabilidade acadêmica consolidada

| Requisito | Resumo fiel | História / prioridade | RN e UC aplicáveis |
| --- | --- | --- | --- |
| RF001 | Cadastro de requisitos | US2 P0 | RN001; UC001 |
| RF002 | Edição de requisitos | US4 P0; US8 P1 | RN002/RN004 quando há sugestão/histórico; UC001/UC004 |
| RF003 | Exclusão de requisitos | US10 P2 | RN004; UC001 |
| RF004 | Validação de campos obrigatórios | US2/US4/US5 P0 | RN001; UC001/UC002 |
| RF005 | Submissão à análise automática | US5 P0 | RN001; UC002 |
| RF006 | Possíveis problemas de escrita | US5/US6 P0 | RN004 após geração; UC002 |
| RF007 | Explicações dos problemas | US6 P0 | RN004; UC002 |
| RF008 | Sugestões de melhoria | US6 P0 | RN002/RN004; UC002/UC004 |
| RF009 | Resultado vinculado ao requisito | US5/US6 P0; US7 P1 | RN004; UC002/UC003 |
| RF010 | Histórico de análises | US7 P1 | RN004; UC003 |
| RF011 | Filtros por requisito/período | US11 P2 | RN004; UC003 |
| RF012 | Aplicar, ajustar ou descartar | US8 P1 | RN002/RN004; UC004 |
| RF013 | Registrar decisão por sugestão | US8/US9 P1 | RN002/RN003/RN004; UC004 |
| RF014 | Atualizar status de revisão | US9 P1 | RN003; UC004 |
| RF015 | Lista de requisitos | US3 P0 | Sem RN específica; UC001 e seleção de UC002 |

RN001: US2/US4/US5. RN002: US5/US6/US8. RN003: US9. RN004: US4–US11 conforme a operação histórica. UC001: US2/US3/US4/US10. UC002: US5/US6. UC003: US7/US11. UC004: US8/US9. US1/US12 vêm de requisitos sem RF próprio; não foram inventados códigos acadêmicos.

### Segurança, integridade e controle de custos

| ID | Restrição obrigatória | Verificação de aceite prevista |
| --- | --- | --- |
| SEC-001 | ASP.NET Core Identity; senhas com hash apropriado e salt, nunca texto puro/reversível; política Q4 no cliente e servidor. Segredos fora do código, Git, logs e respostas. | Inspeção futura de armazenamento e cenários US1, incluindo senha insuficiente, e-mail inválido/duplicado e campos só com espaços. Constituição V; Q4. |
| SEC-002 | Autenticação e autorização no servidor: cada usuário acessa somente seus requisitos, análises e sugestões; sem compartilhamento no MVP; comunicação Web por HTTPS. | Dois analistas não consultam/modificam registros alheios, inclusive por identificadores diretos e filtros. US1.4/US3.4/US7/US11; Q3/R6. |
| SEC-003 | Confirmar antes de cada análise que título, descrição e candidatos derivados serão processados localmente pelo Ollama; não enviar nome, e-mail, senha ou outros dados da conta. Minimizar dados sensíveis em logs. | Cancelar resulta em zero inferências/análises/alterações; conteúdo enviado não contém dados da conta. Configuração inicial restrita ao Ollama local; provedor externo depende de nova aprovação. Constituição V; Q3/R6. |
| SEC-004 | Q9 aprova modelo local e limites por operação, com custo de API zero e sem saldo de provedor pago nesta configuração. Integração externa futura, ampliação dos limites e chamadas pagas dependem de nova aprovação; preservam-se controles constitucionais de orçamento/consumo e saldo para eventual serviço pago. | Conferir limites locais antes da inferência e ausência de fallback externo. Não exigir saldo de serviço pago inexistente para a análise local; nada nesta etapa autoriza chamadas reais. Constituição IX; US5; Q9/R6. |
| SEC-005 | Identificar operação e impedir duplicação na interface/servidor; uma análise por vez, sem fila e sem liberar controle com execução local ativa. Registrar modelo, tokens consumidos e estimativa de custo de API zero por chamada autorizada, sem dados sensíveis desnecessários. | Repetição não duplica inferência/persistência; timeout não permite sobreposição; tentativa de regravação reutiliza resultado sem inferência. Constituição IX; US5.4–US5.9; Q5/Q9. |
| SEC-006 | Histórico/vínculos preservados, inclusive em acesso direto. Conteúdo da IA é imutável; decisão definitiva e TextoAplicado são registrados na confirmação humana, conforme refinamento aprovado Q6/R2. | Comparar histórico antes/depois de edição, revisão e arquivamento: somente registros humanos permitidos são preenchidos, sem reescrever origem da IA ou criar vínculos órfãos. RN004; Constituição IV; US4/US7/US8/US10. |
| SEC-007 | IA é apoio; geração/exibição não altera requisito. Confirmação explícita deve corresponder à alteração proposta. | Abrir, fechar ou não confirmar mantém texto; aplicação confirmada corresponde à decisão. RN002; Constituição VII; US6.5/US8. |
| SEC-008 | Testes de falhas/consumo usam respostas simuladas; evidência da integração real é separada e autorizada. Responsabilidade de IA deve ser isolada e testável, sem desenho nesta etapa. | Relatório futuro distingue simulações de chamada real, resultados, falhas, não executados e pendências; não declara simulação como integração real. Constituição VI/VII/IX. |

### Integração de IA local e contrato aprovado

Q9 substitui a integração externa paga por serviço local com custo zero de API. Trata-se de restrição aprovada para o futuro MVP, sem implementação ou inferência nesta atualização.

| Configuração inicial aprovada | Valor |
| --- | --- |
| Serviço / endereço | Ollama / `http://127.0.0.1:11434` |
| Modelo | `qwen2.5:3b` |
| Contexto total / saída máxima | 4.096 tokens / 1.024 tokens |
| Tempo limite | 60 segundos |
| Concorrência / fila | Uma análise por vez / sem fila automática |
| Permanência em memória | Cinco minutos após a análise |
| Quantidade de apontamentos | De zero a cinco por análise |
| Configuração | Externa à lógica do código, com configuração padrão do ASP.NET Core e variáveis de ambiente |

Fluxo híbrido aprovado:

1. Regras textuais simples identificam candidatos no título e na descrição.
2. O requisito completo e os candidatos são enviados ao modelo local em uma única chamada, após confirmação e verificação de contexto.
3. A IA avalia o contexto, podendo confirmar, rejeitar ou acrescentar apontamentos.
4. O servidor valida a resposta antes de persistir atomicamente análise e sugestões.
5. O analista revisa os resultados e toma a decisão final; a IA não altera o requisito por conta própria.

| Grupo de indicadores iniciais | Expressões candidatas |
| --- | --- |
| Desempenho | rapidamente, imediatamente, sem demora |
| Quantidade | vários, muitos, poucos, diversos |
| Qualidade e facilidade | fácil, intuitivo, eficiente, adequado |
| Permissões | usuários autorizados, pessoas responsáveis |
| Condições | quando necessário, se possível, conforme necessário |

Essas expressões são apenas candidatas, nunca defeitos automaticamente confirmados. Sua ausência não dispensa análise pela IA. A interface informa que a análise pode não ser exaustiva e exige revisão humana.

**Contrato funcional Q5, com esclarecimento final US6.1**: resultado em JSON estruturado contendo uma lista de problemas, inclusive lista vazia válida. Cada apontamento exige `tipoProblema` em `Ambiguidade`, `Termo vago`, `Falta de clareza` ou `Falta de especificidade`, e `explicacao` obrigatória, clara e compreensível, de até 500 caracteres. `textoSugerido` é opcional, podendo ser nulo; sua ausência é exibida como `Sugestão não disponível`. Exemplos podem integrar a explicação quando úteis, dentro do mesmo limite de 500 caracteres, mas não são obrigatórios; sua ausência não invalida explicação válida e suficiente. Não há nova propriedade obrigatória para exemplos. Os demais detalhes do contrato e a estratégia concreta de validação ficam para o planejamento autorizado.

**Aceite do contrato**: respostas inválidas, incompletas, truncadas, com categoria não permitida, explicação ausente/acima do limite ou mais de cinco apontamentos são rejeitadas integralmente. Não corrigir, truncar ou repetir inferências silenciosamente. Uma lista vazia válida deve poder ser persistida sem sugestões e deixar a versão atual Analisado, sem certificação de qualidade.

**Aceite dos limites**: antes da inferência, verificar se título, descrição, instruções, candidatos e espaço reservado para a resposta cabem nos 4.096 tokens configurados; a saída respeita o máximo de 1.024 tokens. Se não couber, informar limitação sem cortar o requisito. A descrição pode ser válida nos 5.000 caracteres e ainda exceder o contexto: validação cadastral e capacidade de análise são verificações distintas. Ollama indisponível gera erro informado; ocupado impede outra análise. Falhas nunca iniciam nova inferência automaticamente. Persistência, identificação de operação e timeout seguem US5.4–US5.10.

**Limitações conhecidas fornecidas pelo responsável**: testes preliminares do Qwen2.5 3B indicaram execução local viável em Intel Core i5-1235U com 8 GB de RAM, respostas JSON válidas nos cenários testados e identificação/explicação de alguns problemas reais. Houve cobertura incompleta mesmo com candidatos fornecidos, sugestões imprecisas/inadequadas e tempos variáveis, inclusive superiores a 30 segundos. São evidências preliminares relatadas, não testes executados nesta etapa nem garantia de precisão, exaustividade, validade de toda resposta ou desempenho em outros ambientes. Não se apresenta o modelo como mecanismo infalível de validação.

### Qualidade e cobertura do TCC

- **Disponibilidade — PEND-002.1 resolvida documentalmente**: referência operacional futura de segunda a sexta-feira, das 8h às 18h, no horário de São Paulo (`America/Sao_Paulo`). Esse período preserva e especifica o requisito acadêmico de horário comercial; não é regra de autorização nem restrição de acesso. O MVP local pode ser iniciado e utilizado em qualquer horário, inclusive fora desse período, em finais de semana e em aulas das 19h às 22h50, dependendo da disponibilidade do computador e dos serviços necessários. Não haverá bloqueio de autenticação, cadastro, edição, consulta ou análise baseado em data/horário, garantia de disponibilidade contínua ou percentual mínimo não aprovado.
- **Independência da IA — PEND-002.1**: Ollama indisponível impede novas análises por IA e gera o tratamento de erro previsto em US5; não bloqueia funcionalidades independentes da IA. Com aplicação Web e SQL Server funcionando, cadastro, edição, consulta de requisitos e consulta ao histórico continuam disponíveis, observadas as demais regras já aprovadas de autenticação, autorização, validação e arquivamento.
- **Recuperabilidade — PEND-002.2 resolvida documentalmente**: backup completo diário do banco SQL Server, conservando as cópias dos últimos sete dias. As cópias devem preservar os dados necessários à recuperação de usuários, requisitos, análises, sugestões, decisões e seus relacionamentos; devem ficar em local protegido, separado do diretório do banco ativo e com acesso restrito. Disponibilizar procedimento de restauração em banco separado para validar a recuperação e verificar integridade de registros e vínculos, sem sobrescrever automaticamente o banco principal durante os testes. Alterações desde a última cópia válida podem ser perdidas; não se promete perda zero. Política aprovada não significa automação ou restauração já executadas; implementação e validação ficam para etapa futura conforme a prioridade do projeto, sem scripts, ferramentas de agendamento ou infraestrutura física definidos aqui.
- **Inteligibilidade**: explicações obrigatórias claras e compreensíveis, com exemplos opcionais em US6, conforme refinamento final aprovado do texto acadêmico que os exigia; **Apreensibilidade**: exemplos da ajuda em US12 permanecem exigidos. “Operacionalidade” está explicitamente sem requisito no original.
- **Plataforma**: navegador em desktop/notebook; verificar fluxos aprovados em Chrome, Firefox e Edge, registrando versões futuramente.
- **Demonstração e implantação — PEND-003 resolvida documentalmente**: a apresentação de 22/10/2026 será demonstração local do MVP em desenvolvimento no ambiente do projeto com ASP.NET Core, SQL Server e Ollama com o modelo local aprovado `qwen2.5:3b`, para apresentar funcionalidades e validar o andamento. Não constitui implantação formal. Preserva-se a implantação acadêmica final única em ambiente de testes, a ocorrer posteriormente quando a versão estiver preparada e validada conforme o escopo acadêmico. Não se acrescentam implantações formais intermediárias nem se afirma implantação ou homologação já realizada.
- **Requisitos inversos originais**: o produto não verifica rastreabilidade com código/testes e não oferece aplicativo móvel nativo nesta etapa. A rastreabilidade deste artefato é documental, sem criar funcionalidade de produto.
- Q10 considera essenciais à validação futura: fluxo funcional completo, entradas, autorização/isolamento, integridade transacional, preservação de análises/sugestões, indisponibilidade/respostas inválidas da IA, concorrência e tempo limite. Combinar testes automatizados adequados e validação manual com evidências, falhas e pendências. Nenhum teste de aplicação foi executado nesta etapa.

**Critérios de aceitação operacional e documental — para verificação futura, sem execução nesta etapa**:

| Origem / cenário | Resultado exigido |
| --- | --- |
| PEND-002.1: computador, aplicação Web e serviços necessários disponíveis antes das 8h, após as 18h ou no fim de semana | Nenhuma funcionalidade é bloqueada exclusivamente pela data ou horário; o horário de referência é operacional, não autorização de acesso. |
| PEND-002.1: aplicação Web e SQL Server funcionando, Ollama indisponível | Nova análise por IA informa indisponibilidade; cadastro, edição, consulta de requisitos e consulta ao histórico continuam acessíveis conforme as demais regras aprovadas. |
| PEND-002.2: inspeção das cópias e de seus registros de realização | É verificável a existência de backups completos diários, com cópias dos últimos sete dias em local protegido, separado do diretório do banco ativo e com acesso restrito. |
| PEND-002.2: restauração de uma cópia válida | O procedimento restaura em banco separado e verifica usuários, requisitos, análises, sugestões, decisões e seus vínculos, sem modificar o banco principal durante os testes de recuperação. |
| PEND-003: revisão da descrição da apresentação e da política de implantação | Demonstração local de 22/10/2026 distingue-se da implantação formal final única em testes; esta permanece condicionada à preparação/validação, sem alegação de implantação ou homologação realizada. |

### Key Entities *(include if feature involves data)*

- **Analista / Usuário**: nome obrigatório até 100 caracteres, e-mail obrigatório válido/único até 150 e credencial protegida pelo ASP.NET Core Identity, conforme Q4. Só acessa seus registros (Q3/R6). A compatibilização técnica com a tabela Usuario acadêmica será planejada futuramente, sem criar esquema nesta etapa.
- **Requisito**: identificação, responsável, título obrigatório até 150, descrição obrigatória até 5.000, prioridade obrigatória/editável Alta/Média/Baixa sem default, status automático e datas de cadastro/atualização. Estados: Rascunho, Analisado, Revisado e Arquivado. Prioridade no modelo lógico é refinamento aprovado Q1 de campo já citado por UC001.
- **Análise**: identificação, requisito, instante em UTC e preservação exata de título/descrição efetivamente analisados, com associação verificável à versão textual correspondente. Conteúdo histórico imutável. Q2/R1 aprovam o vínculo funcional; a estratégia concreta de versionamento fica para planejamento. R7 define como ativa somente a análise válida, concluída e persistida mais recentemente para a versão atual. Falha, invalidez ou persistência malsucedida de nova análise não substituem a ativa anterior. Nenhuma análise de versão antiga ou substituída governa a revisão; resultados e decisões históricos não são transferidos para a nova ativa.
- **Sugestão**: análise associada, tipo de problema no domínio aprovado, explicação obrigatória até 500, TextoSugerido opcional e imutável, Decisao inicialmente Pendente e definitiva após confirmação, e **TextoAplicado nullable**. TextoAplicado guarda redação efetivamente adotada em Aplicada/Ajustada e fica nulo em Pendente/Descartada. Q6/R2 refinam o modelo acadêmico: o registro humano complementar não modifica explicação nem proposta da IA.
- **Autorização e evidência de consumo**: registro de governança sobre aprovador, escopo, modelo, limites e consumo; custo de API local zero. Identificação de operação é necessária à deduplicação Q5. Não se define nova tabela nem esquema físico nesta etapa.

**Reflexos futuros na monografia**: atualizar, após autorização, UC001 (prioridade obrigatória/status automático), UC003 (entrada sem análises/filtros), UC004 e RN003 (versão atual/confirmação explícita/nova análise após mudança), interpretação refinada da RN004 (decisão definitiva e TextoAplicado complementar), modelos conceitual/lógico/físico (prioridade, TextoAplicado e vínculo análise–versão), limites de descrição, credenciais com Identity, UTC/fuso, exclusão sempre lógica e arquitetura híbrida Ollama com limitações conhecidas. O DOCX original e a Constitution permanecem inalterados; este registro distingue os refinamentos aprovados da fonte acadêmica.

**Complemento R7 para a monografia**: refletir em UC002/UC004, RN003/RN004 e na descrição da associação análise–requisito a definição funcional de análise ativa, sua substituição somente após sucesso da nova análise da versão atual, retorno de Revisado a Analisado e preservação de resultados/decisões anteriores. Não se define campo físico ou estratégia técnica nesta etapa.

## Success Criteria *(mandatory)*

### Measurable Outcomes

Critérios para validação futura, não resultados obtidos. As prioridades existentes são preservadas; Q10 aprova a demonstração integrada P0/P1. Nenhuma meta exige precisão perfeita ou detecção exaustiva da IA.

- **SC-001 — Fluxo completo P0/P1**: demonstrar autenticação; cadastro de título, descrição e prioridade; análise local ao vivo com confirmação; visualização de problemas, explicações e sugestões; aplicação, ajuste ou descarte confirmados; consulta ao histórico. Localização, edição e reabertura persistida de US3/US4/US6 continuam verificáveis. Evidências identificam requisito, versão textual e análise; nenhum passo anuncia sucesso sem persistência.
- **SC-002 — Validações e isolamento**: todos os cenários de entradas inválidas de US1/US2/US4/US5 são recusados, inclusive espaços isolados, limites e prioridade ausente. Com dois usuários, nenhuma consulta ou mutação de registros alheios é permitida, inclusive por identificador direto. Estados vazios e registros inexistentes têm as mensagens previstas, sem conteúdo fictício.
- **SC-003 — Compreensão e limites**: para cada apontamento demonstrado, o analista identifica tipo obrigatório, explicação obrigatória, clara e compreensível de até 500 caracteres e proposta quando existente; exemplos são opcionais dentro da explicação. Apontamento com explicação válida e suficiente é aceito sem exemplo e sem propriedade adicional obrigatória para esse fim. Sem proposta, vê “Sugestão não disponível”. Distingue texto analisado, gerado e aplicado e vê o aviso de cobertura não exaustiva. Discordâncias humanas são evidência válida, sem exigir concordância total com a IA.
- **SC-004 — Integridade e versões**: comparar histórico antes/depois de edição, abertura sem confirmação e revisão; nenhum conteúdo gerado é reescrito. Cada análise corresponde verificavelmente ao título/descrição enviados. Alteração textual retorna a Rascunho e bloqueia aplicação de sugestões antigas, sem descartá-las automaticamente. Injetar falha em gravação de análise/sugestões e de descrição/TextoAplicado/decisão/status confirma ausência de mudanças parciais; regravar resultado preservado não executa outra inferência.
- **SC-005 — Falhas e controle local**: simular indisponibilidade, JSON inválido/incompleto/truncado, categoria indevida, explicação ausente/acima de 500, mais de cinco problemas, contexto insuficiente e timeout. Nenhum caso produz conclusão falsa, truncamento silencioso ou repetição automática. Cancelamento gera zero inferências e alterações. Duplo acionamento por interface/servidor não duplica operação; execução ainda ativa impede outra análise mesmo após timeout, sem fila. Registrar modelo, tokens consumidos e custo de API zero nas futuras chamadas reais autorizadas, sem logs sensíveis desnecessários.
- **SC-006 — Revisão e histórico P1**: consultar ao menos duas análises preservadas; verificar Aplicada, Ajustada e Descartada em casos preparados independentes, já que alteração textual impede aplicar outra proposta da versão anterior. TextoAplicado corresponde à redação confirmada e fica nulo no descarte. Revisado exige somente a análise ativa da versão atual, todas as suas decisões e confirmação explícita, também com zero apontamentos ou todos descartados; decisão confirmada não é reaberta. Verificar R7 com duas análises da mesma versão: somente a nova válida, concluída e persistida substitui a ativa; falha/invalidez/ausência de persistência preserva ativa e status anteriores. A nova ativa retorna Revisado a Analisado, começa com sugestões Pendentes sem herdar decisões e bloqueia aplicação de sugestões da análise substituída mesmo sem mudança textual. Conferir preservação integral dos resultados e decisões históricos (US5.11–US5.12, US7.5, US8.9 e US9.1–US9.8).
- **SC-007 — P2/TCC**: arquivar requisitos com e sem análises, consultar seção de arquivados e conferir bloqueios no servidor; testar filtros opcionais/combinados, datas inicial/final integralmente incluídas no fuso America/Sao_Paulo, ordenação decrescente, estado vazio e intervalos inválidos; acessar ajuda com exemplos. P2 permanece no escopo acadêmico, sem virar condição obrigatória da demonstração P0/P1.

**Demonstração e contingência Q10**: a evidência principal será análise local ao vivo. Se houver impedimento, podem ser consultadas análises reais previamente armazenadas, explicitamente identificadas como resultados históricos; nunca apresentá-las como execução ao vivo. Respostas simuladas servem aos testes controlados de falha, não à comprovação da integração real nem como histórico real. Registrar quais passos foram efetivamente demonstrados e quais ficaram pendentes.

A demonstração de 22/10/2026 é local e não constitui implantação formal, conforme PEND-003. Os critérios operacionais de disponibilidade e recuperação estão em “Qualidade e cobertura do TCC”; sua definição documental não comprova disponibilidade, geração de backups ou restauração executadas.

Não se promete detectar todos os problemas, obter respostas idênticas entre análises ou comprovar correção pela ausência de apontamentos. Os quatro tipos são capacidades a avaliar com amostras e revisão humana. Composição da amostra e qualquer meta adicional de acerto exigem aprovação, sem inferência neste documento.

## Assumptions

- O ator funcional é o Analista. QA e gestão são áreas interessadas, sem novos perfis de acesso presumidos.
- A fonte acadêmica é preservada; Q1–Q10 e os refinamentos expressamente fornecidos pelo responsável prevalecem nos pontos aprovados e estão rastreados abaixo.
- Serviço, modelo e limites locais estão aprovados como especificação. A execução de inferências, testes da aplicação e demais etapas continua fora da autorização desta tarefa.
- P0/P1/P2 são prioridades de histórias, distintas da prioridade dos RF acadêmicos e da prioridade Alta/Média/Baixa escolhida no cadastro. Q10 inclui revisão e histórico P1 na demonstração, sem excluir P2 do TCC.
- Esta etapa altera exclusivamente spec.md e checklists/requirements.md. Não produz apontamento da feature ativa, plano, tarefas, código, esquema de banco, migrations, chamadas de IA, novos arquivos ou commits.

### Questões pendentes de aprovação e decisões incorporadas

As antigas alternativas de Q1–Q10 foram substituídas pelos resultados aprovados do prompt consolidado de 2026-10-18. Não se atribui ao DOCX o conteúdo desses refinamentos. A aprovação recebida autoriza esta atualização documental, sem transição automática de etapa.

#### Q1 — Campos e autoridade sobre status — Aprovada

Título obrigatório até 150, descrição obrigatória até 5.000, prioridade obrigatória Alta/Média/Baixa sem seleção automática e editável; status automático Rascunho/Analisado/Revisado/Arquivado. Rejeitar campos obrigatórios vazios ou só com espaços. Refina UC001 e o modelo sem prioridade, preservando RF001/RF002/RF004 e RN001. Aceite: US2/US4 e FR-002/FR-004.

#### Q2 — Análise que governa a revisão e conflitos — Aprovada nos limites fornecidos; R1 incorporado

Somente análise vinculada à versão atual do título/descrição orienta revisão. Mudança textual retorna a Rascunho e exige nova análise antes de nova conclusão, inclusive aplicação/ajuste. Histórico e sugestões antigas permanecem preservados; aplicação de sugestões de versões antigas é proibida. Demais sugestões de análise anterior permanecem visíveis, sem descarte automático. Resultado válido vazio fica Analisado; Revisado exige confirmação explícita, inclusive com todas descartadas. A escolha entre múltiplas análises da mesma versão foi resolvida pelo complemento R7 abaixo, encerrando PEND-001. Refina RN003/RN004 e UC002/UC004; aceite US4–US9.

#### Q3 — Acesso e conteúdo enviado à IA — Aprovada; R6 incorporado

Somente dados próprios, com autorização no servidor inclusive por identificador; sem compartilhamento. Antes de cada análise, confirmação explícita informando processamento local pelo Ollama de título, descrição e candidatos derivados. Não enviar dados da conta; cancelar não cria análise nem altera requisito. Configuração inicial exclusivamente local; integração externa futura requer nova aprovação. Minimizar dados sensíveis nos logs. Origem: segurança/conformidade, Constituição V; aceite US3.4/US5.6 e SEC-002/SEC-003.

#### Demais lacunas e inconsistências — Decisões aprovadas

| ID / origem | Decisão aprovada e refinamento | Aceite / impacto |
| --- | --- | --- |
| Q4 — RF004/RN001; Usuario/Requisito | Nome até 100; e-mail válido único até 150; título até 150 e descrição até 5.000, obrigatórios; cliente e servidor validam. Identity e senha de pelo menos oito caracteres com maiúscula/minúscula/número/especial; nunca texto puro. | US1/US2/US4/US5; adequação futura da identidade ao modelo acadêmico, sem código agora. |
| Q5 — RF005–RF009; UC002 | JSON estruturado, quatro categorias exatas, explicação obrigatória até 500, proposta opcional/nula e lista vazia válida. Resposta incompatível é rejeitada sem correção/truncamento/repetição automática. Conclusão só após persistência atômica; recuperação temporária quando possível e regravação sem inferência. Identificação de operação e deduplicação em interface/servidor; execução ativa mantém bloqueio. | US5/US6; contrato local; SC-004/SC-005. |
| Q6 — RF012/RF013; RN002/RN004; R2 | Pendente inicial; Aplicada/Ajustada/Descartada somente por confirmação explícita, definitiva. TextoSugerido e explicação imutáveis. TextoAplicado nullable registra redação adotada em Aplicada/Ajustada e fica nulo no descarte. Descrição, TextoAplicado, decisão e status na mesma transação. Marcadores não aprovam valores de negócio. | US8/US9; FR-008/FR-010; refinamento explícito da restrição acadêmica a alterar apenas Decisao; DOCX não modificado. |
| Q7 — RF003; UC001; RN004 | Exclusão sempre lógica, com ou sem análises; Arquivado sai da lista principal e tem consulta própria. Bloquear edição, análise, revisão e restauração também no servidor; nenhuma exclusão física na aplicação. | US10; compatível com Constituição IV e refinamento da formulação acadêmica condicional. |
| Q8 — RF010/RF011; UC003 | Acesso ao histórico mesmo vazio, mensagem própria; filtros opcionais/combináveis por requisito e período, datas integrais inclusivas, rejeição de inválidos/invertidos, ordem decrescente e isolamento. Armazenar instantes em UTC e exibir/interpretar em America/Sao_Paulo. | US7/US11; refina pré-condição contraditória de UC003. |
| Q9 — RF005/RF006; arquitetura; Constituição V/IX | Arquitetura híbrida local Ollama com parâmetros e indicadores aprovados, uma chamada por análise, contexto verificado antes, sem cortes, fila ou novas inferências automáticas. Limitações preliminares registradas sem promessa de precisão perfeita. | US5; contrato local; custo zero de API. As lacunas operacionais antigas foram resolvidas documentalmente pelas decisões finais PEND-002.1/PEND-002.2, sem alterar Q9. |
| Q10 — demonstração; Constituição II/III/VI/VII | Demonstração completa P0/P1 com análise ao vivo; contingência por histórico real identificado. Essenciais: fluxo, validações, autorização, transações, preservação, falhas, concorrência e timeout. P0/P1/P2 preservados. | SC-001–SC-007; não autoriza implantação adicional, planejamento ou implementação. |

**Referências R**: o texto recebido nomeia expressamente R1 em análise/versões, R2 em TextoAplicado e R6 em privacidade. As demais regras complementares foram incorporadas conforme os itens temáticos do prompt, sem inventar correspondência individual R3/R4/R5. A identificação desses rótulos pode ser completada documentalmente pelo responsável, sem presumir decisões adicionais.

#### R7 — Análise ativa da versão atual — Aprovada

Complemento funcional aprovado pelo responsável nesta atualização, resolvendo integralmente PEND-001:

1. Para cada requisito, somente a análise válida, concluída e persistida mais recentemente, correspondente à versão atual do título e da descrição, pode orientar sua revisão: essa é a análise ativa.
2. Uma nova análise dessa versão passa a ser ativa somente quando concluída e armazenada com sucesso.
3. Análises anteriores permanecem preservadas e acessíveis no histórico, com seus resultados e decisões originais.
4. Sugestões de análises anteriores não podem mais ser aplicadas ao requisito atual, mesmo quando pertencem à mesma versão textual.
5. Se a nova análise falhar, for inválida ou não for armazenada com sucesso, a análise ativa anterior permanece inalterada.
6. Nova análise válida concluída e persistida da versão atual após Revisado retorna o status para Analisado.
7. Sugestões da nova ativa começam Pendente, quando houver; decisões anteriores não são transferidas.
8. Nova conclusão de revisão utiliza somente as sugestões pertinentes à análise ativa e exige confirmação explícita do analista, preservando as regras aprovadas Q2/Q6/R1 e de confirmação humana referidas como R3 no complemento recebido. Alteração textual continua retornando a Rascunho e exigindo nova análise.
9. Uma análise válida com zero apontamentos também pode tornar-se ativa, mas não promove automaticamente o requisito para Revisado.
10. A substituição não altera nem exclui o conteúdo das análises anteriores, conforme RN004.

**Rastreabilidade R7**: RF005/RF009 (conclusão e persistência), RF010 (histórico), RF012/RF013 (sugestões e decisões), RF014 (status); RN002/RN003/RN004; UC002/UC003/UC004. Aceite: US5.11–US5.12, US7.5, US8.1–US8.3/US8.9, US9.1–US9.8 e SC-006. Preservados RF001–RF015, RN001–RN004, UC001–UC004 e prioridades existentes; não se cria novo identificador de regra de negócio, campo físico ou funcionalidade fora de R7.

#### Consolidação final — US6.1 e PEND-002–PEND-004 — Aprovada

O responsável aprovou nesta consolidação: exemplos opcionais nas explicações da IA, sem nova propriedade obrigatória; disponibilidade de referência das 8h às 18h sem bloqueio de acesso fora desse horário e com independência das funções sem IA; backup completo diário com retenção de sete dias e recuperação em banco separado; demonstração local distinta de implantação formal final única. O conteúdo detalhado e seus critérios verificáveis estão em US6.1/US6.6, no contrato Q5, em SC-003 e em “Qualidade e cobertura do TCC”.

**Rastreabilidade**: clareza das explicações refina RF007, RF006–RF008 e UC002, além de “Inteligibilidade”; disponibilidade refina o requisito acadêmico “Disponibilidade” e apoia US1–US7 sem alterar autorização; backup refina “Recuperabilidade”, preservando RN004 e dados das funcionalidades RF001–RF015; demonstração/implantação esclarece “Requisitos Tecnológicos — Implantação” e a arquitetura acadêmica. Não se atribuem novos identificadores RF/RN/UC às decisões operacionais. Os refinamentos de exemplos opcionais, disponibilidade, backup e distinção demonstração/implantação deverão ser refletidos futuramente na monografia após autorização, sem modificar agora o DOCX.

**PEND-004 — Encaminhada ao planejamento técnico, sem resolução técnica nesta etapa**: as regras funcionais necessárias estão definidas; as seguintes questões continuam identificadas para planejamento posterior expressamente autorizado:

1. Estratégia de representação das versões textuais dos requisitos.
2. Identificação e proteção da análise ativa conforme R7.
3. Estrutura das entidades e relacionamentos no SQL Server.
4. Organização dos serviços responsáveis pela integração com o Ollama.
5. Controle de concorrência, deduplicação e cancelamento seguro das inferências.
6. Estratégia concreta de geração, retenção e restauração dos backups.
7. Configuração do ASP.NET Core Identity.
8. Políticas de autorização e isolamento dos dados dos usuários, implementando as regras funcionais já aprovadas.
9. Compatibilização dos refinamentos aprovados com o modelo acadêmico original.

Permanecem igualmente encaminhados os detalhes anteriores do contrato JSON além dos campos aprovados, da preservação temporária de resultados, da identificação de operação e da contagem de tokens/controle efetivo de execução local. Este encaminhamento não seleciona novas soluções de arquitetura, não constitui plano técnico nem autoriza iniciar planejamento ou implementação.

#### Pendências e encerramentos — Situação documental

| ID | Assunto | Tratamento nesta atualização |
| --- | --- | --- |
| PEND-001 — Encerrada por R7; RF013/RF014; RN003; UC004 | Definir qual análise da mesma versão orienta revisão e efeito sobre Revisado. | Resolvida: somente a válida, concluída e persistida mais recentemente para a versão atual é ativa; nova ativa retorna a Analisado, exige nova confirmação humana e preserva histórico. Falha não substitui a ativa. Não há seleção manual nem consolidação de decisões. |
| PEND-002 — Resolvida documentalmente | PEND-002.1: disponibilidade de referência; PEND-002.2: backup e recuperação. | Segunda a sexta, 8h–18h em São Paulo, sem bloqueio de acesso por horário e sem garantia contínua do MVP local; IA indisponível não bloqueia funções independentes. Backup completo diário, retenção dos últimos sete dias, proteção e restauração em banco separado sem modificar o principal. Risco de perda desde a última cópia válida reconhecido; sem percentual mínimo ou promessa de perda zero. Implementação/validação operacional ainda não realizadas. |
| PEND-003 — Resolvida documentalmente | Demonstração local de 22/10/2026 versus implantação acadêmica formal. | Demonstração do MVP em desenvolvimento com ASP.NET Core, SQL Server e Ollama local não constitui implantação formal. Preservada implantação final única em ambiente de testes, posterior à preparação/validação; sem implantação intermediária nem alegação de implantação/homologação realizada. |
| PEND-004 — Encaminhada ao planejamento técnico | Questões técnicas enumeradas acima, mantendo as regras funcionais aprovadas. | Não tecnicamente resolvida; nenhum planejamento executado. Detalhes de implementação, automação de backups e validações ficam para etapa futura expressamente autorizada, conforme prioridade do projeto. |

### Conformidade com a Constituição v1.0.0

| Princípio | Evidência nesta etapa | Pendência/divergência |
| --- | --- | --- |
| I — Fidelidade | Fontes lidas, matriz RF001–RF015/RN001–RN004/UC001–UC004 preservada, refinamentos distinguidos e DOCX inalterado. | PEND-001 encerrada por R7; o registro dos rótulos do prompt consolidado original permanece preservado, com a referência complementar a R3 registrada em R7. |
| II — Simplicidade | Escopo preservado, IA local aprovada e demonstração integrada sem eliminar P2. | Stack constitucional continua C#, ASP.NET Core MVC/.NET 10, EF Core e SQL Server; não se propõe tecnologia divergente. |
| III — Incremental | Somente dois arquivos documentais autorizados são atualizados. | Nenhuma transição autorizada; aguardar aprovação explícita. |
| IV — Integridade | Histórico/vínculos preservados, transações e arquivamento sempre lógico. | Modelo refinado documentalmente; nenhuma alteração de banco. |
| V — Segurança | Identity, validações, isolamento no servidor, confirmação local e minimização de logs. | Não se declara conformidade operacional/LGPD validada; somente critérios documentados. |
| VI — Qualidade | Cenários e critérios verificáveis de fluxo, entradas, autorização, transações, falhas, concorrência, disponibilidade e recuperação; exemplos opcionais explicitados. | Consolidação documental concluída não equivale a implementação, testes ou validação operacional; detalhes técnicos encaminhados em PEND-004. |
| VII — Supervisão | Confirmação explícita, decisão definitiva, origem da IA imutável, TextoAplicado complementar e análise ativa definidos. | Nova análise após mudança textual; nova ativa após Revisado exige nova conclusão humana; nenhuma inferência realizada. |
| VIII — Mudanças | Prompt consolidado registrado como aprovação dos refinamentos funcionais/modelo nesta especificação. | Revisão final e próxima etapa dependem de aprovação; não há emenda implícita da Constituição. |
| IX — Custos | Custo zero de API, modelo/limites locais, deduplicação e registros de consumo previstos. | Ativação real não autorizada nesta tarefa; controles de serviços pagos preservados para eventual integração futura, sujeita a nova aprovação. |

Nenhuma emenda da Constituição foi realizada. As tensões acadêmicas sobre prioridade/status, exclusão, pré-condição de histórico e registro de TextoAplicado estão resolvidas pelos refinamentos expressamente aprovados; R7 resolve a escolha entre análises da mesma versão e o efeito sobre a revisão concluída. A expressão acadêmica “criptografada” não autoriza senha reversível: prevalece hash seguro com Identity. A integração usa API local de modelo preexistente e mantém a tecnologia fixada na Constituição.

### Registro de aprovação

**Decisões recebidas e incorporadas em 2026-10-18**: Q1–Q10, R1/R2/R6 explicitamente nomeados e demais regras complementares do prompt consolidado, conforme registro acima. **Complemento R7 preservado**: definição de análise ativa aprovada pelo responsável, com PEND-001 encerrada. **Consolidação final incorporada**: US6.1 com exemplos opcionais; PEND-002 e PEND-003 resolvidas documentalmente; PEND-004 encaminhada ao planejamento técnico, sem resolução técnica antecipada. Especificação documental concluída no escopo aprovado; **aguardando revisão e aprovação humanas** desta atualização exclusiva de spec.md e checklist. Implementação e testes/validações operacionais ainda não realizados. A autorização é exclusivamente documental: nenhuma funcionalidade implementada, nenhum novo arquivo criado e nenhuma alteração da Constitution, do DOCX, de código ou banco. Sem planejamento, build, testes da aplicação, migrações, instalação, inferência, acesso a API externa, implantação, mudança de branch, commit ou push. Não executar /speckit.plan, /speckit.tasks ou /speckit.implement nem iniciar outra etapa sem aprovação explícita do responsável.
