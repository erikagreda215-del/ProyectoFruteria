using System.Globalization;

namespace FruteriaApp;

/// <summary>Registro de ventas: carrito con recálculo de totales e historial de ventas.</summary>
public class FormVentas : Form
{
    private readonly ApiClient _api;

    // --- Pestaña "Nueva venta"
    private readonly ComboBox _cmbProducto = new() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _numKilos = new() { Width = 90, DecimalPlaces = 2, Increment = 0.25m, Minimum = 0.01m, Maximum = 10000, Value = 1 };
    private readonly Button _btnAnadir = Ui.Boton("Añadir al carrito", 150);
    private readonly DataGridView _gridCarrito = Ui.Tabla();
    private readonly Button _btnQuitar = Ui.Boton("Quitar línea");
    private readonly Button _btnVaciar = Ui.Boton("Vaciar");
    private readonly Button _btnConfirmar = Ui.Boton("Confirmar venta", 160);
    private readonly Label _lblTotal = new() { AutoSize = true, Font = new Font("Segoe UI", 12f, FontStyle.Bold), Padding = new Padding(20, 6, 0, 0) };

    // --- Pestaña "Historial"
    private readonly DataGridView _gridVentas = Ui.Tabla();
    private readonly DataGridView _gridLineas = Ui.Tabla();
    private List<Venta> _ventas = new();

    private const string ColKilos = "Kilos";
    private const string ColSubtotal = "Subtotal";
    private const string ColPrecio = "Precio";

    public FormVentas(ApiClient api)
    {
        _api = api;
        ConstruirInterfaz();
        Load += async (_, _) => { await CargarProductosAsync(); await CargarHistorialAsync(); };
    }

    private void ConstruirInterfaz()
    {
        Text = "Registro de ventas";
        ClientSize = new Size(860, 540);
        MinimumSize = new Size(760, 450);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var tabNueva = new TabPage("Nueva venta");
        var tabHistorial = new TabPage("Historial de ventas");
        tabs.TabPages.Add(tabNueva);
        tabs.TabPages.Add(tabHistorial);
        Controls.Add(tabs);

        // ---------- Nueva venta ----------
        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(6) };
        barra.Controls.Add(new Label { Text = "Producto:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        barra.Controls.Add(_cmbProducto);
        barra.Controls.Add(new Label { Text = "Kilos:", AutoSize = true, Padding = new Padding(10, 8, 0, 0) });
        barra.Controls.Add(_numKilos);
        barra.Controls.Add(_btnAnadir);

        _gridCarrito.ReadOnly = false;
        _gridCarrito.Columns.Add("Producto", "Producto");
        _gridCarrito.Columns.Add(ColPrecio, "Precio/kg");
        _gridCarrito.Columns.Add(ColKilos, "Kilos (editable)");
        _gridCarrito.Columns.Add(ColSubtotal, "Subtotal");
        foreach (DataGridViewColumn c in _gridCarrito.Columns) c.ReadOnly = c.Name != ColKilos;
        _gridCarrito.Columns[ColPrecio]!.DefaultCellStyle.Format = "0.00 €";
        _gridCarrito.Columns[ColSubtotal]!.DefaultCellStyle.Format = "0.00 €";

        var abajo = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(6) };
        abajo.Controls.AddRange(new Control[] { _btnQuitar, _btnVaciar, _btnConfirmar, _lblTotal });

        tabNueva.Controls.Add(_gridCarrito);
        tabNueva.Controls.Add(barra);
        tabNueva.Controls.Add(abajo);

        // ---------- Historial ----------
        // Arriba la lista de ventas, abajo las líneas de la venta seleccionada (50% / 50%)
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        split.Controls.Add(_gridVentas, 0, 0);
        split.Controls.Add(_gridLineas, 0, 1);
        var btnRefrescarHist = Ui.Boton("Refrescar");
        var barraHist = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6) };
        barraHist.Controls.Add(btnRefrescarHist);
        barraHist.Controls.Add(new Label { Text = "Selecciona una venta para ver sus líneas", AutoSize = true, Padding = new Padding(10, 8, 0, 0) });
        tabHistorial.Controls.Add(split);
        tabHistorial.Controls.Add(barraHist);

        // ---------- Eventos ----------
        _btnAnadir.Click += (_, _) => AnadirAlCarrito();
        _btnQuitar.Click += (_, _) => QuitarLinea();
        _btnVaciar.Click += (_, _) => { _gridCarrito.Rows.Clear(); RecalcularTotal(); };
        _btnConfirmar.Click += async (_, _) => await ConfirmarVentaAsync();
        _gridCarrito.CellValueChanged += GridCarrito_CellValueChanged;
        _gridCarrito.DataError += (_, e) => e.Cancel = true;
        btnRefrescarHist.Click += async (_, _) => await CargarHistorialAsync();
        _gridVentas.SelectionChanged += (_, _) => MostrarLineasSeleccionadas();

        RecalcularTotal();
    }

    // ------------------------------------------------------------------ datos
    private async Task CargarProductosAsync()
    {
        try
        {
            int? idActual = (_cmbProducto.SelectedItem as Producto)?.Id;
            var productos = (await _api.GetProductosAsync())
                .Where(p => p.StockKg > 0)
                .OrderBy(p => p.Nombre)
                .ToArray();
            _cmbProducto.Items.Clear();
            _cmbProducto.Items.AddRange(productos);
            _cmbProducto.SelectedItem = productos.FirstOrDefault(p => p.Id == idActual) ?? productos.FirstOrDefault();
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private async Task CargarHistorialAsync()
    {
        try
        {
            _ventas = (await _api.GetVentasAsync()).OrderByDescending(v => v.Fecha).ToList();
            _gridVentas.DataSource = _ventas
                .Select(v => new { v.Id, Fecha = v.Fecha.ToString("dd/MM/yyyy HH:mm"), Lineas = v.Lineas.Count, v.Total })
                .ToList();
            if (_gridVentas.Columns.Count > 0)
            {
                _gridVentas.Columns["Lineas"]!.HeaderText = "Nº líneas";
                _gridVentas.Columns["Total"]!.DefaultCellStyle.Format = "0.00 €";
            }
            MostrarLineasSeleccionadas();
        }
        catch (Exception ex) { Ui.Error(ex); }
    }

    private void MostrarLineasSeleccionadas()
    {
        int index = _gridVentas.CurrentRow?.Index ?? -1;
        var venta = index >= 0 && index < _ventas.Count ? _ventas[index] : null;
        _gridLineas.DataSource = venta?.Lineas.ToList() ?? new List<LineaVenta>();
        if (_gridLineas.Columns.Count > 0)
        {
            _gridLineas.Columns[nameof(LineaVenta.ProductoId)]!.HeaderText = "ID producto";
            _gridLineas.Columns[nameof(LineaVenta.PrecioKg)]!.DefaultCellStyle.Format = "0.00 €";
            _gridLineas.Columns[nameof(LineaVenta.Subtotal)]!.DefaultCellStyle.Format = "0.00 €";
        }
    }

    // ---------------------------------------------------------------- carrito
    private void AnadirAlCarrito()
    {
        if (_cmbProducto.SelectedItem is not Producto p)
        {
            Ui.Info("Selecciona un producto.");
            return;
        }

        decimal kilos = _numKilos.Value;

        // Si ya está en el carrito, se suman los kilos
        DataGridViewRow? fila = _gridCarrito.Rows.Cast<DataGridViewRow>()
            .FirstOrDefault(r => (r.Tag as Producto)?.Id == p.Id);
        decimal yaEnCarrito = fila is null ? 0 : LeerKilos(fila);

        if (yaEnCarrito + kilos > p.StockKg)
        {
            MessageBox.Show($"Solo quedan {p.StockKg:0.##} kg de {p.Nombre}.", "Stock insuficiente",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (fila is null)
        {
            int i = _gridCarrito.Rows.Add(p.Nombre, p.PrecioKg, kilos, p.PrecioKg * kilos);
            _gridCarrito.Rows[i].Tag = p;
        }
        else
        {
            fila.Cells[ColKilos].Value = yaEnCarrito + kilos;   // dispara CellValueChanged
        }
        RecalcularTotal();
    }

    private void QuitarLinea()
    {
        if (_gridCarrito.CurrentRow is { } fila)
        {
            _gridCarrito.Rows.Remove(fila);
            RecalcularTotal();
        }
    }

    // Se recalcula el subtotal cuando el usuario edita los kilos de una línea
    private void GridCarrito_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _gridCarrito.Columns[e.ColumnIndex].Name != ColKilos) return;

        var fila = _gridCarrito.Rows[e.RowIndex];
        if (fila.Tag is not Producto p) return;

        decimal kilos = LeerKilos(fila);
        if (kilos <= 0 || kilos > p.StockKg)
        {
            MessageBox.Show($"Kilos no válidos. Debe ser mayor que 0 y como máximo {p.StockKg:0.##} kg.",
                "Valor no válido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            kilos = Math.Clamp(kilos, 0.01m, p.StockKg);
            fila.Cells[ColKilos].Value = kilos;    // vuelve a entrar aquí ya con un valor válido
            return;
        }

        fila.Cells[ColSubtotal].Value = Math.Round(kilos * p.PrecioKg, 2);
        RecalcularTotal();
    }

    private static decimal LeerKilos(DataGridViewRow fila)
    {
        object? v = fila.Cells[ColKilos].Value;
        if (v is decimal d) return d;
        string texto = Convert.ToString(v, CultureInfo.CurrentCulture) ?? "";
        // Acepta tanto "1,5" como "1.5"
        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out d)) return d;
        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out d)) return d;
        return 0;
    }

    private decimal RecalcularTotal()
    {
        decimal total = _gridCarrito.Rows.Cast<DataGridViewRow>()
            .Sum(r => Convert.ToDecimal(r.Cells[ColSubtotal].Value ?? 0m));
        _lblTotal.Text = $"TOTAL: {total:0.00} €";
        _btnConfirmar.Enabled = _gridCarrito.Rows.Count > 0;
        return total;
    }

    private async Task ConfirmarVentaAsync()
    {
        if (_gridCarrito.Rows.Count == 0) return;
        _gridCarrito.EndEdit();   // por si hay una celda a medio editar

        var venta = new VentaEntrada
        {
            Lineas = _gridCarrito.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Tag is Producto)
                .Select(r => new LineaVentaEntrada { ProductoId = ((Producto)r.Tag!).Id, Kilos = LeerKilos(r) })
                .ToList()
        };

        try
        {
            _btnConfirmar.Enabled = false;
            Venta registrada = await _api.RegistrarVentaAsync(venta);
            Ui.Info($"Venta #{registrada.Id} registrada.\nTotal: {registrada.Total:0.00} €");
            _gridCarrito.Rows.Clear();
            await CargarProductosAsync();    // el stock ha cambiado
            await CargarHistorialAsync();
        }
        catch (Exception ex) { Ui.Error(ex); }
        finally { RecalcularTotal(); }
    }
}
