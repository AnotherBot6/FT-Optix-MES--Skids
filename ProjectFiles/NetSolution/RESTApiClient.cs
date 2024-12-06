#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.Retentivity;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using FTOptix.RAEtherNetIP;
#endregion

public class RESTApiClient : BaseNetLogic
{
    readonly struct HTTPResponse
    {
        public HTTPResponse(string payload, int code)
        {
            Payload = payload;
            Code = code;
        }

        public string Payload { get; }
        public int Code { get; }
    };

    public override void Start()
    {
    }

    public override void Stop()
    {
    }

    private long GetTimeout()
    {
        var timeoutVariable = LogicObject.Get<IUAVariable>("Timeout");
        if (timeoutVariable == null)
            throw new Exception($"Missing Timeout variable under the NetLogic {LogicObject.BrowseName}");

        return timeoutVariable.Value;
    }

    private string GetUserAgent()
    {
        var userAgentVariable = LogicObject.Get<IUAVariable>("UserAgent");
        if (userAgentVariable == null)
            throw new Exception($"Missing UserAgent variable under the NetLogic {LogicObject.BrowseName}");

        return userAgentVariable.Value;
    }

    private bool IsSupportedScheme(string scheme)
    {
        return scheme == "http" || scheme == "https";
    }

    private bool IsSecureScheme(string scheme)
    {
        return scheme == "https";
    }

    private HttpRequestMessage BuildGetMessage(Uri url, string userAgent, string bearerToken)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(userAgent))
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue(userAgent)));

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return request;
    }

    private HttpRequestMessage BuildPostMessage(Uri url, string body, string contentType, string userAgent, string bearerToken)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

        if (!string.IsNullOrWhiteSpace(body))
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, contentType);

        if (!string.IsNullOrWhiteSpace(userAgent))
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue(userAgent)));

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return request;
    }

    private HttpRequestMessage BuildPutMessage(Uri url, string body, string contentType, string userAgent, string bearerToken)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url);

        if (!string.IsNullOrWhiteSpace(body))
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, contentType);

        if (!string.IsNullOrWhiteSpace(userAgent))
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue(userAgent)));

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        return request;
    }

    private async Task<HTTPResponse> PerformRequest(HttpRequestMessage request, TimeSpan timeout)
    {
        HttpClient client = new HttpClient();
        client.Timeout = timeout;

        using HttpResponseMessage httpResponse = await client.SendAsync(request);
        string responseBody = await httpResponse.Content.ReadAsStringAsync();

        return new HTTPResponse(responseBody, (int)httpResponse.StatusCode);
    }

    private HttpRequestMessage BuildMessage(HttpMethod verb, Uri url, string requestBody, string bearerToken, string contentType)
    {
        TimeSpan timeout = TimeSpan.FromMilliseconds(GetTimeout());
        string userAgent = GetUserAgent();

        if (string.IsNullOrWhiteSpace(contentType))
            contentType = "application/json";

        if (!IsSupportedScheme(url.Scheme))
            throw new Exception($"The URI scheme {url.Scheme} is not supported");

        if (!IsSecureScheme(url.Scheme) && !string.IsNullOrWhiteSpace(bearerToken))
            Log.Warning("Possible sending of unencrypted confidential information");

        if (verb == HttpMethod.Get)
            return BuildGetMessage(url, userAgent, bearerToken);
        if (verb == HttpMethod.Post)
            return BuildPostMessage(url, requestBody, contentType, userAgent, bearerToken);
        if (verb == HttpMethod.Put)
            return BuildPutMessage(url, requestBody, contentType, userAgent, bearerToken);

        throw new Exception($"Unsupported verb { verb }");
    }

    [ExportMethod]
    public void Get(string apiUrl, string queryString, string bearerToken, out string response, out int code)
    {
        TimeSpan timeout = TimeSpan.FromMilliseconds(GetTimeout());
        UriBuilder uriBuilder = new UriBuilder(apiUrl);
        uriBuilder.Query = queryString;

        var requestMessage = BuildMessage(HttpMethod.Get, uriBuilder.Uri, "", bearerToken, "");
        var requestTask = PerformRequest(requestMessage, timeout);
        var httpResponse = requestTask.Result;

        (response, code) = (httpResponse.Payload, httpResponse.Code);
    }

    [ExportMethod]
    public void Post(string apiUrl, string requestBody, string bearerToken, string contentType, out string response, out int code)
    {
        TimeSpan timeout = TimeSpan.FromMilliseconds(GetTimeout());
        UriBuilder uriBuilder = new UriBuilder(apiUrl);

        var requestMessage = BuildMessage(HttpMethod.Post, uriBuilder.Uri, requestBody, bearerToken, contentType);
        var requestTask = PerformRequest(requestMessage, timeout);
        var httpResponse = requestTask.Result;

        (response, code) = (httpResponse.Payload, httpResponse.Code);
    }

    [ExportMethod]
    public void Put(string apiUrl, string requestBody, string bearerToken, string contentType, out string response, out int code)
    {
        TimeSpan timeout = TimeSpan.FromMilliseconds(GetTimeout());
        UriBuilder uriBuilder = new UriBuilder(apiUrl);

        var requestMessage = BuildMessage(HttpMethod.Put, uriBuilder.Uri, requestBody, bearerToken, contentType);
        var requestTask = PerformRequest(requestMessage, timeout);
        var httpResponse = requestTask.Result;

        (response, code) = (httpResponse.Payload, httpResponse.Code);
    }
    [ExportMethod]
    public void CallGeminiApi(string bearerToken, string prompt, out string response, out int code)
    {
        
        // URL de la API de Gemini
        string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=" + bearerToken;

        string content = $"Eres una IA integrada " +
    $"en un sistema MES (Manufacturing Execution System) especializado en el ensamblaje de skids para coches eléctricos. " +
    $"Este sistema MES gestiona múltiples áreas clave, incluyendo órdenes de producción, inventarios, monitoreo en tiempo real, " +
    $"análisis de datos, mantenimiento predictivo e integración con tecnología avanzada como QR, RFID y bases de datos SQL. " +
    $"Tu principal tarea es proporcionar análisis predictivos, recomendaciones y alertas basadas en el uso histórico, " +
    $"la frecuencia de fallos y los datos en tiempo real recopilados por sensores y sistemas de monitoreo del equipo. " +
    $"En la planta, los equipos monitoreados incluyen:" +
    $"- **Transportadoras automáticas**, utilizadas para mover componentes y skids entre estaciones." +
    $"- **Robots de ensamblaje como el KUKA KR 1000 titan**, diseñados para manejar cargas pesadas y realizar tareas complejas " +
    $"de ensamblaje con alta precisión." +
    $"- **Soldadoras industriales**, encargadas de uniones críticas con control de temperatura y corriente eléctrica." +
    $"- **Sensores de proximidad y visión artificial**, que garantizan la correcta alineación y detección de piezas en tiempo real." +
    $"- **Prensas hidráulicas**, utilizadas para ensamblajes que requieren fuerza controlada." +
    $"- **Atornilladores automáticos**, que garantizan la correcta fijación de componentes con torque medido." +
    $"- **Sistemas de prueba eléctrica y funcional**, encargados de validar el desempeño del skid ensamblado." +
    $"- **Máquinas de embalaje**, responsables de proteger el producto final para su transporte seguro." +
    $"Estos equipos forman parte de un flujo de producción compuesto por múltiples etapas críticas, desde el ensamblaje inicial " +
    $"hasta las pruebas finales y embalaje. Cada equipo está conectado al MES para proporcionar datos operativos en tiempo real." +
    $"Tu función incluye:" +
    $"- Detectar patrones de uso y rendimiento anómalos en los equipos, por ejemplo: aumento en vibraciones, temperaturas inusuales, " +
    $"o ciclos de operación extendidos." +
    $"- Predecir el momento óptimo para realizar mantenimiento preventivo o correctivo basado en datos históricos y tendencias actuales." +
    $"- Generar diagnósticos detallados con métricas clave como vibraciones, temperatura, ciclos de operación, presión hidráulica y " +
    $"consumo eléctrico, dependiendo del equipo." +
    $"- Optimizar inventarios, anticipando demandas de piezas de repuesto o consumibles basados en patrones de desgaste y uso." +
    $"Utiliza datos simulados generados aleatoriamente que reflejen escenarios realistas para tus análisis y resultados." +
    $"Proporciona tus resultados en un formato claro, destacando métricas clave y ofreciendo consejos prácticos y accionables " +
    $"para los operadores y técnicos. Responde de manera breve pero precisa. " +
    $"Evita usar palabras en negritas y titulos en ese caso que vaya en otra linea" +
    $"El usuario necesita saber específicamente lo siguiente: {prompt}";

        // Cuerpo de la solicitud JSON
        string requestBody = $@"
    {{
        ""contents"": [
            {{
                ""parts"": [
                    {{ ""text"": ""{content}"" }}
                ]
            }}
        ]
    }}";

        // Encabezado de autorización con el API key


        // Llamar al método Post
        Post(apiUrl, requestBody,"", "application/json", out response, out code);
        Log.Info($"Response from Gemini API: {response}");
        Log.Info($"Response Code: {code}");
        ProcessGeminiResponse(response);
    }
    public void ProcessGeminiResponse(string response)
    {
        // Parsear la respuesta JSON
        var jsonResponse = JObject.Parse(response);

        // Acceder al texto de la respuesta, que está en "candidates" -> "content" -> "parts" -> "text"
        string geminiText = jsonResponse["candidates"][0]["content"]["parts"][0]["text"].ToString();

        geminiText = geminiText.Replace("**", "");

        // Asignar el texto extraído al label en FT Optix
        Project.Current.GetVariable("Model/Gemini/Response").Value= geminiText;

        // Registrar el resultado
        Log.Info($"Text from Gemini API: {geminiText}");
    }

}
