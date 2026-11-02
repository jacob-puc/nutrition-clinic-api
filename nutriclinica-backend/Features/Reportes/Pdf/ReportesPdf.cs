using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using nutriclinica_backend.Core.Entities;
using nutriclinica_backend.Core.Enums;
using nutriclinica_backend.Core.Utils;
using nutriclinica_backend.Features.Antropometria.DTOs;
using nutriclinica_backend.Features.Consultas.DTOs;
using nutriclinica_backend.Features.Menus.DTOs;
using nutriclinica_backend.Features.Pacientes.DTOs;

namespace nutriclinica_backend.Features.Reportes.Pdf;

/// <summary>
/// Documentos imprimibles para el paciente: su menu semanal y su historial
/// clinico. Misma linea visual que el recibo y la factura (TransaccionesPdf):
/// encabezado con los datos de la clinica, titulo del documento a la
/// derecha, acento teal y pie con la fecha de generacion. Todas las horas en
/// hora de Mexico (ZonaClinica).
/// </summary>
public static class ReportesPdf
{
    private static readonly string Acento = Colors.Teal.Darken2;
    private static readonly string Suave = Colors.Teal.Lighten5;
    private static readonly string Tenue = Colors.Grey.Darken1;
    private static readonly string Linea = Colors.Grey.Lighten2;

    private static readonly DiaSemana[] Dias =
    [
        DiaSemana.Lunes, DiaSemana.Martes, DiaSemana.Miercoles, DiaSemana.Jueves,
        DiaSemana.Viernes, DiaSemana.Sabado, DiaSemana.Domingo
    ];

    private static readonly TiempoComida[] Tiempos =
    [
        TiempoComida.Desayuno, TiempoComida.MediaManana, TiempoComida.Almuerzo,
        TiempoComida.Merienda, TiempoComida.Cena, TiempoComida.Colacion
    ];

    private static string EtiquetaDia(DiaSemana dia) => dia switch
    {
        DiaSemana.Miercoles => "Miércoles",
        DiaSemana.Sabado => "Sábado",
        _ => dia.ToString()
    };

    private static string EtiquetaTiempo(TiempoComida tiempo) => tiempo switch
    {
        TiempoComida.MediaManana => "Media mañana",
        TiempoComida.Colacion => "Colación",
        _ => tiempo.ToString()
    };

    private static string Kcal(decimal valor) => $"{valor.ToString("N0", ZonaClinica.Cultura)} kcal";

    private static string Num(decimal valor) => valor.ToString("0.#", ZonaClinica.Cultura);

    /* ==================== MENU DEL PACIENTE ==================== */

    /// <summary>
    /// Menu semanal en horizontal: una cuadricula con los dias como columnas y
    /// los tiempos de comida como filas, para leer la semana de un vistazo (y
    /// pegarla en el refri). Cada plato lleva su desglose de ingredientes con
    /// los gramos ya multiplicados por las porciones. Un dia sin platos no
    /// desaparece: su columna queda marcada como "Día libre".
    /// </summary>
    public static byte[] MenuPaciente(
        MenuAsignadoRespuestaDto menu,
        IReadOnlyDictionary<Guid, IReadOnlyList<(string Alimento, decimal Gramos)>> ingredientesPorPlato,
        DatosFiscalesClinica? clinica)
    {
        var diasConPlatos = Dias.Where(d => menu.Detalles.Any(x => x.DiaSemana == d)).ToList();
        var promedio = diasConPlatos.Count == 0
            ? 0
            : menu.Detalles.Sum(d => d.Calorias) / diasConPlatos.Count;

        /* Solo las filas de tiempos que se usan en la semana: una colacion
           vacia los siete dias no ocupa espacio. */
        var tiempos = Tiempos.Where(t => menu.Detalles.Any(d => d.TiempoComida == t)).ToList();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                ConfigurarPagina(page);
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.Header().Element(c => Encabezado(c, clinica, "MENÚ SEMANAL", menu.PacienteNombre));

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Background(Suave).PaddingVertical(7).PaddingHorizontal(10).Row(row =>
                    {
                        Dato(row.RelativeItem(2), "Plan", menu.Nombre);
                        Dato(row.RelativeItem(), "Desde", menu.FechaInicio.ToString("dd/MM/yyyy", ZonaClinica.Cultura));
                        Dato(row.RelativeItem(), "Objetivo diario",
                            menu.ObjetivoCalorico.HasValue ? Kcal(menu.ObjetivoCalorico.Value) : "—");
                        Dato(row.RelativeItem(), "Promedio por día", promedio > 0 ? Kcal(promedio) : "—");
                        if (!string.IsNullOrWhiteSpace(menu.Descripcion))
                        {
                            Dato(row.RelativeItem(2), "Indicaciones generales", menu.Descripcion!);
                        }
                    });

                    if (tiempos.Count == 0)
                    {
                        col.Item().PaddingTop(20).AlignCenter()
                            .Text("Este plan todavía no tiene platos asignados.").FontColor(Tenue);
                        return;
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(64);
                            foreach (var _ in Dias) c.RelativeColumn();
                        });

                        /* Encabezado: dia y su total. Se repite en cada hoja si la
                           semana no cabe en una. */
                        table.Header(h =>
                        {
                            h.Cell().Element(CeldaEncabezado).Text("");
                            foreach (var dia in Dias)
                            {
                                var libre = !diasConPlatos.Contains(dia);
                                var total = menu.Detalles.Where(d => d.DiaSemana == dia).Sum(d => d.Calorias);
                                h.Cell().Element(c => CeldaEncabezadoBase(c, libre)).Column(c =>
                                {
                                    c.Item().AlignCenter().Text(EtiquetaDia(dia)).Bold()
                                        .FontColor(libre ? Tenue : Colors.White);
                                    c.Item().AlignCenter().Text(libre ? "Día libre" : Kcal(total)).FontSize(7.5f)
                                        .FontColor(libre ? Tenue : Colors.Teal.Lighten4);
                                });
                            }
                        });

                        for (var fila = 0; fila < tiempos.Count; fila++)
                        {
                            var tiempo = tiempos[fila];
                            var filaTabla = (uint)fila + 1;

                            table.Cell().Row(filaTabla).Column(1).Element(CeldaTiempo)
                                .Text(EtiquetaTiempo(tiempo)).SemiBold().FontColor(Acento);

                            for (var i = 0; i < Dias.Length; i++)
                            {
                                var dia = Dias[i];
                                var columna = (uint)i + 2;

                                /* Dia libre: una sola celda que ocupa toda la columna. */
                                if (!diasConPlatos.Contains(dia))
                                {
                                    if (fila == 0)
                                    {
                                        table.Cell().Row(1).Column(columna).RowSpan((uint)tiempos.Count)
                                            .Background(Colors.Grey.Lighten4).Border(0.5f).BorderColor(Linea)
                                            .AlignCenter().AlignMiddle().Padding(6).Column(c =>
                                            {
                                                c.Item().AlignCenter().Text("Día libre").SemiBold().FontColor(Tenue);
                                                c.Item().AlignCenter().Text("Sin alimentos asignados")
                                                    .FontSize(7).FontColor(Tenue);
                                            });
                                    }
                                    continue;
                                }

                                var platos = menu.Detalles
                                    .Where(d => d.DiaSemana == dia && d.TiempoComida == tiempo)
                                    .OrderBy(d => d.Orden)
                                    .ToList();

                                table.Cell().Row(filaTabla).Column(columna).Element(CeldaPlato).Column(c =>
                                {
                                    if (platos.Count == 0)
                                    {
                                        c.Item().AlignCenter().Text("—").FontColor(Colors.Grey.Lighten1);
                                        return;
                                    }

                                    c.Spacing(5);
                                    foreach (var p in platos)
                                    {
                                        c.Item().Column(plato =>
                                        {
                                            plato.Item().Text(t =>
                                            {
                                                t.Span(p.PlatoNombre).SemiBold().FontSize(8);
                                                if (p.Cantidad != 1)
                                                {
                                                    t.Span($" ×{Num(p.Cantidad)}").FontSize(8).FontColor(Acento);
                                                }
                                            });

                                            /* Desglose: gramos por porcion × porciones. */
                                            if (ingredientesPorPlato.TryGetValue(p.PlatoId, out var ingredientes))
                                            {
                                                foreach (var (alimento, gramos) in ingredientes)
                                                {
                                                    plato.Item().Text($"· {alimento} {Num(gramos * p.Cantidad)} g")
                                                        .FontSize(7).FontColor(Colors.Grey.Darken2);
                                                }
                                            }

                                            if (!string.IsNullOrWhiteSpace(p.Indicaciones))
                                            {
                                                plato.Item().Text(p.Indicaciones).FontSize(7).Italic()
                                                    .FontColor(Colors.Orange.Darken3);
                                            }

                                            plato.Item().Text(Kcal(p.Calorias)).FontSize(6.5f).FontColor(Tenue);
                                        });
                                    }
                                });
                            }
                        }
                    });
                });

                page.Footer().Element(c => Pie(c,
                    "Cantidades por comida, ya multiplicadas por las porciones. Ante cualquier duda, consulta a tu nutricionista antes de hacer cambios."));
            });
        }).GeneratePdf();

        static IContainer CeldaEncabezado(IContainer c) => CeldaEncabezadoBase(c, false);

        static IContainer CeldaEncabezadoBase(IContainer c, bool libre) =>
            c.Background(libre ? Colors.Grey.Lighten3 : Colors.Teal.Darken2)
                .Border(0.5f).BorderColor(Colors.White).PaddingVertical(4).PaddingHorizontal(3);

        static IContainer CeldaTiempo(IContainer c) =>
            c.Background(Colors.Teal.Lighten5).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                .PaddingHorizontal(5).PaddingVertical(5).AlignMiddle();

        static IContainer CeldaPlato(IContainer c) =>
            c.Border(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingHorizontal(4).PaddingVertical(5);
    }

    /* ==================== HISTORIAL CLINICO ==================== */

    public static byte[] HistorialClinico(
        PacienteRespuestaDto paciente,
        ExpedienteCompletoDto expediente,
        IReadOnlyList<ConsultaRespuestaDto> consultas,
        MenuAsignadoRespuestaDto? planVigente,
        DatosFiscalesClinica? clinica)
    {
        var medidas = expediente.MedidasAntropometricas.OrderBy(m => m.FechaMedicion).ToList();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                ConfigurarPagina(page);
                page.Header().Element(c => Encabezado(c, clinica, "HISTORIAL CLÍNICO", paciente.NombreCompleto));

                page.Content().PaddingVertical(14).Column(col =>
                {
                    col.Spacing(14);

                    /* ---------- Datos del paciente ---------- */
                    col.Item().Element(c => Seccion(c, "Datos del paciente", s =>
                    {
                        s.Item().Row(row =>
                        {
                            Dato(row.RelativeItem(), "Edad", paciente.Edad.HasValue ? $"{paciente.Edad} años" : "—");
                            Dato(row.RelativeItem(), "Sexo", paciente.Sexo switch
                            {
                                Sexo.M => "Masculino",
                                Sexo.F => "Femenino",
                                _ => "Otro"
                            });
                            Dato(row.RelativeItem(), "Nacimiento",
                                paciente.FechaNacimiento?.ToString("dd/MM/yyyy", ZonaClinica.Cultura) ?? "—");
                            Dato(row.RelativeItem(), "Paciente desde", ZonaClinica.Fecha(paciente.FechaRegistro));
                        });
                        s.Item().Row(row =>
                        {
                            Dato(row.RelativeItem(), "Teléfono", paciente.Telefono);
                            Dato(row.RelativeItem(2), "Correo", paciente.CorreoElectronico);
                            Dato(row.RelativeItem(), "Objetivo",
                                string.IsNullOrWhiteSpace(paciente.TituloObjetivo) ? "—" : paciente.TituloObjetivo!);
                        });
                        if (paciente.PesoObjetivo.HasValue)
                        {
                            s.Item().Text(t =>
                            {
                                t.Span("Peso objetivo: ").FontColor(Tenue);
                                t.Span($"{Num(paciente.PesoObjetivo.Value)} kg").Bold();
                            });
                        }
                    }));

                    /* ---------- Alergias y preferencias ---------- */
                    var historial = expediente.HistorialClinico;
                    col.Item().Element(c => Seccion(c, "Alergias y preferencias", s =>
                    {
                        Lista(s, "Alergias", historial?.Alergias, Colors.Red.Darken2);
                        Lista(s, "Le gusta", historial?.AlimentosFavoritos, null);
                        Lista(s, "No le gusta", historial?.AlimentosNoFavoritos, null);
                    }));

                    /* ---------- Medidas ---------- */
                    col.Item().Element(c => Seccion(c, "Evolución antropométrica", s =>
                    {
                        if (medidas.Count == 0)
                        {
                            s.Item().Text("Sin mediciones registradas.").FontColor(Tenue);
                            return;
                        }

                        var primera = medidas[0];
                        var ultima = medidas[^1];
                        if (medidas.Count > 1)
                        {
                            var cambio = ultima.Peso - primera.Peso;
                            s.Item().Text(t =>
                            {
                                t.Span("Peso: ").FontColor(Tenue);
                                t.Span($"{Num(primera.Peso)} → {Num(ultima.Peso)} kg").Bold();
                                t.Span($"  ({(cambio > 0 ? "+" : cambio < 0 ? "−" : "")}{Num(Math.Abs(cambio))} kg entre el " +
                                       $"{ZonaClinica.Fecha(primera.FechaMedicion)} y el {ZonaClinica.Fecha(ultima.FechaMedicion)})")
                                    .FontColor(Tenue);
                            });
                        }

                        s.Item().Element(c => TablaMedidas(c, medidas));
                    }));

                    /* ---------- Plan vigente ---------- */
                    if (planVigente is not null)
                    {
                        col.Item().Element(c => Seccion(c, "Plan nutricional vigente", s =>
                        {
                            s.Item().Row(row =>
                            {
                                Dato(row.RelativeItem(2), "Plan", planVigente.Nombre);
                                Dato(row.RelativeItem(), "Estado", planVigente.Estado.ToString());
                                Dato(row.RelativeItem(), "Desde",
                                    planVigente.FechaInicio.ToString("dd/MM/yyyy", ZonaClinica.Cultura));
                                Dato(row.RelativeItem(), "Objetivo",
                                    planVigente.ObjetivoCalorico.HasValue ? Kcal(planVigente.ObjetivoCalorico.Value) : "—");
                            });
                        }));
                    }

                    /* ---------- Consultas ---------- */
                    col.Item().Element(c => Seccion(c, $"Consultas ({consultas.Count})", s =>
                    {
                        if (consultas.Count == 0)
                        {
                            s.Item().Text("Sin consultas registradas.").FontColor(Tenue);
                            return;
                        }

                        foreach (var consulta in consultas.OrderByDescending(x => x.FechaInicio))
                        {
                            s.Item().ShowEntire().BorderBottom(1).BorderColor(Linea).PaddingBottom(6).Column(c =>
                            {
                                c.Item().Text(t =>
                                {
                                    t.Span(ZonaClinica.Formatear(consulta.FechaInicio, "dd/MM/yyyy")).Bold();
                                    t.Span($"  ·  {consulta.TipoConsulta}  ·  {consulta.NutricionistaNombre}").FontColor(Tenue);
                                    if (consulta.FechaFin is null)
                                    {
                                        t.Span("  ·  en curso").FontColor(Colors.Orange.Darken2);
                                    }
                                });
                                if (!string.IsNullOrWhiteSpace(consulta.NotasClinicas))
                                {
                                    c.Item().PaddingTop(2).Text(consulta.NotasClinicas);
                                }
                            });
                        }
                    }));

                    /* ---------- Archivos ---------- */
                    if (expediente.Documentos.Count > 0 || expediente.Fotos.Count > 0)
                    {
                        col.Item().Element(c => Seccion(c, "Archivos del expediente", s =>
                        {
                            s.Item().Text(
                                    $"{expediente.Fotos.Count} foto(s) de seguimiento y {expediente.Documentos.Count} documento(s) " +
                                    "disponibles en el expediente digital.")
                                .FontColor(Tenue);
                            foreach (var doc in expediente.Documentos)
                            {
                                s.Item().Text($"• {doc.NombreDocumento} ({ZonaClinica.Fecha(doc.FechaSubida)})");
                            }
                        }));
                    }
                });

                page.Footer().Element(c => Pie(c, "Documento confidencial: contiene información clínica del paciente."));
            });
        }).GeneratePdf();
    }

    /* ==================== Piezas comunes ==================== */

    private static void ConfigurarPagina(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(36);
        page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Colors.Grey.Darken4));
    }

    private static void Encabezado(IContainer contenedor, DatosFiscalesClinica? clinica, string titulo, string paciente)
    {
        contenedor.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(clinica?.RazonSocial ?? "NutriClínica").FontSize(16).Bold().FontColor(Acento);
                    if (clinica is not null)
                    {
                        c.Item().Text(clinica.Direccion).FontColor(Tenue);
                        var contacto = string.Join("  ·  ",
                            new[] { clinica.Telefono, clinica.Correo }.Where(x => !string.IsNullOrWhiteSpace(x)));
                        if (contacto.Length > 0) c.Item().Text(contacto).FontColor(Tenue);
                    }
                });
                row.ConstantItem(200).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(titulo).FontSize(13).Bold();
                    c.Item().AlignRight().Text(paciente).SemiBold();
                    c.Item().AlignRight().Text(ZonaClinica.Formatear(DateTime.UtcNow, "dd/MM/yyyy")).FontColor(Tenue);
                });
            });
            col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Acento);
        });
    }

    private static void Pie(IContainer contenedor, string leyenda)
    {
        contenedor.Row(row =>
        {
            row.RelativeItem().Text(leyenda).FontSize(7.5f).FontColor(Tenue);
            row.AutoItem().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Tenue));
                t.Span($"Generado el {ZonaClinica.FechaHora(DateTime.UtcNow)}  ·  Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });
    }

    private static void Seccion(IContainer contenedor, string titulo, Action<ColumnDescriptor> contenido)
    {
        contenedor.Column(col =>
        {
            col.Spacing(5);
            col.Item().Text(titulo).FontSize(11).Bold().FontColor(Acento);
            contenido(col);
        });
    }

    private static void Dato(IContainer contenedor, string etiqueta, string valor)
    {
        contenedor.Column(c =>
        {
            c.Item().Text(etiqueta).FontSize(7.5f).FontColor(Tenue);
            c.Item().Text(valor).SemiBold();
        });
    }

    private static void Lista(ColumnDescriptor col, string etiqueta, List<string>? valores, string? color)
    {
        col.Item().Text(t =>
        {
            t.Span($"{etiqueta}: ").FontColor(Tenue);
            var texto = valores is { Count: > 0 } ? string.Join(", ", valores) : "Ninguno registrado";
            var span = t.Span(texto).SemiBold();
            if (color is not null && valores is { Count: > 0 }) span.FontColor(color);
        });
    }

    private static void TablaMedidas(IContainer contenedor, List<MedidaRespuestaDto> medidas)
    {
        contenedor.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(62);
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });

            static IContainer Cabecera(IContainer c) =>
                c.Background(Colors.Teal.Darken2).PaddingVertical(4).PaddingHorizontal(4);

            table.Header(h =>
            {
                foreach (var t in new[] { "Fecha", "Peso (kg)", "Estatura", "IMC", "Grasa %", "Músculo %", "Cintura", "Cadera" })
                {
                    h.Cell().Element(Cabecera).AlignCenter().Text(t).FontSize(8).Bold().FontColor(Colors.White);
                }
            });

            var par = false;
            foreach (var m in medidas)
            {
                var fondo = par ? Colors.Grey.Lighten4 : Colors.White;
                par = !par;
                IContainer Celda(IContainer c) => c.Background(fondo).PaddingVertical(3).PaddingHorizontal(4);

                table.Cell().Element(Celda).Text(ZonaClinica.Fecha(m.FechaMedicion));
                table.Cell().Element(Celda).AlignCenter().Text(Num(m.Peso)).SemiBold();
                table.Cell().Element(Celda).AlignCenter().Text($"{Num(m.Estatura)} cm");
                table.Cell().Element(Celda).AlignCenter().Text(Num(m.Imc));
                table.Cell().Element(Celda).AlignCenter().Text(m.PorcentajeGrasa.HasValue ? Num(m.PorcentajeGrasa.Value) : "—");
                table.Cell().Element(Celda).AlignCenter().Text(m.PorcentajeMasaMuscular.HasValue ? Num(m.PorcentajeMasaMuscular.Value) : "—");
                table.Cell().Element(Celda).AlignCenter().Text(m.MedidaCintura.HasValue ? $"{Num(m.MedidaCintura.Value)} cm" : "—");
                table.Cell().Element(Celda).AlignCenter().Text(m.MedidaCadera.HasValue ? $"{Num(m.MedidaCadera.Value)} cm" : "—");
            }
        });
    }
}
