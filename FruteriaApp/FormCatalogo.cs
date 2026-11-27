namespace FruteriaApp;

/// <summary>Catálogo: listado, búsqueda, filtro por categoría y CRUD de productos.</summary>
public class FormCatalogo : Form
{
    private readonly ApiClient _api;
    private List<Producto> _productos = new();
    private Producto? _seleccionado;

    private readonly DataGridView _grid = Ui.Tabla();
    private readonly TextBox _txtBuscar = new() { Width = 200, PlaceholderText = "Buscar por nombre..." };
    private readonly ComboBox _cmbCategoria = new() { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _btnRefrescar = Ui.Boton("Refrescar");
    private readonly Button _btnNuevo = Ui.Boton("Nuevo");
    private readonly Button _btnEditar = Ui.Boton("Editar");
    private readonly Button _btnEliminar = Ui.Boton("Eliminar");
    private readonly Label _lblTotal = new() { AutoSize = true, Padding = new Padding(10, 8, 0, 0) };

    private const string Todas = "(Todas)";

    public FormCatalogo(ApiClient api)
    {
        _api = api;
        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        Text = "Catálogo de productos";
        ClientSize = new Size(820, 500);
        MinimumSize = new Size(700, 400);

        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(6) };
        barra.Controls.Add(new Label { Text = "Buscar:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        barra.Controls.Add(_txtBuscar);
        barra.Controls.Add(new Label { Text = "Categoría:", AutoSize = true, Padding = new Padding(10, 8, 0, 0) });
        barra.Controls.Add(_cmbCategoria);
        barra.Controls.Add(_lblTotal);

        var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(6) };
        botones.Controls.AddRange(new Control[] { _btnRefrescar, _btnNuevo, _btnEditar, _btnEliminar });

        Controls.Add(_grid);
        Controls.Add(barra);
        Controls.Add(botones);

        // Eventos
        _btnRefrescar.Click += async (_, _) => await CargarAsync();
        _btnNuevo.Click += async (_, _) => await AbrirEdicionAsync(null);
        _btnEditar.Click += async (_, _) => await AbrirEdicionAsync(_seleccionado);
        _btnEliminar.Click += async (_, _) => await EliminarAsync();
        _grid.SelectionChanged += Grid_SelectionChanged;
        _grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0) await AbrirEdicionAsync(_seleccionado); };
        _txtBuscar.TextChanged += (_, _) => AplicarFiltros();
        _cmbCategoria.SelectedIndexChanged += (_, _) => AplicarFiltros();
        ActualizarBotones();
    }

    private async Task CargarAsync()
    {
        try
        {
            UseWaitCursor = true;
            _productos = await _api.GetProductosAsync();

            // Rellenar categorías manteniendo la elegida
            string actual = _cmbCategoria.SelectedItem as string ?? Todas;
            _cmbCategoria.Items.Clear();
            _cmbCategoria.Items.Add(Todas);
            foreach (var c in _productos.Select(p => p.Categoria).Distinct().OrderBy(c => c))
                _cmbCategoria.Items.Add(c);
            _cmbCategoria.SelectedItem = _cmbCategoria.Items.Contains(actual) ? actual : Todas;

            AplicarFiltros();
        }
        catch (Exception ex) { Ui.Error(ex); }
        finally { UseWaitCursor = false; }
    }

    private void AplicarFiltros()
    {
        string texto = _txtBuscar.Text.Trim();
        string? categoria = _cmbCategoria.SelectedItem as string;

        var filtrados = _productos
            .Where(p => texto == "" || p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .Where(p => categoria is null || categoria == Todas || p.Categoria == categoria)
            .OrderBy(p => p.Id)
            .ToList();

        _grid.DataSource = filtrados;
        Ui.FormatearColumnasProducto(_grid);
        _lblTotal.Text = $"{filtrados.Count} de {_productos.Count} productos";
        if (filtrados.Count == 0) { _seleccionado = null; ActualizarBotones(); }
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        _seleccionado = _grid.CurrentRow?.DataBoundItem as Producto;
        ActualizarBotones();
    }

    private void ActualizarBotones()
    {
        _btnEditar.Enabled = _seleccionado != null;
        _btnEliminar.Enabled = _seleccionado != null;
    }

    private async Task AbrirEdicionAsync(Producto? producto)
    {
        var categorias = _productos.Select(p => p.Categoria).Distinct().OrderBy(c => c);
        using var f = new FormProducto(_api, producto, categorias);
        if (f.ShowDialog(this) == DialogResult.OK)
            await CargarAsync();
    }

    private async Task EliminarAsync()
    {
        if (_seleccionado is null) return;
        var r = MessageBox.Show($"¿Eliminar \"{_seleccionado.Nombre}\"?", "Confirmar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (r != DialogResult.Yes) return;

        try
        {
            await _api.EliminarProductoAsync(_seleccionado.Id);
            await CargarAsync();
        }
        catch (Exception ex) { Ui.Error(ex); }
    }
}
