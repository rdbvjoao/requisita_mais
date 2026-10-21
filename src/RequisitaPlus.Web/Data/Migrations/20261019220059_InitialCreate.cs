using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequisitaPlus.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.CheckConstraint("CK_AspNetUsers_Nome", "DATALENGTH([Nome]) <= 200 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Nome] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Requisito",
                columns: table => new
                {
                    IdRequisito = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Prioridade = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VersaoTextual = table.Column<long>(type: "bigint", nullable: false),
                    UltimaOrdemAnalise = table.Column<long>(type: "bigint", nullable: false),
                    DataCadastroUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAtualizacaoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisito", x => x.IdRequisito);
                    table.CheckConstraint("CK_Requisito_Descricao", "DATALENGTH([Descricao]) <= 10000 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Descricao] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0");
                    table.CheckConstraint("CK_Requisito_Prioridade", "(([Prioridade] COLLATE Latin1_General_100_BIN2 = N'Alta' AND DATALENGTH([Prioridade]) = 8) OR ([Prioridade] COLLATE Latin1_General_100_BIN2 = N'Média' AND DATALENGTH([Prioridade]) = 10) OR ([Prioridade] COLLATE Latin1_General_100_BIN2 = N'Baixa' AND DATALENGTH([Prioridade]) = 10))");
                    table.CheckConstraint("CK_Requisito_Status", "(([Status] COLLATE Latin1_General_100_BIN2 = N'Rascunho' AND DATALENGTH([Status]) = 16) OR ([Status] COLLATE Latin1_General_100_BIN2 = N'Analisado' AND DATALENGTH([Status]) = 18) OR ([Status] COLLATE Latin1_General_100_BIN2 = N'Revisado' AND DATALENGTH([Status]) = 16) OR ([Status] COLLATE Latin1_General_100_BIN2 = N'Arquivado' AND DATALENGTH([Status]) = 18))");
                    table.CheckConstraint("CK_Requisito_Titulo", "DATALENGTH([Titulo]) <= 300 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Titulo] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0");
                    table.CheckConstraint("CK_Requisito_UltimaOrdemAnalise", "[UltimaOrdemAnalise] >= 0");
                    table.CheckConstraint("CK_Requisito_VersaoTextual", "[VersaoTextual] > 0");
                    table.ForeignKey(
                        name: "FK_Requisito_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperacaoAnalise",
                columns: table => new
                {
                    IdOperacao = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    IdRequisito = table.Column<int>(type: "int", nullable: false),
                    VersaoTextual = table.Column<long>(type: "bigint", nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnvioRegistradoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EncerradaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrazoExcedidoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TerminoComprovado = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MotivoFalha = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenciaRecuperacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokensEntrada = table.Column<int>(type: "int", nullable: true),
                    TokensSaida = table.Column<int>(type: "int", nullable: true),
                    CustoApi = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacaoAnalise", x => x.IdOperacao);
                    table.CheckConstraint("CK_OperacaoAnalise_Estado", "(([Estado] COLLATE Latin1_General_100_BIN2 = N'Recebida' AND DATALENGTH([Estado]) = 16) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'Enviada' AND DATALENGTH([Estado]) = 14) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'ResultadoDisponivel' AND DATALENGTH([Estado]) = 38) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'Persistida' AND DATALENGTH([Estado]) = 20) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'FalhaEncerrada' AND DATALENGTH([Estado]) = 28) OR ([Estado] COLLATE Latin1_General_100_BIN2 = N'Incerta' AND DATALENGTH([Estado]) = 14))");
                    table.CheckConstraint("CK_OperacaoAnalise_Fingerprint", "DATALENGTH([Fingerprint]) = 128 AND [Fingerprint] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^0-9A-Fa-f]%'");
                    table.CheckConstraint("CK_OperacaoAnalise_IdOperacao", "[IdOperacao] <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_OperacaoAnalise_Modelo", "DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Modelo] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0");
                    table.CheckConstraint("CK_OperacaoAnalise_TokensEntrada", "[TokensEntrada] IS NULL OR [TokensEntrada] >= 0");
                    table.CheckConstraint("CK_OperacaoAnalise_TokensSaida", "[TokensSaida] IS NULL OR [TokensSaida] >= 0");
                    table.CheckConstraint("CK_OperacaoAnalise_VersaoTextual", "[VersaoTextual] > 0");
                    table.ForeignKey(
                        name: "FK_OperacaoAnalise_AspNetUsers_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperacaoAnalise_Requisito_IdRequisito",
                        column: x => x.IdRequisito,
                        principalTable: "Requisito",
                        principalColumn: "IdRequisito",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Analise",
                columns: table => new
                {
                    IdAnalise = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdRequisito = table.Column<int>(type: "int", nullable: false),
                    IdOperacao = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersaoTextualAnalisada = table.Column<long>(type: "bigint", nullable: false),
                    TituloAnalisado = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DescricaoAnalisada = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    DataAnaliseUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PersistidaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrdemPersistencia = table.Column<long>(type: "bigint", nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DigestModelo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VersaoPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokensEntrada = table.Column<int>(type: "int", nullable: true),
                    TokensSaida = table.Column<int>(type: "int", nullable: true),
                    CustoApi = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RevisaoConcluidaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analise", x => x.IdAnalise);
                    table.CheckConstraint("CK_Analise_DescricaoAnalisada", "DATALENGTH([DescricaoAnalisada]) <= 10000 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([DescricaoAnalisada] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0");
                    table.CheckConstraint("CK_Analise_Modelo", "DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Modelo] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0");
                    table.CheckConstraint("CK_Analise_OrdemPersistencia", "[OrdemPersistencia] > 0");
                    table.CheckConstraint("CK_Analise_TituloAnalisado", "DATALENGTH([TituloAnalisado]) <= 300 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([TituloAnalisado] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0");
                    table.CheckConstraint("CK_Analise_TokensEntrada", "[TokensEntrada] IS NULL OR [TokensEntrada] >= 0");
                    table.CheckConstraint("CK_Analise_TokensSaida", "[TokensSaida] IS NULL OR [TokensSaida] >= 0");
                    table.CheckConstraint("CK_Analise_VersaoTextualAnalisada", "[VersaoTextualAnalisada] > 0");
                    table.ForeignKey(
                        name: "FK_Analise_OperacaoAnalise_IdOperacao",
                        column: x => x.IdOperacao,
                        principalTable: "OperacaoAnalise",
                        principalColumn: "IdOperacao",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Analise_Requisito_IdRequisito",
                        column: x => x.IdRequisito,
                        principalTable: "Requisito",
                        principalColumn: "IdRequisito",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sugestao",
                columns: table => new
                {
                    IdSugestao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAnalise = table.Column<int>(type: "int", nullable: false),
                    OrdemNoResultado = table.Column<int>(type: "int", nullable: false),
                    TipoProblema = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Explicacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TextoSugerido = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Decisao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TextoAplicado = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    DecididaEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sugestao", x => x.IdSugestao);
                    table.CheckConstraint("CK_Sugestao_Decisao", "(([Decisao] COLLATE Latin1_General_100_BIN2 = N'Pendente' AND DATALENGTH([Decisao]) = 16) OR ([Decisao] COLLATE Latin1_General_100_BIN2 = N'Aplicada' AND DATALENGTH([Decisao]) = 16) OR ([Decisao] COLLATE Latin1_General_100_BIN2 = N'Ajustada' AND DATALENGTH([Decisao]) = 16) OR ([Decisao] COLLATE Latin1_General_100_BIN2 = N'Descartada' AND DATALENGTH([Decisao]) = 20))");
                    table.CheckConstraint("CK_Sugestao_DecisaoTextoData", "([Decisao] = N'Pendente' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NULL) OR ([Decisao] = N'Descartada' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NOT NULL) OR ([Decisao] IN (N'Aplicada', N'Ajustada') AND [TextoAplicado] IS NOT NULL AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([TextoAplicado] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0 AND [DecididaEmUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_Sugestao_Explicacao", "DATALENGTH([Explicacao]) <= 1000 AND DATALENGTH(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Explicacao] COLLATE Latin1_General_100_BIN2, NCHAR(9), N''), NCHAR(10), N''), NCHAR(11), N''), NCHAR(12), N''), NCHAR(13), N''), NCHAR(32), N''), NCHAR(133), N''), NCHAR(160), N''), NCHAR(5760), N''), NCHAR(8192), N''), NCHAR(8193), N''), NCHAR(8194), N''), NCHAR(8195), N''), NCHAR(8196), N''), NCHAR(8197), N''), NCHAR(8198), N''), NCHAR(8199), N''), NCHAR(8200), N''), NCHAR(8201), N''), NCHAR(8202), N''), NCHAR(8232), N''), NCHAR(8233), N''), NCHAR(8239), N''), NCHAR(8287), N''), NCHAR(12288), N'')) > 0");
                    table.CheckConstraint("CK_Sugestao_OrdemNoResultado", "[OrdemNoResultado] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_Sugestao_TextoAplicado", "[TextoAplicado] IS NULL OR DATALENGTH([TextoAplicado]) <= 10000");
                    table.CheckConstraint("CK_Sugestao_TipoProblema", "(([TipoProblema] COLLATE Latin1_General_100_BIN2 = N'Ambiguidade' AND DATALENGTH([TipoProblema]) = 22) OR ([TipoProblema] COLLATE Latin1_General_100_BIN2 = N'Termo vago' AND DATALENGTH([TipoProblema]) = 20) OR ([TipoProblema] COLLATE Latin1_General_100_BIN2 = N'Falta de clareza' AND DATALENGTH([TipoProblema]) = 32) OR ([TipoProblema] COLLATE Latin1_General_100_BIN2 = N'Falta de especificidade' AND DATALENGTH([TipoProblema]) = 46))");
                    table.ForeignKey(
                        name: "FK_Sugestao_Analise_IdAnalise",
                        column: x => x.IdAnalise,
                        principalTable: "Analise",
                        principalColumn: "IdAnalise",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Analise_IdOperacao",
                table: "Analise",
                column: "IdOperacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Analise_IdRequisito_OrdemPersistencia",
                table: "Analise",
                columns: new[] { "IdRequisito", "OrdemPersistencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Analise_IdRequisito_VersaoTextualAnalisada_OrdemPersistencia",
                table: "Analise",
                columns: new[] { "IdRequisito", "VersaoTextualAnalisada", "OrdemPersistencia" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperacaoAnalise_IdRequisito",
                table: "OperacaoAnalise",
                column: "IdRequisito");

            migrationBuilder.CreateIndex(
                name: "IX_OperacaoAnalise_IdUsuario",
                table: "OperacaoAnalise",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Requisito_IdUsuario_Status_IdRequisito",
                table: "Requisito",
                columns: new[] { "IdUsuario", "Status", "IdRequisito" });

            migrationBuilder.CreateIndex(
                name: "IX_Sugestao_IdAnalise_OrdemNoResultado",
                table: "Sugestao",
                columns: new[] { "IdAnalise", "OrdemNoResultado" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Sugestao");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Analise");

            migrationBuilder.DropTable(
                name: "OperacaoAnalise");

            migrationBuilder.DropTable(
                name: "Requisito");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
