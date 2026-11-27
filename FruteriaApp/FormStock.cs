namespace FruteriaApp;

/// <summary>Control de stock: filtro de stock bajo, colores por CellFormatting y reposición.</summary>
public class FormStock : Form
{
    private readonly ApiClient _api;
    private List<Producto> _productos = new();

    private readonly DataGridView _grid = Ui.Tabla();
    private readonly CheckBox _chkSoloBajo = new() { Text = "Solo stock bajo", AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
    private readonly NumericUpDown _numUmbral = new() { Width = 70, DecimalPlaces = 1, Minimum = 0, Maximum = 1000, Value = 5 };
    private readonly NumericUpDown _numReponer = new() { Width = 80, DecimalPlaces = 2, Increment = 1, Minimum = 0.01m, Maximum = 10000, Value = 10 };
    private readonly Button _btnRefrescar = Ui.Boton("Refrescar");
    private readonly Button _btnReponer = Ui.Boton("Reponer", 100);
    private Font? _fuenteNegrita;
    private readonly Label _lblResumen = new() { AutoSize = true, Padding = new Padding(10, 8, 0, 0) };

    public FormStock(ApiClient api)
    {
        _api = api;
        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        Text = "Control de stock";
        ClientSize = new Size(820, 500);
        MinimumSize = new Size(700, 400);

        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(6) };
        barra.Controls.Add(_chkSoloBajo);
        barra.Controls.Add(new Label { Text = "Umbral (kg):", AutoSize = true, Padding = new Padding(10, 8, 0, 0) });
        barra.Controls.Add(_numUmbral);
        barra.Controls.Add(_btnRefrescar);
        barra.Controls.Add(_lblResumen);

        var abajo = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(6) };
        abajo.Controls.Add(new Label { Text = "Reponer producto seleccionado (+kg):", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        abajo.Controls.Add(_numReponer);
        abajo.Controls.Add(_btnReponer);
        abajo.Controls.Add(new Label
        {
            Text = "■ Crítico (≤ umbral)   ■ Bajo (≤ 2× umbral)",
            AutoSize = true,
            Padding = new Padding(20, 8, 0, 0)
        });

        Controls.Add(_grid);
        Controls.Add(barra);
        Controls.Add(abajo);

        _btnRefrescar.Click += async (_, _) => await CargarAsync();
        _btnReponer.Click += async (_, _) => await ReponerAsync();
        _chkSoloBajo.CheckedChanged += (_, _) => Mostrar();
        _numUmbral.ValueChanged += (_, _) => Mostrar();
        _grid.CellFormatting += Grid_CellFormatting;
    }

    private async Task CargarAsync()
    {
        try
        {
            UseWaitCursor = true;
            _productos = await _api.GetProductosAsync();
            Mostrar();
        }
        catch (Exception ex) { Ui.Error(ex); }
        finally { UseWaitCursor = false; }
    }

    private void Mostrar()
    {
        decimal umbral = _numUmbral.Value;
        var lista = _productos
            .Where(p => !_chkSoloBajo.Checked || p.StockKg <= umbral * 2)
            .OrderBy(p => p.StockKg)
            .ToList();

        _grid.DataSource = lista;
        Ui.FormatearColumnasProducto(_grid);

        int criticos = _productos.Count(p => p.StockKg <= umbral);
        _lblResumen.Text = $"{criticos} producto(s) en estado crítico";
        _lblResumen.ForeColor = criticos > 0 ? Color.Firebrick : SystemColors.ControlText;
    }

    // Colorea toda la fila según el stock del producto
    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.CellStyle is null) return;
        if (_grid.Rows[e.RowIndex].DataBoundItem is not Producto p) return;

        decimal umbral = _numUmbral.Value;
        if (p.StockKg <= umbral)
        {
            e.CellStyle.BackColor = Ui.RojoCritico;
            e.CellStyle.ForeColor = Color.DarkRed;
            e.CellStyle.Font = _fuenteNegrita ??= new Font(_grid.Font, FontStyle.Bold);
        }
        else if (p.StockKg <= umbral * 2)
        {
            e.CellStyle.BackColor = Ui.NaranjaAviso;
        }
    }

    private async Task ReponerAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Producto p)
        {
            Ui.Info("Selecciona un producto de la lista.");
            return;
        }

        var entrada = ProductoEntrada.Desde(p);
        entrada.StockKg += _numReponer.Value;
        try
        {
            await _api.ActualizarProductoAsync(p.Id, entrada);
            await CargarAsync();
        }
        catch (Exception ex) { Ui.Error(ex); }
    }
}
