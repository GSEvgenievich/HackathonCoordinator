using HackathonCoordinator.ServiceLayer.DTOs;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class ChatService : BaseService
    {
        public async Task<ApiResponse<ChatDto>> GetTeamChatAsync(int teamId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"chats/team/{teamId}");
                return await HandleResponseAsync<ChatDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatDto>.Fail($"Ошибка получения чата команды: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ChatDto>> GetTaskChatAsync(int taskId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"chats/task/{taskId}");
                return await HandleResponseAsync<ChatDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatDto>.Fail($"Ошибка получения чата задачи: {ex.Message}");
            }
        }

        public async Task<ApiResponse<MessageDto>> SendMessageAsync(int chatId, string text)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(new { ChatId = chatId, Text = text });
                var response = await _client.PostAsync("chats/send", content);
                return await HandleResponseAsync<MessageDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<MessageDto>.Fail($"Ошибка отправки сообщения: {ex.Message}");
            }
        }

        public async Task<ApiResponse> EditMessageAsync(int messageId, string newText)
        {
            SetAuthHeader();

            try
            {
                var content = CreateJsonContent(newText);
                var response = await _client.PutAsync($"chats/messages/{messageId}", content);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка редактирования сообщения: {ex.Message}");
            }
        }

        public async Task<ApiResponse> DeleteMessageAsync(int messageId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.DeleteAsync($"chats/messages/{messageId}");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка удаления сообщения: {ex.Message}");
            }
        }
    }
}