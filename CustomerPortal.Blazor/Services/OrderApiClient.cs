using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrderProcessing.Application.Dtos;
using OrderProcessing.Application.Requests;

namespace CustomerPortal.Blazor.Services;

public class OrderApiClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public OrderApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductDto>> GetProductsAsync()
    {
        var response = await _httpClient.GetAsync("api/Products");

        if (!response.IsSuccessStatusCode)
        {
            return new List<ProductDto>();
        }

        var json = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<ProductDto>>(json, JsonOptions);

        return products ?? new List<ProductDto>();
    }

    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        var response = await _httpClient.GetAsync("api/Orders");

        if (!response.IsSuccessStatusCode)
        {
            return new List<OrderDto>();
        }

        var json = await response.Content.ReadAsStringAsync();
        var orders = JsonSerializer.Deserialize<List<OrderDto>>(json, JsonOptions);

        return orders ?? new List<OrderDto>();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/Orders/{id}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OrderDto>(json, JsonOptions);
    }

    public async Task<string?> GetOrderStatusAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/Orders/{id}/status");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<OrderDto?> CheckoutAsync(CheckoutOrderRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Orders/checkout", request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OrderDto>(json, JsonOptions);
    }
}