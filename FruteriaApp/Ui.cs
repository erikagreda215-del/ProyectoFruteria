namespace FruteriaApp;

/// <summary>Utilidades comunes de interfaz (controles y mensajes) para no repetir código.</summary>
internal static class Ui
{
    // Colores del control de stock
    public static readonly Color RojoCritico = Color.FromArgb(255, 205, 210);
    public static readonly Color NaranjaAviso = Color.FromArgb(255, 236, 179);

    public static Button Boton(string texto, int ancho = 100) => new()
    {
        Text = texto,
        Width = ancho,
        Height = 30,
        Margin = new Padding(4)
    };

    public static DataGridView Tabla() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false,
        BackgroundColor = SystemColors.Window
    };

    /// <summary>Pone cabeceras y formato a las columnas de un DataGridView enlazado a Producto.</summary>
    public static void FormatearColumnasProducto(DataGridView g)
    {
        if (g.Columns.Count == 0) return;
        g.Columns[nameof(Producto.Id)]!.HeaderText = "ID";
        g.Columns[nameof(Producto.Id)]!.FillWeight = 30;
        g.Columns[nameof(Producto.Nombre)]!.HeaderText = "Nombre";
        g.Columns[nameof(Producto.Categoria)]!.HeaderText = "Categoría";
        g.Columns[nameof(Producto.PrecioKg)]!.HeaderText = "Precio/kg";
        g.Columns[nameof(Producto.PrecioKg)]!.DefaultCellStyle.Format = "0.00 €";
        g.Columns[nameof(Producto.StockKg)]!.HeaderText = "Stock (kg)";
        g.Columns[nameof(Producto.StockKg)]!.DefaultCellStyle.Format = "0.00";
    }

    public static void Error(Exception ex)
    {
        string texto = ex switch
        {
            ApiException api => api.Message,
            HttpRequestException => "No se puede conectar con la API.\n¿Está arrancada? (uvicorn main:app --reload)",
            TaskCanceledException => "La API ha tardado demasiado en responder.",
            _ => ex.Message
        };
        MessageBox.Show(texto, "Frutería", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void Info(string texto) =>
        MessageBox.Show(texto, "Frutería", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
