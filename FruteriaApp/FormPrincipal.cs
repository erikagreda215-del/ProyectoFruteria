namespace FruteriaApp;

/// <summary>Menú principal: abre el resto de pantallas compartiendo el mismo ApiClient.</summary>
public class FormPrincipal : Form
{
    private readonly ApiClient _api;
    private readonly Label _lblEstado = new();

    public FormPrincipal(ApiClient api)
    {
        _api = api;
        ConstruirInterfaz();
        Load += async (_, _) => await ComprobarConexionAsync();
    }

    private void ConstruirInterfaz()
    {
        Text = "Frutería EA - Menú principal";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(360, 360);

        var titulo = new Label
        {
            Text = "Frutería",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(70, 0, 0, 0)
        };

        var btnCatalogo = Ui.Boton("Catálogo de productos", 220);
        var btnStock = Ui.Boton("Control de stock", 220);
        var btnVentas = Ui.Boton("Registro de ventas", 220);
        var btnInformes = Ui.Boton("Informes", 220);
        var btnSalir = Ui.Boton("Salir", 220);
        foreach (var b in new[] { btnCatalogo, btnStock, btnVentas, btnInformes, btnSalir })
        {
            b.Height = 36;
            b.Margin = new Padding(0, 4, 0, 4);
            panel.Controls.Add(b);
        }

        btnCatalogo.Click += (_, _) => Abrir(new FormCatalogo(_api));
        btnStock.Click += (_, _) => Abrir(new FormStock(_api));
        btnVentas.Click += (_, _) => Abrir(new FormVentas(_api));
        btnInformes.Click += (_, _) => Abrir(new FormInformes(_api));
        btnSalir.Click += (_, _) => Close();

        _lblEstado.Dock = DockStyle.Bottom;
        _lblEstado.Height = 30;
        _lblEstado.TextAlign = ContentAlignment.MiddleCenter;
        _lblEstado.Cursor = Cursors.Hand;
        _lblEstado.Click += async (_, _) => await ComprobarConexionAsync();

        Controls.Add(panel);
        Controls.Add(titulo);
        Controls.Add(_lblEstado);
    }

    private async void Abrir(Form f)
    {
        using (f)
        {
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowDialog(this);
        }
        await ComprobarConexionAsync();
    }

    private async Task ComprobarConexionAsync()
    {
        _lblEstado.Text = "Comprobando conexión con la API...";
        _lblEstado.ForeColor = Color.Gray;
        bool ok = await _api.ProbarConexionAsync();
        _lblEstado.Text = ok
            ? $"Conectado a {_api.UrlBase}"
            : $"Sin conexión con la API (clic para reintentar)";
        _lblEstado.ForeColor = ok ? Color.DarkGreen : Color.Firebrick;
    }
}
