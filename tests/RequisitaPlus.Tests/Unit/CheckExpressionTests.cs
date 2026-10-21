using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using RequisitaPlus.Tests.Integration.SqlServer;
using RequisitaPlus.Web.Data;
using Xunit;

namespace RequisitaPlus.Tests.Unit;

public sealed class CheckExpressionTests
{
    // sys.check_constraints.definition captured in requisita-checks.txt.
    // Only physical export line wraps (including those inside tokens) were removed.
    [Theory]
    [InlineData("CK_Analise_DescricaoAnalisada", "Analise", """(datalength([DescricaoAnalisada])<=(10000) AND datalength(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(([DescricaoAnalisada]) collate Latin1_General_100_BIN2,nchar((9)),N''),nchar((10)),N''),nchar((11)),N''),nchar((12)),N''),nchar((13)),N''),nchar((32)),N''),nchar((133)),N''),nchar((160)),N''),nchar((5760)),N''),nchar((8192)),N''),nchar((8193)),N''),nchar((8194)),N''),nchar((8195)),N''),nchar((8196)),N''),nchar((8197)),N''),nchar((8198)),N''),nchar((8199)),N''),nchar((8200)),N''),nchar((8201)),N''),nchar((8202)),N''),nchar((8232)),N''),nchar((8233)),N''),nchar((8239)),N''),nchar((8287)),N''),nchar((12288)),N''))>(0))""")]
    [InlineData("CK_Analise_Modelo", "Analise", """(datalength(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(([Modelo]) collate Latin1_General_100_BIN2,nchar((9)),N''),nchar((10)),N''),nchar((11)),N''),nchar((12)),N''),nchar((13)),N''),nchar((32)),N''),nchar((133)),N''),nchar((160)),N''),nchar((5760)),N''),nchar((8192)),N''),nchar((8193)),N''),nchar((8194)),N''),nchar((8195)),N''),nchar((8196)),N''),nchar((8197)),N''),nchar((8198)),N''),nchar((8199)),N''),nchar((8200)),N''),nchar((8201)),N''),nchar((8202)),N''),nchar((8232)),N''),nchar((8233)),N''),nchar((8239)),N''),nchar((8287)),N''),nchar((12288)),N''))>(0))""")]
    [InlineData("CK_Analise_OrdemPersistencia", "Analise", """([OrdemPersistencia]>(0))""")]
    [InlineData("CK_Analise_TituloAnalisado", "Analise", """(datalength([TituloAnalisado])<=(300) AND datalength(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(([TituloAnalisado]) collate Latin1_General_100_BIN2,nchar((9)),N''),nchar((10)),N''),nchar((11)),N''),nchar((12)),N''),nchar((13)),N''),nchar((32)),N''),nchar((133)),N''),nchar((160)),N''),nchar((5760)),N''),nchar((8192)),N''),nchar((8193)),N''),nchar((8194)),N''),nchar((8195)),N''),nchar((8196)),N''),nchar((8197)),N''),nchar((8198)),N''),nchar((8199)),N''),nchar((8200)),N''),nchar((8201)),N''),nchar((8202)),N''),nchar((8232)),N''),nchar((8233)),N''),nchar((8239)),N''),nchar((8287)),N''),nchar((12288)),N''))>(0))""")]
    [InlineData("CK_Analise_TokensEntrada", "Analise", """([TokensEntrada] IS NULL OR [TokensEntrada]>=(0))""")]
    [InlineData("CK_Analise_TokensSaida", "Analise", """([TokensSaida] IS NULL OR [TokensSaida]>=(0))""")]
    [InlineData("CK_Analise_VersaoTextualAnalisada", "Analise", """([VersaoTextualAnalisada]>(0))""")]
    [InlineData("CK_AspNetUsers_Nome", "AspNetUsers", """(datalength([Nome])<=(200) AND datalength(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(([Nome]) collate Latin1_General_100_BIN2,nchar((9)),N''),nchar((10)),N''),nchar((11)),N''),nchar((12)),N''),nchar((13)),N''),nchar((32)),N''),nchar((133)),N''),nchar((160)),N''),nchar((5760)),N''),nchar((8192)),N''),nchar((8193)),N''),nchar((8194)),N''),nchar((8195)),N''),nchar((8196)),N''),nchar((8197)),N''),nchar((8198)),N''),nchar((8199)),N''),nchar((8200)),N''),nchar((8201)),N''),nchar((8202)),N''),nchar((8232)),N''),nchar((8233)),N''),nchar((8239)),N''),nchar((8287)),N''),nchar((12288)),N''))>(0))""")]
    [InlineData("CK_OperacaoAnalise_Estado", "OperacaoAnalise", """(([Estado]) collate Latin1_General_100_BIN2=N'Recebida' AND datalength([Estado])=(16) OR ([Estado]) collate Latin1_General_100_BIN2=N'Enviada' AND datalength([Estado])=(14) OR ([Estado]) collate Latin1_General_100_BIN2=N'ResultadoDisponivel' AND datalength([Estado])=(38) OR ([Estado]) collate Latin1_General_100_BIN2=N'Persistida' AND datalength([Estado])=(20) OR ([Estado]) collate Latin1_General_100_BIN2=N'FalhaEncerrada' AND datalength([Estado])=(28) OR ([Estado]) collate Latin1_General_100_BIN2=N'Incerta' AND datalength([Estado])=(14))""")]
    [InlineData("CK_OperacaoAnalise_Fingerprint", "OperacaoAnalise", """(datalength([Fingerprint])=(128) AND NOT ([Fingerprint]) collate Latin1_General_100_BIN2 like N'%[^0-9A-Fa-f]%')""")]
    [InlineData("CK_OperacaoAnalise_IdOperacao", "OperacaoAnalise", """([IdOperacao]<>'00000000-0000-0000-0000-000000000000')""")]
    [InlineData("CK_OperacaoAnalise_Modelo", "OperacaoAnalise", """(datalength(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(([Modelo]) collate Latin1_General_100_BIN2,nchar((9)),N''),nchar((10)),N''),nchar((11)),N''),nchar((12)),N''),nchar((13)),N''),nchar((32)),N''),nchar((133)),N''),nchar((160)),N''),nchar((5760)),N''),nchar((8192)),N''),nchar((8193)),N''),nchar((8194)),N''),nchar((8195)),N''),nchar((8196)),N''),nchar((8197)),N''),nchar((8198)),N''),nchar((8199)),N''),nchar((8200)),N''),nchar((8201)),N''),nchar((8202)),N''),nchar((8232)),N''),nchar((8233)),N''),nchar((8239)),N''),nchar((8287)),N''),nchar((12288)),N''))>(0))""")]
    [InlineData("CK_OperacaoAnalise_TokensEntrada", "OperacaoAnalise", """([TokensEntrada] IS NULL OR [TokensEntrada]>=(0))""")]
    [InlineData("CK_OperacaoAnalise_TokensSaida", "OperacaoAnalise", """([TokensSaida] IS NULL OR [TokensSaida]>=(0))""")]
    [InlineData("CK_OperacaoAnalise_VersaoTextual", "OperacaoAnalise", """([VersaoTextual]>(0))""")]
    [InlineData("CK_Requisito_Descricao", "Requisito", """(datalength([Descricao])<=(10000) AND datalength(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(([Descricao]) collate Latin1_General_100_BIN2,nchar((9)),N''),nchar((10)),N''),nchar((11)),N''),nchar((12)),N''),nchar((13)),N''),nchar((32)),N''),nchar((133)),N''),nchar((160)),N''),nchar((5760)),N''),nchar((8192)),N''),nchar((8193)),N''),nchar((8194)),N''),nchar((8195)),N''),nchar((8196)),N''),nchar((8197)),N''),nchar((8198)),N''),nchar((8199)),N''),nchar((8200)),N''),nchar((8201)),N''),nchar((8202)),N''),nchar((8232)),N''),nchar((8233)),N''),nchar((8239)),N''),nchar((8287)),N''),nchar((12288)),N''))>(0))""")]
    [InlineData("CK_Requisito_Prioridade", "Requisito", """(([Prioridade]) collate Latin1_General_100_BIN2=N'Alta' AND datalength([Prioridade])=(8) OR ([Prioridade]) collate Latin1_General_100_BIN2=N'Média' AND datalength([Prioridade])=(10) OR ([Prioridade]) collate Latin1_General_100_BIN2=N'Baixa' AND datalength([Prioridade])=(10))""")]
    [InlineData("CK_Requisito_Status", "Requisito", """(([Status]) collate Latin1_General_100_BIN2=N'Rascunho' AND datalength([Status])=(16) OR ([Status]) collate Latin1_General_100_BIN2=N'Analisado' AND datalength([Status])=(18) OR ([Status]) collate Latin1_General_100_BIN2=N'Revisado' AND datalength([Status])=(16) OR ([Status]) collate Latin1_General_100_BIN2=N'Arquivado' AND datalength([Status])=(18))""")]
    [InlineData("CK_Requisito_Titulo", "Requisito", """(datalength([Titulo])<=(300) AND datalength(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(([Titulo]) collate Latin1_General_100_BIN2,nchar((9)),N''),nchar((10)),N''),nchar((11)),N''),nchar((12)),N''),nchar((13)),N''),nchar((32)),N''),nchar((133)),N''),nchar((160)),N''),nchar((5760)),N''),nchar((8192)),N''),nchar((8193)),N''),nchar((8194)),N''),nchar((8195)),N''),nchar((8196)),N''),nchar((8197)),N''),nchar((8198)),N''),nchar((8199)),N''),nchar((8200)),N''),nchar((8201)),N''),nchar((8202)),N''),nchar((8232)),N''),nchar((8233)),N''),nchar((8239)),N''),nchar((8287)),N''),nchar((12288)),N''))>(0))""")]
    [InlineData("CK_Requisito_UltimaOrdemAnalise", "Requisito", """([UltimaOrdemAnalise]>=(0))""")]
    [InlineData("CK_Requisito_VersaoTextual", "Requisito", """([VersaoTextual]>(0))""")]
    [InlineData("CK_Sugestao_Decisao", "Sugestao", """(([Decisao]) collate Latin1_General_100_BIN2=N'Pendente' AND datalength([Decisao])=(16) OR ([Decisao]) collate Latin1_General_100_BIN2=N'Aplicada' AND datalength([Decisao])=(16) OR ([Decisao]) collate Latin1_General_100_BIN2=N'Ajustada' AND datalength([Decisao])=(16) OR ([Decisao]) collate Latin1_General_100_BIN2=N'Descartada' AND datalength([Decisao])=(20))""")]
    [InlineData("CK_Sugestao_DecisaoTextoData", "Sugestao", """([Decisao]=N'Pendente' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NULL OR [Decisao]=N'Descartada' AND [TextoAplicado] IS NULL AND [DecididaEmUtc] IS NOT NULL OR ([Decisao]=N'Ajustada' OR [Decisao]=N'Aplicada') AND [TextoAplicado] IS NOT NULL AND datalength(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(([TextoAplicado]) collate Latin1_General_100_BIN2,nchar((9)),N''),nchar((10)),N''),nchar((11)),N''),nchar((12)),N''),nchar((13)),N''),nchar((32)),N''),nchar((133)),N''),nchar((160)),N''),nchar((5760)),N''),nchar((8192)),N''),nchar((8193)),N''),nchar((8194)),N''),nchar((8195)),N''),nchar((8196)),N''),nchar((8197)),N''),nchar((8198)),N''),nchar((8199)),N''),nchar((8200)),N''),nchar((8201)),N''),nchar((8202)),N''),nchar((8232)),N''),nchar((8233)),N''),nchar((8239)),N''),nchar((8287)),N''),nchar((12288)),N''))>(0) AND [DecididaEmUtc] IS NOT NULL)""")]
    [InlineData("CK_Sugestao_Explicacao", "Sugestao", """(datalength([Explicacao])<=(1000) AND datalength(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(([Explicacao]) collate Latin1_General_100_BIN2,nchar((9)),N''),nchar((10)),N''),nchar((11)),N''),nchar((12)),N''),nchar((13)),N''),nchar((32)),N''),nchar((133)),N''),nchar((160)),N''),nchar((5760)),N''),nchar((8192)),N''),nchar((8193)),N''),nchar((8194)),N''),nchar((8195)),N''),nchar((8196)),N''),nchar((8197)),N''),nchar((8198)),N''),nchar((8199)),N''),nchar((8200)),N''),nchar((8201)),N''),nchar((8202)),N''),nchar((8232)),N''),nchar((8233)),N''),nchar((8239)),N''),nchar((8287)),N''),nchar((12288)),N''))>(0))""")]
    [InlineData("CK_Sugestao_OrdemNoResultado", "Sugestao", """([OrdemNoResultado]>=(1) AND [OrdemNoResultado]<=(5))""")]
    [InlineData("CK_Sugestao_TextoAplicado", "Sugestao", """([TextoAplicado] IS NULL OR datalength([TextoAplicado])<=(10000))""")]
    [InlineData("CK_Sugestao_TipoProblema", "Sugestao", """(([TipoProblema]) collate Latin1_General_100_BIN2=N'Ambiguidade' AND datalength([TipoProblema])=(22) OR ([TipoProblema]) collate Latin1_General_100_BIN2=N'Termo vago' AND datalength([TipoProblema])=(20) OR ([TipoProblema]) collate Latin1_General_100_BIN2=N'Falta de clareza' AND datalength([TipoProblema])=(32) OR ([TipoProblema]) collate Latin1_General_100_BIN2=N'Falta de especificidade' AND datalength([TipoProblema])=(46))""")]
    public void RealSqlServerDefinitionMatchesApprovedModel(string name, string table, string definition)
    {
        using var db = new ApplicationDbContextFactory().CreateDbContext([]);
        var entity = Assert.Single(db.GetService<IDesignTimeModel>().Model.GetEntityTypes(),
            entity => entity.GetTableName() == table);
        var check = Assert.Single(entity.GetCheckConstraints(), check => check.Name == name);

        Assert.Equal(CheckExpression.Canonical(check.Sql), CheckExpression.Canonical(definition));
    }

    [Theory]
    [InlineData("NOT ", "")]
    [InlineData("(128)", "(126)")]
    [InlineData("Latin1_General_100_BIN2", "Latin1_General_100_CI_AS")]
    [InlineData("%[^0-9A-Fa-f]%", "%[^0-9A-F]%")]
    public void PrefixNotLikePreservesNegationLengthCollationAndPattern(string before, string after)
    {
        const string definition = "(datalength([Fingerprint])=(128) AND NOT ([Fingerprint]) collate Latin1_General_100_BIN2 like N'%[^0-9A-Fa-f]%')";
        Assert.NotEqual(CheckExpression.Canonical(definition),
            CheckExpression.Canonical(definition.Replace(before, after, StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("len([Fingerprint]) = 128")]
    [InlineData("NOT unknown([Fingerprint]) LIKE N'x'")]
    public void UnknownFunctionsStillFailClosed(string definition)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CheckExpression.Canonical(definition));
        Assert.Equal("SQL_CHECK_FUNCTION_NOT_RECOGNIZED", exception.Message);
    }

    [Theory]
    [InlineData("NOT ([TokensEntrada] >= (0))")]
    [InlineData("NOT ([Fingerprint])")]
    [InlineData("NOT [like]")]
    [InlineData("NOT ([Fingerprint] LIKE N'x' OR [Estado] = N'Recebida')")]
    public void PrefixNotOutsideLikeStillFailsClosed(string definition) =>
        Assert.Throws<InvalidOperationException>(() => CheckExpression.Canonical(definition));
}
