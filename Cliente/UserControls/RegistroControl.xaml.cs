using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
using Cliente_TFG.Classes;
using Newtonsoft.Json;

namespace Cliente_TFG.UserControls
{
    /// <summary>
    /// Lógica de interacción para RegistroControl.xaml
    /// </summary>
    public partial class RegistroControl : UserControl
    {
        public delegate void LoginRequested();
        public event LoginRequested VolverAlLogin;

        public RegistroControl()
        {
            InitializeComponent();
            txtNombreCuenta.TextChanged += (s, e) => ResaltarCampo(txtNombreCuenta, false);
            txtCorreo.TextChanged += (s, e) => ResaltarCampo(txtCorreo, false);
            txtPassword.PasswordChanged += (s, e) => ResaltarCampo(txtPassword, false);
        }

        private void ResaltarCampo(Control control, bool error)
        {
            if (error)
                control.BorderBrush = Brushes.Red;
            else
                control.ClearValue(Border.BorderBrushProperty);
        }

        private void VolverLogin_Click(object sender, MouseButtonEventArgs e)
        {
            VolverAlLogin?.Invoke();
        }

        private async void Registrarse_Click(object sender, RoutedEventArgs e)
        {
            txtErrores.Text = "";
            string nombreCuenta = txtNombreCuenta.Text.Trim();
            string correo = txtCorreo.Text.Trim();
            string contrasena = txtPassword.Password;

            //VALIDACIONES BASICAS
            if (string.IsNullOrEmpty(nombreCuenta) || string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contrasena))
            {
                txtErrores.Text = "Por favor, rellena todos los campos.";
                //MessageBox.Show("Por favor, rellena todos los campos.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool exito = await RegistrarUsuarioAsync(nombreCuenta, correo, contrasena);
            if (exito)
            {
                var registroExitoso = new Windows.VRegistroExitoso();
                registroExitoso.Show();
                //MessageBox.Show("Registro exitoso. Ahora puedes iniciar sesión.", "Registro", MessageBoxButton.OK, MessageBoxImage.Information);
                VolverAlLogin?.Invoke();
            }
        }


        public async Task<bool> RegistrarUsuarioAsync(string nombreCuenta, string correo, string contrasena)
        {
            // Limpiar errores anteriores
            txtErrores.Text = "";
            ResaltarCampo(txtNombreCuenta, false);
            ResaltarCampo(txtCorreo, false);
            ResaltarCampo(txtPassword, false);

            if (nombreCuenta.Length < 3)
            {
                txtErrores.Text = "El nombre de cuenta debe tener al menos 3 caracteres";
                ResaltarCampo(txtNombreCuenta, true);
                return false;
            }

            if (!Regex.IsMatch(correo, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
            {
                txtErrores.Text = "Correo electrónico no válido";
                ResaltarCampo(txtCorreo, true);
                return false;
            }

            if (contrasena.Length < 4 || !Regex.IsMatch(contrasena, "^[a-zA-Z0-9]+$"))
            {
                txtErrores.Text = "La contraseña debe tener más de 3 caracteres y ser alfanumérica";
                ResaltarCampo(txtPassword, true);
                return false;
            }

            using (HttpClient client = new HttpClient())
            {
                var datos = new
                {
                    nombre_cuenta = nombreCuenta,
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
                    string url = "http://" + Config.IP + ":"+Config.Puerto+"/users/";

                    HttpResponseMessage response = await client.PostAsync(url, contenido);

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        string errorJson = await response.Content.ReadAsStringAsync();

                        try
                        {
                            var errorObj = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(errorJson);
                            if (errorObj != null && errorObj.ContainsKey("Errores"))
                            {
                                txtErrores.Text = string.Join("\n", errorObj["Errores"]);
                            }
                            else
                            {
                                txtErrores.Text = "Error desconocido al registrar.";
                            }
                        }
                        catch
                        {
                            txtErrores.Text = "Error inesperado al procesar la respuesta del servidor.";
                        }

                        return false;
                    }
                }
                catch (Exception ex)
                {
                    txtErrores.Text = "ERROR DE CONEXIÓN: " + ex.Message;
                    return false;
                }
            }
        }
    }
}
