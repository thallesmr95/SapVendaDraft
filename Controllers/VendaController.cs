using Microsoft.AspNetCore.Mvc;
using SapVendaDraft.Models;
using System.Text;
using System.Text.Json;

namespace SapVendaDraft.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendaController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VendaController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> CriarEsbocoVenda([FromBody] VendaDto venda)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("NoSsl");

                // 1. Autenticar na Service Layer
                var loginData = new
                {
                    UserName = "manager",
                    Password = "ramo01",
                    CompanyDB = "INTERWAY"
                };

                var loginResponse = await client.PostAsync(
                    "https://10.11.100.140:50000/b1s/v1/Login",
                    new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json"));

                if (!loginResponse.IsSuccessStatusCode)
                    return StatusCode(500, "Erro ao autenticar no SAP");

                var loginContent = await loginResponse.Content.ReadAsStringAsync();

                // Inspeciona a resposta de login
                Console.WriteLine("Resposta do login SAP:");
                Console.WriteLine(loginContent);

                // Tenta parsear a resposta
                var loginJson = JsonDocument.Parse(loginContent).RootElement;

                // Verifica se a resposta contém o SessionId
                if (!loginJson.TryGetProperty("SessionId", out var sessionIdProp))
                {
                    return StatusCode(500, $"Resposta de login inválida: SessionId ausente.\nResposta: {loginContent}");
                }

                var sessionId = sessionIdProp.GetString();
                var cookieHeader = $"B1SESSION={sessionId}";

                // Se houver RouteId, adiciona ao cookie
                if (loginJson.TryGetProperty("RouteId", out var routeIdProp))
                {
                    var routeId = routeIdProp.GetString();
                    cookieHeader += $"; RouteId={routeId}";
                }

                client.DefaultRequestHeaders.Add("Cookie", cookieHeader);

                // 2. Montar corpo do Draft
                var draft = new
                {
                    CardCode = venda.CardCode,
                    DocObjectCode = "17", // 17 = Pedido de venda - Draft
                    BPL_IDAssignedToInvoice = 1, // <- Substitua pelo ID real da filial ativa no seu SAP
                    DocumentLines = venda.Itens.Select(i => new
                    {
                        ItemCode = i.ItemCode,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        BPLId = 1 // <- Também precisa informar aqui, por linha
                    })
                };

                // 3. Enviar Draft
                var draftResponse = await client.PostAsync(
                    "https://10.11.100.140:50000/b1s/v1/Drafts",
                    new StringContent(JsonSerializer.Serialize(draft), Encoding.UTF8, "application/json"));

                if (!draftResponse.IsSuccessStatusCode)
                {
                    var errorContent = await draftResponse.Content.ReadAsStringAsync();
                    Console.WriteLine("Erro ao criar draft:");
                    Console.WriteLine(errorContent);

                    return StatusCode(500, $"Erro ao criar rascunho de venda. Detalhes: {errorContent}");
                }


                var result = await draftResponse.Content.ReadAsStringAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro: {ex.Message}");
            }
        }
    }
}
