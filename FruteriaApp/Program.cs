namespace FruteriaApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var api = new ApiClient("http://127.0.0.1:8000/");
        Application.Run(new FormPrincipal(api));
    }
}
