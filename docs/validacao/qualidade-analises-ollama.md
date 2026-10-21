# Qualidade das análises Ollama — prompt v2

## Escopo

Branch `feat/integracao-ollama`. Ajuste localizado em `ContratoIA`, no arquivo
`OllamaClient.cs`, sem mudar a interface do cliente, o transporte, as entidades,
migrations, banco, CRUD, persistência, concorrência, recuperação ou revisão humana.
As alterações locais anteriores foram preservadas. Não houve commit, push ou
execução de análises reais.

O prompt `requisita-ptbr-v2` pede de zero a `MaxProblems` lacunas relevantes e
distintas, priorizadas por impacto. Exige explicação curta com o trecho afetado,
a dificuldade de interpretação/verificação e a definição pendente do analista.
Proíbe acrescentar decisões, tecnologias, números, segurança ou permissões sem
origem no requisito. Usa campos específicos ou propostas condicionais para
informações ausentes, com exemplo de tempo de resposta e alerta sobre dois fatores.

## Proteções e limites

- Schema mantém o contrato JSON e acrescenta `uniqueItems`. O validador local
  também elimina explicações repetidas, mesmo em categorias diferentes, ignorando
  caixa, acentos, aspas, espaços e pontuação final de frase. Conserva a primeira
  ocorrência e a ordem original. Pontuação interna de valores e operações é preservada.
- Todos os itens são validados antes da deduplicação. Excesso de itens continua
  inválido, inclusive quando contém duplicatas; nenhum conteúdo inválido é ocultado.
- Explicações distintas não são agrupadas apenas por palavras em comum ou pela
  mesma proposta. Isso evita perder lacunas diferentes. Paráfrases semanticamente
  equivalentes continuam dependendo do modelo e da revisão humana.
- Foram avaliados bloqueios por números e palavras de segurança/tecnologia.
  Não foram adotados: o contrato de resposta não fornece evidência de origem por
  afirmação, e esses elementos podem estar legitimamente no requisito, representar
  conversões de unidade ou aparecer em perguntas condicionais. Uma lista de palavras
  não comprova invenção e poderia rejeitar sugestões válidas.
- A validação estrutural não aprova decisões de negócio. A regressão sobre critérios
  inventados verifica as instruções do prompt, a separação dos dados não confiáveis
  e a aceitação de pendências/condicionais. Não constitui avaliação semântica real
  do modelo. O analista deve conferir a fundamentação de cada proposta.
- Permanecem os limites de contexto/saída, contadores, término `done/stop`, transporte,
  explicações de até 500 caracteres e resposta vazia válida. A guarda considera os
  bytes do prompt integral, que ficou maior; textos extensos podem atingir o limite
  mais cedo. Nenhum texto é truncado.
- O histórico novo registra a versão v2 pelo mecanismo existente. Resultados
  anteriores não são regenerados. O requisito original não é aplicado/reescrito.

## Verificação automatizada segura

```powershell
dotnet build --no-restore
$env:REQUISITAPLUS_TEST_SQL_ENABLED = 'false'
$env:REQUISITAPLUS_TEST_OLLAMA_ENABLED = 'false'
dotnet test --no-build --no-restore --filter 'Category!=SqlServer&Category!=Ollama' --verbosity minimal
```

Resultado em 20/10/2026: build com zero avisos/erros; 634 testes aprovados,
zero falhas e zero ignorados na seleção segura, incluindo 15 casos novos.
Nenhum teste de integração real SQL/Ollama foi executado.

`AnalysisQualityTests` cobre duplicatas, categorias diferentes para a mesma
explicação, diferenças relevantes de pontuação, lacunas distintas com palavras
comuns, limite antes da deduplicação, itens inválidos, resposta vazia pelo HTTP
simulado com limites 1/3/5, critérios pendentes/condicionais, instruções contra
invenções e escape de delimitadores. A suíte existente também verifica histórico,
texto original preservado, sugestões pendentes, concorrência e recuperação com
armazenamento simulado.

## Validação posterior no navegador

Este roteiro inicia novas análises e grava histórico. Não foi executado nesta tarefa.
Use o ambiente de demonstração e requisitos de teste, com a aplicação reiniciada
para carregar o novo assembly e o Ollama local disponível.

1. Em um requisito de teste, abra **Detalhes → Analisar com IA** e confirme a análise.
   Exemplo: título `Consulta de requisitos`; descrição `O sistema deve listar título,
   descrição e status dos requisitos do usuário rapidamente.`
2. Confira se a análise identifica o termo `rapidamente`, explica a ausência do
   critério e pede tempo/condições ao analista. Não deve fixar segundos nem propor
   outra tecnologia. Tempo e condições da mesma lacuna não devem gerar itens repetidos.
3. Com `O sistema deve permitir acesso seguro aos requisitos do usuário.`, confira
   se pede ao analista os critérios de segurança aplicáveis. Não deve impor dois
   fatores, perfis ou permissões como decisões já aprovadas.
4. Com `Ao abrir a página de requisitos, o sistema deve listar título, descrição e
   status dos requisitos do usuário autenticado, ordenados por título em ordem
   crescente. Se a lista estiver vazia, deve exibir "Nenhum requisito cadastrado".`,
   confira que não tenta completar cinco apontamentos. Se não houver lacunas
   relevantes, deve mostrar a mensagem existente de nenhum problema identificado.
   Uma resposta vazia não certifica a qualidade nem conclui a revisão humana.
5. Confira que cada explicação é curta e distinta, que campos pendentes dizem o que
   definir e que cada reescrita melhora a verificabilidade após seu preenchimento.
6. Volte aos detalhes e ao histórico. Confira o texto original intacto, a análise
   anterior preservada e as sugestões novas pendentes de revisão humana.

O comportamento textual do `qwen2.5:3b` deve ser avaliado nesses exemplos antes da
demonstração; a suíte segura não executa inferência nem mede sua qualidade real.
