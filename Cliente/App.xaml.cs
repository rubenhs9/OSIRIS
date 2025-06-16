using Cliente_TFG.Classes;
using Cliente_TFG.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cliente_TFG
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string ip = Config.IP;

        //METODO PARA COMPROBAR SI TENEMOS UN TOKEN VALIDO
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            string rutaToken = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Osiris", "token.txt"
            );

            if (File.Exists(rutaToken))
            {
                string tokenGuardado = File.ReadAllText(rutaToken);

                using (HttpClient client = new HttpClient())
                {
                    var datos = new { token = tokenGuardado };
                    var contenido = new StringContent(JsonConvert.SerializeObject(datos), Encoding.UTF8, "application/json");

                    try
                    {
                        HttpResponseMessage resp = await client.PostAsync($"http://{ip}:{Config.Puerto}/login/validar_token", contenido);

                        if (resp.IsSuccessStatusCode)
                        {
                            string json = await resp.Content.ReadAsStringAsync();
                            dynamic data = JsonConvert.DeserializeObject(json);
                            int idUsuario = data.id_usuario;

                            MainWindow mainWindow = new MainWindow(idUsuario);
                            mainWindow.Show();
                            return;
                        }
                        else
                        {
                            // Token inválido o expirado - abrir login
                            LoginWindow loginWindow = new LoginWindow();
                            loginWindow.Show();
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        // FALLÓ LA CONEXIÓN - INTENTAR MODO OFFLINE
                        int idUsuarioOffline = LocalStorage.CargarIdUsuarioLocal();

                        if (idUsuarioOffline != -1)
                        {
                            MainWindow mainWindow = new MainWindow(idUsuarioOffline);
                            mainWindow.Show();
                            MessageBox.Show("No hay conexión, se abrió la aplicación en modo offline.");
                        }
                        else
                        {
                            MessageBox.Show("No hay conexión y no hay datos guardados. Debes iniciar sesión cuando tengas conexión.");
                            LoginWindow loginWindow = new LoginWindow();
                            loginWindow.Show();
                        }
                        return;
                    }
                }
            }
            else
            {
                // NO HAY TOKEN - ABRIR LOGIN
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
            }
        }

    }
}