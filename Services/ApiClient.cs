using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoVentaWeb.Models;

namespace PuntoVentaWeb.Services
{
    public record ResultadoApi(bool Ok, string Mensaje);

    public record ResultadoApi<T>(bool Ok, string Mensaje, T? Datos);

    // Todas las llamadas a la API REST pasan por aquí. Agrega el token JWT en cada petición.
    public class ApiClient
    {
        private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly SesionService _sesion;

        public ApiClient(HttpClient http, SesionService sesion)
        {
            _http = http;
            _sesion = sesion;
        }

        // ---------- Inicio de sesión ----------
        public async Task<ResultadoApi<LoginRespuesta>> IniciarSesionAsync(string usuario, string password)
        {
            try
            {
                using var peticion = Crear(HttpMethod.Post, "api/auth/login", new { usuario, password }, conToken: false);
                using var respuesta = await _http.SendAsync(peticion);

                if (respuesta.IsSuccessStatusCode)
                {
                    var datos = await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>(Opciones);
                    return new ResultadoApi<LoginRespuesta>(true, string.Empty, datos);
                }

                return new ResultadoApi<LoginRespuesta>(false, await LeerErrorAsync(respuesta, cerrarSesionSi401: false), default);
            }
            catch (HttpRequestException)
            {
                return new ResultadoApi<LoginRespuesta>(false, MensajeSinConexion, default);
            }
            catch (TaskCanceledException)
            {
                return new ResultadoApi<LoginRespuesta>(false, MensajeTiempoAgotado, default);
            }
        }

        // ---------- GET ----------
        public async Task<ResultadoApi<T>> ObtenerAsync<T>(string url)
        {
            try
            {
                using var peticion = Crear(HttpMethod.Get, url, null, conToken: true);
                using var respuesta = await _http.SendAsync(peticion);

                if (respuesta.IsSuccessStatusCode)
                {
                    var datos = await respuesta.Content.ReadFromJsonAsync<T>(Opciones);
                    return new ResultadoApi<T>(true, string.Empty, datos);
                }

                return new ResultadoApi<T>(false, await LeerErrorAsync(respuesta, cerrarSesionSi401: true), default);
            }
            catch (HttpRequestException)
            {
                return new ResultadoApi<T>(false, MensajeSinConexion, default);
            }
            catch (TaskCanceledException)
            {
                return new ResultadoApi<T>(false, MensajeTiempoAgotado, default);
            }
        }

        // ---------- POST / PUT / DELETE ----------
        public async Task<ResultadoApi> EnviarAsync(HttpMethod metodo, string url, object? cuerpo = null)
        {
            try
            {
                using var peticion = Crear(metodo, url, cuerpo, conToken: true);
                using var respuesta = await _http.SendAsync(peticion);

                if (respuesta.IsSuccessStatusCode)
                {
                    return new ResultadoApi(true, string.Empty);
                }

                return new ResultadoApi(false, await LeerErrorAsync(respuesta, cerrarSesionSi401: true));
            }
            catch (HttpRequestException)
            {
                return new ResultadoApi(false, MensajeSinConexion);
            }
            catch (TaskCanceledException)
            {
                return new ResultadoApi(false, MensajeTiempoAgotado);
            }
        }

        private const string MensajeSinConexion = "No se pudo conectar con la API. Verifique que esté en ejecución.";
        private const string MensajeTiempoAgotado = "La API tardó demasiado en responder. Intente de nuevo.";

        private HttpRequestMessage Crear(HttpMethod metodo, string url, object? cuerpo, bool conToken)
        {
            var peticion = new HttpRequestMessage(metodo, url);

            if (conToken && !string.IsNullOrEmpty(_sesion.Token))
            {
                peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sesion.Token);
            }

            if (cuerpo is not null)
            {
                peticion.Content = JsonContent.Create(cuerpo, cuerpo.GetType(), options: Opciones);
            }

            return peticion;
        }

        private async Task<string> LeerErrorAsync(HttpResponseMessage respuesta, bool cerrarSesionSi401)
        {
            if (respuesta.StatusCode == HttpStatusCode.Unauthorized && cerrarSesionSi401)
            {
                await _sesion.CerrarAsync();
                return "Su sesión expiró. Inicie sesión de nuevo.";
            }

            try
            {
                var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>(Opciones);

                // 400: errores de validación { "errors": { "campo": ["mensaje"] } }
                if (json.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
                {
                    var mensajes = errores.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray().Select(m => m.GetString()))
                        .Where(m => !string.IsNullOrWhiteSpace(m));

                    return string.Join(" ", mensajes);
                }

                // 401, 404, 409: { "mensaje": "..." }
                if (json.TryGetProperty("mensaje", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
                {
                    return mensaje.GetString() ?? "No se pudo completar la operación.";
                }
            }
            catch (Exception)
            {
                // la respuesta no traía JSON: se usa un mensaje genérico
            }

            return respuesta.StatusCode switch
            {
                HttpStatusCode.NotFound => "El registro ya no existe.",
                HttpStatusCode.BadRequest => "Los datos enviados no son válidos.",
                HttpStatusCode.Unauthorized => "Usuario o contraseña incorrectos.",
                _ => $"La API respondió con un error ({(int)respuesta.StatusCode})."
            };
        }

        public async Task<ResultadoApi> SubirImagenAsync(int idProducto, Stream archivo, string nombreArchivo, string tipoContenido)
        {
            try
            {
                using var contenido = new MultipartFormDataContent();
                using var streamContenido = new StreamContent(archivo);
                streamContenido.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(tipoContenido);
                contenido.Add(streamContenido, "archivo", nombreArchivo);

                using var peticion = new HttpRequestMessage(HttpMethod.Post, $"api/productos/{idProducto}/imagen")
                {
                    Content = contenido
                };

                if (!string.IsNullOrEmpty(_sesion.Token))
                {
                    peticion.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sesion.Token);
                }

                using var respuesta = await _http.SendAsync(peticion);

                if (respuesta.IsSuccessStatusCode)
                {
                    return new ResultadoApi(true, string.Empty);
                }

                return new ResultadoApi(false, await LeerErrorAsync(respuesta, true));
            }
            catch (HttpRequestException)
            {
                return new ResultadoApi(false, MensajeSinConexion);
            }
        }
    }
}