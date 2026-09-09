using System.Text.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

public class OrdenImportService : IOrdenImportService
{
    private readonly IImportacionOrdenRepository _importacionRepository;
    private readonly IOrdenRepository _ordenRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IUbicacionRepository _ubicacionRepository;
    private readonly ITipoMercanciaRepository _tipoMercanciaRepository;
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;
    private readonly IAuditoriaRepository _auditoriaRepository;
    private readonly ILogger<OrdenImportService> _logger;

    public OrdenImportService(
        IImportacionOrdenRepository importacionRepository,
        IOrdenRepository ordenRepository,
        IClienteRepository clienteRepository,
        IUbicacionRepository ubicacionRepository,
        ITipoMercanciaRepository tipoMercanciaRepository,
        IUnidadMedidaRepository unidadMedidaRepository,
        IAuditoriaRepository auditoriaRepository,
        ILogger<OrdenImportService> logger)
    {
        _importacionRepository = importacionRepository;
        _ordenRepository = ordenRepository;
        _clienteRepository = clienteRepository;
        _ubicacionRepository = ubicacionRepository;
        _tipoMercanciaRepository = tipoMercanciaRepository;
        _unidadMedidaRepository = unidadMedidaRepository;
        _auditoriaRepository = auditoriaRepository;
        _logger = logger;
    }

    public async Task<string> ObtenerPlantillaCsvAsync()
    {
        var headers = "referencia_cliente,nombre_cliente,ciudad_origen,ciudad_destino,tipo_mercancia,cantidad,peso_kg,modo_transporte,nivel_servicio,fecha_pickup,fecha_entrega";
        var r1 = "REF-001,Cliente A,Bogotá,Medellín,Electrónicos,10,250.5,TERRESTRE,ESTANDAR,2026-10-01,2026-10-05";
        var r2 = "REF-002,Cliente B,Cali,Barranquilla,Textiles,50,500.0,TERRESTRE,EXPRESS,2026-10-02,2026-10-04";
        var r3 = "REF-003,Cliente A,Bogotá,Cartagena,Maquinaria,2,1500.0,TERRESTRE,PROGRAMADO,2026-10-10,2026-10-15";
        
        return await Task.FromResult(string.Join(Environment.NewLine, headers, r1, r2, r3));
    }

    public async Task<IEnumerable<ImportacionOrdenResultDto>> GetHistorialImportacionesAsync(Guid empresaId)
    {
        var historial = await _importacionRepository.GetAllAsync(empresaId);
        return historial.Select(h => new ImportacionOrdenResultDto
        {
            ImportacionId = h.Id,
            TotalFilas = h.TotalFilas,
            FilasOk = h.FilasOk,
            FilasError = h.FilasError,
            DetalleErrores = h.DetalleErrores != null 
                ? JsonSerializer.Deserialize<List<ErrorFilaOrdenDto>>(h.DetalleErrores) ?? new()
                : new()
        });
    }

    public async Task<ImportacionOrdenResultDto> ImportarCsvAsync(Stream csvStream, string nombreArchivo, Guid empresaId, Guid usuarioId)
    {
        var clientes = await _clienteRepository.GetAllAsync(empresaId);
        var ubicaciones = await _ubicacionRepository.GetAllAsync(empresaId);
        var tiposMercancia = await _tipoMercanciaRepository.GetAllAsync(empresaId);
        var unidadesMedida = await _unidadMedidaRepository.GetAllAsync(empresaId);
        
        var unidadPredeterminada = unidadesMedida.FirstOrDefault()?.Id; // Asumiremos la primera si no viene explícita o se usa default

        using var reader = new StreamReader(csvStream);
        var headersLine = await reader.ReadLineAsync(); // skip headers
        
        var ordenesValidas = new List<Orden>();
        var errores = new List<ErrorFilaOrdenDto>();
        
        int rowCount = 0;
        int filasOk = 0;
        int filasError = 0;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            rowCount++;
            var parts = line.Split(',');
            
            if (parts.Length < 11)
            {
                errores.Add(new ErrorFilaOrdenDto { Fila = rowCount, Campo = "Fila", Error = "No tiene suficientes columnas" });
                filasError++;
                continue;
            }

            var refCliente = parts[0].Trim();
            var nomCliente = parts[1].Trim();
            var cOrigen = parts[2].Trim();
            var cDestino = parts[3].Trim();
            var tMercancia = parts[4].Trim();
            
            if (!decimal.TryParse(parts[5].Trim(), out decimal cantidad) || cantidad <= 0)
            {
                errores.Add(new ErrorFilaOrdenDto { Fila = rowCount, Campo = "cantidad", Error = "Debe ser número mayor a cero" });
                filasError++;
                continue;
            }

            if (!decimal.TryParse(parts[6].Trim(), out decimal peso) || peso <= 0)
            {
                errores.Add(new ErrorFilaOrdenDto { Fila = rowCount, Campo = "peso_kg", Error = "Debe ser número mayor a cero" });
                filasError++;
                continue;
            }

            var modo = parts[7].Trim().ToUpper();
            var nivel = parts[8].Trim().ToUpper();
            
            DateOnly? fPick = null, fEnt = null;
            if (DateOnly.TryParse(parts[9].Trim(), out DateOnly fp)) fPick = fp;
            if (DateOnly.TryParse(parts[10].Trim(), out DateOnly fe)) fEnt = fe;

            var cliente = clientes.FirstOrDefault(c => c.Nombre.Equals(nomCliente, StringComparison.OrdinalIgnoreCase));
            if (cliente == null)
            {
                errores.Add(new ErrorFilaOrdenDto { Fila = rowCount, Campo = "nombre_cliente", Error = "No se encontró un cliente con ese nombre" }); // CA-09
                filasError++;
                continue;
            }

            var origen = ubicaciones.FirstOrDefault(u => u.Ciudad != null && u.Ciudad.Equals(cOrigen, StringComparison.OrdinalIgnoreCase));
            var destino = ubicaciones.FirstOrDefault(u => u.Ciudad != null && u.Ciudad.Equals(cDestino, StringComparison.OrdinalIgnoreCase));
            var tipoM = tiposMercancia.FirstOrDefault(t => t.Nombre.Equals(tMercancia, StringComparison.OrdinalIgnoreCase));

            if (origen == null) { errores.Add(new ErrorFilaOrdenDto { Fila = rowCount, Campo = "ciudad_origen", Error = "No encontrada" }); filasError++; continue; }
            if (destino == null) { errores.Add(new ErrorFilaOrdenDto { Fila = rowCount, Campo = "ciudad_destino", Error = "No encontrada" }); filasError++; continue; }
            if (tipoM == null) { errores.Add(new ErrorFilaOrdenDto { Fila = rowCount, Campo = "tipo_mercancia", Error = "No encontrada" }); filasError++; continue; }
            if (unidadPredeterminada == null) { errores.Add(new ErrorFilaOrdenDto { Fila = rowCount, Campo = "unidad_medida", Error = "No hay unidad de medida en el sistema" }); filasError++; continue; }

            var orden = new Orden
            {
                EmpresaId = empresaId,
                ClienteId = cliente.Id,
                OrigenId = origen.Id,
                DestinoId = destino.Id,
                TipoMercanciaId = tipoM.Id,
                UnidadMedidaId = unidadPredeterminada.Value,
                Cantidad = cantidad,
                PesoKg = peso,
                ModoTransporte = modo,
                NivelServicio = nivel,
                Prioridad = "NORMAL",
                FechaPickupSolicitada = fPick,
                FechaEntregaRequerida = fEnt,
                ReferenciaCliente = refCliente,
                Estado = OrdenEstado.Draft,
                OrigenCreacion = "CSV",
                CreadoPor = usuarioId,
                ModificadoPor = usuarioId
            };
            ordenesValidas.Add(orden);
            filasOk++;
        }

        if (ordenesValidas.Any())
        {
            await _ordenRepository.CreateBulkAsync(ordenesValidas);
        }

        var jsonErrores = JsonSerializer.Serialize(errores);

        var importacion = new ImportacionOrden
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            NombreArchivo = nombreArchivo,
            TotalFilas = rowCount,
            FilasOk = filasOk,
            FilasError = filasError,
            DetalleErrores = jsonErrores
        };
        var impId = await _importacionRepository.CreateAsync(importacion);

        await _auditoriaRepository.RegistrarAsync(new AuditoriaActividad
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Modulo = "ordenes",
            Accion = "IMPORTAR_ORDENES",
            EntidadId = impId,
            Detalles = JsonSerializer.Serialize(new { filasOk, filasError, nombreArchivo })
        });

        return new ImportacionOrdenResultDto
        {
            ImportacionId = impId,
            TotalFilas = rowCount,
            FilasOk = filasOk,
            FilasError = filasError,
            DetalleErrores = errores
        };
    }
}
