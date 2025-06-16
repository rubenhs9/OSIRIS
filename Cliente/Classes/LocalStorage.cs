using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Windows.Media.Imaging;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Cliente_TFG.Classes
{
    public static class LocalStorage
    {
        private static string rutaUsuario = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Osiris", "usuario.json");
        private static string rutaBiblioteca = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Osiris", "biblioteca.json");
        private static string rutaBibliotecaNombres = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Osiris", "bibliotecaNombres.json");
        private static string rutaImagenes = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Osiris", "imagenes");


        static LocalStorage()
        {
            var carpetaPrincipal = Path.GetDirectoryName(rutaUsuario);
            Directory.CreateDirectory(carpetaPrincipal);
            Directory.CreateDirectory(rutaImagenes); //CREAMOS LA CARPETA DE IMÁGENES
        }


        public static void GuardarUsuario(Usuario usuario)
        {
            var json = JsonConvert.SerializeObject(usuario);
            File.WriteAllText(rutaUsuario, json);
        }

        public static Usuario CargarUsuario()
        {
            if (!File.Exists(rutaUsuario))
                return null;

            var json = File.ReadAllText(rutaUsuario);
            return JsonConvert.DeserializeObject<Usuario>(json);
        }

        public static void GuardarBiblioteca(List<int> biblioteca)
        {
            var json = JsonConvert.SerializeObject(biblioteca);
            File.WriteAllText(rutaBiblioteca, json);
        }

        public static List<int> CargarBiblioteca()
        {
            if (!File.Exists(rutaBiblioteca))
                return new List<int>();

            var json = File.ReadAllText(rutaBiblioteca);
            return JsonConvert.DeserializeObject<List<int>>(json);
        }

        public static void GuardarBibliotecaNombreJuegos(List<string> nombreJuego)
        {
            var nombresUnicos = nombreJuego.Distinct().ToList(); // ELIMINA DUPLICADOS
            var json = JsonConvert.SerializeObject(nombresUnicos, Formatting.Indented);
            File.WriteAllText(rutaBibliotecaNombres, json);
        }

        public static List<string> CargarBibliotecaNombreJuegos()
        {
            if (!File.Exists(rutaBibliotecaNombres))
                return new List<string>();

            var json = File.ReadAllText(rutaBibliotecaNombres);
            return JsonConvert.DeserializeObject<List<string>>(json);
        }


        //GUARDAR LA IMAGEN EN BYTES[]
        public static void GuardarImagen(string nombreArchivo, byte[] datosImagen)
        {
            string rutaImagen = Path.Combine(rutaImagenes, nombreArchivo);
            File.WriteAllBytes(rutaImagen, datosImagen);
        }

        //GUARDAR IMAGEN QUE GENERAMOS NOSOTROS
        public static void GuardarImagenRenderizada(string nombreArchivo, BitmapSource imagen)
        {
            string ruta = Path.Combine(rutaImagenes, nombreArchivo);

            //CREAMOS EL ENCODER PNG
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imagen));

            using (FileStream stream = new FileStream(ruta, FileMode.Create))
            {
                encoder.Save(stream);
            }

            //MessageBox.Show("Imagen guardada");
        }


        // Comprobar si la imagen existe localmente
        public static bool ExisteImagen(string nombreArchivo)
        {
            string rutaImagen = Path.Combine(rutaImagenes, nombreArchivo);
            //MessageBox.Show(rutaImagen);
            //MessageBox.Show(File.Exists(rutaImagen).ToString());
            return File.Exists(rutaImagen);
        }

        // Cargar imagen localmente y devolver como BitmapImage para WPF
        public static BitmapImage CargarImagenLocal(string nombreArchivo)
        {
            string rutaImagen = Path.Combine(rutaImagenes, nombreArchivo);
            if (!File.Exists(rutaImagen))
                return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri($"file:///{rutaImagen.Replace('\\', '/')}");
            bitmap.EndInit();
            bitmap.Freeze(); // para usar en UI thread
            return bitmap;
        }

        //METODO PARA SACAR EL ID DEL USER EN MODO OFFLINE
        public static int CargarIdUsuarioLocal()
        {
            try
            {
                string ruta = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Osiris",
                    "usuario.json"
                );

                if (!File.Exists(ruta))
                    return -1;

                string contenido = File.ReadAllText(ruta);
                var json = JObject.Parse(contenido);

                int idUsuario = json["id_usuario"] != null
                    ? (int)json["id_usuario"]
                    : (json["IdUsuario"] != null ? (int)json["IdUsuario"] : -1);

                return idUsuario;
            }
            catch
            {
                return -1;
            }
        }

    }
}
