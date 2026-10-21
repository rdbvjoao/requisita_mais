# Specification Quality Checklist: Requisita+ — MVP de análise e controle de requisitos

**Purpose**: Validar completude e qualidade da especificação antes do planejamento.
**Created**: 2026-10-17
**Reviewed**: 2026-10-18 — consolidação final US6.1/PEND-002–PEND-004; decisões Q1–Q10 e R1–R7 preservadas
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No unapproved implementation details; approved technical constraints are identified
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain from Q1–Q3; residual questions are explicit
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Specification defines verifiable outcomes for the feature; implementation compliance is not asserted
- [x] No unapproved implementation design; approved constraints are distinguished from later design

## Notes

- Revisão documental final em 2026-10-18: **16/16 critérios documentais atendidos; nenhum item desmarcado**, após reavaliação das explicações, critérios operacionais, implantação e encaminhamentos técnicos. Há cenários verificáveis para as decisões finais, sem contradição funcional restante identificada no escopo desta atualização. A marcação não representa implementação, testes, aprovação final ou prontidão operacional plena. O item de resultados da feature explicita que avalia a definição documental de resultados verificáveis, sem afirmar atendimento pela aplicação. As restrições técnicas aprovadas permanecem distinguidas de escolhas de implementação ainda não feitas.
- Foram lidos Constituição, especificação, checklist e DOCX acadêmico. O SHA-256 do DOCX coincide com o registrado na especificação. Fonte acadêmica e Constituição permanecem inalteradas.
- Q1–Q10 foram registradas com as decisões consolidadas; R1 (versões), R2 (TextoAplicado) e R6 (privacidade) têm identificação expressa no prompt. O responsável não individualizou R3–R5; as regras complementares foram incorporadas por tema sem inventar rótulos ou decisões.
- **PEND-001 encerrada por R7**: somente a análise válida, concluída e persistida mais recentemente para a versão atual é ativa. Nova ativa substitui a anterior e retorna Revisado a Analisado; falha, invalidez ou persistência malsucedida preservam a ativa anterior e o status. Resultados e decisões anteriores permanecem históricos, sem transferência nem aplicação de sugestões substituídas, mesmo da mesma versão textual.
- **Requisitos inequívocos**, **todos os cenários definidos** e **critérios claros para todos os requisitos** foram reavaliados documentalmente no alcance funcional aprovado. US5.11–US5.12 cobrem substituição/sucesso e preservação/falha; US6.3 distingue consulta de resultado vazio de conclusão de nova ativa; US7.5 cobre histórico intacto; US8.1–US8.3/US8.9 restringem decisões à ativa e bloqueiam sugestões substituídas; US9.1–US9.8 cobrem status, sugestões Pendentes sem herança de decisões, zero apontamentos e nova conclusão humana. FR-005/FR-008–FR-011, Key Entities, SC-006 e registro de aprovação foram alinhados à R7.
- R7 complementa Q2/Q6/R1 e a confirmação humana referida como R3 no complemento recebido; não modifica as demais decisões aprovadas nem inventa enunciados para os rótulos não individualizados no prompt consolidado original.
- **US6.1 consolidada**: cada apontamento exige tipo permitido e explicação clara/compreensível de até 500 caracteres. Exemplos podem integrar a explicação quando úteis, mas não são obrigatórios; sua ausência não invalida explicação válida e suficiente. US6.6, FR-006, contrato Q5, SC-003 e Inteligibilidade estão alinhados. Preservados `tipoProblema` obrigatório, `explicacao` obrigatória e `textoSugerido` opcional/nulo; nenhuma nova propriedade obrigatória para exemplos. Os exemplos da ajuda em US12 permanecem exigidos.
- **PEND-002 resolvida documentalmente**: referência de disponibilidade de segunda a sexta, 8h–18h em America/Sao_Paulo, sem bloqueio por horário/data, inclusive à noite e em finais de semana. MVP local depende do computador/serviços, sem garantia contínua ou percentual mínimo não aprovado. Ollama indisponível impede novas análises, sem impedir cadastro, edição, consulta de requisitos e histórico quando Web/SQL Server funcionam, observadas as demais regras aprovadas.
- **PEND-002.2 — política aprovada**: backup completo diário com retenção das cópias dos últimos sete dias, protegido e separado do diretório do banco ativo, com acesso restrito. Recuperação abrange usuários, requisitos, análises, sugestões, decisões e vínculos; procedimento e verificação de integridade em banco separado não modificam o banco principal durante testes de recuperação. Pode haver perda desde a última cópia válida; não se promete perda zero. Critérios de frequência, retenção, proteção e restauração estão definidos, sem automação, scripts ou validações executados.
- **PEND-003 resolvida documentalmente**: apresentação de 22/10/2026 é demonstração local do MVP em desenvolvimento com ASP.NET Core, SQL Server e Ollama/modelo aprovado; não é implantação formal nem homologação. Preservada implantação acadêmica final única em ambiente de testes, futura e condicionada à preparação e validação, sem implantações formais intermediárias.
- **PEND-004 encaminhada ao planejamento técnico, não tecnicamente resolvida**: versões textuais; identificação/proteção da análise ativa R7; entidades/relacionamentos SQL Server; serviços de integração Ollama; concorrência/deduplicação/cancelamento seguro; geração/retenção/restauração de backups; configuração Identity; autorização/isolamento; compatibilização dos refinamentos com o modelo acadêmico. Detalhes técnicos anteriormente identificados continuam encaminhados sem decisões antecipadas. Nenhum plano foi produzido ou iniciado.
- US1–US11 foram refinadas nos pontos necessários; US12 permanece preservada. Mantidos os identificadores FR-001–FR-015, matriz RF001–RF015, RN001–RN004 e UC001–UC004. P0/P1/P2 permanecem distintos; Q10 inclui histórico/revisão P1 na demonstração completa sem excluir funcionalidades P2.
- Cadastro/edição: limites, espaços isolados, prioridade obrigatória sem default e status automático. Revisão: vínculo à versão textual atual, nova análise após mudança, confirmação explícita para Revisado mesmo sem apontamentos/todos descartados, decisões definitivas e ausência de descarte automático de pendências antigas.
- Integridade/segurança: TextoAplicado nullable explicitamente aprovado; transações de análise/sugestões e descrição/TextoAplicado/decisão/status; isolamento no servidor; confirmação do processamento local sem dados da conta; arquivamento sempre lógico com consulta separada e bloqueios no servidor.
- Contrato/falhas: JSON validado, quatro categorias, explicação até 500, texto opcional, lista vazia válida, até cinco problemas, sem correção/truncamento/retentativa automática; recuperação de gravação sem inferência; identificação da operação e bloqueio enquanto execução local estiver ativa. Contexto e timeout aprovados são verificáveis sem prometer detecção exaustiva.
- Histórico/filtros: acesso mesmo vazio, mensagem explícita, filtros opcionais combináveis, datas inclusivas, UTC no armazenamento e America/Sao_Paulo na exibição/interpretação, ordem decrescente e dados próprios.
- SC-001–SC-007 são critérios para validação futura; a aplicação não foi declarada validada. Fluxo completo, entradas, autorização, transações, preservação, indisponibilidade/respostas inválidas, concorrência e timeout são essenciais. Demonstração principal ao vivo; contingência apenas com análises reais históricas identificadas, sem simulação apresentada como execução real.
- Os critérios de aceitação em “Qualidade e cobertura do TCC” permitem verificar futuramente acesso fora do horário comercial/finais de semana, independência de funções sem IA, frequência/retenção/proteção de backups, restauração separada com integridade e preservação do banco principal, além da distinção documental entre demonstração e implantação. Não constituem resultados operacionais já obtidos.
- Refinamentos a refletir futuramente na monografia estão registrados em Key Entities: prioridade, TextoAplicado, vínculo análise–versão, limites, identidade, temporalidade, arquivamento e arquitetura/limitações locais. Nenhuma alteração do DOCX foi autorizada ou efetuada.
- A verificação desta etapa limita-se à revisão documental e de escopo dos arquivos alterados. Nenhum build, teste da aplicação, migração, instalação, chamada de IA/API externa, commit, push ou mudança de branch foi realizado. Nenhum arquivo adicional foi criado.
- **Situação final**: especificação documental concluída no escopo autorizado, aguardando revisão/aprovação humanas; implementação não realizada; testes e validações operacionais não realizados; questões técnicas encaminhadas ao planejamento posterior autorizado. Nenhum código, planejamento, inferência ou implantação foi executado nesta consolidação. Constitution e DOCX preservados. Não há nova contradição funcional identificada; a execução futura e PEND-004 não são declaradas concluídas pela marcação de 16/16.
- Aguardar revisão e aprovação explícitas do responsável. Não executar /speckit.plan, /speckit.tasks ou /speckit.implement nem iniciar qualquer outra etapa automaticamente.
