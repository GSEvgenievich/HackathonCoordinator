using HackathonCoordinator.ServiceLayer.DTOs;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class CompetitionService : BaseService
    {
        public async Task<ApiResponse<List<CompetitionDto>>> GetCompetitionsAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("competitions");
                return await HandleResponseAsync<List<CompetitionDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<CompetitionDto>>.Fail($"Ошибка получения соревнований: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CompetitionDto>> GetCompetitionAsync(int id)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"competitions/{id}");
                return await HandleResponseAsync<CompetitionDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<CompetitionDto>.Fail($"Ошибка получения соревнования: {ex.Message}");
            }
        }

        public async Task<ApiResponse> CreateCompetitionAsync(CreateCompetitionDto dto)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(dto);
                var response = await _client.PostAsync("competitions", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка создания соревнования: {ex.Message}");
            }
        }

        public async Task<ApiResponse> UpdateCompetitionAsync(int id, CreateCompetitionDto dto)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(dto);
                var response = await _client.PutAsync($"competitions/{id}", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка обновления соревнования: {ex.Message}");
            }
        }

        public async Task<ApiResponse> CreateTeamAsync(int competitionId, string teamName)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { Name = teamName });
                var response = await _client.PostAsync($"competitions/{competitionId}/teams", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка создания команды: {ex.Message}");
            }
        }

        public async Task<ApiResponse> DeleteTeamAsync(int teamId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.DeleteAsync($"teams/{teamId}");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка удаления команды: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CompetitionExportDataDto>> GetCompetitionExportDataAsync(int competitionId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"export/competition-data/{competitionId}");
                return await HandleResponseAsync<CompetitionExportDataDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<CompetitionExportDataDto>.Fail($"Ошибка получения данных для экспорта: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохранить все результаты соревнования
        /// </summary>
        public async Task<ApiResponse> SaveAllResultsAsync(int competitionId, List<TeamResultDto> results)
        {
            SetAuthHeader();

            try
            {
                var dtoList = results.Select(r => new SaveTeamResultDto
                {
                    CompetitionId = competitionId,
                    TeamId = r.TeamId,
                    Place = r.Place ?? 0,
                    Comment = r.Comment
                }).ToList();

                var content = CreateJsonContent(dtoList);
                var response = await _client.PostAsync($"competitions/{competitionId}/save-all-results", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка сохранения результатов: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить результаты соревнования
        /// </summary>
        public async Task<ApiResponse<List<TeamResultDto>>> GetCompetitionResultsAsync(int competitionId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"competitions/{competitionId}/results");
                return await HandleResponseAsync<List<TeamResultDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<TeamResultDto>>.Fail($"Ошибка получения результатов: {ex.Message}");
            }
        }
    }
}