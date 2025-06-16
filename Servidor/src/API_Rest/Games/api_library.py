from flask import Flask, jsonify, request, Blueprint
import logging
from BD.conexion_bd import get_session
from BD.models import *
from sqlalchemy import desc
from flask import send_file, send_from_directory
import os


logger = logging.getLogger('Server.library')
# Blueprint para endpoints de la lista de amigos, no es necesario repetir el prefijo en los metodos
blueprint_library = Blueprint('library', __name__)

@blueprint_library.route('/<int:id_user>', methods=['GET'])
def get_library_by_user_id(id_user):
    try:
        logger.info(f"Se ha recibido una petición para recibir la biblioteca de juegos del usuario con id {id_user}")
        
        with get_session() as session:
            games_query = session.query(Juegos)\
                .join(UsuarioJuego, Juegos.steam_appid == UsuarioJuego.id_juego)\
                .filter(UsuarioJuego.id_usuario == id_user)\
                .group_by(Juegos.steam_appid)\
                .order_by(desc(Juegos.nombre))
            logger.debug(f"Se ha realizado con éxito la consulta a la base de datos sobre los juegos del usuario con id {id_user}")
                
            json_string = [library_to_dict(juego) for juego in games_query]
            logger.info(f"Se ha convertido en json correctamente y se devolverá medienta el endpoint /library/{id_user}")
            return jsonify({
                'juegos': json_string
            })
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /library/{id_user}: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500      

def library_to_dict(juego: Juegos):
    juego_dict = {
        'app_id': juego.steam_appid,
        'nombre': juego.nombre,
        'captura': [cap.path_full for cap in juego.capturas][0] if juego.capturas else None,
        'header': juego.header_image,
    } 
    return juego_dict


# Metodo que saca los tres ultimos juegos comprados por el usuario:
#         GET || http://26.84.183.227:50000/library/5/ultimos_juegos

@blueprint_library.route('/<int:id_user>/ultimos_juegos', methods=['GET'])
def get_ultimas_compras(id_user):
    NUM_JUEGOS = 3
    try:
        logger.info(f"Petición recibida para obtener las últimas compras del usuario con id {id_user}")
        
        with get_session() as session:
            ultimas_compras = session.query(Juegos, UsuarioJuego.fecha_compra)\
                .join(UsuarioJuego, Juegos.steam_appid == UsuarioJuego.id_juego)\
                .filter(UsuarioJuego.id_usuario == id_user)\
                .order_by(desc(UsuarioJuego.fecha_compra))\
                .limit(NUM_JUEGOS)\
                .all()
            
            logger.debug(f"Consulta realizada con éxito para las últimas compras del usuario con id {id_user}")
            
            juegos_json = [
                {
                    **library_to_dict(juego),
                    "fecha_compra": fecha_compra.strftime("%Y-%m-%d %H:%M:%S")
                }
                for juego, fecha_compra in ultimas_compras
            ]
            
            return jsonify({'ultimas_compras': juegos_json})
    
    except Exception as e:
        logger.error(f"Error en el endpoint /library/{id_user}/ultimas-compras: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500


#PARA LA DESCARGA DE JUEGOS
#SE USA CON: http://127.0.0.1:50000/library/<ID_USER>/download/<appid_juego>  (el appid debe de estar en la carpeta de descargas)
# Calculamos la ruta absoluta de la carpeta 'downloads'
import os

base_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
downloads_dir = os.path.join(base_dir, 'static', 'downloads')

@blueprint_library.route('/<int:id_user>/download/<int:app_id>', methods=['GET'])
def download_game(id_user, app_id):
    try:
        logger.info(f"Petición de descarga del juego {app_id} por el usuario {id_user}")

        with get_session() as session:
            tiene_juego = session.query(UsuarioJuego)\
                .filter_by(id_usuario=id_user, id_juego=app_id)\
                .first()

            if not tiene_juego:
                logger.warning(f"El usuario {id_user} intentó descargar un juego que no posee: {app_id}")
                return jsonify({'error': 'El usuario no posee este juego'}), 403

            ruta_archivo = os.path.join(downloads_dir, f"{app_id}.zip")
            logger.info(f"Buscando archivo en ruta absoluta: {ruta_archivo}")

            if not os.path.exists(ruta_archivo):
                logger.error(f"Archivo de juego no encontrado: {ruta_archivo}")
                return jsonify({'error': 'Archivo no encontrado en el servidor'}), 404

            logger.info(f"Descarga permitida del juego {app_id} para el usuario {id_user}")
            return send_file(ruta_archivo, as_attachment=True)
    
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /library/{id_user}/download/{app_id}: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500

#METODO PARA COMPROBAR SI SE PUEDE DESCARGAR UN JUEGO O NO
#SE USA CON: http://127.0.0.1:50000/library/<id_user>/downloadable/<app_id>
@blueprint_library.route('/<int:id_user>/downloadable/<int:app_id>', methods=['GET'])
def es_descargable(id_user, app_id):
    try:
        ruta_archivo = os.path.join(downloads_dir, f"{app_id}.zip")

        if os.path.exists(ruta_archivo):
            return jsonify({'descargable': True})
        else:
            return jsonify({'descargable': False})
    except Exception as e:
        logger.error(f"Error en el endpoint /library/{id_user}/downloadable/{app_id}: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500
