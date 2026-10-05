using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using MindRush.Network.DTOs;

namespace MindRush.Network
{
    public class ApiClient : MonoBehaviour
    {
        private static ApiClient _instance;
        public static ApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ApiClient");
                    _instance = go.AddComponent<ApiClient>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private HttpClient _httpClient;
        private string _authToken;
        private readonly JsonSerializerOptions _jsonOptions;

        public string AuthToken => _authToken;
        public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

        private const int MaxRetries = 3;
        private const float RetryDelaySeconds = 1f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true // For dev only
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.BaseAddress = new Uri(ApiRoutes.BaseUrl);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public void SetAuthToken(string token)
        {
            _authToken = token;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearAuthToken()
        {
            _authToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        // Generic request with retry logic
        public async Task<ApiResponse<T>> Request<T>(HttpMethod method, string endpoint, object body = null)
        {
            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(method, endpoint);
                    
                    if (body != null)
                    {
                        var json = JsonSerializer.Serialize(body, _jsonOptions);
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    }

                    var response = await _httpClient.SendAsync(request);
                    var responseJson = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
                        return new ApiResponse<T> { success = true, data = result };
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return new ApiResponse<T> { success = false, error = "Unauthorized" };
                    }
                    else
                    {
                        var error = JsonSerializer.Deserialize<ErrorResponse>(responseJson, _jsonOptions);
                        return new ApiResponse<T> { success = false, error = error?.error ?? response.ReasonPhrase };
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (attempt == MaxRetries)
                        return new ApiResponse<T> { success = false, error = $"Network error: {ex.Message}" };
                    
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds * (attempt + 1)));
                }
                catch (TaskCanceledException)
                {
                    if (attempt == MaxRetries)
                        return new ApiResponse<T> { success = false, error = "Request timeout" };
                    
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds * (attempt + 1)));
                }
                catch (Exception ex)
                {
                    return new ApiResponse<T> { success = false, error = ex.Message };
                }
            }
            return new ApiResponse<T> { success = false, error = "Max retries exceeded" };
        }

        // Convenience methods
        public Task<ApiResponse<T>> Get<T>(string endpoint) => Request<T>(HttpMethod.Get, endpoint);
        public Task<ApiResponse<T>> Post<T>(string endpoint, object body) => Request<T>(HttpMethod.Post, endpoint, body);
        public Task<ApiResponse<T>> Put<T>(string endpoint, object body) => Request<T>(HttpMethod.Put, endpoint, body);
        public Task<ApiResponse<T>> Delete<T>(string endpoint) => Request<T>(HttpMethod.Delete, endpoint);
    }
}