from flask import Flask, jsonify, request, Blueprint, render_template, url_for
from flask_socketio import emit, join_room, leave_room, rooms
from sqlalchemy import func
from sqlalchemy.exc import IntegrityError
from BD.conexion_bd import get_session
from BD.models import *
from werkzeug.security import generate_password_hash
from werkzeug.security import check_password_hash
from extensions import socketio
import re
import logging
import requests
from datetime import datetime
import pytz


logger = logging.getLogger('Server.chats')

blueprint_chats = Blueprint('chats', __name__)


@blueprint_chats.route('/crear_grupo_chat', methods=['POST'])
def crear_grupo_chat():
    try:
        with get_session() as session:
            nuevo_chat = GrupoChat()
            session.add(nuevo_chat)
            session.commit()
            return jsonify({'mensaje': 'Grupo de chat creado', 'id_chat': nuevo_chat.id_chat}), 201
    except Exception as e:
        session.rollback()
        return jsonify({'error': str(e)}), 500

# {
#   "id_usuario": 1,
#   "id_chat": 1
# }    
@blueprint_chats.route('/agregar_usuario_a_grupo', methods=['POST'])
def agregar_usuario_a_grupo():
    data = request.json
    try:
        with get_session() as session:
            id_usuario = data['id_usuario']
            id_chat = data['id_chat']
            
            # Verificación previa
            ya_esta = session.query(UsuarioGrupoChat).filter_by(id_usuario=id_usuario, id_chat=id_chat).first()
            if ya_esta:
                return jsonify({'error': 'El usuario ya está en el grupo'}), 409
            
            relacion = UsuarioGrupoChat(id_usuario=id_usuario, id_chat=id_chat)
            session.add(relacion)
            session.commit()
            return jsonify({'mensaje': f'Usuario {id_usuario} agregado al grupo {id_chat}'}), 200
    except Exception as e:
        session.rollback()
        return jsonify({'error': str(e)}), 500
 
# Metodo que crea un chat privado entre dos usuarios, comprobando que no exista ya    
# {
#   "id_usuario_1": 1,
#   "id_usuario_2": 2
# }
@blueprint_chats.route('/crear_chat_privado', methods=['POST'])
def crear_chat_privado():
    data = request.json
    id_usuario_1 = data.get('id_usuario_1')
    id_usuario_2 = data.get('id_usuario_2')

    if not id_usuario_1 or not id_usuario_2:
        return jsonify({'error': 'Faltan los ID de usuario'}), 400

    if id_usuario_1 == id_usuario_2:
        return jsonify({'error': 'No se puede crear un chat con uno mismo'}), 400

    try:
        with get_session() as session:
            # Hace una subconsulta para ver si el chat ya existe
            subquery = (
                session.query(UsuarioGrupoChat.id_chat)
                .filter(UsuarioGrupoChat.id_usuario.in_([id_usuario_1, id_usuario_2]))
                .group_by(UsuarioGrupoChat.id_chat)
                .having(func.count(UsuarioGrupoChat.id_usuario) == 2)
                .subquery()
            )

            chat_existente = (
                session.query(UsuarioGrupoChat.id_chat)
                .filter(UsuarioGrupoChat.id_chat.in_(subquery))
                .group_by(UsuarioGrupoChat.id_chat)
                .having(func.count(UsuarioGrupoChat.id_usuario) == 2)
                .first()
            )

            if chat_existente:
                return jsonify({
                    'mensaje': 'Ya existe un chat privado entre estos dos usuarios',
                    'id_chat': chat_existente.id_chat
                }), 200

            # CCrea el nuevo chat
            nuevo_chat = GrupoChat()
            session.add(nuevo_chat)
            session.flush()

            # Aqui agrega a los dos usuarios
            session.add_all([
                UsuarioGrupoChat(id_usuario=id_usuario_1, id_chat=nuevo_chat.id_chat),
                UsuarioGrupoChat(id_usuario=id_usuario_2, id_chat=nuevo_chat.id_chat)
            ])
            session.commit()

            return jsonify({
                'mensaje': f'Chat privado creado entre los usuarios {id_usuario_1} y {id_usuario_2}',
                'id_chat': nuevo_chat.id_chat
            }), 201

    except Exception as e:
        session.rollback()
        return jsonify({'error': str(e)}), 500

# Metodo para obtener los chat en comun de dos usuarios
#   http://127.0.0.1:50000/chats/chat_en_comun?id_usuario_1=5&id_usuario_2=12
@blueprint_chats.route('/chat_en_comun', methods=['GET'])
def obtener_chat_en_comun():
    id_usuario_1 = request.args.get('id_usuario_1', type=int)
    id_usuario_2 = request.args.get('id_usuario_2', type=int)
    logger.info(f"Se ha recibido una peticion para saber que chat comparten los usuarios con id {id_usuario_1} y {id_usuario_2}")

    if not id_usuario_1 or not id_usuario_2:
        return jsonify({'error': 'Faltan los ID de usuario'}), 400

    if id_usuario_1 == id_usuario_2:
        return jsonify({'error': 'Debe proporcionar dos usuarios diferentes'}), 400

    try:
        with get_session() as session:
            subquery = (
                session.query(UsuarioGrupoChat.id_chat)
                .filter(UsuarioGrupoChat.id_usuario.in_([id_usuario_1, id_usuario_2]))
                .group_by(UsuarioGrupoChat.id_chat)
                .having(func.count(UsuarioGrupoChat.id_usuario) == 2)
                .subquery()
            )

            chat = session.query(UsuarioGrupoChat.id_chat)\
                .filter(UsuarioGrupoChat.id_chat.in_(subquery))\
                .first()

            if chat:
                logger.info(f"Se ha encontrado que {id_usuario_1} y {id_usuario_2} comparten el chat {chat.id_chat}")
                return jsonify({'chat_en_comun': chat.id_chat}), 200
            else:
                logger.warning(f"{id_usuario_1} y {id_usuario_2} no comparten ningun chat")
                return jsonify({'mensaje': 'No hay chats en común entre estos usuarios'}), 404

    except Exception as e:
        return jsonify({'error': str(e)}), 500

# Obtener los mensajes de un chat
# GET http://127.0.0.1:50000/chats/mensajes_chat?id_chat=1
@blueprint_chats.route('/mensajes_chat', methods=['GET'])
def obtener_mensajes_chat():
    id_chat = request.args.get('id_chat', type=int)
    logger.info(f"Se ha recibido una peticion para recibir los mensajes del chat {id_chat}")

    if not id_chat:
        return jsonify({'error': 'Falta el ID del chat'}), 400

    try:
        with get_session() as session:
            mensajes = session.query(Mensaje).filter_by(id_chat=id_chat).order_by(Mensaje.timestamp).all()

            mensajes_json = [{
                'id_mensaje': m.id_mensaje,
                'mensaje': m.mensaje,
                'timestamp': m.timestamp.isoformat(),
                'id_usuario_remitente': m.id_usuario_remitente,
                'nombre_usuario': m.remitente.nombre_usuario,
                'id_chat': m.id_chat
            } for m in mensajes]

            logger.debug(f"mensajes encontrados: {mensajes_json}")
            return jsonify({'mensajes': mensajes_json}), 200

    except Exception as e:
        logger.error(f"Error al obtener mensajes: {e}")
        return jsonify({'error': str(e)}), 500
    

@blueprint_chats.route('/grupos', methods=['GET'])
def listar_grupos():
    logger.info("Se ha recibido una peticion para saber todos los grupos de chat existentes")
    try:
        with get_session() as session:
            grupos = session.query(GrupoChat).all()
            resultado = [{'id_chat': g.id_chat} for g in grupos]
            logger.info(f"Se han encontrado los siguientes grupos: {resultado}")
            return jsonify(resultado), 200
    except Exception as e:
        logger.error(f"Error al obtener los grupos: {e}")
        return jsonify({'error': str(e)}), 500


@blueprint_chats.route('/mensajes/<int:id_chat>', methods=['GET'])
def obtener_mensajes(id_chat):
    try:
        with get_session() as session:
            logger.info(f"Se ha recibido una peticion para recibir los mensajes del chat {id_chat}")
            mensajes = session.query(Mensaje).filter_by(id_chat=id_chat).order_by(Mensaje.timestamp).all()
            resultado = [
                {
                    'id_mensaje': m.id_mensaje,
                    'mensaje': m.mensaje,
                    'timestamp': m.timestamp.isoformat(),
                    'id_usuario_remitente': m.id_usuario_remitente,
                    'nombre_usuario': m.remitente.nombre_usuario
                }
                for m in mensajes
            ]
            logger.debug(f"mensajes encontrados: {resultado}")
            return jsonify(resultado), 200
    except Exception as e:
        logger.error(f"Error al obtener mensajes: {e}")
        return jsonify({'error': str(e)}), 500

@blueprint_chats.route('/integrantes/<int:id_chat>', methods=['GET'])
def integrantes_grupo(id_chat):
    try:
        with get_session() as session:
            integrantes = session.query(UsuarioGrupoChat, Usuario).join(
                Usuario, UsuarioGrupoChat.id_usuario == Usuario.id_usuario
            ).filter(UsuarioGrupoChat.id_chat == id_chat).all()

            resultado = [
                {
                    'id_usuario': u.id_usuario,
                    'nombre_usuario': u.nombre_usuario
                } for _, u in integrantes
            ]
            return jsonify(resultado), 200
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@blueprint_chats.route('/grupos_usuario/<int:id_usuario>', methods=['GET'])
def grupos_usuario(id_usuario):
    logger.info(f"Se ha recibido una peticion para saber en que grupos de chat está el usuario {id_usuario}")
    try:
        with get_session() as session:
            grupos = session.query(UsuarioGrupoChat).filter_by(id_usuario=id_usuario).all()
            resultado = [{'id_chat': g.id_chat} for g in grupos]
            logger.info(f"El usuario {id_usuario} está en los siguientes grupos: {resultado}")
            return jsonify(resultado), 200
    except Exception as e:
        logger.error(f"Ha ocurrido un error al buscar los grupos de chat del usuario {id_usuario}: {e}")
        return jsonify({'error': str(e)}), 500


