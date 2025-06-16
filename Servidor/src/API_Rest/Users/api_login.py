from flask import Flask, jsonify, request, Blueprint, render_template, url_for
from sqlalchemy.exc import IntegrityError
from BD.conexion_bd import get_session
from BD.models import *
from werkzeug.security import generate_password_hash
from werkzeug.security import check_password_hash
import re
import logging
import requests
import uuid

logger = logging.getLogger('Server.login')

blueprint_login = Blueprint('login', __name__)

# Método para logear el usuario
#           http://127.0.0.1:5000/login

# EJEMPLO DE USO CON POSTMAN
# {
#   "correo": "PRUEBA21q@PRUEBA21.COM",
#   "contrasena": "1234321"
# }

@blueprint_login.route('/', methods=['POST'])
def login_usuario():
    data = request.json
    correo = data.get('correo')
    contrasena = data.get('contrasena')
    logger.info(f"Intento de inicio de sesion con el correo: {correo}")

    if not correo or not contrasena:
        logger.warning("Inicio de sesion fallido: Correo y contraseña obligatorios")
        return jsonify({"error": "Correo y contraseña obligatorios"}), 400

    with get_session() as session:
        usuario = session.query(Usuario).filter_by(correo=correo).first()
        if not usuario or not check_password_hash(usuario.contrasena, contrasena):
            logger.warning("Inicio de sesion fallido: Credenciales inválidas")
            return jsonify({"error": "Credenciales inválidas"}), 401

        # Generar nuevo token
        nuevo_token = str(uuid.uuid4())
        usuario.token = nuevo_token
        logger.debug(f"Se ha generado un token para el usuario {usuario.nombre_cuenta}")
        session.commit()

        logger.info(f"El usuario {usuario.nombre_cuenta} ha iniciado sesion con exito")
        return jsonify({
            "mensaje": "Inicio de sesión correcto",
            "id_usuario": usuario.id_usuario,
            "token": nuevo_token
        })


#PARA LA VERIFICACION DEL TOKEN
@blueprint_login.route('/validar_token', methods=['POST'])
def validar_token():
    data = request.json
    token = data.get("token")
    logger.info("Se va a intentar validar un token")

    if not token:
        logger.warning("El usuario no tiene un token")
        return jsonify({"error": "Token no proporcionado"}), 400

    with get_session() as session:
        usuario = session.query(Usuario).filter_by(token=token).first()
        if not usuario:
            logger.warning("El usuario no tiene un token valido")
            return jsonify({"error": "Token inválido"}), 401

        logger.info(f"El token validado pertenece al usuario {usuario.nombre_cuenta}, se procederá a iniciar su sesion de forma automatica")
        return jsonify({
            "mensaje": "Token válido",
            "id_usuario": usuario.id_usuario
        })

