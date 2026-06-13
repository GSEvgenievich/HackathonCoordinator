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

        /// <summary>
        /// Отправить сообщение с вложениями
        /// </summary>
        public async Task<ApiResponse<MessageDto>> SendMessageWithAttachmentsAsync(int chatId, string text, List<FileUploadData> attachments)
        {
            SetAuthHeader();

            try
            {
                using var formData = new MultipartFormDataContent();

                // Добавляем ID чата
                formData.Add(new StringContent(chatId.ToString()), "ChatId");

                // Добавляем текст сообщения
                if (!string.IsNullOrEmpty(text))
                {
                    formData.Add(new StringContent(text), "Text");
                }

                // Добавляем файлы
                for (int i = 0; i < attachments.Count; i++)
                {
                    var file = attachments[i];
                    var fileContent = new ByteArrayContent(file.Data);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    formData.Add(fileContent, "Attachments", file.FileName);
                }

                var response = await _client.PostAsync("chats/send-with-attachments", formData);
                return await HandleResponseAsync<MessageDto>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<MessageDto>.Fail($"Ошибка отправки сообщения с вложениями: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить полное изображение для просмотра
        /// </summary>
        public async Task<ApiResponse<byte[]>> GetFullImageAsync(int attachmentId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"chats/image/{attachmentId}");

                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    return ApiResponse<byte[]>.Ok(imageBytes, "Изображение загружено");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ApiResponse<byte[]>.Fail(errorContent);
            }
            catch (Exception ex)
            {
                return ApiResponse<byte[]>.Fail($"Ошибка получения изображения: {ex.Message}");
            }
        }

        /// <summary>
        /// Скачать вложение
        /// </summary>
        public async Task<ApiResponse<byte[]>> DownloadAttachmentAsync(int attachmentId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync($"chats/download/{attachmentId}");

                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    return ApiResponse<byte[]>.Ok(fileBytes, "Файл загружен");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ApiResponse<byte[]>.Fail(errorContent);
            }
            catch (Exception ex)
            {
                return ApiResponse<byte[]>.Fail($"Ошибка скачивания файла: {ex.Message}");
            }
        }
    }
}