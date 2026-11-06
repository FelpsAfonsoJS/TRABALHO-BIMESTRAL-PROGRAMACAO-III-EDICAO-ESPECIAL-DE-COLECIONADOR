using Microsoft.EntityFrameworkCore;
using QuestPDF;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using umfg.programacaoiii.minimalapi._2026.Contexto;
using umfg.programacaoiii.minimalapi._2026.DTO;
using umfg.programacaoiii.minimalapi._2026.Entidades;

namespace umfg.programacaoiii.minimalapi._2026;

public class Program
{    
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration["ConnectionString"] ?? string.Empty;
        builder.Services
            .AddDbContext<MySqlContexto>(options => options.UseMySQL(connectionString));
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        app.MapPost("/lembretes", async (Lembrete lembrete, MySqlContexto contexto) =>
        {
            await contexto.Lembrete.AddAsync(lembrete);
            await contexto.SaveChangesAsync();

            return Results.Created($"/lembretes/{lembrete.Id}", lembrete);
        });

        app.MapGet("/lembretes/{id}", async (string id, MySqlContexto contexto) =>
        {
            if (string.IsNullOrWhiteSpace(id))
                return Results.BadRequest("id invalido!");

            return await contexto.Lembrete.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id) && x.IsAtivo) 
            is Lembrete lembrete ? Results.Ok(lembrete) : Results.NotFound();
        });

        app.MapGet("/lembretes", async (MySqlContexto contexto) =>
        {
            return await contexto.Lembrete.Where(x => x.IsAtivo).ToListAsync();            
        });

        app.MapGet("/lembretes/filtro", async ([AsParameters] LembreteQueryDTO.LembreteQueryRequestDTO dto, MySqlContexto contexto) =>
        {
            if (dto.NumeroPagina <= 0 || dto.TamanhoPagina <= 0)
                return Results.BadRequest("Página e tamanho da página são obrigatórios.");

            var (lembretes, total) = await FiltrarLembretes(dto, contexto);

            var resultado = new LembreteQueryDTO.LembreteQueryResponseDTO()
            {
                Total = total,
                NumeroPagina = dto.NumeroPagina,
                TamanhoPagina = dto.TamanhoPagina,
                Lembretes = lembretes,
            };

            return Results.Ok(resultado);
        });


        Settings.License = LicenseType.Community;
        Settings.UseSystemFonts = true;
        FontManager.RegisterFontsFromDirectory(Path.Combine(AppContext.BaseDirectory, "Fontes"));

        app.MapGet("/lembretes/imprimir", async ([AsParameters] LembreteQueryDTO.LembreteQueryRequestDTO dto, MySqlContexto contexto) =>
        {
            if (dto.NumeroPagina <= 0 || dto.TamanhoPagina <= 0)
                return Results.BadRequest("Página e tamanho da página são obrigatórios.");

            var (lembretes, total) = await FiltrarLembretes(dto, contexto);

            return Results.File(GerarRelatorioPdf(dto, lembretes, total), "application/pdf", "relatorio-lembretes.pdf");
        });


        app.MapDelete("/lembretes/{id}", async (string id, MySqlContexto contexto) =>
        {
            var lembrete = await contexto.Lembrete.FindAsync(Guid.Parse(id));


            if (lembrete is null)
                return Results.NotFound();

            lembrete.SetAtivo(false);
            lembrete.Update();

            contexto.Lembrete.Update(lembrete);
            await contexto.SaveChangesAsync();

            return Results.NoContent();
        });

        app.MapPut("/lembretes/{id}", async (string id, LembreteDTO dto, MySqlContexto contexto) =>
        {
            var lembreteExistente = await contexto.Lembrete.FindAsync(Guid.Parse(id));

            if (lembreteExistente is null)
                return Results.NotFound();

            lembreteExistente.SetDescricao(dto.Descricao);
            lembreteExistente.Update();

            contexto.Lembrete.Update(lembreteExistente);
            await contexto.SaveChangesAsync();

            return Results.Ok(lembreteExistente);
        });


        app.UseHttpsRedirection();

        app.UseCors("Frontend");

        app.Run();

    }

    private static async Task<(List<Lembrete>, long)>FiltrarLembretes(
        LembreteQueryDTO.LembreteQueryRequestDTO dto, MySqlContexto contexto)
    {
        var query = contexto.Lembrete
                .AsNoTracking()
                .Where(x => x.IsAtivo);


        if (!string.IsNullOrWhiteSpace(dto.Descricao))
        {
            var termo = dto.Descricao.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            query = query.Where(x => EF.Functions.Like(x.Descricao, $"%{termo}%"));
        }

        if (dto.DataCadastroInicial > DateTime.MinValue)
            query = query.Where(x => x.DataCadastro >= dto.DataCadastroInicial);

        if (dto.DataCadastroFinal < DateTime.MaxValue)
            query = query.Where(x => x.DataCadastro < dto.DataCadastroFinal.Date.AddDays(1)); 

        if (dto.DataAtualizacaoInicial > DateTime.MinValue)
            query = query.Where(x => x.DataAtualizacao >= dto.DataAtualizacaoInicial);

        if (dto.DataAtualizacaoFinal < DateTime.MaxValue)
            query = query.Where(x => x.DataAtualizacao < dto.DataAtualizacaoFinal.Date.AddDays(1));

        if (dto.Direcao.ToUpper() != "ASC" && dto.Direcao.ToUpper() != "DESC")
            dto.Direcao = "DESC";

       
        var desc = dto.Direcao.ToUpper() == "DESC";
        query = dto.Ordem switch
        {
            "Descricao" => desc ? query.OrderByDescending(x => x.Descricao) : query.OrderBy(x => x.Descricao),
            "DataAtualizacao" => desc ? query.OrderByDescending(x => x.DataAtualizacao) : query.OrderBy(x => x.DataAtualizacao),
            "IsAtivo" => desc ? query.OrderByDescending(x => x.IsAtivo) : query.OrderBy(x => x.IsAtivo),
            _ => desc ? query.OrderByDescending(x => x.DataCadastro) : query.OrderBy(x => x.DataCadastro),
        };

  
        var pular = (dto.NumeroPagina - 1) * dto.TamanhoPagina;
        var lembretes = await query.Skip(pular).Take(dto.TamanhoPagina).ToListAsync();
        var total = await query.CountAsync();

        return (lembretes, total);
    }

   
    private const string C_COR_FUNDO = "#2B3024";
    private const string C_COR_ESCURO = "#14170F";
    private const string C_COR_VGUI = "#4C5844";
    private const string C_COR_VGUI_DESTAQUE = "#56634C";
    private const string C_COR_CAMPO = "#3E4637";
    private const string C_COR_CAMPO_2 = "#383F32";
    private const string C_COR_RELEVO_CLARO = "#889180";
    private const string C_COR_RELEVO_ESCURO = "#282E22";
    private const string C_COR_TEXTO = "#DEDFD6";
    private const string C_COR_TEXTO_CLARO = "#F0EEE4";
    private const string C_COR_TEXTO_SUAVE = "#A0AA95";
    private const string C_COR_DOURADO = "#C4B550";
    private const string C_COR_HUD = "#FFA000";
    private const string C_COR_MIRA = "#4CFF4C";
    private const string C_COR_PERIGO = "#FF7A5C";

    private const string C_FONTE_TITULO = "Stencil";
    private const string C_FONTE_TEXTO = "Tahoma";
    private const string C_FONTE_HUD = "Lucida Console";
    private const string C_FONTE_SIMBOLOS = "Segoe UI Symbol";

  
    private const string C_EMBLEMA_SVG = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">
            <rect x="2" y="2" width="60" height="60" fill="#14170F" stroke="#C4B550" stroke-width="2"/>
            <rect x="30" y="8" width="4" height="16" fill="#4CFF4C"/>
            <rect x="30" y="40" width="4" height="16" fill="#4CFF4C"/>
            <rect x="8" y="30" width="16" height="4" fill="#4CFF4C"/>
            <rect x="40" y="30" width="16" height="4" fill="#4CFF4C"/>
        </svg>
        """;

    private static byte[] GerarRelatorioPdf(
        LembreteQueryDTO.LembreteQueryRequestDTO dto, List<Lembrete> lembretes, long total)
    {
        var geradoEm = DateTime.Now.ToString("dd/MM/yyyy 'às' HH:mm");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(C_COR_FUNDO);
                page.DefaultTextStyle(x => x.FontFamily(C_FONTE_TEXTO, C_FONTE_SIMBOLOS).FontSize(9).FontColor(C_COR_TEXTO));

               
                page.Header()
                    .Background(C_COR_ESCURO)
                    .Border(1).BorderColor(C_COR_DOURADO)
                    .Padding(10)
                    .Row(linha =>
                    {
                        linha.ConstantItem(42).Height(42).Svg(C_EMBLEMA_SVG);

                        linha.RelativeItem().PaddingLeft(12).AlignMiddle().Column(titulo =>
                        {
                            titulo.Item().Text("LEMBRETES DA MISSÃO")
                                .FontFamily(C_FONTE_TITULO).FontSize(22).LetterSpacing(0.05f).FontColor(C_COR_TEXTO_CLARO);

                            titulo.Item().PaddingTop(2).Text("RELATÓRIO DE MISSÕES · PLANEJE A RODADA ANTES QUE A BOMBA EXPLODA")
                                .FontFamily(C_FONTE_HUD).FontSize(7).FontColor(C_COR_HUD);
                        });

                        linha.AutoItem().AlignBottom().AlignRight().Text($"Gerado em {geradoEm}")
                            .FontSize(8).FontColor(C_COR_TEXTO_SUAVE);
                    });

                page.Content().PaddingVertical(14).Column(conteudo =>
                {
                    conteudo.Spacing(12);

                    
                    conteudo.Item().Row(resumo =>
                    {
                        resumo.Spacing(8);

                        Indicador(resumo.RelativeItem(), "Total encontrado", total.ToString());
                        Indicador(resumo.RelativeItem(), "Nesta folha", lembretes.Count.ToString());
                        Indicador(resumo.RelativeItem(), "Página da listagem", dto.NumeroPagina.ToString());
                        Indicador(resumo.RelativeItem(2), "Ordenação", DescreverOrdenacao(dto));
                    });

                    conteudo.Item().Text(texto =>
                    {
                        texto.Span("■ ").FontColor(C_COR_DOURADO);
                        texto.Span("Filtros: ").Bold().FontColor(C_COR_DOURADO);
                        texto.Span(DescreverFiltros(dto)).FontColor(C_COR_TEXTO_SUAVE);
                    });

                    
                    conteudo.Item().Element(c => Relevo(c, elevado: true)).Background(C_COR_VGUI).Padding(6).Column(janela =>
                    {
                        janela.Item().PaddingBottom(6).Text(texto =>
                        {
                            texto.Span("■ ").FontColor(C_COR_DOURADO);
                            texto.Span("Lembretes").Bold().FontSize(10);
                        });

                        janela.Item().Element(c => Relevo(c, elevado: false)).Background(C_COR_CAMPO).Table(tabela =>
                        {
                            tabela.ColumnsDefinition(colunas =>
                            {
                                colunas.RelativeColumn();
                                colunas.ConstantColumn(96);
                                colunas.ConstantColumn(96);
                                colunas.ConstantColumn(58);
                            });

                            var colunaOrdenada = ColunaOrdenada(dto);
                            var desc = dto.Direcao.ToUpper() != "ASC";

                            tabela.Header(header =>
                            {
                                var colunasCabecalho = new[]
                                {
                                    ("Descrição", "Descricao"),
                                    ("Cadastro", "DataCadastro"),
                                    ("Atualização", "DataAtualizacao"),
                                    ("Status", "IsAtivo"),
                                };

                                foreach (var (titulo, campo) in colunasCabecalho)
                                {
                               
                                    var ordenada = campo == colunaOrdenada;

                                    header.Cell()
                                        .Element(c => Relevo(c, elevado: true))
                                        .Background(ordenada ? C_COR_VGUI_DESTAQUE : C_COR_VGUI)
                                        .PaddingVertical(5).PaddingHorizontal(7)
                                        .Text(texto =>
                                        {
                                            texto.Span(titulo).Bold().FontColor(ordenada ? C_COR_DOURADO : C_COR_TEXTO);

                                            if (ordenada)
                                                texto.Span(desc ? "  ▼" : "  ▲").FontSize(7).FontColor(C_COR_HUD);
                                        });
                                }
                            });

                            var indice = 0;

                            foreach (var lembrete in lembretes)
                            {
                                var fundo = indice++ % 2 == 0 ? C_COR_CAMPO : C_COR_CAMPO_2;

                                IContainer Celula() => tabela.Cell()
                                    .Background(fundo)
                                    .BorderBottom(0.5f).BorderColor(C_COR_RELEVO_ESCURO)
                                    .PaddingVertical(6).PaddingHorizontal(7)
                                    .AlignMiddle();

                                Celula().Text(lembrete.Descricao);
                                Celula().Text(lembrete.DataCadastro.ToString("dd/MM/yyyy HH:mm")).FontFamily(C_FONTE_HUD).FontSize(8).FontColor(C_COR_TEXTO_SUAVE);
                                Celula().Text(lembrete.DataAtualizacao.ToString("dd/MM/yyyy HH:mm")).FontFamily(C_FONTE_HUD).FontSize(8).FontColor(C_COR_TEXTO_SUAVE);
                                Status(Celula(), lembrete.IsAtivo);
                            }
                        });

                        if (lembretes.Count == 0)
                        {
                            janela.Item().PaddingVertical(20).AlignCenter()
                                .Text("Nenhum lembrete encontrado.")
                                .Bold().FontSize(11);
                        }
                    });
                });

                page.Footer().BorderTop(1).BorderColor(C_COR_RELEVO_CLARO).PaddingTop(6).Row(rodape =>
                {
                    rodape.RelativeItem().Text("UMFG · PROGRAMAÇÃO III · COUNTER-STRIKE 1.6")
                        .Bold().FontSize(7).LetterSpacing(0.08f).FontColor(C_COR_TEXTO_SUAVE);

                    rodape.RelativeItem().AlignRight().Text(texto =>
                    {
                        texto.DefaultTextStyle(x => x.FontFamily(C_FONTE_HUD).FontSize(8).FontColor(C_COR_HUD));
                        texto.Span("FOLHA ");
                        texto.CurrentPageNumber();
                        texto.Span(" DE ");
                        texto.TotalPages();
                    });
                });
            });
        }).GeneratePdf();
    }

    private static IContainer Relevo(IContainer container, bool elevado)
    {
        var claro = elevado ? C_COR_RELEVO_CLARO : C_COR_RELEVO_ESCURO;
        var escuro = elevado ? C_COR_RELEVO_ESCURO : C_COR_RELEVO_CLARO;

        return container
            .BorderTop(1).BorderLeft(1).BorderColor(claro)
            .Padding(0)
            .BorderBottom(1).BorderRight(1).BorderColor(escuro);
    }

    private static void Indicador(IContainer container, string rotulo, string valor)
    {
        container
            .Element(c => Relevo(c, elevado: false))
            .Background(C_COR_ESCURO)
            .PaddingVertical(6).PaddingHorizontal(9)
            .Column(coluna =>
            {
                coluna.Item().Text(rotulo)
                    .Bold().FontSize(7).FontColor(C_COR_TEXTO_SUAVE);

                coluna.Item().PaddingTop(2).Text(valor)
                    .FontFamily(C_FONTE_HUD).FontSize(13).FontColor(C_COR_HUD);
            });
    }

    private static void Status(IContainer container, bool isAtivo)
    {
        var cor = isAtivo ? C_COR_MIRA : C_COR_PERIGO;

        container
            .AlignLeft()
            .Element(c => Relevo(c, elevado: false))
            .Background(C_COR_ESCURO)
            .PaddingVertical(1).PaddingHorizontal(5)
            .Text(texto =>
            {
                texto.Span("■ ").FontSize(6).FontColor(cor);
                texto.Span(isAtivo ? "ATIVO" : "INATIVO").Bold().FontSize(7).FontColor(cor);
            });
    }

    private static string ColunaOrdenada(LembreteQueryDTO.LembreteQueryRequestDTO dto) => dto.Ordem switch
    {
        "Descricao" or "DataAtualizacao" or "IsAtivo" => dto.Ordem,
        _ => "DataCadastro",
    };

    private static string DescreverOrdenacao(LembreteQueryDTO.LembreteQueryRequestDTO dto)
    {
        var campo = dto.Ordem switch
        {
            "Descricao" => "Descrição",
            "DataAtualizacao" => "Atualização",
            "IsAtivo" => "Status",
            _ => "Cadastro",
        };

        return $"{campo} {(dto.Direcao.ToUpper() == "ASC" ? "cresc." : "decresc.")}";
    }

    private static string DescreverFiltros(LembreteQueryDTO.LembreteQueryRequestDTO dto)
    {
        static bool Informada(DateTime data) => data.Year > 1900 && data.Year < 2999;

        static string Intervalo(string nome, DateTime inicial, DateTime final)
        {
            if (!Informada(inicial) && !Informada(final))
                return string.Empty;

            if (!Informada(final))
                return $"{nome} a partir de {inicial:dd/MM/yyyy}";

            if (!Informada(inicial))
                return $"{nome} até {final:dd/MM/yyyy}";

            return $"{nome} de {inicial:dd/MM/yyyy} até {final:dd/MM/yyyy}";
        }

        var filtros = new List<string>();

        if (!string.IsNullOrWhiteSpace(dto.Descricao))
            filtros.Add($"descrição contém \"{dto.Descricao}\"");

        filtros.Add(Intervalo("cadastro", dto.DataCadastroInicial, dto.DataCadastroFinal));
        filtros.Add(Intervalo("atualização", dto.DataAtualizacaoInicial, dto.DataAtualizacaoFinal));
        filtros.RemoveAll(string.IsNullOrEmpty);

        return filtros.Count == 0 ? "nenhum filtro aplicado" : string.Join(" · ", filtros);
    }
}