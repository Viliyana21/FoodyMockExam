using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using FoodyMockExam2.Models;
using FoodyMockExam2;
using NUnit.Framework.Internal;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
namespace FoodyMockExam2
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedFoodId;

        private const string BaseUrl = "http://144.91.123.158:81";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJhYzM4MjZjMi05ODIxLTQ3YzMtODM0Mi05YWE0MjE0N2FjNTgiLCJpYXQiOiIwNC8xNi8yMDI2IDA5OjMxOjIyIiwiVXNlcklkIjoiMjFiZGIzMGUtMTY4Ni00MGQ2LTc0OGYtMDhkZTc2OGU4Zjk3IiwiRW1haWwiOiIxNjA0QHNvZnR1bmkuY29tIiwiVXNlck5hbWUiOiJGb29keUV4YW1QcmVwIiwiZXhwIjoxNzc2MzUzNDgyLCJpc3MiOiJGb29keV9BcHBfU29mdFVuaSIsImF1ZCI6IkZvb2R5X1dlYkFQSV9Tb2Z0VW5pIn0.cenJv3WO2Nkl1LSJh--rCDdt7dlftFeYW3u1sdv1wJE";
        private const string LoginEmail = "FoodyExamPrep";
        private const string LoginPassword = "12345678";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);//да внимавам дали апи заявките и наклонените черти //са ок
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }



        [Test, Order(1)]
        public void CreateFood_WithRequiredFields_ShouldReturnCreated()
        {
            // Arrange
            FoodDTO foodData = new FoodDTO
            {
                Name = "Test Food",
                Description = "This is a test food description.",
                Url = "https://example.com/food.bmp"
            };

            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(foodData);

            // Act
            RestResponse response = this.client.Execute(request);
            ApiResponseDTO? createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code 201 Created.");
            Assert.That(createResponse, Is.Not.Null, "Response body should be successfully deserialized.");
            Assert.That(createResponse.FoodId, Is.Not.Null.And.Not.Empty, "Response body should contain foodId.");

            // Save created food id for next tests
            lastCreatedFoodId = createResponse.FoodId;
        }
        [Test, Order(2)]
        public void EditFoodTitle_ShouldReturnSuccessMessage()
        {
            // Arrange
            var patchBody = new[]
            {
        new
        {
            operationType = 0,
            path = "/name",
            op = "replace",
            from = "",
            value = "Updated Food Name"
        }
    };

            RestRequest request = new RestRequest($"/api/Food/Edit/{lastCreatedFoodId}", Method.Patch);
            request.AddJsonBody(patchBody);

            // Act
            RestResponse response = this.client.Execute(request);
            ApiResponseDTO? responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseData, Is.Not.Null, "Response body should be deserialized.");
            Assert.That(responseData.Msg, Is.EqualTo("Successfully edited"), "Expected success message.");
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnNonEmptyList()
        {
            // Arrange
            RestRequest request = new RestRequest("/api/Food/All", Method.Get);

            // Act
            RestResponse response = this.client.Execute(request);

            TestContext.WriteLine(response.Content);

            // Assert status first
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(response.Content, Is.Not.Null.And.Not.Empty, "Response content should not be empty.");

            List<FoodDTO>? foods = JsonSerializer.Deserialize<List<FoodDTO>>(response.Content);

            Assert.That(foods, Is.Not.Null, "Response should be deserialized to a list.");
            Assert.That(foods.Count, Is.GreaterThan(0), "Foods list should not be empty.");
        }
        [Test, Order(4)]
        public void DeleteFood_ShouldReturnSuccessMessage()
        {
            // Arrange
            RestRequest request = new RestRequest($"/api/Food/Delete/{lastCreatedFoodId}", Method.Delete);

            // Act
            RestResponse response = this.client.Execute(request);

            TestContext.WriteLine($"Status code: {response.StatusCode}");
            TestContext.WriteLine($"Raw response: [{response.Content}]");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(response.Content, Is.Not.Null, "Response content should not be null.");

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                Assert.Fail("The API returned 200 OK but no response body, so the success message cannot be verified.");
            }

            ApiResponseDTO? responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(responseData, Is.Not.Null);
            Assert.That(responseData.Msg, Is.EqualTo("Deleted successfully!"));
        }
        [Test, Order(5)]
        public void CreateFood_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            // Arrange
            FoodDTO invalidFood = new FoodDTO
            {
                Name = "",              // missing required field
                Description = "",       // missing required field
                Url = "https://example.com/food.bmp"
            };

            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(invalidFood);

            // Act
            RestResponse response = this.client.Execute(request);

            TestContext.WriteLine($"Status code: {response.StatusCode}");
            TestContext.WriteLine($"Response content: {response.Content}");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 BadRequest.");
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            // Arrange
            string nonExistingFoodId = "999999";

            var patchBody = new[]
            {
        new
        {
            op = "replace",
            path = "/name",
            value = "Updated Food Name"
        }
    };

            RestRequest request = new RestRequest($"/api/Food/Edit/{nonExistingFoodId}", Method.Patch);
            request.AddHeader("Content-Type", "application/json-patch+json");
            request.AddJsonBody(patchBody);

            // Act
            RestResponse response = this.client.Execute(request);

            TestContext.WriteLine($"Status code: {response.StatusCode}");
            TestContext.WriteLine($"Response content: {response.Content}");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 NotFound.");
        }
        
        [Test, Order(7)]
        public void DeleteNonExistingFood_ShouldReturnNotFound()
        {
            // Arrange
            string nonExistingFoodId = "999999";

            RestRequest request = new RestRequest($"/api/Food/Delete/{nonExistingFoodId}", Method.Delete);

            // Act
            RestResponse response = this.client.Execute(request);

            TestContext.WriteLine($"Status code: {response.StatusCode}");
            TestContext.WriteLine($"Response content: {response.Content}");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 NotFound.");
            Assert.That(response.Content, Is.Not.Null.And.Not.Empty, "Response content should not be empty.");

            ApiResponseDTO? responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(responseData, Is.Not.Null, "Response body should be deserialized.");
            Assert.That(responseData.Msg, Does.Contain("No food"), "Expected not found message.");
        }
        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}