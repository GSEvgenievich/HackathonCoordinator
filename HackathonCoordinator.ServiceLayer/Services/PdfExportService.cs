using HackathonCoordinator.ServiceLayer.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public interface IPdfExportService
    {
        Task<bool> ExportResultsToPdfAsync(CompetitionDto competition, List<TeamResultDto> results, string filePath);
    }

    public class PdfExportService : IPdfExportService
    {
        public PdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<bool> ExportResultsToPdfAsync(CompetitionDto competition, List<TeamResultDto> results, string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var document = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.DefaultTextStyle(x => x.FontSize(10));

                            // Заголовок - всё по центру
                            page.Header()
                                .AlignCenter()
                                .Column(col =>
                                {
                                    col.Item().Text("РЕЗУЛЬТАТЫ СОРЕВНОВАНИЯ")
                                        .FontSize(20)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2)
                                        .AlignCenter();

                                    col.Item().Text(competition.Name)
                                        .FontSize(16)
                                        .SemiBold()
                                        .AlignCenter();

                                    col.Item().PaddingTop(5).Text($"{competition.StartDate:dd.MM.yyyy} - {competition.EndDate:dd.MM.yyyy}")
                                        .FontSize(12)
                                        .AlignCenter();
                                });

                            // Содержимое
                            page.Content().PaddingVertical(10).Column(col =>
                            {
                                // Информация о подведении итогов - отдельные строки
                                if (competition.ResultsCreatedByUsername != null)
                                {
                                    col.Item().PaddingBottom(3).Text(text =>
                                    {
                                        text.Span("👤 Результаты подведены: ").SemiBold();
                                        text.Span($"{competition.ResultsCreatedByUsername} ({competition.ResultsCreatedAt:dd.MM.yyyy HH:mm})");
                                    });
                                }

                                if (competition.ResultsUpdatedByUsername != null)
                                {
                                    col.Item().PaddingBottom(15).Text(text =>
                                    {
                                        text.Span("✏️ Результаты обновлены: ").SemiBold();
                                        text.Span($"{competition.ResultsUpdatedByUsername} ({competition.ResultsUpdatedAt:dd.MM.yyyy HH:mm})");
                                    });
                                }

                                // Таблица результатов
                                col.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(60);  // Место
                                        columns.RelativeColumn(3);    // Команда
                                        columns.RelativeColumn(4);    // Комментарий
                                    });

                                    // Заголовки таблицы с фоном
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(x => x
                                                .Background(Colors.Grey.Lighten2)
                                                .Padding(8)
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Black))
                                            .Text("Место")
                                            .Bold()
                                            .AlignCenter();

                                        header.Cell().Element(x => x
                                                .Background(Colors.Grey.Lighten2)
                                                .Padding(8)
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Black))
                                            .Text("Команда")
                                            .Bold()
                                            .AlignLeft();

                                        header.Cell().Element(x => x
                                                .Background(Colors.Grey.Lighten2)
                                                .Padding(8)
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Black))
                                            .Text("Комментарий")
                                            .Bold()
                                            .AlignLeft();
                                    });

                                    // Данные таблицы
                                    foreach (var team in results.OrderBy(r => r.Place))
                                    {
                                        var place = team.Place ?? 0;

                                        // Строка с местом
                                        table.Cell().Element(x => x
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Grey.Lighten1)
                                                .Padding(8))
                                            .Text(place.ToString())
                                            .AlignCenter();

                                        // Строка с названием команды
                                        table.Cell().Element(x => x
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Grey.Lighten1)
                                                .Padding(8))
                                            .Text(team.TeamName)
                                            .AlignLeft();

                                        // Строка с комментарием
                                        table.Cell().Element(x => x
                                                .BorderBottom(1)
                                                .BorderColor(Colors.Grey.Lighten1)
                                                .Padding(8))
                                            .Text(team.Comment ?? "—")
                                            .AlignLeft();
                                    }
                                });
                            });

                            // Нижний колонтитул
                            page.Footer()
                                .AlignCenter()
                                .Text($"Отчет о результатах от: {DateTime.Now:dd.MM.yyyy HH:mm}")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Medium);
                        });
                    });

                    document.GeneratePdf(filePath);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при создании PDF файла: {ex.Message}");
                    return false;
                }
            });
        }
    }
}