namespace FruteriaApp;

/// <summary>Diálogo para crear (POST) o editar (PUT) un producto.</summary>
public class FormProducto : Form
{
    private readonly ApiClient _api;
    private readonly Producto? _original;   // null = producto nuevo

    private readonly TextBox _txtNombre = new() { Width = 220 };
    private readonly ComboBox _cmbCategoria = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDown };
    private readonly NumericUpDown _numPrecio = new() { Width = 120, DecimalPlaces = 2, Increment = 0.05m, Minimum = 0.01m, Maximum = 1000 };
    private readonly NumericUpDown _numStock = new() { Width = 120, DecimalPlaces = 2, Increment = 0.5m, Minimum = 0, Maximum = 100000 };

    public FormProducto(ApiClient api, Producto? producto, IEnumerable<string> categorias)
    {
        _api = api;
        _original = producto;
        _cmbCategoria.Items.AddRange(categorias.Cast<object>().ToArray());
        ConstruirInterfaz();

        if (producto != null)
        {
            _txtNombre.Text = producto.Nombre;
            _cmbCategoria.Text = producto.Categoria;
            _numPrecio.Value = Math.Clamp(producto.PrecioKg, _numPrecio.Minimum, _numPrecio.Maximum);
            _numStock.Value = Math.Clamp(producto.StockKg, _numStock.Minimum, _numStock.Maximum);
        }
    }

    private void ConstruirInterfaz()
    {
        Text = _original is null ? "Nuevo producto" : $"Editar producto #{_original.Id}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(380, 240);

        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12) };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        void Fila(string texto, Control c)
        {
            tabla.Controls.Add(new Label { Text = texto, AutoSize = true, Anchor = AnchorStyles.Left });
            tabla.Controls.Add(c);
        }
        Fila("Nombre:", _txtNombre);
        Fila("Categoría:", _cmbCategoria);
        Fila("Precio (€/kg):", _numPrecio);
        Fila("Stock (kg):", _numStock);

        var btnGuardar = Ui.Boton("Guardar");
        var btnCancelar = Ui.Boton("Cancelar");
        btnCancelar.DialogResult = DialogResult.Cancel;
        btnGuardar.Click += async (_, _) => await GuardarAsync(btnGuardar);

        var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        botones.Controls.Add(btnCancelar);
        botones.Controls.Add(btnGuardar);

        Controls.Add(tabla);
        Controls.Add(botones);
        AcceptButton = btnGuardar;
        CancelButton = btnCancelar;
    }

    private async Task GuardarAsync(Button btn)
    {
        string nombre = _txtNombre.Text.Trim();
        string categoria = _cmbCategoria.Text.Trim();
        if (nombre == "" || categoria == "")
        {
            MessageBox.Show("El nombre y la categoría son obligatorios.", "Datos incompletos",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var entrada = new ProductoEntrada
        {
            Nombre = nombre,
            Categoria = categoria,
            PrecioKg = _numPrecio.Value,
            StockKg = _numStock.Value
        };

        try
        {
            btn.Enabled = false;
            if (_original is null)
                await _api.CrearProductoAsync(entrada);
            else
                await _api.ActualizarProductoAsync(_original.Id, entrada);

            DialogResult = DialogResult.OK;   // cierra el diálogo
        }
        catch (Exception ex) { Ui.Error(ex); }
        finally { btn.Enabled = true; }
    }
}
