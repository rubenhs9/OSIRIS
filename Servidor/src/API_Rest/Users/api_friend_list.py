from flask import Flask, jsonify, request, Blueprint, render_template, url_for
from BD.conexion_bd import get_session
from BD.models import *
from sqlalchemy import text
from werkzeug.utils import secure_filename

import os
import logging

logger = logging.getLogger('Server.friendlist')
# Blueprint para endpoints de la lista de amigos, no es necesario repetir el prefijo en los metodos
blueprint_friend_list = Blueprint('friend_list', __name__)


BASE_DIR = os.path.dirname(os.path.abspath(__file__))
UPLOAD_FOLDER = os.path.join(BASE_DIR, '..', 'static', 'fotos_perfil') 
ALLOWED_EXTENSIONS = {"png", "jpg", "jpeg"}

os.makedirs(UPLOAD_FOLDER, exist_ok=True)

def allowed_file(filename):
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS


# # Las consultas de solicitudes son desde users

# # Ejemplos de consultas AMIGOS y SOLICITUDES:
# #           http://127.0.0.1:5000/users/?id=1&amigos
# #           http://127.0.0.1:5000/users/?id=1&solicitudes -> Aqui muestra las solicitudes que tiene el usuario, incluso las rechazadas. Para despues gestionar esos datos mas adelante. 
# #           http://127.0.0.1:5000/users/?id=4&solicitudes&estado_solicitudes=aceptado
# #           http://127.0.0.1:5000/users/?id=2&solicitudes&estado_solicitudes=rechazada
# #           http://127.0.0.1:5000/users/?id=2&solicitudes&estado_solicitudes=pendiente
# #
# # Solicitudes Enviadas
# #           http://127.0.0.1:5000/users/?id=1&solicitudes&sol_enviadas -> TODAS
# #           http://127.0.0.1:5000/users/?id=1&solicitudes&sol_enviadas&estado_solicitudes=pendiente -> Pendientes por ID 1
# #           http://127.0.0.1:5000/users/?id=1&solicitudes&sol_enviadas&estado_solicitudes=aceptada -> Aceptada por ID 1 
# #           http://127.0.0.1:5000/users/?id=1&solicitudes&sol_enviadas&estado_solicitudes=rechazada -> Rechazada por ID 1

# # Solicitudes Recibidas
# #           http://127.0.0.1:5000/users/?id=1&solicitudes&sol_recibidas -> Todas las solicitudes recibidas por ID 1                
# #           http://127.0.0.1:5000/users/?id=1&solicitudes&sol_recibidas&estado_solicitudes=pendiente -> Todas las solicitudes recibidas pendientes por ID 1
# #           http://127.0.0.1:5000/users/?id=1&solicitudes&sol_recibidas&estado_solicitudes=aceptada -> Todas las solicitudes recibidas aceptadas por ID 1
# #           http://127.0.0.1:5000/users/?id=1&solicitudes&sol_recibidas&estado_solicitudes=rechazada -> Todas las solicitudes recibidas rechazadas por ID 1

# Método para enviar solicitud de amistad
# Endpoint: POST  http://127.0.0.1:5000/friend_list/solicitudes_amistad
#   Ejemplo de uso con postman
    #   {
    #     "de_usuario_id": 14,
    #     "para_usuario_id": 15
    #  }
@blueprint_friend_list.route('/solicitudes_amistad', methods=['POST'])
def enviar_solicitud_amistad():
    data = request.json
    de_usuario_id = data.get('de_usuario_id')
    para_usuario_id = data.get('para_usuario_id')

    if not de_usuario_id or not para_usuario_id:
        return jsonify({"error": "Se requieren de_usuario_id y para_usuario_id"}), 400

    if de_usuario_id == para_usuario_id:
        return jsonify({"error": "No puedes enviarte una solicitud de amistad a ti mismo"}), 400

    with get_session() as session:
        # Comprobar si existen ambos usuarios
        de_usuario = session.query(Usuario).filter_by(id_usuario=de_usuario_id).first()
        para_usuario = session.query(Usuario).filter_by(id_usuario=para_usuario_id).first()

        if not de_usuario or not para_usuario:
            return jsonify({"error": "Uno o ambos usuarios no existen"}), 404

        # Comprobar si ya existe una solicitud de amistad desde de_usuario a para_usuario
        solicitud_existente = session.query(SolicitudAmistad).filter_by(
            de_usuario_id=de_usuario_id,
            para_usuario_id=para_usuario_id
        ).first()

        if solicitud_existente:
            return jsonify({"error": "Ya has enviado una solicitud a este usuario"}), 409

        # Crear nueva solicitud
        nueva_solicitud = SolicitudAmistad(
            de_usuario_id=de_usuario_id,
            para_usuario_id=para_usuario_id,
            estado='pendiente'
        )

        try:
            logger.info(f"Se ha enviado una solicitud amistad de el user {de_usuario_id} al user {para_usuario_id}")
            session.add(nueva_solicitud)
            session.commit()
            return jsonify({"mensaje": "Solicitud enviada con éxito"}), 201
        except Exception as e:
            session.rollback()
            return jsonify({"error": "Error al crear la solicitud", "detalle": str(e)}), 500


# Método para ACEPTAR solicitud de amistad
#           http://127.0.0.1:5000/users/solicitudes_amistad/aceptar

# EJEMPLO DE USO EN POSTMAN
# {
#   "solicitud_id": 123
# }
@blueprint_friend_list.route('/solicitudes_amistad/aceptar', methods=['POST'])
def aceptar_solicitud_amistad():
    data = request.json
    solicitud_id = data.get('solicitud_id')

    if not solicitud_id:
        return jsonify({"error": "Falta el ID de la solicitud"}), 400

    with get_session() as session:
        solicitud = session.query(SolicitudAmistad).filter_by(id=solicitud_id).first()

        if not solicitud:
            return jsonify({"error": "Solicitud no encontrada"}), 404

        if solicitud.estado != 'pendiente':
            return jsonify({"error": f"La solicitud ya está en estado '{solicitud.estado}'"}), 400

        # Cambiamos el estado a aceptado
        solicitud.estado = 'aceptada'

        # Con esta linea evitamos duplicados en la tabla para que no se repita la amistad bidireccionalmente. Por ejemplo id(3,4) y (4,3)
        usuario1_id = min(solicitud.de_usuario_id, solicitud.para_usuario_id)
        usuario2_id = max(solicitud.de_usuario_id, solicitud.para_usuario_id)

        # Aqui se verifica que la amistad no exista ya en la tabla
        amistad_existente = session.query(Amigos).filter_by(
            usuario1_id=usuario1_id,
            usuario2_id=usuario2_id
        ).first()

        if amistad_existente:
            return jsonify({"error": "La amistad ya está registrada"}), 409

        # Se crea la amistad
        nueva_amistad = Amigos(
            usuario1_id=usuario1_id,
            usuario2_id=usuario2_id
        )

        try:
            session.add(nueva_amistad) # Agregamos la amistad a la tabla
            session.commit()
            return jsonify({"mensaje": "Solicitud aceptada y amistad registrada"}), 200
        except Exception as e:
            session.rollback()
            return jsonify({"error": f"Error al aceptar la solicitud: {str(e)}"}), 500



# Método para RECHAZAR solicitud de amistad
#       http://127.0.0.1:5000/users/solicitudes_amistad/rechazar

# EJEMPLO DE USO EN POSTMAN
# {
#   "solicitud_id": 123
# }

@blueprint_friend_list.route('/solicitudes_amistad/rechazar', methods=['POST'])
def rechazar_solicitud_amistad():
    data = request.json
    solicitud_id = data.get('solicitud_id')

    if not solicitud_id:
        return jsonify({"error": "Falta el ID de la solicitud"}), 400

    with get_session() as session:
        solicitud = session.query(SolicitudAmistad).filter_by(id=solicitud_id).first()

        if not solicitud:
            return jsonify({"error": "Solicitud no encontrada"}), 404

        if solicitud.estado != 'pendiente':
            return jsonify({"error": f"La solicitud ya está en estado '{solicitud.estado}'"}), 400

        # Cambiamos el estado a rechazada
        solicitud.estado = 'rechazada'

        try:
            session.commit()
            return jsonify({"mensaje": "Solicitud rechazada"}), 200
        except Exception as e:
            session.rollback()
            return jsonify({"error": f"Error al rechazar la solicitud: {str(e)}"}), 500
        
@blueprint_friend_list.route('/amigos/borrar', methods=['POST'])
def borrar_amigo():
    data = request.json

    try:
        usuario_id = int(data.get('usuario_id'))
        amigo_id = int(data.get('amigo_id'))
    except (TypeError, ValueError):
        return jsonify({"error": "usuario_id y amigo_id deben ser enteros"}), 400

    usuario1_id = min(usuario_id, amigo_id)
    usuario2_id = max(usuario_id, amigo_id)

    with get_session() as session:
        amistad = session.query(Amigos).filter_by(
            usuario1_id=usuario1_id,
            usuario2_id=usuario2_id
        ).first()

        if not amistad:
            return jsonify({"error": "Amistad no encontrada"}), 404

        try:
            session.delete(amistad)

            # Borrar todas las solicitudes entre ambos usuarios (en cualquier orden)
            solicitudes = session.query(SolicitudAmistad).filter(
                ((SolicitudAmistad.de_usuario_id == usuario_id) & (SolicitudAmistad.para_usuario_id == amigo_id)) |
                ((SolicitudAmistad.de_usuario_id == amigo_id) & (SolicitudAmistad.para_usuario_id == usuario_id))
            ).all()

            for solicitud in solicitudes:
                session.delete(solicitud)

            session.commit()
            return jsonify({"mensaje": "Amigo y solicitudes eliminadas"}), 200

        except Exception as e:
            session.rollback()
            return jsonify({"error": f"Error al eliminar amigo: {str(e)}"}), 500
