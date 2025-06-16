using Cliente_TFG.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cliente_TFG.UserControls
{
    /// <summary>
    /// Lógica de interacción para LoginControl.xaml
    /// </summary>
    public partial class LoginControl : UserControl
    {
        public delegate void RegistroRequested();
        public event RegistroRequested AbrirRegistro;
        //public string ip = "26.84.183.227";
        public string ip = Config.IP;


        public LoginControl()
        {
            InitializeComponent();
        }

        private void AbrirRegistro_Click(object sender, MouseButtonEventArgs e)
        {
            AbrirRegistro?.Invoke();
        }

        private async void BotonLogin_Click(object sender, RoutedEventArgs e)
        {
            txtErrores.Text = "";
            string correo = txtCorreo.Text;
            string contrasena = txtPassword.Password;

            int? idUsuario = await LoginUsuarioAsync(correo, contrasena);
            if (idUsuario.HasValue)
            {
                MainWindow mainWindow = new MainWindow(idUsuario.Value);
                mainWindow.Show();
                Window.GetWindow(this)?.Close();
            }
        }

        public async Task<int?> LoginUsuarioAsync(string correo, string contrasena)
        {
            using (HttpClient client = new HttpClient())
            {
                var datos = new
                {
                    correo = correo,
                    contrasena = contrasena
                };

                var contenido = new StringContent(
                    JsonConvert.SerializeObject(datos),
                    Encoding.UTF8,
                    "application/json"
                );

                try
                {
                    HttpResponseMessage response = await client.PostAsync("http://" + ip + ":"+Config.Puerto+"/login/", contenido);

                    if (response.IsSuccessStatusCode)
                    {
                        string respuestaJson = await response.Content.ReadAsStringAsync();
                        dynamic datosRespuesta = JsonConvert.DeserializeObject(respuestaJson);
                        int idUsuario = datosRespuesta.id_usuario;
                        string token = datosRespuesta.token;

                        //GUARDAMOS TOKEN EN LOCAL
                        string rutaToken = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Osiris", "token.txt"
                        );
                        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(rutaToken));
                        File.WriteAllText(rutaToken, token);

                        string rutaJsonJuegos = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Osiris", "juegos_biblioteca.json"
                        );
                        BorrarJuegosBibliotecaJson(rutaJsonJuegos);

                        //MessageBox.Show("Login correcto. ID: " + idUsuario);
                        return idUsuario; // DEVUELVE EL ID DEL USUARIO
                    }
                    else
                    {
                        string errorJson = await response.Content.ReadAsStringAsync();
                        dynamic errorObj = JsonConvert.DeserializeObject(errorJson);
                        string mensajeError = errorObj.error;
                        txtErrores.Text = "Error en login: " + mensajeError;
                        //MessageBox.Show("Error en login: " + error);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERROR DE CONEXIÓN: " + ex.Message);
                    return null;
                }
            }
        }


        //BORRAMOS LOS DATOS DE LOS JUEGOS DEL ANTERIOR MENSAJE
        private void BorrarJuegosBibliotecaJson(string rutaJsonJuegos)
        {
            if (File.Exists(rutaJsonJuegos))
            {
                File.Delete(rutaJsonJuegos);
            }
        }



    }
}
