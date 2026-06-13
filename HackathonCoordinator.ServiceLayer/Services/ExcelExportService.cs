using ClosedXML.Excel;
using HackathonCoordinator.ServiceLayer.DTOs;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public interface IExcelExportService
    {
        Task<bool> ExportCompetitionToExcelAsync(CompetitionExportDataDto exportData, string filePath);
    }

    public class ExcelExportService : IExcelExportService
    {
        public async Task<bool> ExportCompetitionToExcelAsync(CompetitionExportDataDto exportData, string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        CreateSummarySheet(workbook, exportData);
                        CreateTeamsSheet(workbook, exportData);
                        CreateTasksSheet(workbook, exportData);
                        CreateResultsSheet(workbook, exportData);

                        workbook.SaveAs(filePath);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при создании Excel файла: {ex.Message}");
                    return false;
                }
            });
        }

        #region Вспомогательные методы

        private void SetTitle(IXLRange range, string title, XLColor backgroundColor)
        {
            range.Merge().Value = title;
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = backgroundColor;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private void SetHeaders(IXLRange range, string[] headers, XLColor backgroundColor)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                range.Cell(1, i + 1).Value = headers[i];
                range.Cell(1, i + 1).Style.Font.Bold = true;
                range.Cell(1, i + 1).Style.Fill.BackgroundColor = backgroundColor;
                range.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        private void ApplyTableStyle(IXLRange range)
        {
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        #endregion

        private void CreateSummarySheet(XLWorkbook workbook, CompetitionExportDataDto exportData)
        {
            var ws = workbook.Worksheets.Add("Общая информация");
            var titleRange = ws.Range(1, 1, 1, 2);
            SetTitle(titleRange, "ДАННЫЕ СОРЕВНОВАНИЯ", XLColor.FromArgb(173, 216, 230));

            var info = new (string label, object value)[]
            {
                ("Название:", exportData.Competition.Name),
                ("Описание:", exportData.Competition.Description),
                ("Дата начала:", exportData.Competition.StartDate.ToString("dd.MM.yyyy HH:mm")),
                ("Дата окончания:", exportData.Competition.EndDate.ToString("dd.MM.yyyy HH:mm")),
                ("Организатор:", exportData.Competition.CreatedByUsername),
                ("Всего команд:", exportData.Teams.Count),
                ("Всего участников:", exportData.Stats.TotalParticipants),
                ("Всего задач:", exportData.Stats.TotalTasks),
                ("Выполнено задач:", exportData.Stats.TotalCompletedTasks),
                ("Общий прогресс:", $"{exportData.Stats.TotalCompletionPercentage}%")
            };

            for (int i = 0; i < info.Length; i++)
            {
                ws.Cell(i + 2, 1).Value = info[i].label;
                ws.Cell(i + 2, 1).Style.Font.Bold = true;
                ws.Cell(i + 2, 2).Value = info[i].value.ToString();
            }

            var dataRange = ws.Range(1, 1, info.Length + 1, 2);
            ApplyTableStyle(dataRange);
            ws.Range(2, 1, info.Length + 1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(211, 235, 241);
            ws.Range(2, 1, info.Length + 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; 
            ws.Columns().AdjustToContents();
        }

        private void CreateTeamsSheet(XLWorkbook workbook, CompetitionExportDataDto exportData)
        {
            var ws = workbook.Worksheets.Add("Команды");
            var titleRange = ws.Range(1, 1, 1, 6);
            SetTitle(titleRange, "СПИСОК КОМАНД", XLColor.FromArgb(144, 238, 144));

            var headers = new[] { "Команда", "Дата создания", "Участников", "Всего задач", "Выполнено", "Прогресс" };
            SetHeaders(ws.Range(2, 1, 2, 6), headers, XLColor.FromArgb(219, 249, 219));

            int row = 3;
            foreach (var team in exportData.Teams.OrderBy(t => t.Name))
            {
                ws.Cell(row, 1).Value = team.Name;
                ws.Cell(row, 2).Value = team.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 3).Value = team.Members.Count;
                ws.Cell(row, 4).Value = team.TeamStats.TotalTasks;
                ws.Cell(row, 5).Value = team.TeamStats.CompletedTasks;
                ws.Cell(row, 6).Value = $"{team.TeamStats.CompletionPercentage}%";

                var progress = team.TeamStats.CompletionPercentage;
                if (progress >= 80)
                    ws.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(144, 238, 144);
                else if (progress >= 50)
                    ws.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 153);
                else
                    ws.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 128, 128);

                row++;
            }

            var tableRange = ws.Range(1, 1, row - 1, 6);
            ApplyTableStyle(tableRange);
            ws.Columns().AdjustToContents();
        }

        private void CreateTasksSheet(XLWorkbook workbook, CompetitionExportDataDto exportData)
        {
            var ws = workbook.Worksheets.Add("Задачи");
            var titleRange = ws.Range(1, 1, 1, 7);
            SetTitle(titleRange, "ВСЕ ЗАДАЧИ КОМАНД", XLColor.FromArgb(255, 185, 121));

            var headers = new[] { "Команда", "Задача", "Тип", "Статус", "Исполнитель", "Дедлайн", "Создана" };
            SetHeaders(ws.Range(2, 1, 2, 7), headers, XLColor.FromArgb(255, 220, 189));

            int row = 3;
            foreach (var team in exportData.Teams.OrderBy(t => t.Name))
            {
                foreach (var task in team.Tasks.OrderBy(t => t.Status).ThenBy(t => t.Title))
                {
                    ws.Cell(row, 1).Value = team.Name;
                    ws.Cell(row, 2).Value = task.Title;
                    ws.Cell(row, 3).Value = task.Type;
                    ws.Cell(row, 4).Value = task.Status;
                    ws.Cell(row, 5).Value = task.AssignedTo ?? "Не назначена";
                    ws.Cell(row, 6).Value = task.Deadline?.ToString("dd.MM.yyyy") ?? "-";
                    ws.Cell(row, 7).Value = task.CreatedAt.ToString("dd.MM.yyyy HH:mm");

                    switch (task.Status.ToLower())
                    {
                        case "завершена":
                            ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(144, 238, 144);
                            break;
                        case "в процессе":
                        case "на проверке":
                            ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 153);
                            break;
                        case "отменена":
                            ws.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 128, 128);
                            break;
                    }
                    row++;
                }
            }

            var tableRange = ws.Range(1, 1, row - 1, 7);
            ApplyTableStyle(tableRange);
            ws.Columns().AdjustToContents();
        }

        private void CreateResultsSheet(XLWorkbook workbook, CompetitionExportDataDto exportData)
        {
            var ws = workbook.Worksheets.Add("Результаты");
            var titleRange = ws.Range(1, 1, 1, 4);
            SetTitle(titleRange, "РЕЗУЛЬТАТЫ СОРЕВНОВАНИЯ", XLColor.FromArgb(255, 215, 0));

            int row = 2;
            var infoColor = XLColor.FromArgb(255, 230, 105);

            if (exportData.Competition.ResultsCreatedByUsername != null)
            {
                var range = ws.Range(row, 1, row, 4);
                range.Merge().Value = $"Результаты подведены: {exportData.Competition.ResultsCreatedByUsername} ({exportData.Competition.ResultsCreatedAt?.ToString("dd.MM.yyyy HH:mm")})";
                range.Style.Font.Bold = true;
                range.Style.Fill.BackgroundColor = infoColor;
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row++;
            }

            if (exportData.Competition.ResultsUpdatedByUsername != null)
            {
                var range = ws.Range(row, 1, row, 4);
                range.Merge().Value = $"Результаты обновлены: {exportData.Competition.ResultsUpdatedByUsername} ({exportData.Competition.ResultsUpdatedAt?.ToString("dd.MM.yyyy HH:mm")})";
                range.Style.Font.Bold = true;
                range.Style.Fill.BackgroundColor = infoColor;
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                row++;
            }

            var startRow = row;

            // Заголовки таблицы
            var resultHeaders = new[] { "Место", "Команда", "Участников", "Комментарий" };
            for (int i = 0; i < resultHeaders.Length; i++)
            {
                ws.Cell(row, i + 1).Value = resultHeaders[i];
                ws.Cell(row, i + 1).Style.Font.Bold = true;
                ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 242, 179);
                ws.Cell(row, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            row++;

            if (exportData.Results != null && exportData.Results.Any())
            {
                foreach (var result in exportData.Results.OrderBy(r => r.Place))
                {
                    ws.Cell(row, 1).Value = result.Place;
                    ws.Cell(row, 2).Value = result.TeamName;
                    ws.Cell(row, 3).Value = result.MembersCount;
                    ws.Cell(row, 4).Value = result.Comment ?? "—";
                    row++;
                }
            }
            else
            {
                ws.Cell(row, 1).Value = "Результаты еще не подведены";
                ws.Range(row, 1, row, 4).Merge();
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row++;
            }

            var tableRange = ws.Range(startRow, 1, row - 1, 4);
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(1, 1, row - 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            ws.Columns().AdjustToContents();
        }
    }
}