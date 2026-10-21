# Contrato técnico proposto — Ollama local

Data: 2026-10-18. Contrato de projeto, não prova da versão instalada. RF005–RF009; RN001/RN002/RN004; UC002; Q5/Q9, US6.1, T5/T6. Nenhuma chamada executada nesta etapa.

## Configuração e destino

Seção `Ollama` via Options ASP.NET Core: `BaseUrl=http://127.0.0.1:11434`, `Model=qwen2.5:3b`, `ContextTokens=4096`, `MaxOutputTokens=1024`, `OperationTimeoutSeconds=60`, `KeepAlive=5m`, `MaxProblems=5`. Permitir sobreposição por ambiente dentro dos limites aprovados; ampliar limites/modelo/destino requer nova aprovação. Não espalhar constantes operacionais pelos serviços.

Validar URI exata: esquema http, host literal 127.0.0.1, porta 11434, sem credenciais/query/fragmento/caminho alternativo. Rota `/api/generate` montada pelo cliente, sem entrada do formulário. Desabilitar redirecionamentos e proxy neste handler. Configuração inválida bloqueia IA com mensagem, mantendo funcionalidades independentes quando Web/SQL estão disponíveis. Sem endereço externo, fallback, retry de HTTP/gateway ou reparo automático.

## Solicitação única

POST JSON ao serviço local. Campos propostos:

| Campo | Valor / finalidade |
| --- | --- |
| model | modelo aprovado configurado |
| prompt | prompt integral raw: instruções, título, descrição, candidatos e delimitação de dados |
| raw | true, para não acrescentar template oculto no servidor |
| stream | false, uma resposta terminal a validar |
| format | JSON Schema correspondente ao objeto de problemas abaixo |
| options.num_ctx | 4096 |
| options.num_predict | 1024 |
| keep_alive | 5m |

Verificar no futuro a versão instalada e a compatibilidade de `raw`, `format` como schema, contadores e sinal de término antes de habilitar análise. Não inferir com outro formato silenciosamente. Não criar OpenAPI pública: a interface de integração é interna ao monólito.

Prompt versionado centralmente: solicitar português e exatamente o objeto JSON; indicar quatro categorias e limites, 0–5 itens, sem obrigatoriedade de exemplo, textoSugerido opcional; instruir o modelo a avaliar candidatos no contexto e acrescentar problemas quando cabível. Título/descrição/candidatos são dados não confiáveis, delimitados e serializados, nunca instruções autorizadas. O servidor trata qualquer resposta como dado. Não enviar nome/e-mail/senha ou dados da conta; não registrar prompt/texto completo em logs.

Indicadores iniciais: rapidamente/imediatamente/sem demora; vários/muitos/poucos/diversos; fácil/intuitivo/eficiente/adequado; usuários autorizados/pessoas responsáveis; quando necessário/se possível/conforme necessário. Busca pode ser insensível à caixa, sem mudar o texto original. Retornar lista deduplicada de candidatos com campo de origem e trecho. Nenhum indicador vira diagnóstico persistido sem a IA. Lista de candidatos vazia ainda segue para inferência confirmada.

## Guarda de contexto antes do envio

Construir exatamente o prompt final, incluindo delimitadores, escapes, instruções e candidatos. Usar `raw=true`; não contar somente o texto do requisito. Proposta inicial: limite superior conservador por quantidade de bytes UTF-8 do prompt final, somado a qualquer token especial inserido pelo runtime cuja sobrecarga tenha sido verificada, mais 1.024 de reserva de saída. Aceitar somente se total <= 4.096.

Essa guarda depende da propriedade do tokenizer byte-level BPE do Qwen instalado: tokens da entrada não excederem os bytes mais a sobrecarga identificada. Antes de habilitar IA, conferir o artefato/tokenizer e raw no runtime real, inclusive BOS/EOS e tokens especiais; registrar evidência local e ensaios de borda. Não considerar a propriedade comprovada por este documento. Se não for demonstrável, bloquear envio e resolver um contador exato local compatível; não usar estimativa média ou tentativa de inferência para medir tokens.

É aceitável a guarda conservadora recusar texto que caberia; informar limite de análise, sem reduzir o limite cadastral de 5.000 caracteres. Não cortar, resumir, retirar candidatos para caber ou enviar em lotes. `prompt_eval_count` após a resposta é evidência complementar, nunca substituto da verificação anterior. Não confiar em truncamento automático do Ollama para cumprir contexto.

## Objeto JSON produzido pelo modelo

Exemplo apenas contratual, sem representar análise real:

```json
{
  "problemas": [
    {
      "tipoProblema": "Termo vago",
      "explicacao": "O termo rapidamente não define um tempo verificável.",
      "textoSugerido": null
    }
  ]
}
```

Objeto raiz exige somente `problemas`, array de 0–5 objetos. Cada objeto exige `tipoProblema` e `explicacao`; permite `textoSugerido` ausente/null/string. Categoria é exatamente Ambiguidade, Termo vago, Falta de clareza ou Falta de especificidade. Explicação string não branca de 1–500 caracteres; exemplos opcionais dentro dela, sem propriedade obrigatória adicional. Proposta não recebe limite funcional extra; aplicação futura precisa respeitar 5.000 caracteres da descrição. Proposta vazia/branca é tratada na apresentação como indisponível, preservando o valor original recebido quando presente.

O schema usado no request deve espelhar esses nomes, required, enum, maxItems e limites. `additionalProperties=false` na raiz e nos itens, sem campo “exemplo” ou “confianca”. Rejeitar propriedades duplicadas, tipos incorretos, JSON seguido de texto, cercas Markdown, null no array, objeto raiz incompleto ou lista ausente. Não extrair fragmentos nem completar campos automaticamente. Um array vazio é sucesso válido com zero sugestões.

Contagem contratual de caracteres segue a validação consistente do servidor e armazenamento; planejar validação em unidades UTF-16 para limites de entrada/explicação, coerente com string .NET e tamanho `nvarchar`, inclusive testes de caracteres suplementares. Não truncar para ajustar divergência de contagem; orientar usuário e preservar conteúdo original.

## Envelope Ollama e conclusão

Somente HTTP bem-sucedido não basta. Exigir envelope completo, `done=true`, motivo de término normal reconhecido na versão verificada, `response` contendo JSON integral válido e contadores de entrada/saída consistentes e dentro dos limites. Término por limite de geração (`length` ou equivalente da versão), `done=false`, erro de modelo, corpo incompleto ou contadores essenciais ausentes torna a análise inválida. Mesmo que um JSON parcial seja parseável, término por limite é rejeitado. Validar tanto envelope quanto conteúdo; nunca persistir itens parciais.

Resposta terminal íntegra pode provar fim da inferência mesmo quando o conteúdo viola o contrato; nesse caso liberar capacidade após registrar término, sem inserir análise. Erro/desconexão cujo término remoto não seja comprovável segue Incerta. Tokens conhecidos ficam no diário mesmo na falha; comunicação perdida deixa consumo desconhecido, não falsamente zero. Custo monetário de API é zero. Métricas/textos não devem expor conteúdo sensível em logs.

## Supervisor, prazo e estados

| Situação | Comportamento |
| --- | --- |
| Cancelamento da confirmação | Não registrar envio, não inferir, não alterar requisito |
| ID conhecido com mesmo fingerprint | Retornar estado/resultado existente, sem chamada |
| ID conhecido com outro conteúdo | Conflito; sem chamada |
| Outra inferência/recuperação ativa | Resposta imediata ocupado/recuperação; sem fila |
| Adquirido controle, falha comprovada antes de enviar | FalhaEncerrada e liberação; zero inferência |
| Envio aceito | Persistir Enviada antes do primeiro byte possível; manter posse no supervisor |
| Término normal antes de 60 s | Validar, persistir ou reter para regravação explícita; comprovar término |
| Prazo de 60 s | Informar “Não foi possível concluir a análise” e timeout; marcar deadline, sem liberar por relógio |
| Resposta terminal após prazo | Usar apenas como evidência de término/consumo; não promover a análise ou status silenciosamente |
| Transporte perdido / parada Web / cancelamento sem prova | Incerta; bloquear novas inferências até recuperação comprovada |
| Resultado válido sem commit | Não anunciar conclusão; regravação explícita sem inferência quando payload preservado |

`HttpClient.Timeout` não deve cancelar automaticamente aos 60 s e devolver o semáforo: o deadline é controlado pelo supervisor. Observação da chamada em curso não é nova chamada. Se acompanhamento não puder continuar, encerrar recursos de transporte sem presumir término remoto e conservar recuperação bloqueada. `RequestAborted` encerra apenas entrega da página, não a posse do controle. Não criar tarefa sem observação nem usar timer para liberar. Cancelamento/host shutdown não é evidência remota.

Registro durável anterior ao envio garante que restart não torne a inferência invisível. Após restart, Enviada/Incerta sem término comprovado exige bloqueio de recuperação, mesmo com SemaphoreSlim recém-criado. Persistência de intenção impossível = envio proibido. Um ID é de uso único, inclusive falha/timeout; nova tentativa de inferência é nova análise explicitamente confirmada após liberar com segurança.

## Recuperação operacional sem infraestrutura distribuída

Se não houver prova de fim pela resposta da chamada original, operador deve encerrar a instância dedicada Ollama e seus processos de inferência, verificar que terminaram, e reiniciar o serviço. Só então reconhecer a recuperação pelo procedimento local protegido/operador e marcar operação falha encerrada antes de reabrir IA. Não disponibilizar botão comum “desbloquear” nem confiar em declaração de cliente Web como evidência. Proposta mínima: reconciliação operacional controlada do diário, durante manutenção com Web parada, a implementar/revisar futuramente; não fornecer edição irrestrita do histórico de domínio.

Não usar `/api/ps`, modelo carregado/descarregado, keep_alive de cinco minutos, passagem de tempo ou reinício só de Web como prova suficiente. Não executar automaticamente processo de kill/restart, inferência de sondagem ou retry. Se o operador não consegue confirmar término, conservar bloqueio; gestão de requisitos e histórico continuam independentes quando SQL/Web estão funcionando. Durante a sessão de uso, não executar inferências por outros clientes Ollama: o semáforo só coordena a única aplicação aprovada.
