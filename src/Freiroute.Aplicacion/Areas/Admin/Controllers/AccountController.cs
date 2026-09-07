using Freiroute.Aplicacion.Areas.Admin.Models;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController : BaseAdminController
{
    private readonly IUsuarioService _usuarioService;
    private readonly IConfiguracion2faRepository _configuracion2faRepository;
    private readonly IStorageService _storageService;

    public AccountController(
        IUsuarioService usuarioService,
        IConfiguracion2faRepository configuracion2faRepository,
        IStorageService storageService)
    {
        _usuarioService = usuarioService;
        _configuracion2faRepository = configuracion2faRepository;
        _storageService = storageService;
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var usuarioId = UsuarioId;
        var empresaId = EmpresaId;

        if (usuarioId == Guid.Empty || empresaId == Guid.Empty)
        {
            return RedirectToAction("Login", "Account", new { area = "Auth" });
        }

        var usuario = await _usuarioService.GetByIdAsync(usuarioId, empresaId);
        if (usuario == null)
        {
            return NotFound();
        }

        var config2fa = await _configuracion2faRepository.GetByUsuarioIdAsync(usuarioId, empresaId);

        var fotoUrl = usuario.FotoUrl;
        if (!string.IsNullOrEmpty(fotoUrl) && !fotoUrl.StartsWith("http"))
        {
            // Es una ruta de Supabase Storage, generar signed URL temporal
            fotoUrl = await _storageService.GetSignedUrlAsync("avatares-usuarios", fotoUrl);
        }

        var viewModel = new ProfileViewModel
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            FotoUrl = fotoUrl,
            PerfilNombre = usuario.PerfilNombre,
            TotpHabilitado = config2fa?.TotpHabilitado ?? false
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateFoto(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Archivo inválido" });

        var extension = Path.GetExtension(file.FileName);
        var nombreArchivo = $"{UsuarioId}{extension}";
        var rutaBase = $"{EmpresaId}";

        using var stream = file.OpenReadStream();
        var rutaAlmacenada = await _storageService.UploadAsync(
            bucket: "avatares-usuarios",
            path: rutaBase,
            fileName: nombreArchivo,
            stream: stream,
            contentType: file.ContentType
        );

        await _usuarioService.UpdateFotoAsync(UsuarioId, EmpresaId, rutaAlmacenada);
        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteFoto()
    {
        await _usuarioService.UpdateFotoAsync(UsuarioId, EmpresaId, null!);
        return Ok(new { success = true });
    }
}
