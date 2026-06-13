using HackathonCoordinator.ServiceLayer.DTOs;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class TeamService : BaseService
    {
        public async Task<ApiResponse> CreateTeamAsync(string name, bool linkToGitHub = false)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { Name = name, LinkToGitHub = linkToGitHub });
                var response = await _client.PostAsync("teams/create", content);
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

        public async Task<ApiResponse> JoinTeamAsync(string inviteCode)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { InviteCode = inviteCode });
                var response = await _client.PostAsync("teams/join", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка присоединения к команде: {ex.Message}");
            }
        }

        public async Task<ApiResponse> AssignCaptainAsync(int teamId, int userId)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { UserId = userId });
                var response = await _client.PostAsync($"teams/{teamId}/assign-captain", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка назначения капитана: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TeamDto>> GetCurrentTeamAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("teams/current");
                return await HandleResponseAsync<TeamDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<TeamDto>.Fail($"Ошибка получения текущей команды: {ex.Message}");
            }
        }

        public async Task<ApiResponse<int>> GetCurrentTeamIdAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("teams/current/id");
                return await HandleResponseAsync<int>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail($"Ошибка получения ID команды: {ex.Message}");
            }
        }

        public async Task<ApiResponse> LeaveTeamAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PostAsync("teams/leave", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка выхода из команды: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить финальный состав команды
        /// </summary>
        public async Task<ApiResponse<List<FinalTeamMemberDto>>> GetFinalTeamMembersAsync(int teamId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"teams/{teamId}/final-members");
                return await HandleResponseAsync<List<FinalTeamMemberDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<FinalTeamMemberDto>>.Fail($"Ошибка получения финального состава: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить результат команды
        /// </summary>
        public async Task<ApiResponse<TeamResultDto>> GetTeamResultAsync(int competitionId, int teamId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"teams/{teamId}/result");
                return await HandleResponseAsync<TeamResultDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<TeamResultDto>.Fail($"Ошибка получения результата: {ex.Message}");
            }
        }

        public async Task<ApiResponse<TeamDto>> GetTeamByIdAsync(int teamId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"teams/{teamId}");
                return await HandleResponseAsync<TeamDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<TeamDto>.Fail($"Ошибка получения команды: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<TaskDto>>> GetTeamTasksAsync(int teamId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"teams/{teamId}/tasks");
                return await HandleResponseAsync<List<TaskDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<TaskDto>>.Fail($"Ошибка получения задач команды: {ex.Message}");
            }
        }

        public async Task<ApiResponse> KickMemberAsync(int memberId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.DeleteAsync($"teams/members/{memberId}/kick");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка выгона участника: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CompetitionDto>> GetCompetitionByTeamIdAsync(int teamId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"teams/{teamId}/competition");
                return await HandleResponseAsync<CompetitionDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<CompetitionDto>.Fail($"Ошибка получения соревнования команды: {ex.Message}");
            }
        }

        public async Task<ApiResponse<GitHubRepoCreationResponseDto>> CreateGitHubRepositoryAsync(
            int teamId, string repoName, string description, bool isPrivate = true)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new
                {
                    RepoName = repoName,
                    Description = description,
                    IsPrivate = isPrivate
                });

                var response = await _client.PostAsync($"teams/{teamId}/create-github-repo", content);
                return await HandleResponseAsync<GitHubRepoCreationResponseDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<GitHubRepoCreationResponseDto>.Fail($"Ошибка создания репозитория: {ex.Message}");
            }
        }
    }
}
