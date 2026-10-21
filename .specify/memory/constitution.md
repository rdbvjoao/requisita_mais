# Requisita+ — Software de Análise e Controle de Requisitos Constitution

## Core Principles

### I. Fidelidade aos requisitos

A documentação acadêmica original DEVE ser a referência principal para funcionalidades,
regras de negócio e casos de uso. Todo trabalho DEVE respeitar RF001–RF015, RN001–RN004 e
UC001–UC004 e registrar a rastreabilidade aos identificadores aplicáveis nos artefatos do
Spec Kit. Ambiguidades, lacunas e conflitos DEVEM ser explicitados e submetidos à aprovação
humana antes de decisões funcionais não documentadas. Nenhum comportamento pode ser presumido
apenas a partir do identificador de um requisito ou de uma regra.

### II. Simplicidade e escopo controlado

A solução DEVE utilizar C#, ASP.NET Core MVC em .NET 10, Entity Framework Core e SQL Server.
O trabalho DEVE priorizar um protótipo funcional até 2026-10-22, limitado ao escopo aprovado.
Funcionalidades desnecessárias e complexidade sem justificativa para os requisitos aprovados
DEVEM ser evitadas. O prazo NÃO autoriza ampliar o escopo nem omitir aprovações, integridade,
segurança ou validação exigidas por esta Constituição.

### III. Desenvolvimento incremental

O desenvolvimento DEVE seguir, nesta ordem: especificação, esclarecimento, planejamento,
tarefas, aprovação humana, implementação, testes e validação. Cada etapa DEVE produzir
artefatos ou evidências revisáveis e indicar suas pendências. A conclusão de uma etapa NÃO
autoriza iniciar automaticamente a seguinte: cada transição depende de autorização humana
explícita. A implementação somente pode começar após a aprovação humana dos artefatos e
tarefas aplicáveis.

### IV. Integridade dos dados

Requisitos, análises e sugestões DEVEM ser preservados, respeitando os relacionamentos do banco,
a RN004 e a exclusão lógica. Alterações e exclusões NÃO podem causar perda indevida de histórico
ou referências órfãs. A interpretação da RN004 DEVE ser conferida na documentação acadêmica;
dúvidas DEVEM seguir os princípios I e VIII antes da implementação.
Mudanças de esquema DEVEM utilizar migrations do Entity Framework Core, versionadas, revisáveis
e aplicadas de forma controlada, com impactos sobre dados e estratégia de recuperação
documentados antes da execução. Mudanças no modelo ou tratamento dos dados exigem aprovação
humana; gerar tecnicamente uma migration já aprovada pode ser uma decisão rotineira.

### V. Segurança

A aplicação DEVE implementar autenticação segura e armazenar senhas exclusivamente com hash
apropriado para senhas, com salt e mecanismo consolidado. Texto puro e criptografia reversível
NÃO são formas permitidas de armazenamento de senhas. Chaves de API e demais segredos DEVEM
permanecer fora do código versionado, protegidos por configuração segura e sem exposição em
logs ou respostas. Credenciais NÃO podem ser incluídas no Git.
O tratamento de dados pessoais DEVE observar os requisitos aplicáveis da LGPD, incluindo
finalidade, necessidade, controle de acesso e proteção dos dados. O envio de dados pessoais
ao provedor de IA DEVE ser avaliado e aprovado conforme a finalidade e as regras aplicáveis.

### VI. Qualidade e testes

Cada funcionalidade DEVE possuir critérios de aceitação verificáveis e rastreáveis aos requisitos
aprovados. A validação DEVE combinar testes automatizados adequados ao comportamento e aos
riscos com validação manual dos fluxos previstos. Os registros de execução DEVEM informar os
testes executados, resultados, falhas, testes não executados e pendências. NÃO se pode declarar
como validado aquilo que não foi verificado. Pendências que impeçam atender aos critérios de
aceitação DEVEM ser resolvidas ou submetidas à decisão humana antes de aceitar a entrega.

### VII. IA sob supervisão humana

A solução DEVE integrar por API um modelo preexistente, configurado para analisar ambiguidades,
termos vagos e falta de clareza e de especificidade dos requisitos. As respostas DEVEM ser
tratadas como análises e sugestões para revisão humana. A IA NÃO pode alterar requisitos sem
confirmação explícita do analista sobre a alteração proposta.
O serviço de IA DEVE permanecer isolado das demais responsabilidades e ser testável por meio de
respostas simuladas, inclusive para falhas do provedor. A integração real DEVE obedecer aos
controles de autorização e custo do princípio IX.

### VIII. Aprovação de mudanças

Decisões que alterem funcionalidades, regras de negócio, dados ou escopo DEVEM ser submetidas
à aprovação humana antes de sua execução. A proposta DEVE registrar a mudança, sua justificativa,
os requisitos afetados e os impactos conhecidos. A aprovação ou rejeição DEVE ser registrada
nos artefatos do Spec Kit. Decisões técnicas rotineiras podem ser tomadas sem nova aprovação
quando respeitarem esta Constituição, o escopo e as decisões já aprovadas. Essa autonomia NÃO
autoriza iniciar outra etapa do processo nem executar chamadas pagas sem autorização.

### IX. Controle de custos da IA

A seleção do modelo DEVE priorizar opções econômicas compatíveis com a análise aprovada.
Antes de ativar chamadas reais, DEVEM ser definidos e aprovados o modelo, o orçamento, os
limites de tokens por chamada e os limites de consumo. Chamadas pagas NÃO podem ser executadas
sem autorização humana explícita, com escopo e limites registrados.
O serviço DEVE evitar chamadas duplicadas para a mesma solicitação e utilizar respostas
simuladas nos testes. Cada chamada real autorizada DEVE registrar o modelo, o consumo de tokens
e a estimativa de custo, sem expor segredos ou dados pessoais desnecessários.
Novas chamadas DEVEM ser impedidas quando os limites aprovados forem atingidos ou quando não
for possível verificar o saldo disponível. Qualquer ampliação exige nova aprovação.

## Restrições e referências

A documentação acadêmica original DEVE ser preservada sem alterações. Refinamentos aprovados
DEVEM ser registrados nos artefatos do Spec Kit, com referência à origem e à decisão humana,
permitindo distinguir o texto original dos refinamentos. Conflitos entre a documentação e
esta Constituição DEVEM ser apresentados à revisão humana antes de decisões funcionais.

Esta Constituição DEVE ser consultada em todas as etapas posteriores. A ausência da documentação
de referência NÃO autoriza inventar o conteúdo dos requisitos ou da RN004. A conferência da
documentação e de sua rastreabilidade permanece pendente até que a fonte esteja disponível.

Durante a etapa inicial de adoção da Constitution v1.0.0, as alterações ficaram limitadas ao arquivo `.specify/memory/constitution.md`, sem autorização para implementar funcionalidades, criar a aplicação ASP.NET Core ou executar commits.

Essa restrição refere-se exclusivamente à etapa de criação da Constituição e não impede alterações futuras nos arquivos do projeto, desde que sejam previamente aprovadas e respeitem os princípios estabelecidos.

Os templates e comandos dependentes devem consultar a Constituição durante as próximas etapas do desenvolvimento.

## Fluxo de trabalho e validação

Cada etapa DEVE apresentar seus artefatos, a conformidade com os nove princípios e as pendências
para revisão humana. Especificação e esclarecimento DEVEM resolver ou registrar ambiguidades;
planejamento e tarefas DEVEM explicitar escopo, dependências, critérios de aceitação e testes.
A aprovação humana DEVE identificar os artefatos e tarefas autorizados antes da implementação.
Testes e validação DEVEM apresentar evidências dos resultados e a decisão humana sobre a entrega.

Antes de qualquer transição, o responsável DEVE informar o resultado da etapa e aguardar a
autorização humana para a próxima. Antes de chamadas reais de IA, DEVE também conferir a
autorização, o orçamento e os limites registrados. O prazo do protótipo NÃO dispensa esses marcos.

## Governance

Esta Constituição é a referência obrigatória para especificações, planos, tarefas, implementação
e revisões do Requisita+. Todos esses artefatos e suas revisões DEVEM verificar conformidade com
os nove princípios e registrar divergências para decisão humana antes de prosseguir.

Emendas DEVEM ser propostas com justificativa, texto afetado, impactos nos artefatos e eventual
plano de adequação. Sua adoção exige aprovação humana explícita e registro da decisão nos
artefatos do Spec Kit. A documentação acadêmica original DEVE permanecer preservada, inclusive
quando houver refinamentos aprovados. Aprovar uma mudança de escopo NÃO altera implicitamente
esta Constituição: conflitos exigem uma emenda explícita.

O versionamento DEVE seguir MAJOR.MINOR.PATCH: MAJOR para remoção ou redefinição incompatível
de princípios ou governança; MINOR para novos princípios ou ampliação material das orientações;
PATCH para esclarecimentos e correções sem mudança de significado. Cada emenda DEVE atualizar
a versão e a data da última alteração em formato ISO, preservando a data da ratificação original.
A versão 1.0.0 corresponde à adoção inicial dos nove princípios em 2026-10-17.

A definição e aprovação do orçamento, modelo de IA e limites de consumo permanecem obrigatórias antes da ativação de chamadas reais à API.

As alterações na Constituição e os commits futuros devem seguir os princípios de aprovação, rastreabilidade e controle de mudanças estabelecidos neste documento.

**Version**: 1.0.0 | **Ratified**: 2026-10-17 | **Last Amended**: 2026-10-17