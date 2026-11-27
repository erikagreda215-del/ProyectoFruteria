namespace FruteriaApp;

/// <summary>Producto tal y como lo devuelve/recibe la API (/productos).</summary>
public class Producto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public decimal PrecioKg { get; set; }
    public decimal StockKg { get; set; }

    // Texto que se ve en los ComboBox
    public override string ToString() => $"{Nombre} ({PrecioKg:0.00} €/kg · {StockKg:0.##} kg)";
}

/// <summary>Cuerpo que se envía en POST/PUT /productos (sin id: lo pone la API).</summary>
public class ProductoEntrada
{
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public decimal PrecioKg { get; set; }
    public decimal StockKg { get; set; }

    public static ProductoEntrada Desde(Producto p) => new()
    {
        Nombre = p.Nombre,
        Categoria = p.Categoria,
        PrecioKg = p.PrecioKg,
        StockKg = p.StockKg
    };
}

/// <summary>Línea que se envía al registrar una venta.</summary>
public class LineaVentaEntrada
{
    public int ProductoId { get; set; }
    public decimal Kilos { get; set; }
}

/// <summary>Cuerpo de POST /ventas.</summary>
public class VentaEntrada
{
    public List<LineaVentaEntrada> Lineas { get; set; } = new();
}

/// <summary>Línea de una venta ya registrada (respuesta de la API).</summary>
public class LineaVenta
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Kilos { get; set; }
    public decimal PrecioKg { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Venta registrada (respuesta de GET/POST /ventas).</summary>
public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public List<LineaVenta> Lineas { get; set; } = new();
    public decimal Total { get; set; }
}

/// <summary>Detalle por producto del informe diario.</summary>
public class DetalleInforme
{
    public string Nombre { get; set; } = "";
    public decimal Kilos { get; set; }
    public decimal Importe { get; set; }
}

/// <summary>Respuesta de GET /informes/ventas-hoy.</summary>
public class InformeVentasHoy
{
    public string Fecha { get; set; } = "";
    public decimal TotalVendido { get; set; }
    public int NumeroVentas { get; set; }
    public string? ProductoMasVendido { get; set; }
    public decimal KilosProductoMasVendido { get; set; }
    public List<DetalleInforme> Detalle { get; set; } = new();
}
