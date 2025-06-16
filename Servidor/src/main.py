import logging

from flask_socketio import SocketIO
from BD.conexion_bd import is_db_online
from flask import Flask, jsonify, redirect, request, Blueprint, render_template, json, url_for
from API_Rest.Users.api_friend_list import blueprint_friend_list
from API_Rest.Users.api_users import blueprint_users
from API_Rest.Games.api_games import blueprint_games
from API_Rest.Games.api_library import blueprint_library
from API_Rest.Games.api_store import blueprint_store
from API_Rest.Users.api_user_profile import blueprint_user_profile
from API_Rest.Users.api_login import blueprint_login
from API_Rest.Users.api_chats import *
from extensions import socketio
from BD.models import *



import os

app= Flask(__name__)
socketio = SocketIO(
    app,
    cors_allowed_origins="*",
    logger=True,
    engineio_logger=True,
    ping_interval=25,
    ping_timeout=20,
    async_mode="threading",
    engineio_options={'transports': ['polling']}
)
logger = logging.getLogger('Server')


@app.route("/", methods=['GET', 'POST'])
def root():
    error = None
    if request.method == 'POST':
        usuario = request.form['usuario']
        contrasena = request.form['contrasena']
        if usuario == 'admin' and contrasena == '1234':
            return redirect(url_for('admin_panel'))
        else:
            error = 'Usuario o contraseña incorrectos.'
    return render_template('login.html', error=error)

@app.route("/server_logs")
def mostrar_logs():
    # Lee el archivo logs, le las lineas, y se las pasa al html
    try:
        with open('server.log', 'r', encoding='utf-8') as log_file:
            logs = log_file.readlines()
    except Exception as e:
        logger.error(f"Error reading log file: {e}")
        logs = [f"Error reading logs: {e}"]
    return render_template("logs.html", logs=logs)



@app.route('/logout')
def logout():
    # lógica para cerrar sesión
    return redirect(url_for('root'))

# En este endpoint se van subiendo los logs en formato json
# y el HTML tiene un pequeño script que va a sacar los logs
# de forma periodica de este endpoint
@app.route("/logs")
def get_logs():
    try:
        with open('server.log', 'r', encoding='utf-8') as log_file:
            logs = log_file.readlines()
        return jsonify({"logs": logs})
    except Exception as e:
        logger.error(f"Error reading log file: {e}")
        return jsonify({"error": str(e)}), 500


@app.route("/users/<int:id>/password")
def cambiar_password_html(id):
    return render_template("cambiar_password.html", id_usuario=id)

@app.route('/chat_test')
def chat_test():
    return render_template('chat_test.html')

@app.route('/creditos')
def creditos():
    return render_template('creditos.html')

@app.route('/admin_panel')
def admin_panel():
    with get_session() as session:
        total_juegos = session.query(Juegos).count()
        total_usuarios = session.query(Usuario).count()
        return render_template('admin_panel.html',total_juegos=total_juegos,total_usuarios=total_usuarios)

# Registra los blueprints, los cuales son necesarios para modularizar la API
def register_flask_blueprints():
    app.register_blueprint(blueprint_friend_list, url_prefix='/friend_list')
    app.register_blueprint(blueprint_games, url_prefix='/games')
    app.register_blueprint(blueprint_library, url_prefix='/library')
    app.register_blueprint(blueprint_store, url_prefix='/store')
    app.register_blueprint(blueprint_users, url_prefix='/users')
    app.register_blueprint(blueprint_user_profile, url_prefix='/user_profile')
    app.register_blueprint(blueprint_login, url_prefix='/login')
    app.register_blueprint(blueprint_chats, url_prefix='/chats')
    logger.debug('Se han registrado los blueprints de flask')


def config_log_system():
    logger.setLevel(logging.DEBUG)

    # Crea el file handler y selecciona el nivel
    # El nivel elige que logs van a salir y cuales no
    fh = logging.FileHandler('server.log',encoding='utf-8')
    fh.setLevel(logging.DEBUG)
    
    # Crea el console handler y selecciona el nivel
    # El nivel elige que logs van a salir y cuales no
    ch = logging.StreamHandler()
    ch.setLevel(logging.DEBUG)
    

    # Crea un formato y lo añade al console handler
    formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
    ch.setFormatter(formatter)
    fh.setFormatter(formatter)

    # Agrega el console handler al logger
    logger.addHandler(ch)
    logger.addHandler(fh)

    # Estos son los comandos que deben usarse para usar los logs
    # logger.debug('debug message')
    # logger.info('info message')
    # logger.warning('warn message')
    # logger.error('error message')
    # logger.critical('critical message')
    
def create_log_file(file : str):
    # Borrar el archivo si existe
    try:
        os.remove(file)
    except Exception as e:
        print(f'Error al borrar el archivo de logs: {e}')

    # Crear un nuevo archivo
    with open(file, 'w', encoding='utf-8') as archivo:
        archivo.write('')

### SOCKETS ###

@socketio.on('connect')
def on_connect():
    print(f"[Conexión] Cliente conectado: {request.sid}")
    emit('status', {'msg': 'Conexión establecida con éxito'}, to=request.sid)


@socketio.on('disconnect')
def on_disconnect():
    print(f"[Desconexión] Cliente desconectado: {request.sid}")


@socketio.on('join_chat')
def on_join(data):
    id_chat = data.get('id_chat')
    id_usuario = data.get('id_usuario')

    print(f"[JOIN_CHAT] Solicitud de unión: usuario={id_usuario}, chat={id_chat}")

    with get_session() as session:
        pertenece = session.query(UsuarioGrupoChat).filter_by(id_usuario=id_usuario, id_chat=id_chat).first()
        if not pertenece:
            print(f"[JOIN_CHAT] ❌ Usuario {id_usuario} NO pertenece al chat {id_chat}")
            emit("error", f"Usuario {id_usuario} no pertenece al chat {id_chat}", room=request.sid)
            return

    sala = f"chat_{id_chat}"
    print(f"[Salas] {rooms(request.sid)}") 
    join_room(sala)
    print(f"[JOIN_CHAT] ✅ Usuario {id_usuario} unido a la sala {sala}")
    print(f"[Salas] {rooms(request.sid)}")  # <- Mueve esto aquí
    emit("status", f"Te has unido a {sala}", room=request.sid)



@socketio.on('leave_chat')
def on_leave(data):
    id_chat = data.get('id_chat')
    id_usuario = data.get('id_usuario')

    leave_room(f"chat_{id_chat}")
    print(f"[Sala] Usuario {id_usuario} salió de la sala chat_{id_chat}")
    emit('status', {'msg': f'Usuario {id_usuario} salió del chat {id_chat}'}, room=f"chat_{id_chat}")


@socketio.on('enviar_mensaje')
def handle_enviar_mensaje(data):
    zona_horaria_local = pytz.timezone('Europe/Madrid')
    hora_local = datetime.now(zona_horaria_local)

    id_usuario_remitente = data.get('id_usuario_remitente')
    id_chat = data.get('id_chat')
    mensaje_texto = data.get('mensaje')

    with get_session() as session:
        pertenece = session.query(UsuarioGrupoChat).filter_by(
            id_usuario=id_usuario_remitente,
            id_chat=id_chat
        ).first()
        if not pertenece:
            emit('error', {'error': 'Usuario no pertenece al chat'}, to=request.sid)
            return
        mensaje = Mensaje(
            mensaje=mensaje_texto,
            id_usuario_remitente=id_usuario_remitente,
            id_chat=id_chat,
            timestamp=hora_local
        )
        session.add(mensaje)
        session.commit()
        remitente = session.query(Usuario).filter_by(id_usuario=id_usuario_remitente).first()
        
        print(f"[Mensaje] Enviado por {id_usuario_remitente} en chat {id_chat}: {mensaje_texto}")
        print(f"[Salas] {rooms(request.sid)}")
        emit('nuevo_mensaje', {
            'id_mensaje': mensaje.id_mensaje,
            'id_chat': mensaje.id_chat,
            'mensaje': mensaje.mensaje,
            'timestamp': mensaje.timestamp.isoformat(),
            'id_usuario_remitente': mensaje.id_usuario_remitente,
            'nombre_usuario': mensaje.remitente.nombre_usuario
        }, room=f'chat_{id_chat}')


#Esto actua como el main de la app
if __name__ == "__main__":
    config_log_system()
    create_log_file("server.log")
    register_flask_blueprints()
    logger.info('El servidor se ha iniciado')
    if is_db_online():
        logger.info("El servidor se ha conectado a PostgreeSQL con exito")
        socketio.run(app, debug=True, host="0.0.0.0", port=50000, use_reloader=False)
        # app.run(debug=True, host="0.0.0.0", port=50000, use_reloader=False)
    else:
        logger.error("Error de conexión con PostgreeSQL")

    