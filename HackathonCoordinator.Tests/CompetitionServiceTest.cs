using Xunit;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.ServiceLayer.DTOs;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using HackathonCoordinator.ServiceLayer;

namespace HackathonCoordinator.Tests.ServiceTests
{
    public class CompetitionServiceTests
    {
        [Fact]
        public async Task GetCompetitionAsync_ValidId_ReturnsCompetitionData()
        {
            // Arrange: Создание тестовых данных
            var competition = new CompetitionDto
            {
                Id = 1,
                Name = "Хакатон 2024",
                Description = "Ежегодный хакатон по разработке ПО",
                StartDate = DateTime.Parse("2024-05-15T10:00:00"),
                EndDate = DateTime.Parse("2024-05-17T18:00:00"),
                CreatedByUsername = "organizer_user",
                Teams = new List<TeamDto>
                {
                    new TeamDto { Id = 1, Name = "Команда А" },
                    new TeamDto { Id = 2, Name = "Команда Б" }
                }
            };

            // Переменная для хранения фактического заголовка авторизации
            string actualAuthHeader = null;

            // Настройка mock HTTP-клиента
            var mockHttp = new Mock<HttpMessageHandler>();
            mockHttp.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString() == "http://localhost:5046/api/competitions/1"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    // Запоминаем заголовок авторизации для проверки
                    if (request.Headers.Authorization != null)
                    {
                        actualAuthHeader = request.Headers.Authorization.ToString();
                    }

                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            JsonConvert.SerializeObject(new ApiResponse<CompetitionDto>
                            {
                                Success = true,
                                Message = "Соревнование получено",
                                Data = competition
                            }),
                            Encoding.UTF8,
                            "application/json")
                    };
                });

            var httpClient = new HttpClient(mockHttp.Object)
            {
                BaseAddress = new Uri("http://localhost:5046/api/")
            };

            // Очищаем токен перед тестом, чтобы использовать реальный из настроек
            ClearTestToken();

            // Создание тестируемого сервиса через наследование
            var service = new CompetitionsService(httpClient);

            // Act: Вызов метода
            var result = await service.GetCompetitionAsync(1);

            // Assert: Проверка результатов
            Assert.True(result.Success);
            Assert.Equal("Соревнование получено", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.Id);
            Assert.Equal("Хакатон 2024", result.Data.Name);
            Assert.Equal("Ежегодный хакатон по разработке ПО", result.Data.Description);
            Assert.Equal("organizer_user", result.Data.CreatedByUsername);
            Assert.Equal(2, result.Data.Teams.Count);
            Assert.Contains("Команда А", result.Data.Teams.Select(t => t.Name));
            Assert.Contains("Команда Б", result.Data.Teams.Select(t => t.Name));

            // Проверка, что HTTP-запрос был выполнен
            mockHttp.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString() == "http://localhost:5046/api/competitions/1"),
                ItExpr.IsAny<CancellationToken>());
        }

        // Вспомогательный метод для очистки тестового токена
        private void ClearTestToken()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HackathonCoordinator"
                );

                var tokenPath = Path.Combine(appDataPath, "token.dat");
                if (File.Exists(tokenPath))
                {
                    File.Delete(tokenPath);
                }
            }
            catch
            {
                // Игнорируем ошибки очистки
            }
        }

        // Тестовый класс, наследующий CompetitionService
        private class CompetitionsService : CompetitionService
        {
            private readonly HttpClient _testClient;

            public CompetitionsService(HttpClient client) : base()
            {
                // Используем reflection для установки protected поля _client
                var clientField = typeof(BaseService).GetField("_client",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (clientField != null)
                {
                    clientField.SetValue(this, client);
                }

                SetAuthHeader();
            }

            // Публичный метод для проверки SetAuthHeader
            public new void SetAuthHeader()
            {
                base.SetAuthHeader();
            }
        }
    }
}