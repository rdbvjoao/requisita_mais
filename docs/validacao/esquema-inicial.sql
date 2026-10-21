IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [AspNetRoles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(100) NOT NULL,
    [UserName] nvarchar(150) NOT NULL,
    [NormalizedUserName] nvarchar(150) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [NormalizedEmail] nvarchar(150) NOT NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_AspNetUsers_Nome] CHECK (DATALENGTH([Nome]) <= 200 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Nome] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0)
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(128) NOT NULL,
    [ProviderKey] nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] int NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Requisito] (
    [IdRequisito] int NOT NULL IDENTITY,
    [IdUsuario] int NOT NULL,
    [Titulo] nvarchar(150) NOT NULL,
    [Descricao] nvarchar(max) NOT NULL,
    [Prioridade] nvarchar(5) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [VersaoTextual] bigint NOT NULL,
    [UltimaOrdemAnalise] bigint NOT NULL,
    [DataCadastroUtc] datetime2 NOT NULL,
    [DataAtualizacaoUtc] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Requisito] PRIMARY KEY ([IdRequisito]),
    CONSTRAINT [CK_Requisito_Descricao] CHECK (DATALENGTH([Descricao]) <= 10000 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Descricao] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0),
    CONSTRAINT [CK_Requisito_Prioridade] CHECK ((([Prioridade] COLLATE Latin1_General_100_BIN2 = N'Alta' AND DATALENGTH([Prioridade]) = 8) OR ([Prioridade] COLLATE Latin1_General_100_BIN2 = N'Média' AND DATALENGTH([Prioridade]) = 10) OR ([Prioridade] COLLATE Latin1_General_100_BIN2 = N'Baixa' AND DATALENGTH([Prioridade]) = 10))),
    CONSTRAINT [CK_Requisito_Status] CHECK ((([Status] COLLATE Latin1_General_100_BIN2 = N'Rascunho' AND DATALENGTH([Status]) = 16) OR ([Status] COLLATE Latin1_General_100_BIN2 = N'Analisado' AND DATALENGTH([Status]) = 18) OR ([Status] COLLATE Latin1_General_100_BIN2 = N'Revisado' AND DATALENGTH([Status]) = 16) OR ([Status] COLLATE Latin1_General_100_BIN2 = N'Arquivado' AND DATALENGTH([Status]) = 18))),
    CONSTRAINT [CK_Requisito_Titulo] CHECK (DATALENGTH([Titulo]) <= 300 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Titulo] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0),
    CONSTRAINT [CK_Requisito_UltimaOrdemAnalise] CHECK ([UltimaOrdemAnalise] >= 0),
    CONSTRAINT [CK_Requisito_VersaoTextual] CHECK ([VersaoTextual] > 0),
    CONSTRAINT [FK_Requisito_AspNetUsers_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OperacaoAnalise] (
    [IdOperacao] uniqueidentifier NOT NULL,
    [IdUsuario] int NOT NULL,
    [IdRequisito] int NOT NULL,
    [VersaoTextual] bigint NOT NULL,
    [Fingerprint] nvarchar(64) NOT NULL,
    [Estado] nvarchar(max) NOT NULL,
    [CriadaEmUtc] datetime2 NOT NULL,
    [EnvioRegistradoEmUtc] datetime2 NULL,
    [EncerradaEmUtc] datetime2 NULL,
    [PrazoExcedidoEmUtc] datetime2 NULL,
    [TerminoComprovado] bit NOT NULL DEFAULT CAST(0 AS bit),
    [MotivoFalha] nvarchar(max) NULL,
    [EvidenciaRecuperacao] nvarchar(max) NULL,
    [Modelo] nvarchar(max) NOT NULL,
    [TokensEntrada] int NULL,
    [TokensSaida] int NULL,
    [CustoApi] decimal(18,6) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_OperacaoAnalise] PRIMARY KEY ([IdOperacao]),
    CONSTRAINT [CK_OperacaoAnalise_Estado] CHECK ((([Estado] COLLATE Latin1_General_100_BIN2 = N'Recebida' AND DATALENGTH([Estado]) = 16) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'Enviada' AND DATALENGTH([Estado]) = 14) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'ResultadoDisponivel' AND DATALENGTH([Estado]) = 38) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'Persistida' AND DATALENGTH([Estado]) = 20) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'FalhaEncerrada' AND DATALENGTH([Estado]) = 28) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'Incerta' AND DATALENGTH([Estado]) = 14))),
    CONSTRAINT [CK_OperacaoAnalise_Fingerprint] CHECK (DATALENGTH([Fingerprint]) = 128 AND [Fingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9A-Fa-f]%'),
    CONSTRAINT [CK_OperacaoAnalise_IdOperacao] CHECK ([IdOperacao] <> '00000000-0000-0000-0000-000000000000'),
    CONSTRAINT [CK_OperacaoAnalise_Modelo] CHECK (DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Modelo] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0),
    CONSTRAINT [CK_OperacaoAnalise_TokensEntrada] CHECK ([TokensEntrada] IS NULL OR [TokensEntrada] >= 0),
    CONSTRAINT [CK_OperacaoAnalise_TokensSaida] CHECK ([TokensSaida] IS NULL OR [TokensSaida] >= 0),
    CONSTRAINT [CK_OperacaoAnalise_VersaoTextual] CHECK ([VersaoTextual] > 0),
    CONSTRAINT [FK_OperacaoAnalise_AspNetUsers_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OperacaoAnalise_Requisito_IdRequisito] FOREIGN KEY ([IdRequisito]) REFERENCES [Requisito] ([IdRequisito]) ON DELETE NO ACTION
);

CREATE TABLE [Analise] (
    [IdAnalise] int NOT NULL IDENTITY,
    [IdRequisito] int NOT NULL,
    [IdOperacao] uniqueidentifier NOT NULL,
    [VersaoTextualAnalisada] bigint NOT NULL,
    [TituloAnalisado] nvarchar(150) NOT NULL,
    [DescricaoAnalisada] nvarchar(max) NOT NULL,
    [DataAnaliseUtc] datetime2 NOT NULL,
    [PersistidaEmUtc] datetime2 NOT NULL,
    [OrdemPersistencia] bigint NOT NULL,
    [Modelo] nvarchar(max) NOT NULL,
    [DigestModelo] nvarchar(max) NULL,
    [VersaoPrompt] nvarchar(max) NULL,
    [TokensEntrada] int NULL,
    [TokensSaida] int NULL,
    [CustoApi] decimal(18,6) NOT NULL,
    [RevisaoConcluidaEmUtc] datetime2 NULL,
    CONSTRAINT [PK_Analise] PRIMARY KEY ([IdAnalise]),
    CONSTRAINT [CK_Analise_DescricaoAnalisada] CHECK (DATALENGTH([DescricaoAnalisada]) <= 10000 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([DescricaoAnalisada] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0),
    CONSTRAINT [CK_Analise_Modelo] CHECK (DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Modelo] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0),
    CONSTRAINT [CK_Analise_OrdemPersistencia] CHECK ([OrdemPersistencia] > 0),
    CONSTRAINT [CK_Analise_TituloAnalisado] CHECK (DATALENGTH([TituloAnalisado]) <= 300 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([TituloAnalisado] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0),
    CONSTRAINT [CK_Analise_TokensEntrada] CHECK ([TokensEntrada] IS NULL OR [TokensEntrada] >= 0),
    CONSTRAINT [CK_Analise_TokensSaida] CHECK ([TokensSaida] IS NULL OR [TokensSaida] >= 0),
    CONSTRAINT [CK_Analise_VersaoTextualAnalisada] CHECK ([VersaoTextualAnalisada] > 0),
    CONSTRAINT [FK_Analise_OperacaoAnalise_IdOperacao] FOREIGN KEY ([IdOperacao]) REFERENCES [OperacaoAnalise] ([IdOperacao]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Analise_Requisito_IdRequisito] FOREIGN KEY ([IdRequisito]) REFERENCES [Requisito] ([IdRequisito]) ON DELETE NO ACTION
);

CREATE TABLE [Sugestao] (
    [IdSugestao] int NOT NULL IDENTITY,
    [IdAnalise] int NOT NULL,
    [OrdemNoResultado] int NOT NULL,
    [TipoProblema] nvarchar(50) NOT NULL,
    [Explicacao] nvarchar(500) NOT NULL,
    [TextoSugerido] nvarchar(max) NULL,
    [Decisao] nvarchar(20) NOT NULL,
    [TextoAplicado] nvarchar(max) NULL,
    [DecididaEmUtc] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Sugestao] PRIMARY KEY ([IdSugestao]),
    CONSTRAINT [CK_Sugestao_Decisao] CHECK ((([Decisao] COLLATE Latin1_General_100_BIN2 = N'Pendente' AND DATALENGTH([Decisao]) = 16) OR ([Decisao] COLLATE Latin1_General_100_BIN2 = N'Aplicada' AND DATALENGTH([Decisao]) = 16) OR ([Decisao] COLLATE Latin1_General_100_BIN2 = N'Ajustada' AND DATALENGTH([Decisao]) = 16) OR ([Decisao] COLLATE Latin1_General_100_BIN2 = N'Descartada' AND DATALENGTH([Decisao]) = 20))),
    CONSTRAINT [CK_Sugestao_DecisaoTextoData] CHECK (([Decisao] = N'Pendente' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NULL) OR ([Decisao] = N'Descartada' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NOT NULL) OR ([Decisao] IN (N'Aplicada', N'Ajustada') AND [TextoAplicado] IS NOT NULL AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([TextoAplicado] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0 AND [DecididaEmUtc] IS NOT NULL)),
    CONSTRAINT [CK_Sugestao_Explicacao] CHECK (DATALENGTH([Explicacao]) <= 1000 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Explicacao] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0),
    CONSTRAINT [CK_Sugestao_OrdemNoResultado] CHECK ([OrdemNoResultado] BETWEEN 1 AND 5),
    CONSTRAINT [CK_Sugestao_TextoAplicado] CHECK ([TextoAplicado] IS NULL OR DATALENGTH([TextoAplicado]) <= 10000),
    CONSTRAINT [CK_Sugestao_TipoProblema] CHECK ((([TipoProblema] COLLATE Latin1_General_100_BIN2 = N'Ambiguidade' AND DATALENGTH([TipoProblema]) = 22) OR ([TipoProblema] COLLATE Latin1_General_100_BIN2 = N'Termo vago' AND DATALENGTH([TipoProblema]) = 20) OR ([TipoProblema] COLLATE Latin1_General_100_BIN2 = N'Falta de clareza' AND DATALENGTH([TipoProblema]) = 32) OR ([TipoProblema] COLLATE Latin1_General_100_BIN2 = N'Falta de especificidade' AND DATALENGTH([TipoProblema]) = 46))),
    CONSTRAINT [FK_Sugestao_Analise_IdAnalise] FOREIGN KEY ([IdAnalise]) REFERENCES [Analise] ([IdAnalise]) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX [IX_Analise_IdOperacao] ON [Analise] ([IdOperacao]);

CREATE UNIQUE INDEX [IX_Analise_IdRequisito_OrdemPersistencia] ON [Analise] ([IdRequisito], [OrdemPersistencia]);

CREATE INDEX [IX_Analise_IdRequisito_VersaoTextualAnalisada_OrdemPersistencia] ON [Analise] ([IdRequisito], [VersaoTextualAnalisada], [OrdemPersistencia] DESC);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE UNIQUE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]);

CREATE INDEX [IX_OperacaoAnalise_IdRequisito] ON [OperacaoAnalise] ([IdRequisito]);

CREATE INDEX [IX_OperacaoAnalise_IdUsuario] ON [OperacaoAnalise] ([IdUsuario]);

CREATE INDEX [IX_Requisito_IdUsuario_Status_IdRequisito] ON [Requisito] ([IdUsuario], [Status], [IdRequisito]);

CREATE UNIQUE INDEX [IX_Sugestao_IdAnalise_OrdemNoResultado] ON [Sugestao] ([IdAnalise], [OrdemNoResultado]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261019220059_InitialCreate', N'10.0.12');

COMMIT;
GO
