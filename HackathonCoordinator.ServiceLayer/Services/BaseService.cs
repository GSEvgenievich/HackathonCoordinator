using HackathonCoordinator.ServiceLayer.Storages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace HackathonCoordinator.ServiceLayer.Services
{
    public abstract class BaseService
    {
        protected readonly HttpClient _client;
        protected const string BaseUrl = "http://localhost:5046/api/";

        protected BaseService()
        {
            _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            SetAuthHeader();
        }

        protected void SetAuthHeader()
        {
            var token = SecureTokenStorage.GetToken();
            _client.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }

        protected async Task<ApiResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                    return errorResponse ?? ApiResponse<T>.Fail("Неизвестная ошибка");
                }
                catch
                {
                    return ApiResponse<T>.Fail(content);
                }
            }

            try
            {
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(content);
                return apiResponse ?? ApiResponse<T>.Fail("Неверный формат ответа");
            }
            catch (Exception ex)
            {
                return ApiResponse<T>.Fail($"Ошибка десериализации: {ex.Message}");
            }
        }

        protected async Task<ApiResponse> HandleResponseAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ApiResponse>(content);
                    return errorResponse ?? ApiResponse.Fail("Неизвестная ошибка");
                }
                catch
                {
                    return ApiResponse.Fail(content);
                }
            }

            try
            {
                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(content);
                return apiResponse ?? ApiResponse.Fail("Неверный формат ответа");
            }
            catch (Exception ex)
            {
                return ApiResponse.Fail($"Ошибка десериализации: {ex.Message}");
            }
        }

        protected StringContent CreateJsonContent(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }
}