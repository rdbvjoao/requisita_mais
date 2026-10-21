# Pesquisa e decisões técnicas — Requisita+

Data: 2026-10-18. Fase 0 de planejamento. Fontes: Constituição, spec/checklist aprovados, DOCX e restrições T1–T10 do responsável. Pesquisa e revisão exclusivamente locais; sem consultas a APIs externas ou serviços. As escolhas abaixo são propostas de engenharia, não resultados de testes. Compatibilidade de versões e APIs será verificada antes do uso real, sem presumir o ambiente instalado.

## D1 — Monólito MVC modular (T1)

**Decisão:** .NET 10, MVC/Razor, Controllers finas, ViewModels para entradas, serviços por responsabilidade e DbContext; um executável Web e um projeto xUnit. Identidade por páginas Identity, com extensão mínima de cadastro para Nome/limites aprovados.

**Justificativa:** mantém as responsabilidades acadêmicas de interface, negócio, IA e dados na mesma aplicação, reduzindo processos e custo cognitivo. As camadas físicas descritas no DOCX podem residir na mesma máquina, como a própria fonte permite. Ollama e SQL são dependências locais, não novos projetos executáveis Web.

**Alternativas consideradas:** microsserviços, SPA com backend separado, bibliotecas por camada, CQRS e repositório genérico acrescentariam coordenação sem benefício ao escopo; não adotar.

## D2 — SQL Express / EF e compatibilização acadêmica (T2/T3)

**Decisão:** Code First com migrations revisáveis e aplicação manual controlada; usar Unicode para preservar português, chaves numéricas de domínio e Identity com chave inteira para manter a correspondência de Usuario. Manter infraestrutura Identity padrão em vez de reproduzir seu esquema manualmente. Registro técnico adicional de operação para controle seguro; ver data-model.md.

**Justificativa:** o modelo acadêmico descreve quatro entidades de negócio; infraestrutura Identity e metadados operacionais não mudam esses conceitos. Prioridade, TextoAplicado e versões implementam refinamentos expressamente aprovados. `nvarchar` evita perda textual; não copiar o DDL antigo nem executar CREATE DATABASE nesta etapa.

**Alternativas consideradas:** senha própria, banco alternativo ou SQL manual em paralelo ao EF divergiriam das decisões. Identity com GUID/string exigiria mudar a correspondência numérica acadêmica sem necessidade. Não existe necessidade de manter coluna SenhaHash(255) própria: usar PasswordHash gerenciado por Identity, sem truncá-lo.

## D3 — Transações e conflitos (T3/T4)

**Decisão:** análise e sugestões persistem juntas; atualização de sequência/status/operação faz parte do mesmo commit. Revisão grava decisão/TextoAplicado/descrição/versão/status no mesmo commit. `rowversion` no requisito e na sugestão; toda mutação de análise ativa ou revisão também atualiza a linha do requisito. Conflito de concorrência causa rollback; usuário recarrega ou tenta explicitamente regravar resultado válido preservado, sem nova inferência.

**Justificativa:** somente validar na leitura deixaria brecha para duas abas aplicarem sugestões substituídas. A disputa pela mesma rowversion serializa logicamente análise ativa, edição e revisão, sem segurar lock durante HTTP. O maior contador confirmado representa a última persistência por requisito. Data/hora isolada poderia empatar ou refletir início da inferência.

**Alternativas consideradas:** transação SQL atravessando os 60 s de HTTP, campo Ativa mutável em várias análises e timestamps como única ordenação aumentariam bloqueios e inconsistências.

## D4 — Versão e ativa R7 (T4)

**Decisão:** contador monotônico de texto, snapshots separados de título/descrição em Analise e ordem monotônica de persistência no requisito. Ativa = análise persistida com versão atual e maior ordem; resultado inválido ou tentativa falha nunca entra nessa relação. RevisaoConcluidaEmUtc fica na análise cuja revisão foi explicitamente concluída, como metadado humano complementar.

**Justificativa:** igualdade de texto não equivale a identidade de versão. Exemplo: v1 texto A → v2 B → v3 A; análise de v1 não rege v3. A1 e A2 da mesma versão preservam conteúdo e decisões; A2 só substitui A1 após commit. Análise bem-sucedida de versão antiga permanece histórica sem mudar o status atual.

**Alternativas consideradas:** hash de texto como versão, reativação automática da análise antiga, flag escolhida pelo usuário ou herança de decisões violam R1/R7.

## D5 — Ollama configurável e local (T5)

**Decisão:** HttpClient dedicado e interface de integração; Options da configuração ASP.NET Core; endereço exato `http://127.0.0.1:11434`, `qwen2.5:3b`, 4.096/1.024 tokens, 60 s, keep_alive cinco minutos e zero a cinco problemas. Bloquear proxy e redirects; URI do request construída no servidor, sem URL fornecida pelo usuário. Sem retry middleware, fallback, chamadas de reparo ou aquecimento automático.

**Justificativa:** uma chamada local por operação confirmada, sem dados da conta. Limites e destino ficam centralizados e verificáveis. Uso inicial de `/api/generate`, `raw=true`, prompt integral serializado e resposta estruturada, conforme contrato. A versão instalada deverá demonstrar os campos necessários antes de ativar IA.

**Alternativas consideradas:** SDK adicional, API externa, múltiplas chamadas para confirmar indicadores ou corrigir JSON e endpoint arbitrário configurável aumentariam risco, consumo ou contrariariam aprovação.

## D6 — Contexto antes da chamada e validação

**Decisão:** usar prompt raw integral e guarda conservadora por bytes UTF-8, reservando 1.024 tokens, conforme contratos/ollama.md. Validar a premissa de limite superior contra o tokenizer byte-level do artefato Qwen instalado antes da habilitação. Se a premissa não puder ser demonstrada, manter análise bloqueada e resolver contagem exata local compatível antes de ativá-la; não usar razão média de caracteres/token nem inferência de sondagem.

**Justificativa:** com raw não há template oculto acrescentado pelo servidor. Um limite superior conservador pode recusar textos que caberiam, mas não permite cortes silenciosos. Os 5.000 caracteres são limite de cadastro, não garantia de análise. Essa escolha evita dependência obrigatória de runtime Python/tokenizador externo no executável; verificação futura do tokenizer é condição técnica real, não validação já realizada.

**Alternativas consideradas:** contar só descrição, dividir por quatro, confiar em `num_ctx` ou conferir `prompt_eval_count` apenas depois não satisfazem US5.8. Tokenizador exato local é alternativa se necessário, sujeito a seleção compatível e revisão de dependências, sem instalar nada nesta etapa.

**Decisão de validação:** servidor aceita apenas objeto JSON completo do contrato, categorias exatas, 0–5 itens, explicação não branca até 500 caracteres e proposta string/nula/ausente. Exemplos são opcionais. Rejeição integral sem extração de Markdown, correção, corte ou nova inferência. Conteúdo é dado não confiável, codificado no Razor; jamais HTML executável.

## D7 — Concorrência, timeout e recuperação (T6)

**Decisão:** supervisor singleton com SemaphoreSlim(1,1), aquisição Wait(0), sem agendador de fila. A operação aceita já começa a única execução; não enfileirar outras. Tarefa supervisionada continua observada dentro do processo, mesmo após desconexão do navegador. O deadline funcional é 60 s; transporte pode continuar sendo observado para comprovar término, sem transformar resposta tardia em sucesso.

**Justificativa:** cancelar await/HttpClient não comprova interrupção no Ollama. Liberação só antes de qualquer envio comprovado, após resposta terminal íntegra ou após recuperação operacional que comprove encerramento. `finally` não pode liberar indiscriminadamente. O bloqueio de recuperação é separado do semáforo, inclusive após restart quando o semáforo foi recriado livre.

**Alternativas consideradas:** liberar após timeout, após os cinco minutos de keep_alive, consultar apenas `/api/ps` ou reiniciar somente Web são provas insuficientes. Infraestrutura distribuída e fila automática são desnecessárias/proibidas.

**Recuperação proposta:** ao perder comunicação, persistir Incerta quando possível e negar novas análises. Operador encerra efetivamente a instância dedicada Ollama e seus processos de inferência, verifica ausência, reinicia o serviço e somente então registra reconhecimento local e reabre Web/IA. Se não puder comprovar, manter bloqueio. Não matar processos automaticamente, não liberar por tempo e não reenviar operação incerta. Encerramento inesperado Web exige conferir o diário antes de habilitar IA; diário/SQL indisponível também impede inferência.

## D8 — Idempotência e resultado não gravado

**Decisão:** identificador GUID por confirmação; registro técnico durável antes de envio, associado a usuário, requisito, versão e hash do snapshot/pedido. Repetição com mesmo ID/fingerprint retorna estado/resultado sem inferir. Mesmo ID com conteúdo diferente é conflito. Índice único da operação e referência única em Analise impedem duplicidade de persistência.

**Justificativa:** cache em memória sozinho não cobre queda entre envio e commit. Estado registrado antes do envio pode gerar falso Incerta se a aplicação cair antes de transmitir; bloqueio conservador é preferível a duplicar uma inferência. Reabertura de tela consulta operação existente e não cria novo ID automaticamente.

**Resultado válido sem commit:** reter temporariamente no supervisor em memória, associado ao proprietário/operação, para regravação explícita no mesmo processo. Não sobrevive obrigatoriamente a reinício; informar perda do resultado e conservar operação como não concluída, sem reenvio automático. Nova análise exige nova confirmação/ID e fim comprovado da anterior. Regravação tardia pode tornar a análise ativa se a versão ainda for atual: R7 ordena persistência, não início da chamada.

**Alternativas consideradas:** refazer inferência para recuperar gravação ou reutilizar ID para enviar novamente contrariam deduplicação. Cache permanente de conteúdo bruto acrescentaria dados sensíveis sem necessidade.

## D9 — Identity, autorização e datas (T7)

**Decisão:** políticas aprovadas de senha, Nome e Email; RequireUniqueEmail e índice único de NormalizedEmail no banco; UserName normalizado derivado de e-mail e campos coerentes com os 150 caracteres. Sem exigir confirmação por e-mail/serviço SMTP no MVP. Cadastro, login e logout padrão; ajustar somente o necessário à política e ao Nome. Identity cuida de hash/salt, cookies e tokens; usar antiforgery nos POSTs e HTTPS no navegador.

**Justificativa:** unicidade validada apenas na aplicação falharia sob concorrência. Todo acesso filtra requisito pelo usuário autenticado; análise e sugestão navegam essa mesma cadeia. Nunca aceitar OwnerId, Status ou decisões históricas por binding de entidades. Instantes em UTC; fuso America/Sao_Paulo para exibição e filtros. Conferir o mapeamento do fuso no runtime Windows; não fixar UTC-3 no cálculo.

**Alternativas consideradas:** autenticação própria, autorização só na View, envio de UserId pelo formulário ou exclusão física não atendem os requisitos.

## D10 — Testes e incremento (T8–T10)

**Decisão:** um projeto xUnit com Unit/Contracts/Integration; HttpMessageHandler/cliente simulados e relógio controlável; SQL Express real para semântica relacional/rowversion/rollback; Ollama real como evidência separada opt-in. Seguir os cinco incrementos e condições de saída em plan.md, preservando P2 para continuidade.

**Justificativa:** testes previsíveis não devem carregar o modelo em cada execução. Simular permite provar controles; só integração real e revisão humana permitem avaliar comportamento/performance/qualidade local. A data curta não permite prometer todos os resultados antes de medir o caminho crítico.

**Alternativas consideradas:** testar só manualmente, executar modelo em toda suite ou considerar EF InMemory equivalente ao SQL deixariam riscos sem evidência adequada.

## D11 — Backup e disponibilidade (PEND-002)

**Decisão:** agendamento futuro pelo Agendador de Tarefas do Windows, sem depender do SQL Server Agent ou de um segundo executável próprio. Acionar ferramenta SQL de linha de comando aprovada/disponível e backup FULL diário do banco inteiro; privilégios mínimos e autenticação Windows preferida quando viável. Destino protegido fora dos arquivos ativos, com ACL para conta de backup/serviço SQL e responsáveis autorizados. Diretório/conta concretos serão definidos na preparação.

**Justificativa:** proporcional ao Express local; preserva Identity, domínio, decisões e diário. Programar execução diária em horário disponível, tentar executar assim que possível após horário perdido e alertar ausência de cópia. Máquina desligada não produz backup: registrar lacuna. Remover cópias fora dos últimos sete dias somente após verificar sucesso de nova cópia; em falha manter última cópia válida e sinalizar retenção excepcional. Não apagar a única cópia válida para cumprir limpeza.

**Verificação futura:** conferir arquivo/registro e verificação de mídia quando suportada; isso não substitui restauração. Restaurar com novos nomes de banco e arquivos em diretório distinto, sem WITH REPLACE sobre o principal; checar integridade e contagens/vínculos, autenticação de teste e amostras de texto/decisões. Não copiar MDF aberto como backup. Guardar evidências sem dados pessoais. Uma cópia no mesmo disco protege menos contra falha física; local separado do banco ativo é obrigatório, mídia/dispositivo separado protegido deve ser escolhido se disponível, sem compra obrigatória.

**Alternativas consideradas:** SQL Agent como dependência presumida, serviço de nuvem obrigatório, cópia de arquivo de banco em uso ou restauração automática sobre produção não são proporcionais/seguros.

**Disponibilidade:** sem regra por dia/hora e sem promessa de uptime; indisponibilidade de IA isola só análise. Demonstração local em 22/10 e implantação acadêmica final única são eventos distintos (PEND-003).

## Revisão humana e verificação técnica

O plano resolve documentalmente os temas técnicos de PEND-004; sua adoção, esquema e próxima etapa ainda dependem da aprovação do responsável. Não há pedido de alteração funcional. Configuração real de SQL, diretório/ACL/conta de backup, versões exatas, teste do tokenizer/raw/sinal terminal e medições de RAM/latência são verificações/configurações futuras, não fatos estabelecidos. Se falharem, manter o gate fechado e revisar a proposta, sem fallback externo. Metas adicionais de qualidade, novos limites e data de implantação final exigem decisão humana.
