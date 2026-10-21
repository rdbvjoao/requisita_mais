# Contratos de interação MVC — proposta

Data: 2026-10-18. Contratos de formulários/Views e respostas, sem API REST pública ou código de Controllers. RF001–RF015, RN001–RN004, UC001–UC004; páginas ainda não implementadas.

## Regras comuns

HTTPS entre navegador e Web. Exigir autenticação Identity em todas as rotas de domínio; cadastro/login são exceções padrão. GET consulta, nunca altera ou infere. POST com antiforgery e ViewModel específico, sem binding direto de entidade. OwnerId vem exclusivamente da identidade autenticada. Sempre carregar recurso junto da cadeia de propriedade; IDs alheios recebem resposta equivalente a inexistente sem expor conteúdo. Aplicar também a consultas de operações, filtros, regravação e decisões.

Validação no cliente como auxílio e no servidor como autoridade. Entradas não brancas, título <=150, descrição <=5.000 e prioridade explícita Alta/Média/Baixa. Nenhum campo de edição manual de Status. Campos não autorizados não podem mudar o domínio. Mensagens distinguem validação, não encontrado, conflito de versão/ativa, ocupado, timeout, indisponibilidade e falha SQL. Páginas Razor codificam texto do usuário/modelo. POST bem-sucedido segue redirect para GET; duplicação por refresh não reinfere.

## Rotas e dados propostos

| Método / rota relativa | Entrada / efeito | Prioridade / origem |
| --- | --- | --- |
| Identity UI padrão de Register/Login/Logout | Nome/email/senha conforme política; logout POST protegido | P0 US1; Segurança |
| GET /Requisitos | Lista própria não arquivada e estado vazio | P0 RF015; UC001/UC002 |
| GET /Requisitos/Detalhes/{id} | Conteúdo atual, prioridade/status/versão, indicação da ativa e acesso ao resultado | P0 RF015/RF009 |
| GET /Requisitos/Criar | Formulário com prioridade sem default | P0 RF001/RF004 |
| POST /Requisitos/Criar | Titulo, Descricao, Prioridade; cria Rascunho v1 | P0 RF001/RF004; RN001 |
| GET/POST /Requisitos/Editar/{id} | POST: Titulo, Descricao, Prioridade, VersaoEsperada, RowVersion | P0 RF002; RN004; UC001 |
| GET /Analises/Confirmar/{requisitoId} | Explicita processamento local de título/descrição/candidatos; gera ID sem chamar IA | P0 RF005; UC002 |
| POST /Analises/Iniciar | IdOperacao, RequisitoId, VersaoEsperada, RowVersion, ConfirmacaoLocal=true | P0 RF005–RF009; RN001; UC002 |
| GET /Analises/Operacao/{idOperacao} | Estado próprio, resultado/erro; consultar nunca reinfere | P0 US5; SEC-005 |
| POST /Analises/Regravar/{idOperacao} | Confirma tentativa de gravação do payload válido retido, sem chamada IA | P0 US5.9; RN004 |
| GET /Analises/Detalhes/{id} | Texto analisado, data, versão, apontamentos, sugestões/decisões; sinaliza histórica/substituída | P0 RF006–RF009; UC002 |
| GET /Analises | Histórico próprio decrescente, inclusive vazio | P1 RF010; RN004; UC003 |
| POST /Revisao/Decidir/{sugestaoId} | AnaliseIdEsperada, VersaoEsperada, RowVersions, Decisao, TextoFinal quando aplicável, Confirmacao=true | P1 RF012/RF013; RN002/RN004; UC004 |
| POST /Revisao/Concluir/{requisitoId} | AnaliseIdEsperada, VersaoEsperada, RowVersion, ConfirmacaoConclusao=true | P1 RF014; RN003; UC004 |
| POST /Requisitos/Arquivar/{id} | RowVersion, confirmação; só exclusão lógica | P2 RF003; RN004; UC001 |
| GET /Requisitos/Arquivados | Consulta própria separada e acesso ao histórico, sem restaurar | P2 US10 |
| GET /Analises com filtros opcionais | RequisitoId, DataInicial, DataFinal | P2 RF011; RN004; UC003 |
| GET /Ajuda | Exemplos bons/ruins e explicações; nenhuma inferência | P2 US12; Apreensibilidade |

Nomes são contratos propostos para organização futura, não declaração de endpoints disponíveis. Não expor rotas de edição/exclusão de análises ou de alteração de decisões definitivas. Não há restore de arquivado.

## Confirmações e revisão

Confirmar análise revalida snapshot/versão vistos no diálogo; mudança interveniente exige nova apresentação/confirmação, sem enviar texto diferente silenciosamente. ID de operação é vinculado ao pedido confirmado e ao proprietário. Confirmar novamente a mesma operação consulta seu estado; nova análise intencional exige novo diálogo/ID. Estado ocupado não inicia trabalho depois quando o controle fica livre: é necessária ação humana novamente.

RT1 aprovada — Aplicada: a proposta da IA representa a nova descrição integral do requisito. Antes de confirmar, o analista deve visualizar claramente a descrição completa resultante; a aplicação exige confirmação explícita, sem substituição silenciosa de trechos. Sem proposta não permitir aplicação. Ajustada: o analista pode editar livremente a descrição final dentro dos limites funcionais existentes, mesmo sem proposta, preservando partes da redação original e incorporando as melhorias que considerar adequadas. Exigir prévia completa da descrição final e confirmação explícita; registrar em TextoAplicado exatamente a descrição final confirmada em Aplicada/Ajustada. Descartada: exige confirmação, não altera a descrição e mantém TextoAplicado = null.

Campo TipoProblema, Explicacao e TextoSugerido nunca são aceitos como alteração histórica. Validar no servidor que sugestão está Pendente e pertence à ativa atual. Mudança textual invalida aplicação das demais sugestões à nova versão, conservando-as no histórico sem descarte automático. Decisão final não pode ser reaberta.

Revisado exige confirmação explícita de conclusão, após todas as sugestões da ativa decididas, também se zero/todas descartadas. Último descarte e abertura da tela não equivalem à conclusão. Proposta com [TEMPO A DEFINIR] deve ser revisada pelo analista; o sistema não supõe valor de negócio aprovado.

## Apresentação e filtros

Mostrar “Sugestão não disponível” quando proposta ausente/nula/branca, “Nenhuma análise encontrada” para histórico vazio e “Requisito não encontrado” para recurso indisponível. Resultado vazio válido informa que não foram identificados problemas, não certifica qualidade. Distinguir texto original analisado, TextoSugerido e TextoAplicado. Sempre indicar cobertura limitada e revisão humana.

Instantes em UTC, exibição e interpretação em America/Sao_Paulo. Filtros P2 são opcionais e combináveis por interseção; rejeitar datas inválidas/período invertido. Converter início local inclusivo para UTC e usar fim exclusivo do dia seguinte à data final local, incluindo integralmente o dia final sem depender da precisão de datetime2. Só data inicial/final informada aplica o respectivo limite aberto do outro lado. Ordenar DataAnaliseUtc DESC e IdAnalise DESC. Consultas incluem somente dados próprios, inclusive histórico de arquivados.

## Resultados e falhas

Sucesso de análise só após commit; operação aceita/em execução não é análise concluída. Pode-se redirecionar imediatamente ao acompanhamento da única execução já adquirida; consulta de estado não é fila de inferências. Falha técnica apresenta “Não foi possível concluir a análise” com causa compreensível. Conflito de concorrência solicita recarregar, sem reaplicar decisão automaticamente. Erro de gravação preserva domínio anterior e oferece regravação explícita somente se houver payload válido preservado; nunca botão que reinfere sob o mesmo ID. Recuperação de inferência incerta é procedimento operacional restrito, não ação irrestrita da interface do analista.
