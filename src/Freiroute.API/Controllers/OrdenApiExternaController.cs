using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Utility.ApiResponse;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoint público (protegido por X-Api-Key) para la ingesta de órdenes
/// desde sistemas externos (HU-023).
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public class OrdenApiExternaController : ControllerBase
{
    private readonly IOrdenApiExternaService _apiExternaService;

    public OrdenApiExternaController(IOrdenApiExternaService apiExternaService)
    {
        _apiExternaService = apiExternaService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrdenResponseDto>>> CreateOrder([FromBody] OrdenRequestDto request)
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey))
        {
            return Unauthorized(ApiResponse<OrdenResponseDto>.Fail("API Key no proporcionada. Use el header X-Api-Key."));
        }

        var apiKey = extractedApiKey.ToString();
        var empresaId = await _apiExternaService.ValidarApiKeyAsync(apiKey);

        if (empresaId == null)
        {
            return Unauthorized(ApiResponse<OrdenResponseDto>.Fail("API Key inválida o inactiva."));
        }

        var result = await _apiExternaService.CrearOrdenDesdeApiAsync(request, empresaId.Value);
        return Created("", ApiResponse<OrdenResponseDto>.Ok(result, "Orden recibida correctamente"));
    }
}
