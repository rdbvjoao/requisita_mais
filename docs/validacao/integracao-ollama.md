# Integração Ollama — entrega de 20/10/2026

Branch: `feat/integracao-ollama`. Recorte: confirmar análise de requisito ativo,
inferir localmente, validar, persistir e consultar resultados/histórico. Nenhuma
aplicação automática ou decisão humana simulada foi implementada.

## Arquivos desta entrega

Alterados:

- `src/RequisitaPlus.Web/Program.cs`: registros de DI e HTTP sem proxy,
  redirecionamento, retry ou log de requisição. Alterações locais anteriores de
  arquivos estáticos foram mantidas.
- `src/RequisitaPlus.Web/Views/Requisitos/Detalhes.cshtml`: entrada da análise e histórico.

Adicionados:

- `src/RequisitaPlus.Web/Services/IA/OllamaClient.cs`: cliente, prompt versionado,
  schema, guarda de contexto e validação integral de envelope/conteúdo.
- `src/RequisitaPlus.Web/Services/IA/AnaliseService.cs`: capacidade exclusiva,
  deduplicação, diário durável, prazo, persistência e tratamento de concorrência.
- `src/RequisitaPlus.Web/Controllers/AnalisesController.cs`: confirmação, POST,
  consulta da operação, resultado e histórico com autenticação/propriedade.
- `src/RequisitaPlus.Web/ViewModels/Analises/AnaliseViewModels.cs`: entradas e projeções.
- `src/RequisitaPlus.Web/Views/Analises/Confirmar.cshtml`.
- `src/RequisitaPlus.Web/Views/Analises/Operacao.cshtml`.
- `src/RequisitaPlus.Web/Views/Analises/Detalhes.cshtml`.
- `src/RequisitaPlus.Web/Views/Analises/Historico.cshtml`.
- `tests/RequisitaPlus.Tests/Support/AnaliseSimulada.cs`.
- `tests/RequisitaPlus.Tests/Unit/OllamaIntegrationTests.cs`.
- `tests/RequisitaPlus.Tests/Contracts/AnaliseHttpTests.cs`.
- `tests/RequisitaPlus.Tests/Integration/Ollama/OllamaContractTests.cs`.
- Este relatório.

Os arquivos SQL, diagnóstico, scripts e testes que já estavam alterados ou não
rastreados antes da tarefa foram preservados. Não foram criadas migrations,
alteradas entidades/configurações EF, executados comandos SQL, commit ou push.

## Comportamento e integridade

O POST exige confirmação, token antiforgery, versão e rowversion vistas. O
proprietário é obtido da identidade autenticada; campos extras do formulário não
definem usuário, texto, estado ou resultado. GETs não inferem nem gravam.

Uma única inferência é permitida no processo Web, sem fila. A operação recebe GUID
de uso único e SHA-256 dos dados efetivos/configuração. Reenvio do mesmo formulário
não repete inferência. O serviço usa escopo próprio e acompanha o trabalho mesmo
quando o navegador deixa de aguardar.

A intenção `Enviada` é salva antes do HTTP. Nenhuma transação SQL permanece aberta
durante a inferência. Após resposta válida, um contexto novo relê o requisito e
disputa seu rowversion. Um único SaveChanges salva análise, sugestões, contador de
ordem e operação; o EF relacional usa transação para esse commit. Não há placeholders
para satisfazer FKs ou propriedades obrigatórias. Tokens desconhecidos permanecem
null; custo monetário local é zero.

Título, descrição e versão analisados são preservados exatamente. Uma edição ou
arquivamento durante a inferência permite guardar o resultado como histórico sem
promover o estado atual. Conflito posterior à releitura faz o commit falhar sem
resultado parcial. A análise atual é a maior ordem da versão textual atual.

A validação rejeita corpo excessivo ou UTF-8 inválido, JSON incompleto, cercas
Markdown, texto excedente, propriedades duplicadas/desconhecidas no conteúdo,
categorias incorretas, explicações brancas ou acima de 500 unidades UTF-16, mais
problemas que o limite configurado, tipos incorretos, envelope incompleto,
contadores ausentes/inconsistentes e término por limite. Zero problemas é sucesso;
o requisito fica Analisado e a revisão humana continua necessária.

As sugestões conservam o texto original e decisão Pendente, sem TextoAplicado ou
data de decisão. A interface usa codificação Razor. O prompt proíbe inventar
critérios e orienta pedir definição ao analista; dados são serializados e seus
delimitadores especiais escapados. Validação estrutural não certifica a correção
semântica da IA: o analista precisa verificar intenção, fatos e critérios.

## Compatibilidade e testes

Consultas locais de metadados confirmaram Ollama 0.40.6, arquitetura qwen2,
tokenizer gpt2/qwen2 e `add_bos_token=false`. O cliente verifica esses atributos
antes de inferir. A guarda usa bytes UTF-8 do prompt raw integral mais 32 de
reserva e MaxOutputTokens; pode recusar textos que caberiam. Nunca corta texto.

Uma chamada real com requisito sintético, sem DbContext ou SQL, passou pelo
OllamaContractTests em aproximadamente 43 segundos. Verificou raw/schema,
done/stop, JSON, contadores e limites. Esse teste é opt-in e não mede qualidade
semântica abrangente ou desempenho sob carga. O contrato baseia-se na
[API oficial do Ollama](https://github.com/ollama/ollama/blob/main/docs/api.md)
e no tokenizer byte-level do
[Qwen2.5-3B-Instruct](https://huggingface.co/Qwen/Qwen2.5-3B-Instruct/blob/main/tokenizer.json),
com conferência dos metadados do artefato local. Ensaios exaustivos de borda e
mudanças futuras do runtime/modelo exigem nova validação.

Verificação automatizada segura:

```powershell
dotnet build --no-restore
dotnet test --no-build --no-restore --filter 'Category!=SqlServer&Category!=Ollama' --verbosity minimal
```

Build sem avisos/erros e 614 testes aprovados. Cobertura inclui Identity/CRUD
existentes, isolamento HTTP, CSRF, confirmação, overposting, HTML codificado,
deduplicação, snapshots, histórico, zero problemas, contexto, falhas, concorrência,
timeout, resposta tardia e bloqueio durável após reinício. Persistência simulada
não comprova constraints, rollback ou rowversion do SQL Server. Testes SQL foram
excluídos explicitamente; nenhum banco de aplicação ou teste foi acessado por esta
validação. O fluxo completo com SQL real no navegador permanece a validar.

## Teste manual no navegador

1. Mantenha o Ollama local disponível com `qwen2.5:3b`. Use uma única instância
   Web e reserve o Ollama para ela durante a demonstração.
2. Preserve a conexão já configurada da aplicação ao banco principal; não use
   variáveis `REQUISITAPLUS_TEST_SQL_*` como conexão da aplicação.
3. Execute `dotnet run --project src/RequisitaPlus.Web --launch-profile https`
   e abra `https://localhost:7234`. Não é necessária migration para este código.
4. Entre com uma conta existente, abra um requisito ativo e clique em
   **Analisar com IA**. Confira texto e versão, marque a confirmação e envie.
5. Aguarde o resultado; se a página mostrar operação em andamento, use
   **Atualizar estado**. Confira problemas, explicações, propostas e decisão Pendente.
6. Volte ao requisito e confirme que sua descrição e versão não mudaram.
   Abra **Histórico de análises**, reabra o resultado e confirme que não há nova inferência.
7. Edite o texto, analise a nova versão e confira os dois snapshots no histórico.
   Arquive um requisito e confira histórico acessível e ausência de análise/edição.
8. Para testar indisponibilidade, feche o Ollama antes de iniciar outra análise:
   a aplicação deve apresentar mensagem e manter o CRUD disponível.

Os passos que cadastram, analisam, editam ou arquivam dados são para execução manual
pelo responsável no ambiente da aplicação; não foram executados nesta entrega.

## Limitações e recuperação

O prazo é funcional. Depois dele, o serviço continua observando a chamada original;
uma resposta tardia comprova término, mas não gera análise. Cancelamento do HTTP
não prova término remoto. Enviada/Incerta sem TerminoComprovado bloqueia novas
inferências, inclusive depois de reiniciar somente a Web. CRUD e histórico continuam
independentes. O prazo não cancela uma transação de persistência já iniciada após
receber tempestivamente um resultado válido.

Se a chamada original nunca comprovar término, a recuperação exige manutenção:
parar a Web; encerrar a instância dedicada Ollama e verificar o término dos processos
de inferência; só então reiniciá-la e reconciliar administrativamente a operação
exata no diário, com evidência técnica de término. A reconciliação deve preservar
GUID, proprietário, fingerprint e histórico, registrando falha encerrada, término
comprovado, instante UTC e evidência mínima sem conteúdo sensível. Não use passagem
de tempo, `/api/ps` ou reinício da Web como prova. Na dúvida, mantenha bloqueado.
Não foi entregue comando de alteração de banco ou botão de desbloqueio; a
reconciliação administrativa controlada precisa ser preparada pelo operador antes
de simular uma interrupção de comunicação na demonstração.

Este recorte não inclui aplicação/ajuste/descarte de sugestões, conclusão de revisão,
regravação de payload mantido em memória ou coordenação entre processos Web. Em
falha de commit não confirmada, consulte a operação: uma análise efetivamente
gravada continua acessível. Se não houver resultado salvo, há mensagem de falha;
uma nova inferência exige nova confirmação e capacidade liberada com segurança.
