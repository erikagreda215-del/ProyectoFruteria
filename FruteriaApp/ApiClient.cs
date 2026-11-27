using System.Net.Http.Json;
using System.Text.Json;

namespace FruteriaApp;

/// <summary>Error devuelto por la API (lee el campo "detail" de FastAPI).</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiException(int statusCode, string mensaje) : base(mensaje) => StatusCode = statusCode;
}

/// <summary>
/// Único punto de comunicación con la API REST. Se crea una vez en Program.cs
/// y se pasa por constructor a todos los formularios.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;

    // camelCase + sin distinguir mayúsculas: "precioKg" <-> PrecioKg
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ApiClient(string urlBase)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(urlBase),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public string UrlBase => _http.BaseAddress?.ToString() ?? "";

    // ------------------------------- Productos -------------------------------
    public async Task<List<Producto>> GetProductosAsync()
    {
        var resp = await _http.GetAsync("productos");
        await ComprobarAsync(resp);
        return await resp.Content.ReadFromJsonAsync<List<Producto>>(Json) ?? new();
    }

    public async Task<Producto> CrearProductoAsync(ProductoEntrada p)
    {
        var resp = await _http.PostAsJsonAsync("productos", p, Json);
        await ComprobarAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<Producto>(Json))!;
    }

    public async Task<Producto> ActualizarProductoAsync(int id, ProductoEntrada p)
    {
        var resp = await _http.PutAsJsonAsync($"productos/{id}", p, Json);
        await ComprobarAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<Producto>(Json))!;
    }

    public async Task EliminarProductoAsync(int id)
    {
        var resp = await _http.DeleteAsync($"productos/{id}");
        await ComprobarAsync(resp);
    }

    // --------------------------------- Ventas --------------------------------
    public async Task<Venta> RegistrarVentaAsync(VentaEntrada venta)
    {
        var resp = await _http.PostAsJsonAsync("ventas", venta, Json);
        await ComprobarAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<Venta>(Json))!;
    }

    public async Task<List<Venta>> GetVentasAsync()
    {
        var resp = await _http.GetAsync("ventas");
        await ComprobarAsync(resp);
        return await resp.Content.ReadFromJsonAsync<List<Venta>>(Json) ?? new();
    }

    // -------------------------------- Informes -------------------------------
    public async Task<InformeVentasHoy> GetInformeHoyAsync()
    {
        var resp = await _http.GetAsync("informes/ventas-hoy");
        await ComprobarAsync(resp);
        return (await resp.Content.ReadFromJsonAsync<InformeVentasHoy>(Json))!;
    }

    /// <summary>Devuelve true si la API responde.</summary>
    public async Task<bool> ProbarConexionAsync()
    {
        try
        {
            var resp = await _http.GetAsync("");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Si la respuesta no es 2xx lanza ApiException con el mensaje de la API
    private static async Task ComprobarAsync(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;

        string mensaje = $"Error {(int)resp.StatusCode} ({resp.ReasonPhrase})";
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("detail", out var detail))
            {
                // 400/404 -> "detail" es texto; 422 (validación) -> "detail" es una lista
                mensaje = detail.ValueKind == JsonValueKind.String
                    ? detail.GetString() ?? mensaje
                    : "Datos no válidos: " + string.Join("; ",
                        detail.EnumerateArray().Select(e =>
                            e.TryGetProperty("msg", out var m) ? m.GetString() : e.ToString()));
            }
        }
        catch (JsonException) { /* la respuesta no era JSON */ }

        throw new ApiException((int)resp.StatusCode, mensaje);
    }
}
