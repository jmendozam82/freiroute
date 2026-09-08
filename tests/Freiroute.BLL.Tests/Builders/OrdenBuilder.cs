using System;
using Freiroute.Entity;
using Freiroute.DTO.Orden;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Tests.Builders;

public class OrdenBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _empresaId = Guid.NewGuid();
    private Guid _clienteId = Guid.NewGuid();
    private Guid _origenId = Guid.NewGuid();
    private Guid _destinoId = Guid.NewGuid();
    private string _estado = OrdenEstado.Draft;
    private string _prioridad = OrdenPrioridad.Normal;
    private decimal _pesoKg = 100m;
    private decimal _cantidad = 10m;

    public OrdenBuilder ConId(Guid id) { _id = id; return this; }
    public OrdenBuilder ConEmpresa(Guid id) { _empresaId = id; return this; }
    public OrdenBuilder ConEstado(string e) { _estado = e; return this; }
    public OrdenBuilder ConPrioridad(string p) { _prioridad = p; return this; }
    public OrdenBuilder ConPeso(decimal kg) { _pesoKg = kg; return this; }
    public OrdenBuilder ConCantidad(decimal c) { _cantidad = c; return this; }
    public OrdenBuilder ConCliente(Guid id) { _clienteId = id; return this; }
    public OrdenBuilder ConOrigenDestino(Guid origen, Guid destino) { _origenId = origen; _destinoId = destino; return this; }

    public Orden Build() => new()
    {
        Id = _id,
        EmpresaId = _empresaId,
        ClienteId = _clienteId,
        OrigenId = _origenId,
        DestinoId = _destinoId,
        Estado = _estado,
        Prioridad = _prioridad,
        PesoKg = _pesoKg,
        Cantidad = _cantidad,
        ModoTransporte = "TERRESTRE",
        NivelServicio = NivelServicio.Estandar,
        OrigenCreacion = OrigenCreacion.Manual,
        Activo = true,
        FechaCreacion = DateTime.UtcNow,
        FechaModificacion = DateTime.UtcNow
    };

    public OrdenRequestDto BuildRequestDto() => new()
    {
        ClienteId = _clienteId,
        OrigenId = _origenId,
        DestinoId = _destinoId,
        TipoMercanciaId = Guid.NewGuid(),
        UnidadMedidaId = Guid.NewGuid(),
        Cantidad = _cantidad,
        PesoKg = _pesoKg,
        ModoTransporte = "TERRESTRE",
        NivelServicio = NivelServicio.Estandar,
        Prioridad = _prioridad,
        Lineas = new System.Collections.Generic.List<LineaOrdenRequestDto>
        {
            new LineaOrdenRequestDto
            {
                Descripcion = "Linea de prueba",
                Cantidad = _cantidad,
                UnidadMedidaId = Guid.NewGuid(),
                PesoKg = _pesoKg,
                ValorUnitario = 10.5m
            }
        }
    };
}
