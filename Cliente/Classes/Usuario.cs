using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Net;

namespace Cliente_TFG.Classes
{
    public class Usuario
    {
        public string codigo_amigo;
        public string contraseña;
        public string correo;
        public string descripcion;
        public double dinero;
        public string estado;
        public string foto_perfil;
        public int id_usuario;
        public string nombre_cuenta;
        public string nombre_usuario { get; set; }
        public string token;
        public List<int> bibliotecaJuegos = new List<int>();
        public List<string> bibliotecaJuegosNombres = new List<string>();
        private MainWindow ventanaPrincipal;

        public Usuario(MainWindow mainWindow)
        {
            this.ventanaPrincipal = mainWindow;
        }

        public void CargarDatos(int idUser)
        {
            var url = $"http://" + Config.IP + $":{Config.Puerto}/users/{idUser}";
            using (var webClient = new WebClient())
            {
                string jsonString = webClient.DownloadString(url);

                var jsonObj = JObject.Parse(jsonString);
                var usuarioJson = jsonObj["usuario"];

                codigo_amigo = (string)usuarioJson["codigo_amigo"];
                contraseña = (string)usuarioJson["contrasena"];
                correo = (string)usuarioJson["correo"];
                descripcion = (string)usuarioJson["descripcion"];
                dinero = usuarioJson["dinero"]?.Type == JTokenType.Null ? 0 : (double)usuarioJson["dinero"]; //COMPROBAMOS SI ES NULL
                estado = (string)usuarioJson["estado"];
                foto_perfil = (string)usuarioJson["foto_perfil"];
                id_usuario = (int)usuarioJson["id_usuario"];
                nombre_cuenta = (string)usuarioJson["nombre_cuenta"];
                nombre_usuario = (string)usuarioJson["nombre_usuario"];
                token = ((string)usuarioJson["token"])?.Trim() ?? "";
            }
            CargarBiblioteca(idUser);
        }

        public void CargarBiblioteca(int idUser)
        {
            var url = $"http://" + Config.IP + $":{Config.Puerto}/library/{idUser}";
            using (var webClient = new WebClient())
            {
                string jsonString = webClient.DownloadString(url);

                var jsonObj = JObject.Parse(jsonString);
                var juegosArray = jsonObj["juegos"] as JArray;

                bibliotecaJuegos.Clear();
                bibliotecaJuegosNombres.Clear();

                foreach (var juego in juegosArray)
                {
                    int appId = (int)juego["app_id"];
                    string nombreJuego = (string)juego["nombre"];

                    bibliotecaJuegos.Add(appId);
                    bibliotecaJuegosNombres.Add(nombreJuego);
                }

                //GUARDAR JSONS SIN DUPLICADOS Y EN ORDEN CORRECTO
                LocalStorage.GuardarBiblioteca(bibliotecaJuegos);
                LocalStorage.GuardarBibliotecaNombreJuegos(bibliotecaJuegosNombres);
            }
        }





        //GETTERS
        public string CodigoAmigo => codigo_amigo;
        public string Contraseña => contraseña;
        public string Correo => correo;
        public string Descripcion
        {
            get => descripcion;
            set => descripcion = value;
        }
        public double Dinero => dinero;
        public string Estado => estado;
        public string FotoPerfil => foto_perfil;
        public int IdUsuario => id_usuario;
        public string NombreCuenta => nombre_cuenta;
        public string NombreUsuario
        {
            get => nombre_usuario;
            set => nombre_usuario = value;
        }
        public string Token => token;
        public List<int> BibliotecaJuegos => bibliotecaJuegos;


    }
}
