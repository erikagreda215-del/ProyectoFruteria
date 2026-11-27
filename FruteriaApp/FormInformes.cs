namespace FruteriaApp;

/// <summary>Informe diario (GET /informes/ventas-hoy).</summary>
public class FormInformes : Form
{
    private readonly ApiClient _api;

    private readonly Label _lblTotal = CrearValor();
    private readonly Label _lblNumVentas = CrearValor();
    private readonly Label _lblMasVendido = CrearValor();
    private readonly Label _lblFecha = new() { AutoSize = true, Padding = new Padding(10, 8, 0, 0) };
    private readonly DataGridView _grid = Ui.Tabla();

    public FormInformes(ApiClient api)
    {
        _api = api;
        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private static Label CrearValor() => new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 14f, FontStyle.Bold),
        Text = "-"
    };

    private static GroupBox Tarjeta(string titulo, Label valor)
    {
        var g = new GroupBox { Text = titulo, Dock = DockStyle.Fill, Margin = new Padding(8) };
        g.Controls.Add(valor);
        return g;
    }

    private void ConstruirInterfaz()
    {
        Text = "Informe de ventas de hoy";
        ClientSize = new Size(760, 460);
        MinimumSize = new Size(640, 380);

        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(6) };
        var btnRefrescar = Ui.Boton("Refrescar");
        barra.Controls.Add(btnRefrescar);
        barra.Controls.Add(_lblFecha);

        var tarjetas = new TableLayoutPanel { Dock = DockStyle.Top, Height = 110, ColumnCount = 3 };
        for (int i = 0; i < 3; i++) tarjetas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        tarjetas.Controls.Add(Tarjeta("Total vendido hoy", _lblTotal), 0, 0);
        tarjetas.Controls.Add(Tarjeta("Número de ventas", _lblNumVentas), 1, 0);
        tarjetas.Controls.Add(Tarjeta("Producto más vendido", _lblMasVendido), 2, 0);
        _lblMasVendido.Font = new Font("Segoe UI", 11f, FontStyle.Bold);

        var tituloDetalle = new Label { Text = "Detalle por producto", Dock = DockStyle.Top, Height = 28, Padding = new Padding(8, 6, 0, 0), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

        Controls.Add(_grid);
        Controls.Add(tituloDetalle);
        Controls.Add(tarjetas);
        Controls.Add(barra);

        btnRefrescar.Click += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            UseWaitCursor = true;
            InformeVentasHoy inf = await _api.GetInformeHoyAsync();

            _lblFecha.Text = $"Fecha: {inf.Fecha}";
            _lblTotal.Text = $"{inf.TotalVendido:0.00} €";
            _lblNumVentas.Text = inf.NumeroVentas.ToString();
            _lblMasVendido.Text = inf.ProductoMasVendido is null
                ? "Sin ventas"
                : $"{inf.ProductoMasVendido}\n({inf.KilosProductoMasVendido:0.##} kg)";

            _grid.DataSource = inf.Detalle;
            if (_grid.Columns.Count > 0)
            {
                _grid.Columns[nameof(DetalleInforme.Nombre)]!.HeaderText = "Producto";
                _grid.Columns[nameof(DetalleInforme.Kilos)]!.HeaderText = "Kilos vendidos";
                _grid.Columns[nameof(DetalleInforme.Importe)]!.DefaultCellStyle.Format = "0.00 €";
            }
        }
        catch (Exception ex) { Ui.Error(ex); }
        finally { UseWaitCursor = false; }
    }
}
