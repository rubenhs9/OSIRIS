from flask import Flask, jsonify, request, Blueprint, render_template, url_for
from sqlalchemy.exc import IntegrityError
from BD.conexion_bd import get_session
from BD.models import *
from werkzeug.security import generate_password_hash
from werkzeug.security import check_password_hash
from dotenv import load_dotenv

import os
import re
import logging
import requests

ALLOWED_EXTENSIONS = {"png", "jpg", "jpeg"}
load_dotenv()
logger = logging.getLogger('Server.user_profile')

blueprint_user_profile = Blueprint('user_profile', __name__)


@blueprint_user_profile.route('/actualizar_foto', methods=['POST'])
def actualizar_foto():
    id_usuario = request.form.get('id_usuario')
    nueva_url = request.form.get('foto_perfil')

    with get_session() as session:
        usuario = session.query(Usuario).get(id_usuario)
        if not usuario:
            return jsonify({"error": "Usuario no encontrado"}), 404

        usuario.foto_perfil = nueva_url
        session.commit()

    return jsonify({"mensaje": "Foto actualizada correctamente"})

@blueprint_user_profile.route('/<int:id_usuario>/foto', methods=['POST'])
def cambiar_foto(id_usuario):
    if 'foto' not in request.files:
        return jsonify({"error": "No se ha enviado ninguna foto"}), 400

    foto = request.files['foto']

    try:
        url_publica = subir_a_imgur(foto)
    except Exception as e:
        return jsonify({"error": str(e)}), 500

    with get_session() as session:
        usuario = session.query(Usuario).get(id_usuario)
        if not usuario:
            return jsonify({"error": "Usuario no encontrado"}), 404

        usuario.foto_perfil = url_publica
        session.commit()

    return jsonify({"mensaje": "Foto actualizada correctamente", "url": url_publica}), 200


# Método para actualizar los campos del usuario
# PATCH || http://127.0.0.1:5000/users/7
# EJEMPLO DE USO EN POSTMAN
# {
#   "nombre_usuario": "Javier Martín Modificado",
#   "descripcion": "Actualización desde Postman"
# }



@blueprint_user_profile.route('/<int:user_id>', methods=['PATCH'])
def actualizar_usuario(user_id):
    data = request.json

    CAMPOS_PERMITIDOS = {
        "nombre_usuario",
        "estado",
        "foto_perfil",
        "codigo_amigo",
        "descripcion",
        "correo",
        "dinero",
        "nombre_cuenta",
        "token",
        "contrasena"
    }

    try:
        with get_session() as session:
            usuario = session.query(Usuario).filter_by(id_usuario=user_id).first()
            if not usuario:
                return jsonify({"mensaje": f"Usuario con ID {user_id} no encontrado"}), 404

            campos_actualizados = []

            for campo, valor in data.items():
                if campo not in CAMPOS_PERMITIDOS:
                    continue  # Ignora los campos no permitidos

                if valor is None or (isinstance(valor, str) and valor.strip() == ""): # Esto ignora los String vacios
                    continue
                # Validaciones para los campos del usuario
                if campo == "contrasena":
                    logger.warning(f"Hasheando nueva contraseña: {valor}")
                    valor = generate_password_hash(valor)

                setattr(usuario, campo, valor)

                if campo == "contrasena":
                    logger.warning(f"Contraseña en usuario después de hash: {usuario.contrasena}")

                elif campo == "dinero":
                    if not isinstance(valor, (int, float)) or valor < 0:
                        return jsonify({"error": "El dinero debe ser un número positivo"}), 400

                elif campo == "correo":
                    patron_correo = r'^[\w\.-]+@[\w\.-]+\.\w+$'
                    if not re.match(patron_correo, valor):
                        return jsonify({"error": "Correo electrónico no válido"}), 400
                
                elif campo == "nombre_cuenta":
                    if len(valor) < 3:
                        return jsonify({"error": "El nombre de cuenta debe tener al menos 3 caracteres"}), 400

                # Aqui pueden ir mas validaciónes pero de momento no se me ocurren más.     

                setattr(usuario, campo, valor)
                campos_actualizados.append(campo)

            if not campos_actualizados:
                return jsonify({"mensaje": "No se enviaron campos válidos para actualizar"}), 400

            session.commit()
            return jsonify({
                "mensaje": "Usuario actualizado correctamente",
                "campos_actualizados": campos_actualizados
            })

    except Exception as e:
        logger.exception(f"Error al actualizar usuario {user_id}: {e}")
        return jsonify({"Error": str(e)}), 500


# CAMBIA LA DESCRIPCION DEL USER
@blueprint_user_profile.route('/<int:id_usuario>/descripcion', methods=['PUT'])
def change_description(id_usuario):
    nueva_descripcion = request.json.get('nueva_descripcion')

    if not nueva_descripcion:
        return jsonify({"Error": "La nueva descripción es obligatoria"}), 400

    try:
        with get_session() as session:
            usuario = session.query(Usuario).get(id_usuario)

            if not usuario:
                return jsonify({"Error": "Usuario no encontrado"}), 404
            
            usuario.descripcion = nueva_descripcion  
            session.commit()

            return jsonify({"mensaje": "Descripción actualizada correctamente"}), 200

    except Exception as e:
        print("Error:", e)
        return jsonify({"Error": "Error interno del servidor"}), 500


 # CAMBIA LA FOTO DE PERFIL DEL USER
 # Fotos guardadas en la carpeta /static. Por defecto Flask deja la carpeta accesible para que el cliente pueda obtenerla. 
 #    
 # Este método GET lo dejo aqui pero es para pruebas en el navegador. Mas adelante modularizaré.
 #          http://127.0.0.1:5000/users/14/foto
 
@blueprint_user_profile.route('/<int:id_usuario>/foto', methods=['GET'])
def show_form_foto(id_usuario):
    # Aquí cargas el HTML con el formulario para cambiar foto
    return render_template('cambiar_password.html', id_usuario=id_usuario)


#METODO PARA CAMBIAR EL ESTADO DE UN USUARIO
#SE USA CON http://127.0.0.1:50000/user_profile/14/estado y pasandole el estado en json
@blueprint_user_profile.route('/<int:id_usuario>/estado', methods=['POST'])
def cambiar_estado_usuario(id_usuario):
    nuevo_estado = request.json.get("estado")

    if not nuevo_estado or not isinstance(nuevo_estado, str):
        return jsonify({"error": "El estado es obligatorio y debe ser una cadena"}), 400

    # OPCIONAL: Validar que el estado esté dentro de un conjunto permitido
    ESTADOS_VALIDOS = {"Conectado", "Ausente", "Ocupado", "Invisible", "Desconectado"}
    if nuevo_estado not in ESTADOS_VALIDOS:
        return jsonify({"error": f"Estado inválido. Valores permitidos: {', '.join(ESTADOS_VALIDOS)}"}), 400

    try:
        with get_session() as session:
            usuario = session.query(Usuario).get(id_usuario)
            if not usuario:
                return jsonify({"error": "Usuario no encontrado"}), 404

            usuario.estado = nuevo_estado
            session.commit()

            return jsonify({"mensaje": "Estado actualizado correctamente", "estado_nuevo": nuevo_estado}), 200

    except Exception as e:
        return jsonify({"error": f"Error interno del servidor: {str(e)}"}), 500



# SERVICIO IMGUR API 
IMGUR_CLIENT_ID = os.getenv('IMGUR_CLIENT_ID') # ESTE ES LA API_ID DE MI CUENTA


def subir_a_imgur(foto):
    headers = {'Authorization': f'Client-ID {IMGUR_CLIENT_ID}'}
    files = {'image': foto}

    response = requests.post('https://api.imgur.com/3/image', headers=headers, files=files)

    if response.status_code == 200:
        return response.json()['data']['link']  # URL pública
    else:
        raise Exception(f"Error al subir a Imgur: {response.json()}")



