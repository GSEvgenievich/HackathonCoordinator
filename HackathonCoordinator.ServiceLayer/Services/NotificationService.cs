using HackathonCoordinator.ServiceLayer.DTOs;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public class NotificationService : BaseService
    {
        public async Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("notifications");
                return await HandleResponseAsync<List<NotificationDto>>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<NotificationDto>>.Fail($"Ошибка получения уведомлений: {ex.Message}");
            }
        }

        public async Task<ApiResponse<int>> GetUnreadCountAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.GetAsync("notifications/unread-count");
                return await HandleResponseAsync<int>(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail($"Ошибка получения количества уведомлений: {ex.Message}");
            }
        }

        public async Task<ApiResponse> MarkAsReadAsync(int notificationId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PutAsync($"notifications/{notificationId}/read", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка отметки уведомления как прочитанного: {ex.Message}");
            }
        }

        public async Task<ApiResponse> MarkAllAsReadAsync()
        {
            SetAuthHeader();

            try
            {
                var response = await _client.PutAsync("notifications/mark-all-read", null);
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка отметки всех уведомлений как прочитанных: {ex.Message}");
            }
        }

        public async Task<ApiResponse> DeleteNotificationAsync(int notificationId)
        {
            SetAuthHeader();

            try
            {
                var response = await _client.DeleteAsync($"notifications/{notificationId}");
                return await HandleResponseAsync(response);
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка удаления уведомления: {ex.Message}");
            }
        }
    }
}