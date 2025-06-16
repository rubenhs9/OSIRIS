import random
import string
from flask import Flask, jsonify, request, Blueprint, render_template, url_for
from sqlalchemy.exc import IntegrityError
from BD.conexion_bd import get_session
from BD.models import *
from sqlalchemy import text
from werkzeug.utils import secure_filename
from werkzeug.utils import secure_filename
from werkzeug.security import generate_password_hash
from werkzeug.security import check_password_hash
import re
from decimal import Decimal
import os
import logging

logger = logging.getLogger('Server.users')

blueprint_users = Blueprint('users', __name__)

# Ejemplos de ENDPOINTS:
#           http://127.0.0.1:5000/users -> Saca todos los usuarios
#           http://127.0.0.1:5000/users/?short -> Saca todos los usuarios de manera reducida
#           http://127.0.0.1:5000/users/?page=1 -> Saca la primera página
#           http://127.0.0.1:5000/users/?limit=2 -> Limita las consultas empezando por el ID 0
#           http://127.0.0.1:5000/users/?full -> Devuelve todos los campos
#           http://127.0.0.1:5000/users/?estado=activo&estado=inactivo&short&limit=2 -> Ejemplo combinado con '&'

# Este método es para sacar la información de usuarios de manera general. Realiza consultas de manera global de usuarios, filtrando por campos
# como número de amigos de un usuario, de soicitudes o estados. Tiene otros metodos anidados para mejorar la legibilidad.
# 
@blueprint_users.route('/', methods=['GET'])
def get_users():
    limit = request.args.get('limit', default=50, type=int)
    page = request.args.get('page', default=1, type=int)
    short = request.args.get('short') is not None
    full = request.args.get('full') is not None

    estado = request.args.getlist('estado', type=str)
    id_usuario = request.args.get('id', type=int)
    solo_amigos = request.args.get('amigos') is not None
    solo_solicitudes = request.args.get('solicitudes') is not None
    estado_solicitudes = request.args.get('estado_solicitudes')
    tipo_solicitudes = request.args.get('tipo')

    try:
        with get_session() as session:
            if solo_amigos and id_usuario:
                usuarios = get_amigos(session, id_usuario)
                resultado = [user_to_dict(u) if full else user_to_dict_short(u) for u in usuarios]
                return jsonify(resultado)

            elif solo_solicitudes and id_usuario:
                resultado = get_solicitudes(session, id_usuario, estado_solicitudes, tipo_solicitudes)
                return jsonify(resultado)

            else:
                usuarios = get_usuarios_generales(session, estado, page, limit)
                resultado = [user_to_dict(u) if full else user_to_dict_short(u) for u in usuarios]
                return jsonify(resultado)

    except Exception as e:
        print("Error:", e)
        return jsonify({"Error": "Error interno del servidor"}), 500


def get_usuarios_generales(session, estado, page, limit):
    query = session.query(Usuario)

    if estado:
        query = query.filter(Usuario.estado.in_(estado))

    return query.order_by(Usuario.id_usuario).offset((page - 1) * limit).limit(limit).all()

def get_amigos(session, id_usuario):
    relaciones = session.query(Amigos).filter(
        (Amigos.usuario1_id == id_usuario) |
        (Amigos.usuario2_id == id_usuario)
    ).all()

    ids_amigos = set()
    for r in relaciones:
        if r.usuario1_id == id_usuario:
            ids_amigos.add(r.usuario2_id)
        else:
            ids_amigos.add(r.usuario1_id)

    return session.query(Usuario).filter(Usuario.id_usuario.in_(ids_amigos)).all()

def get_solicitudes(session, id_usuario, estado_solicitudes=None, tipo=None):
    sol_q = session.query(SolicitudAmistad)
    if estado_solicitudes:
        sol_q = sol_q.filter(SolicitudAmistad.estado == estado_solicitudes)

    solicitudes_enviadas = []
    solicitudes_recibidas = []

    if tipo in (None, 'enviadas'):
        enviadas = sol_q.filter(SolicitudAmistad.de_usuario_id == id_usuario).all()
        if enviadas:
            ids_enviadas = [s.para_usuario_id for s in enviadas]
            usuarios_enviadas = session.query(Usuario).filter(Usuario.id_usuario.in_(ids_enviadas)).all()
            # Crear diccionario con la solicitud y su usuario correspondiente
            for solicitud in enviadas:
                usuario = next((u for u in usuarios_enviadas if u.id_usuario == solicitud.para_usuario_id), None)
                if usuario:
                    solicitudes_enviadas.append(solicitud_to_dict(solicitud, usuario))

    if tipo in (None, 'recibidas'):
        recibidas = sol_q.filter(SolicitudAmistad.para_usuario_id == id_usuario).all()
        if recibidas:
            ids_recibidas = [s.de_usuario_id for s in recibidas]
            usuarios_recibidas = session.query(Usuario).filter(Usuario.id_usuario.in_(ids_recibidas)).all()
            # Crear diccionario con la solicitud y su usuario correspondiente
            for solicitud in recibidas:
                usuario = next((u for u in usuarios_recibidas if u.id_usuario == solicitud.de_usuario_id), None)
                if usuario:
                    solicitudes_recibidas.append(solicitud_to_dict(solicitud, usuario))

    return {
        "enviadas": solicitudes_enviadas,
        "recibidas": solicitudes_recibidas
    }

# Borrar Usuario de la base de datos
@blueprint_users.route('/<int:id_usuario>', methods=['DELETE'])
def delete_user(id_usuario):
    try:
        with get_session() as session:
            usuario = session.query(Usuario).get(id_usuario)
            if not usuario:
                return jsonify({"error": "Usuario no encontrado"}), 404

            session.delete(usuario)
            session.commit()

        return jsonify({"mensaje": f"Usuario con ID {id_usuario} eliminado correctamente."})

    except Exception as e:
        return jsonify({"error": "Error al eliminar el usuario", "detalle": str(e)}), 500
    
    
# Método para crear usuario. Tiene validado que no se pueda crear un usuario con el mismo nombre de cuenta y contraseña.
# Tiene campos opcionales, los cuales son foto_perfil y descripcion.
#          Endpoint: POST || http://127.0.0.1:5000/users/
# EJEMPLO DE USO CON POSTMAN
# {
#   "nombre_cuenta": "PRUEBAq21",
#   "nombre_usuario": "PRUEBA21",
#   "correo": "PRUEBA21q@PRUEBA21.COM",
#   "contrasena": "1234321"
# }
@blueprint_users.route('/', methods=['POST'])
def crear_usuario():
    data = request.json

    CAMPOS_PERMITIDOS = {
        "foto_perfil",
        "descripcion",
        "nombre_usuario",
        "nombre_cuenta",
        "correo",
        "contrasena"
    }

    CAMPOS_OBLIGATORIOS = {"nombre_cuenta", "correo", "contrasena"}

    if not data or not all(campo in data for campo in CAMPOS_OBLIGATORIOS):
        return jsonify({"Errores": ["Faltan campos obligatorios: " + ", ".join(CAMPOS_OBLIGATORIOS)]}), 400

    errores = []

    # Validación correo
    patron_correo = r'^[\w\.-]+@[\w\.-]+\.\w+$'
    correo = data.get("correo", "")
    if not re.match(patron_correo, correo):
        errores.append("Correo electrónico no válido")

    # Validación nombre cuenta
    nombre_cuenta = data.get("nombre_cuenta", "")
    if len(nombre_cuenta) < 3:
        errores.append("El nombre de cuenta debe tener al menos 3 caracteres")

    # Validación contraseña
    contrasena = data.get("contrasena", "")
    if len(contrasena) < 4 or not re.match(r'^[a-zA-Z0-9]+$', contrasena):
        errores.append("La contraseña debe tener más de 3 caracteres y ser alfanumérica")

    if errores:
        return jsonify({"Errores": errores}), 400

    try:
        with get_session() as session:

            if session.query(Usuario).filter_by(nombre_cuenta=data["nombre_cuenta"]).first():
                return jsonify({"Errores": ["El nombre de cuenta ya está en uso"]}), 409

            if session.query(Usuario).filter_by(correo=data["correo"]).first():
                return jsonify({"Errores": ["El correo ya está registrado"]}), 409

            nuevo_usuario = Usuario()

            for campo, valor in data.items():
                if campo in CAMPOS_PERMITIDOS and valor is not None:
                    if campo == "contrasena":
                        valor = generate_password_hash(valor)
                    setattr(nuevo_usuario, campo, valor)

            if not hasattr(nuevo_usuario, "nombre_usuario") or not nuevo_usuario.nombre_usuario:
                nuevo_usuario.nombre_usuario = nuevo_usuario.nombre_cuenta

            nuevo_usuario.codigo_amigo = get_unique_friend_code(session)

            session.add(nuevo_usuario)
            session.commit()

            return jsonify({
                "mensaje": "Usuario creado correctamente",
                "id_usuario": nuevo_usuario.id_usuario
            }), 201

    except IntegrityError as e:
        logger.error(f"Error de integridad al crear usuario: {e}")
        return jsonify({"Errores": ["Ya existe un usuario con ese nombre de cuenta, correo o token"]}), 409

    except Exception as e:
        logger.error(f"Error general al crear usuario: {e}")
        return jsonify({"Errores": ["Error interno del servidor"]}), 500

    
    
# Metodo para recargar saldo de un usuario    
@blueprint_users.route('/<int:user_id>/recargar', methods=['PUT'])
def recharge_currency(user_id):
    with get_session() as session:
        currency_amount: string = request.args.get('cantidad')
        currency_amount = currency_amount.replace(',', '.')
        user: Usuario = session.query(Usuario).filter(Usuario.id_usuario == user_id).first()
        # Comprobar si no hay usuario 
        if not user:
            logger.info(f"Se ha intentado recargar dinero al usuario con id {user_id}, pero dicho usuario no existe")
            return jsonify({
                'success': False,
                'message': f'No existe el usuario con id {user_id}',
            }), 404
        logger.info(f"{user.nombre_cuenta} quiere recargar dinero: {currency_amount}")
        # Comprobar si se ha introducido la cantidad
        if currency_amount is None:
            logger.warning('Falta el parametro obligatorio: "cantidad"')
            return jsonify({
                'success': False,
                'message': 'Falta el parametro obligatorio: "cantidad"',
                'dinero_restante': str(user.dinero)
            }), 400
        try:
            # Validar tipo de cantidad
            if not isinstance(currency_amount, (int, float, str)):
                logger.warning('El parámetro "cantidad" debe ser un número entero, decimal o string convertible.')
                return jsonify({'success': False, 'message': 'El parámetro "cantidad" debe ser un número.'}), 400
            currency_amount = Decimal(str(currency_amount))
            if currency_amount <= 0.00:
                raise ValueError('La cantidad debe ser positiva')
        except Exception as e:
            logger.warning(f'Cantidad inválida: {e}')
            return jsonify({'success': False, 'message': f'Cantidad inválida: {e}'}), 400
        # Recargar saldo
        user.dinero = user.dinero + currency_amount
        session.commit()
        logger.info(f'{user.nombre_cuenta} ha recargado {currency_amount}$, saldo actual : {user.dinero}$')
        return jsonify({
            'success': True,
            'message': f'{user.nombre_cuenta} ha recargado {currency_amount}, su saldo actual es: {user.dinero}',
            'dinero_restante': str(user.dinero)
        }), 200
        
        
        
# Ejemplos de ENDPOINTS:
#       http://127.0.0.1:5000/users/search?nombre=Ma -> Busca primero los que empiezan por 'Ma' y luego los que contiene en su nombre o apellidos 'Ma'

@blueprint_users.route('/search', methods=['GET'])
def search_users():
    short = request.args.get('short') is not None
    nombre = request.args.get('nombre', type=str)

    if not nombre:
        return jsonify({"Error": "El parámetro 'nombre' es obligatorio."}), 400

    try:
        with get_session() as session:
            # 1. Primero busca los usuarios cuyo nombre empieza por la cadena
            empieza_por = session.query(Usuario).filter(
                Usuario.nombre_usuario.ilike(f'{nombre}%')
            ).all()

            # 2. Despues busca los usuarios cuyo nombre contiene la cadena pero no empieza por ella
            contiene = session.query(Usuario).filter(
                Usuario.nombre_usuario.ilike(f'%{nombre}%'),
                ~Usuario.nombre_usuario.ilike(f'{nombre}%')  # excluye los que ya están en el primer grupo
            ).all()

            # Combina sin duplicados
            resultados = empieza_por + contiene

            # Serializa
            resultado_serializado = [
                user_to_dict_short(u) if short else user_to_dict(u)
                for u in resultados
            ]

            return jsonify(resultado_serializado if resultados else {"mensaje": "No se encontraron usuarios con ese nombre."})

    except Exception as e:
        print("Error:", e)
        return jsonify({"Error": "Error interno del servidor"}), 500


# Este metodo es para sacar información de un usuario en especifico. Devuelve el usuario con la id que le digas 
#           http://127.0.0.1:5000/users/1 -> Devuelve toda la info del user 
#           http://127.0.0.1:5000/users/1?short -> Devuelve solo 'estado', 'foto' y 'nombre'
#           http://127.0.0.1:5000/users/1?campos=nombre_usuario,estado,token -> despues de la coma pones todos los campos que quieras

@blueprint_users.route('/<int:user_id>', methods=['GET'])
def get_user(user_id):
    short = request.args.get('short') is not None
    campos_param = request.args.get('campos')
    
    CAMPOS_PERMITIDOS = {
    "id_usuario",
    "nombre_usuario",
    "estado",
    "foto_perfil",
    "codigo_amigo",
    "descripcion",
    "correo",
    "dinero",              
    "nombre_cuenta",       
    "token",               
    "contrasena",          
}
    
    campos_solicitados = set()
    if campos_param:
        print(request.args)
        campos_solicitados = set(campos_param.split(','))
        campos_solicitados = campos_solicitados.intersection(CAMPOS_PERMITIDOS)

    try:
        with get_session() as session:
            usuario = session.query(Usuario).filter(Usuario.id_usuario == user_id).first()
            if not usuario:
                return jsonify({"mensaje": f"Usuario con ID {user_id} no encontrado"}), 404

            if campos_param and campos_solicitados:
                user_dict = user_to_dict_campos(usuario, campos_solicitados)
                return jsonify({'usuario': user_dict})    

            elif short:
                user_dict = user_to_dict_short(usuario)
                return jsonify({'usuario': user_dict})

            else:
                user_dict = user_to_dict(usuario)
                return jsonify({'usuario': user_dict})

            

    except Exception as e:
        logger.error(f"Error al obtener el usuario con ID {user_id}: {e}")
        return jsonify({"Error": "Error interno del servidor"}), 500


# Metodo GET que te devuelve el usuario con el nombre que le pases al endpoint
#       http://127.0.0.1:5000/users/name/Ana%20Torres -------> Busqueda por nombre
#       http://127.0.0.1:5000/users/name/Ana%20Torres?short -> Devuelve solo 'estado', 'foto' y 'nombre' || (ENCODING UTF-8 = %20) || -> No es necesario poner %20, el navegador lo pone solo

@blueprint_users.route('/name/<string:nombre>', methods=['GET'])
def get_user_by_name(nombre):
    short = request.args.get('short') is not None
    logger.debug(f"Recibido metodo GET name/{nombre}")
    try:
        with get_session() as session:
            user_query = session.query(Usuario).filter(Usuario.nombre_usuario == nombre).first()
            if user_query:
                if short:
                    user_dict = user_to_dict_short(user_query)
                    logger.debug(f"Datos del usuario serializados: {user_dict}")
                else:
                    user_dict = user_to_dict(user_query)
                    logger.debug(f"Datos del usuario serializados: {user_dict}")
                return jsonify({'usuario': user_dict}) 
            else:
                return jsonify({"mensaje": f"usuario con id{nombre} no encontrado"}),404   
    except Exception as e:
        logger.error(f"Error al obtener el usuario con ID{nombre}:{e}")
        return jsonify({"Error": "Error interno del servidor"}), 500
    
# Metodo GET que te devuelve el usuario con el codigo de amigo que le pases al endpoint
#       http://127.0.0.1:5000/users/friend_code/4H54K34H -------> Busqueda por friend_code
#       http://127.0.0.1:5000/users/friend_code/4H54K34H?short -> Devuelve solo 'estado', 'foto' y 'nombre' || (ENCODING UTF-8 = %20) || -> No es necesario poner %20, el navegador lo pone solo

@blueprint_users.route('/friend_code/<string:friend_code>', methods=['GET'])
def get_user_by_friend_code(friend_code):
    short = request.args.get('short') is not None
    logger.debug(f"Recibido metodo GET friend_code/{friend_code}")
    try:
        with get_session() as session:
            user_query = session.query(Usuario).filter(Usuario.codigo_amigo == friend_code).first()
            if user_query:
                if short:
                    user_dict = user_to_dict_short(user_query)
                    logger.debug(f"Datos del usuario serializados: {user_dict}")
                else:
                    user_dict = user_to_dict(user_query)
                    logger.debug(f"Datos del usuario serializados: {user_dict}")
                return jsonify({'usuario': user_dict}) 
            else:
                return jsonify({"mensaje": f"usuario con friend_code {friend_code} no encontrado"}),404   
    except Exception as e:
        logger.error(f"Error al obtener el usuario con friend_code {friend_code}: {e}")
        return jsonify({"Error": "Error interno del servidor"}), 500
    
        
def generate_friend_code(length=8):
    # Genera un código aleatorio de letras y números.
    return ''.join(random.choices(string.ascii_uppercase + string.digits, k=length))

def get_unique_friend_code(session):
    # Genera y asigna un código único a un usuario.
    max_intentos = 1000  # Para evitar bucles infinitos
    intentos = 0

    while intentos < max_intentos:
        codigo = generate_friend_code()
        # Comprobar si el código ya existe en la base de datos
        exists = session.query(Usuario).filter(Usuario.codigo_amigo == codigo).first()
        if not exists:
            return codigo
        intentos += 1

    raise Exception("No se pudo generar un código único después de muchos intentos.")

# ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓ METODOS EXTRA ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
    
def user_to_dict_campos(usuario, campos: set):
      user_dict = {
        'id_usuario': usuario.id_usuario,
        'nombre_usuario': usuario.nombre_usuario,
        'contrasena': usuario.contrasena,
        'descripcion': usuario.descripcion,
        'foto_perfil': usuario.foto_perfil if usuario.foto_perfil else None,
        'codigo_amigo': usuario.codigo_amigo,
        'correo': usuario.correo,
        'nombre_cuenta': usuario.nombre_cuenta,
        'token': usuario.token,
        'dinero': usuario.dinero,
        'estado': usuario.estado,
    }
      
      return {campo: valor for campo, valor in user_dict.items() if campo in campos}
 
def user_to_dict(usuario: Usuario):
    user_dict = {
        'id_usuario': usuario.id_usuario,
        'nombre_usuario': usuario.nombre_usuario,
        #'contrasena': usuario.contrasena,
        'descripcion': usuario.descripcion,
        "foto_perfil": usuario.foto_perfil if usuario.foto_perfil else None,
        'codigo_amigo': usuario.codigo_amigo,
        'correo': usuario.correo,
        'nombre_cuenta': usuario.nombre_cuenta,
        'token': usuario.token,
        'dinero': usuario.dinero,
        'estado': usuario.estado,
 
    }
    return user_dict

def user_to_dict_short(usuario: Usuario):
    user_dict = {
        'nombre_usuario': usuario.nombre_usuario,
        'foto_perfil': usuario.foto_perfil if usuario.foto_perfil else None,
        'estado': usuario.estado,
        'id_usuario': usuario.id_usuario,
    }
    return user_dict

def solicitud_to_dict(solicitud: SolicitudAmistad, usuario: Usuario):
    return {
        "id_solicitud": solicitud.id,
        'nombre_usuario': usuario.nombre_usuario,
        'foto_perfil': usuario.foto_perfil if usuario.foto_perfil else None,
        'estado': usuario.estado,
        'id_usuario': usuario.id_usuario,
    }