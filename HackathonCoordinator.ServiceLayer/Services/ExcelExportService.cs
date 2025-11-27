using ClosedXML.Excel;
using System.Windows;

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
                        // Лист с общей информацией
                        CreateSummarySheet(workbook, exportData);

                        // Лист с командами
                        CreateTeamsSheet(workbook, exportData);

                        // Лист с задачами
                        CreateTasksSheet(workbook, exportData);

                        // Лист со статистикой
                        CreateStatisticsSheet(workbook, exportData);

                        workbook.SaveAs(filePath);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    // Для ServiceLayer используем Console.WriteLine вместо MessageBox
                    Console.WriteLine($"Ошибка при создании Excel файла: {ex.Message}");
                    return false;
                }
            });
        }

        private void CreateSummarySheet(XLWorkbook workbook, CompetitionExportDataDto exportData)
        {
            var worksheet = workbook.Worksheets.Add("Общая информация");

            // Заголовок
            worksheet.Cell(1, 1).Value = "ДАННЫЕ СОРЕВНОВАНИЯ";
            worksheet.Range(1, 1, 1, 2).Merge().Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(173, 216, 230); // LightBlue
            worksheet.Range(1, 1, 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Основная информация
            worksheet.Cell(3, 1).Value = "Название:";
            worksheet.Cell(3, 2).Value = exportData.Competition.Name;
            worksheet.Cell(4, 1).Value = "Описание:";
            worksheet.Cell(4, 2).Value = exportData.Competition.Description;
            worksheet.Cell(5, 1).Value = "Дата начала:";
            worksheet.Cell(5, 2).Value = exportData.Competition.StartDate.ToString("dd.MM.yyyy HH:mm");
            worksheet.Cell(6, 1).Value = "Дата окончания:";
            worksheet.Cell(6, 2).Value = exportData.Competition.EndDate.ToString("dd.MM.yyyy HH:mm");
            worksheet.Cell(7, 1).Value = "Организатор:";
            worksheet.Cell(7, 2).Value = exportData.Competition.CreatedByUsername;

            // Статистика
            worksheet.Cell(9, 1).Value = "ОБЩАЯ СТАТИСТИКА";
            worksheet.Range(9, 1, 9, 4).Merge().Style.Font.Bold = true;

            worksheet.Cell(10, 1).Value = "Всего команд:";
            worksheet.Cell(10, 2).Value = exportData.Teams.Count;
            worksheet.Cell(11, 1).Value = "Всего участников:";
            worksheet.Cell(11, 2).Value = exportData.Stats.TotalParticipants;
            worksheet.Cell(12, 1).Value = "Всего задач:";
            worksheet.Cell(12, 2).Value = exportData.Stats.TotalTasks;
            worksheet.Cell(13, 1).Value = "Выполнено задач:";
            worksheet.Cell(13, 2).Value = exportData.Stats.TotalCompletedTasks;
            worksheet.Cell(14, 1).Value = "Общий прогресс:";
            worksheet.Cell(14, 2).Value = $"{exportData.Stats.TotalCompletionPercentage}%";

            // Настройка стилей
            worksheet.Columns().AdjustToContents();
            worksheet.Range(3, 1, 7, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(10, 1, 14, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        private void CreateTeamsSheet(XLWorkbook workbook, CompetitionExportDataDto exportData)
        {
            var worksheet = workbook.Worksheets.Add("Команды");

            // Заголовок
            worksheet.Cell(1, 1).Value = "СПИСОК КОМАНД";
            worksheet.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(144, 238, 144); // LightGreen
            worksheet.Range(1, 1, 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Заголовки таблицы
            var headers = new[] { "Команда", "Дата создания", "Участников", "Всего задач", "Выполнено", "Прогресс" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(211, 211, 211); // LightGray
            }

            // Данные команд
            int row = 4;
            foreach (var team in exportData.Teams.OrderBy(t => t.Name))
            {
                worksheet.Cell(row, 1).Value = team.Name;
                worksheet.Cell(row, 2).Value = team.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                worksheet.Cell(row, 3).Value = team.Members.Count;
                worksheet.Cell(row, 4).Value = team.TeamStats.TotalTasks;
                worksheet.Cell(row, 5).Value = team.TeamStats.CompletedTasks;
                worksheet.Cell(row, 6).Value = $"{team.TeamStats.CompletionPercentage}%";

                // Подсветка прогресса
                if (team.TeamStats.CompletionPercentage >= 80)
                    worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(144, 238, 144); // LightGreen
                else if (team.TeamStats.CompletionPercentage >= 50)
                    worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 153); // LightYellow
                else
                    worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 128, 128); // LightCoral

                row++;
            }

            // Настройка таблицы
            var tableRange = worksheet.Range(3, 1, row - 1, 6);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Columns().AdjustToContents();
        }

        private void CreateTasksSheet(XLWorkbook workbook, CompetitionExportDataDto exportData)
        {
            var worksheet = workbook.Worksheets.Add("Задачи");

            // Заголовок
            worksheet.Cell(1, 1).Value = "ВСЕ ЗАДАЧИ КОМАНД";
            worksheet.Range(1, 1, 1, 7).Merge().Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 218, 185); // LightOrange (PeachPuff)
            worksheet.Range(1, 1, 1, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Заголовки таблицы
            var headers = new[] { "Команда", "Задача", "Тип", "Статус", "Исполнитель", "Дедлайн", "Создана" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style.Font.Bold = true;
                worksheet.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(211, 211, 211); // LightGray
            }

            // Данные задач
            int row = 4;
            foreach (var team in exportData.Teams.OrderBy(t => t.Name))
            {
                foreach (var task in team.Tasks.OrderBy(t => t.Status).ThenBy(t => t.Title))
                {
                    worksheet.Cell(row, 1).Value = team.Name;
                    worksheet.Cell(row, 2).Value = task.Title;
                    worksheet.Cell(row, 3).Value = task.Type;
                    worksheet.Cell(row, 4).Value = task.Status;
                    worksheet.Cell(row, 5).Value = task.AssignedTo ?? "Не назначена";
                    worksheet.Cell(row, 6).Value = task.Deadline?.ToString("dd.MM.yyyy") ?? "-";
                    worksheet.Cell(row, 7).Value = task.CreatedAt.ToString("dd.MM.yyyy HH:mm");

                    // Подсветка статусов
                    switch (task.Status.ToLower())
                    {
                        case "завершена":
                            worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(144, 238, 144); // LightGreen
                            break;
                        case "в процессе":
                        case "на проверке":
                            worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(255, 255, 153); // LightYellow
                            break;
                        case "отменена":
                            worksheet.Cell(row, 4).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 128, 128); // LightCoral
                            break;
                    }

                    row++;
                }
            }

            // Настройка таблицы
            var tableRange = worksheet.Range(3, 1, row - 1, 7);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Columns().AdjustToContents();
        }

        private void CreateStatisticsSheet(XLWorkbook workbook, CompetitionExportDataDto exportData)
        {
            var worksheet = workbook.Worksheets.Add("Статистика");

            // Заголовок
            worksheet.Cell(1, 1).Value = "ДЕТАЛЬНАЯ СТАТИСТИКА";
            worksheet.Range(1, 1, 1, 6).Merge().Style.Font.Bold = true;
            worksheet.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.FromArgb(216, 191, 216); // LightPurple (Thistle)
            worksheet.Range(1, 1, 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Статистика по командам
            worksheet.Cell(3, 1).Value = "СТАТИСТИКА ПО КОМАНДАМ";
            worksheet.Range(3, 1, 3, 6).Merge().Style.Font.Bold = true;

            int row = 4;
            foreach (var team in exportData.Teams.OrderByDescending(t => t.TeamStats.CompletionPercentage))
            {
                worksheet.Cell(row, 1).Value = team.Name;
                worksheet.Cell(row, 2).Value = $"Участников: {team.Members.Count}";
                worksheet.Cell(row, 3).Value = $"Капитан: {team.Members.FirstOrDefault(m => m.IsCaptain)?.Username ?? "Не назначен"}";
                worksheet.Cell(row, 4).Value = $"Задач: {team.TeamStats.TotalTasks}";
                worksheet.Cell(row, 5).Value = $"Выполнено: {team.TeamStats.CompletedTasks}";
                worksheet.Cell(row, 6).Value = $"Прогресс: {team.TeamStats.CompletionPercentage}%";

                // Прогресс-бар в виде текста
                var progressBar = new string('█', team.TeamStats.CompletionPercentage / 10) +
                                new string('░', 10 - team.TeamStats.CompletionPercentage / 10);
                worksheet.Cell(row + 1, 1).Value = progressBar;
                worksheet.Range(row + 1, 1, row + 1, 6).Merge();

                row += 2;
            }

            worksheet.Columns().AdjustToContents();
        }
    }
}