# T001 — Diagnóstico inicial do ambiente de desenvolvimento

**Data:** 18/10/2026.  
**Projeto:** Requisita+.  
**Natureza:** diagnóstico inicial preservado, acrescido de atualização posterior à T001 com evidências de instalação e validação inicial fornecidas pelo responsável em 18/10/2026. Este documento não constitui validação integral do ambiente.

## Escopo e origem das evidências

A verificação inicial ocorreu entre 11h01 e 11h03, e a verificação complementar do SQL Server ocorreu a partir de 11h07, no fuso -03:00. As consultas identificaram versões, registros, serviços e metadados; não comprovaram o funcionamento integrado da aplicação, do banco de dados ou do modelo de IA.

O registro documental inicial não envolveu novos diagnósticos ou consultas ao ambiente. Nas duas verificações iniciais, anteriores à instalação, não foram executados instalações, configurações, conexões SQL, inicializações de serviços, inferências, migrations, builds ou testes. Não foram utilizados Win32_Product ou métodos de reparo de software. A instalação manual e a consulta SQL posteriores foram realizadas pelo responsável e estão descritas separadamente abaixo; não foram repetidas nesta atualização documental.

## Ambiente de desenvolvimento

| Componente | Versão ou estado identificado | Evidência registrada | Situação e limite da confirmação |
| --- | --- | --- | --- |
| Windows | 64 bits, versão 25H2, build 26200.9711 | Registro CurrentVersion e informações de Environment | Confirmado por consulta. O nome de produto retornado pelo Registro era legado; não foi usado para atribuir outra versão ao sistema. |
| PowerShell | 5.1.26100.9711, edição Desktop | PSVersionTable | Confirmado por consulta de versão. A política de execução não foi alterada. |
| SDK .NET | SDKs 9.0.201 e 10.0.401; selecionado 10.0.401 | dotnet --list-sdks e dotnet --version | Confirmado por consulta; nenhum projeto compilado. |
| Runtime ASP.NET Core | 10.0.12 | dotnet --list-runtimes | Confirmado no inventário de runtimes; aplicação não executada. |
| Ferramentas EF Core globais | 10.0.12 | Localização do shim dotnet-ef e versão no armazenamento global de ferramentas | Instalação identificada por metadados. Execução da ferramenta ainda não confirmada. |
| Git | 2.53.0.windows.1 | git --version | Confirmado por consulta de versão; sem commits ou alterações de branch. |
| Codex | 0.163.0 | Metadados do pacote instalado @openai/codex | Versão do pacote confirmada. A tentativa codex --version foi bloqueada pela política de execução de scripts; execução CLI não confirmada. |
| Ollama | 0.40.6; servidor local identificado em http://127.0.0.1:11434 | GET /api/version, HTTP 200 | Serviço HTTP local respondeu à consulta. Não houve inferência ou validação integral do contrato. |
| Modelo local | qwen2.5:3b presente no catálogo; 3,1 bilhões de parâmetros, quantização Q4_K_M | GET /api/tags, HTTP 200, com o modelo listado | Presença no catálogo confirmada. Funcionamento, desempenho, qualidade das respostas e compatibilidade com o contrato ainda não validados. |
| RAM | 7,71 GiB totais e 0,28 GiB disponíveis às 11h03min23s de 18/10/2026 (-03:00) | Microsoft.VisualBasic.Devices.ComputerInfo | Medição pontual confirmada. A memória disponível varia e não representa disponibilidade permanente nem capacidade validada de execução simultânea dos serviços. |

No inventário inicial, global.json e .config/dotnet-tools.json não foram encontrados. A preparação desses arquivos pertence às tarefas futuras autorizadas; sua ausência não foi corrigida neste diagnóstico.

## Banco de dados — histórico anterior à instalação

**Classificação da verificação complementar: Não identificado**, quanto à instância convencional do SQL Server Express prevista pela tecnologia T2 aprovada. Nenhuma instalação desse tipo foi encontrada nas consultas acessíveis; isso não comprova ausência absoluta.

| Componente ou consulta | Evidência registrada | Conclusão e limite |
| --- | --- | --- |
| Microsoft SQL Server 2019 LocalDB | Programa instalado, versão 15.0.4382.1; chave MSSQL15E.LOCALDB/Setup informa Express Edition e versão 15.0.4382.1 | LocalDB identificado. A edição Express observada pertence especificamente ao LocalDB e não comprova uma instância convencional Express como serviço. |
| Arquivo do mecanismo LocalDB | sqlservr.exe no caminho padrão Microsoft SQL Server/150/LocalDB/Binn, versão 15.0.4382.1 | Confirma arquivos do LocalDB; não foi executado nem conectado. |
| Cliente SQLCMD | Microsoft Command Line Utilities 15 for SQL Server, versão 15.0.1300.359; executável localizado | Cliente identificado. Sua presença não comprova instalação do mecanismo SQL Server Express. |
| Serviço SQLWriter | SQL Server VSS Writer, em execução no instante da consulta | Componente auxiliar de backup identificado; não é evidência suficiente de instância do mecanismo Express. |
| Serviços do mecanismo SQL Server | Nenhum MSSQLSERVER ou MSSQL$... encontrado na enumeração; chaves de serviço MSSQLSERVER e MSSQL$SQLEXPRESS não encontradas | Nenhuma instância convencional identificada por serviços. Não há nome ou estado de instância Express convencional confirmado. |
| Registro de instâncias | InstalledInstances sem valor e Instance Names/SQL não encontrado; visão de 32 bits sem registro SQL Server identificado | Nenhuma instância convencional identificada nos registros consultados. |
| Diretórios padrão de instalação | Nenhuma pasta de instância MSSQL... encontrada nos diretórios Microsoft SQL Server de Program Files e Program Files (x86) | Não foram encontrados arquivos de instância convencional nesses caminhos. Caminhos não consultados não estão cobertos por essa conclusão. |
| Outros componentes | ODBC Driver 17 for SQL Server 17.10.6.1 e System CLR Types para SQL Server 2019 15.0.2000.5 | Bibliotecas auxiliares identificadas; não comprovam instalação de instância Express convencional. |

LocalDB não deve ser utilizado como substituto automático da tecnologia T2 aprovada. Ao término desse diagnóstico anterior à instalação, instância convencional, edição, versão do mecanismo, porta, autenticação e permissões permaneciam sem confirmação para o alvo futuro do Requisita+. A situação posterior está registrada na seção seguinte.

## Atualização posterior à T001 — instalação e validação inicial em 18/10/2026

**Origem:** evidências confirmadas pelo responsável pelo projeto e fornecidas na conversa do Codex. Nenhum comando de diagnóstico, serviço ou SQL foi executado novamente para produzir esta atualização. A T001 já havia sido concluída e aprovada; este registro não modifica o estado de tarefas.

**Situação atual documentada:** Microsoft SQL Server 2025 Express convencional, 64 bits, instalado na instância nomeada `REQUISITAPLUS`, com serviço em execução e conexão local por autenticação Windows validada pelo responsável. A classificação histórica “Não identificado” descreve exclusivamente o momento anterior à instalação.

### Instalação informada pelo responsável

O assistente oficial da Microsoft concluiu a instalação com êxito. Foram apresentados como bem-sucedidos Serviços de Mecanismo de Banco de Dados, SQL Server Browser, SQL Server Writer e arquivos de suporte. Sucesso de instalação de componente não comprova seu estado de execução; o Browser foi selecionado como desabilitado.

A instalação não exigiu alteração manual do Registro do Windows para a configuração de setores NVMe, conforme informado pelo responsável. Esse resultado não demonstra, isoladamente, ausência de problemas de armazenamento em operações futuras.

### Valores observados ou selecionados no assistente

Os valores desta tabela são evidências do assistente de instalação. Não foram conferidos posteriormente por consultas SQL de configuração e não devem ser tratados como confirmação das configurações efetivas atuais.

| Item | Valor observado ou selecionado |
| --- | --- |
| Produto e instância | Microsoft SQL Server 2025 Express, 64 bits; `REQUISITAPLUS` |
| Autenticação | Windows |
| Conta administrativa | Usuário atual do Windows, sem identificação pessoal registrada |
| Conta virtual do serviço | `NT Service\MSSQL$REQUISITAPLUS` |
| SQL Server Browser | Desabilitado durante a instalação |
| Recursos opcionais do mecanismo | Somente Database Engine Services selecionado |
| Extensão do Azure e FILESTREAM | Desabilitados |
| Instâncias de usuário | Desabilitadas |
| Ordenação | `Latin1_General_CI_AS` |
| Diretório raiz da instância | Diretório local da instalação SQL Server; caminho específico omitido |
| Diretórios de dados, logs e tempdb | Configurados na unidade de dados, distinta da unidade do sistema |
| Memória mínima | 0 MB |
| Memória máxima | 1.410 MB |

### Evidências de serviço e conexão fornecidas pelo responsável

Os comandos abaixo são transcrições documentais de execuções já realizadas pelo responsável, não instruções executadas nesta atualização.

| Verificação | Comando informado | Resultado informado e alcance |
| --- | --- | --- |
| Estado do serviço | `Get-Service -Name 'MSSQL$REQUISITAPLUS'` | Serviço `SQL Server (REQUISITAPLUS)` com status `Running` no instante da consulta. |
| Conexão local e versão do mecanismo | `sqlcmd -S '.\REQUISITAPLUS' -E -Q "SELECT @@VERSION AS VersaoSQL;"` | Conexão local com autenticação Windows bem-sucedida e consulta somente de leitura executada com sucesso. Retorno: Microsoft SQL Server 2025 (RTM), versão `17.0.1000.7 (X64)`, Express Edition (64-bit). |

`17.0.1000.7` é a versão do mecanismo identificada por `@@VERSION`. A versão do instalador não foi informada e não deve ser inferida desse resultado. A conexão bem-sucedida confirma o funcionamento básico observado, sem comprovar diretórios efetivos, limite de memória, porta, protocolo, criptografia, validação de certificado ou permissões da futura aplicação.

### Armazenamento informado pelo responsável

| Unidade | Dispositivo e barramento | Disco físico | Setor lógico | Setor físico reportado |
| --- | --- | --- | --- | --- |
| Unidade do sistema | SSD Samsung NVMe | 1 | 512 bytes | 16.384 bytes |
| Unidade de dados | HD Western Digital SATA | 0 | 512 bytes | 4.096 bytes |

Conforme essas evidências, as unidades do sistema e de dados pertencem a discos físicos diferentes. Os diretórios do banco foram direcionados à unidade de dados e a instalação foi concluída. Isso não comprova, isoladamente, que todos os componentes e operações futuras estejam livres de problemas relacionados aos setores da unidade do sistema. Não foram registrados números de série ou identificadores pessoais.

## Limitações das consultas iniciais e da validação posterior

- Na verificação inicial, consultas CIM de sistema operacional, computador e serviços retornaram acesso negado. Não houve tentativa de elevação de privilégios. Informações de sistema e memória foram obtidas por outras consultas de leitura disponíveis.
- A consulta ao executável do Ollama em seu caminho padrão retornou acesso negado, e a CLI não foi localizada no PATH. A identificação do servidor e do modelo decorre das respostas HTTP de versão e catálogo.
- A execução de codex --version foi bloqueada pela política de scripts. A versão registrada provém dos metadados do pacote instalado; a política foi preservada.
- Não houve acesso negado nas consultas da verificação complementar do SQL Server. A conclusão permanece limitada aos serviços, registros e caminhos efetivamente consultados.
- Presença de ferramenta, arquivo, serviço ou modelo não equivale a teste funcional, integração validada ou desempenho satisfatório.
- A atualização posterior baseia-se no relato do responsável; não houve nova consulta nesta etapa. A validação SQL informada limitou-se à conexão local e à leitura de `@@VERSION`; não verificou leitura ou gravação de dados da aplicação nem integração com EF Core.

## Pendências técnicas e próximas ações

**Resolvido conforme evidências do responsável:** instalação da instância convencional `REQUISITAPLUS`, identificação de edição e versão do mecanismo, serviço observado em execução e conexão local Windows com leitura de versão bem-sucedida. O tipo de armazenamento e a separação física das unidades do sistema e de dados também foram informados. Essas evidências substituem a pendência de identificação da instância, preservando o histórico anterior.

As pendências abaixo permanecem abertas. Suas ações futuras dependem de aprovação humana conforme o escopo; este documento não autoriza sua execução.

1. **Configurações efetivas da instância:** conferir diretórios, ordenação, limites de memória, conta e permissões do serviço, estado do Browser e políticas de conexão, incluindo protocolo, porta, criptografia e certificado. Distinguir valores selecionados no assistente de configurações efetivamente consultadas.
2. **Bancos separados de desenvolvimento e testes:** definir e criar futuramente os bancos na instância escolhida, com autenticação e permissões apropriadas, isolamento e ausência de fallback para o banco principal ou LocalDB. A instalação não comprova criação desses bancos.
3. **Permissões da aplicação e de migrations:** definir e configurar futuramente privilégios mínimos e separar responsabilidades operacionais das alterações de esquema. A conta administrativa selecionada no assistente não constitui política de acesso da aplicação.
4. **Backup, retenção, restauração e conta operacional:** definir diretório protegido, separado dos arquivos ativos, ACL e permissões mínimas, conta operacional e responsabilidades. A política aprovada prevê backup completo diário e retenção de sete dias; sua configuração, execução e controle permanecem pendentes, assim como a restauração em destino separado para validar a recuperação.
5. **Memória e coexistência dos serviços:** avaliar consumo de RAM e execução simultânea de Windows, SQL Server, aplicação Web e Ollama. A medição inicial de 0,28 GiB disponíveis foi pontual; o limite de 1.410 MB selecionado para SQL não comprova capacidade de execução conjunta nem configuração efetiva verificada.
6. **EF Core e operações no banco:** validar futuramente integração com EF Core e operações de leitura e gravação em banco autorizado. A consulta de versão não comprova persistência, integridade ou funcionamento da aplicação; migrations e testes exigem autorização própria.
7. **Funcionamento real do Ollama e contexto:** validar futuramente o modelo local e sua compatibilidade com o contrato aprovado e com os limites de contexto de 4.096 tokens e saída de 1.024 tokens. A presença no catálogo não satisfaz essa validação; qualquer inferência exige autorização própria.
8. **Execução de ferramentas:** conferir execução das ferramentas EF Core e Codex quando necessário, sem alterar a política de execução do PowerShell. Distinguir a versão instalada da comprovação de funcionamento.
9. **Armazenamento em operações futuras:** conferir os destinos efetivos dos arquivos SQL e avaliar eventuais limitações relacionadas aos setores da unidade do sistema conforme a operação prevista. O êxito inicial com diretórios na unidade de dados não exige nem justifica automaticamente alteração do Registro.

## Estado do registro

O SQL Server 2025 Express convencional está instalado e possui validação inicial de serviço e conexão local conforme evidências fornecidas pelo responsável. Configurações efetivas, capacidade de execução integrada, persistência e demais validações funcionais permanecem pendentes. O documento preserva o diagnóstico inicial e não declara o ambiente integralmente validado ou pronto para integração.

Na criação original deste documento, a T001 ainda não havia sido marcada como concluída. Seu encerramento foi posteriormente aprovado e registrado em tasks.md em execução própria. Esta atualização documental não altera esse estado nem o de qualquer outra tarefa.

Somente docs/validacao/ambiente.md foi atualizado nesta etapa. tasks.md e os demais artefatos aprovados não foram modificados. Nenhum diagnóstico ou comando SQL foi repetido, a T002 não foi iniciada e o fluxo speckit-implement não foi executado. Não houve instalação, configuração, alteração de serviços, bancos ou Registro, inferência, build, teste, migration, commit, push ou mudança de branch nesta atualização. O registro segue para revisão humana; qualquer ação posterior depende de nova autorização.
