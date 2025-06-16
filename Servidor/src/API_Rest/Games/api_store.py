from flask import Flask, jsonify, request, Blueprint
from BD.conexion_bd import get_session
from BD.models import *
from sqlalchemy import asc,desc,func,cast,Integer,Numeric, Date, DECIMAL
import logging
from decimal import Decimal


logger = logging.getLogger('Server.store')
# Blueprint para endpoints de la lista de amigos, no es necesario repetir el prefijo en los metodos
blueprint_store = Blueprint('store', __name__)

@blueprint_store.route('/carrusel', methods=['GET'])
def get_carrusel_games():
    try:
            
        limit = request.args.get('limit', default=10, type=int)
        with get_session() as session:
            
            logger.info("Se ha recibido una solicitud para actualizar el carrusel de la tienda")
            
            games_query = session.query(Juegos)    \
                .filter(Juegos.tipo == "game", Juegos.nombre != "")  \
                .join(Capturas, Juegos.steam_appid == Capturas.juego_id)    \
                .group_by(Juegos.steam_appid)   \
                .having(func.count(Capturas.path_thumbnail)>= 4)    \
                .order_by(desc(Juegos.player_count)) \
                .limit(limit)
            logger.debug("Se ha realizado una consulta a la base de datos para recibir los juegos del carrusel")
                
            json_string = [game_to_dict_carrusel(session,juego) for juego in games_query]
            logger.info("Se ha convertido en json correctamente y se devolverá medienta el endpoint /store/carrusel")
            return jsonify({
                'juegos': json_string
            })
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /store/carrusel: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500        
        
@blueprint_store.route('/ofertas', methods=['GET'])
def get_on_sale_games():
    try:
        limit = request.args.get('limit', default=6, type=int)
        with get_session() as session:
            
            logger.info("Se ha recibido una solicitud para actualizar las ofertas de la tienda")
            
            games_query = session.query(Juegos)    \
                .filter(Juegos.tipo == "game", Juegos.nombre != "")  \
                .join(PriceOverview, Juegos.steam_appid == PriceOverview.juego_id)    \
                .group_by(Juegos.steam_appid, PriceOverview.descuento) \
                .filter(Juegos.header_image.is_not(None), cast(PriceOverview.descuento,Integer) > 0)    \
                .order_by(desc(Juegos.player_count),desc(PriceOverview.descuento)) \
                .limit(limit)
            logger.debug("Se ha realizado una consulta a la base de datos para recibir los juegos en oferta")
                
            json_string = [game_to_dict_ofertas(session,juego) for juego in games_query]
            logger.info("Se ha convertido en json correctamente y se devolverá medienta el endpoint /store/ofertas")
            return jsonify({
                'juegos': json_string
            })
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /store/ofertas: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500     
               
@blueprint_store.route('/<float:price>', methods=['GET'])
def get_games_under_certain_price(price):
    try:
        limit = request.args.get('limit', default=4, type=int)
        with get_session() as session:
            
            logger.info(f"Se ha recibido una solicitud para actualizar los juegos por debajo de {price} euros")
            
            
            final_price = func.round(
                cast(
                    cast(PriceOverview.precio_inicial, Integer) * (1 - cast(PriceOverview.descuento, Integer) / 100.0),
                    Numeric
                ),
                0
            )
            
            games_query = session.query(Juegos)    \
                .filter(Juegos.tipo == "game", Juegos.nombre != "")  \
                .join(PriceOverview, Juegos.steam_appid == PriceOverview.juego_id)    \
                .group_by(Juegos.steam_appid,PriceOverview.precio_inicial,PriceOverview.descuento) \
                .filter(Juegos.capsule_image.is_not(None))    \
                .filter(final_price < price * 100)   \
                .order_by(desc(Juegos.player_count),desc(final_price)) \
                .limit(limit)
            logger.debug(f"Se ha realizado una consulta a la base de datos para recibir los juegos por debajo de {price} euros")
                
            json_string = [game_to_dict_under_price(session,juego) for juego in games_query]
            logger.info("Se ha convertido en json correctamente y se devolverá medienta el endpoint /store/<float>")
            return jsonify({
                'juegos': json_string
            })
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /store/<float>: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500                
        
@blueprint_store.route('/nuevos_lanzamientos', methods=['GET'])
def get_on_last_released_games():
    try:
        limit = request.args.get('limit', default=10, type=int)
        with get_session() as session:
            
            logger.info("Se ha recibido una solicitud para actualizar los nuevos lanzamientos")
            
                
            # Fecha actual
            fecha_actual = datetime.now().date()
            
            # Consulta para obtener juegos
            games_query = session.query(Juegos) \
                .filter(Juegos.tipo == "game", Juegos.nombre != "") \
                .filter(Juegos.capsule_image.is_not(None), Juegos.fecha_salida != "") \
                .join(PriceOverview, Juegos.steam_appid == PriceOverview.juego_id)    \
                .group_by(Juegos.steam_appid,PriceOverview.precio_inicial) \
                .filter(Juegos.is_free == False and PriceOverview.precio_inicial.is_not(None))
            
            # Recuperamos todos los juegos y parseamos las fechas
            juegos = games_query.all()
            juegos_con_fecha = []
            for juego in juegos:
                fecha_dt = parse_release_date(juego.fecha_salida)
                if fecha_dt and fecha_dt.date() <= fecha_actual:
                    juegos_con_fecha.append((juego, fecha_dt))
            
            # Ordenamos por fecha descendente (más recientes primero)
            juegos_con_fecha.sort(key=lambda x: x[1], reverse=True)
            
            # Tomamos solo el límite solicitado
            games_query = [juego for juego, _ in juegos_con_fecha[:limit]]
        

            json_string = [game_to_dict_last_releases(session,juego) for juego in games_query]
            logger.info("Se ha convertido en json correctamente y se devolverá medienta el endpoint /store/nuevos_lanzamientos")
            return jsonify({
                'juegos': json_string
            })
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /store/nuevos_lanzamientos: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500                

# Endpoint para comprar un juego de la tienda
# Se espera que se le pase el id del usuario y el id del juego
# /store/buy?user=<int>&game=<int>
@blueprint_store.route('/buy', methods=['POST'])
def buy_a_game():
    try:
        game_id = request.args.get('game',type=int)
        user_id = request.args.get('user',type=int)
        
        if game_id == None or user_id == None:
            logger.error("Se ha recibido una solicitud en /buy y faltan parámetros obligatorios: user=<int> y game=<int>.")
            return jsonify({'success': False, 'message': 'Faltan parámetros obligatorios.'}), 400
        
        with get_session() as session:
            logger.info(f"Se ha recibido una solicitud para comprar el juego con id {game_id} por el usuario {user_id}")
            
            # Encontrar el juego y el usuario
            game = session.query(Juegos).filter(Juegos.steam_appid == game_id).first()
            user = session.query(Usuario).filter(Usuario.id_usuario == user_id).first()

            if not game or not user:
                logger.warning(f"Compra fallida: usuario {user_id} o juego {game_id} no encontrado.")
                return jsonify({'success': False, 'message': 'Usuario o juego no encontrado.'}), 404
            logger.debug(f"Se ha realizado con exito la consulta del usuario {user_id} y el juego {game_id}")

            # Comprobar si el usuario ya tiene el juego en su biblioteca
            existing_entry = session.query(UsuarioJuego).filter_by(id_usuario=user_id, id_juego=game_id).first()
            if existing_entry:
                logger.warning(f"El usuario {user_id} ya posee el juego {game_id}.")
                return jsonify({'success': False, 'message': 'El usuario ya posee este juego.'}), 409

                # Comprobar si el juego es gratuito
            if game.is_free:
                logger.info(f"El juego {game_id} es gratuito. No se requiere compra.")
                precio_final: Decimal = Decimal('0.00')
            else:
                # Obtener el precio con descuento
                price = session.query(PriceOverview).filter_by(juego_id=game_id).first()
                if not price or price.precio_inicial is None:
                    logger.warning(f"No se encontró precio para el juego {game_id}.")
                    return jsonify({'success': False, 'message': 'No se encontró precio para el juego.'}), 404
                logger.debug(f"Se ha realizado con exito la consulta del precio del juego {game_id}")
                
                try:
                    precio_inicial = Decimal(price.precio_inicial) / Decimal('100.0')
                    descuento = Decimal(price.descuento) if price.descuento else Decimal('0')
                    logger.debug(f"{game.nombre}« Precio inicial: {precio_inicial} Descuento: {descuento}»")
                    precio_final = precio_inicial * (Decimal('1') - descuento / Decimal('100.0'))
                except Exception as e:
                    logger.error(f"Error al convertir precio o descuento: {e}")
                    return jsonify({'success': False, 'message': 'Error en los datos de precio.'}), 500

                logger.info(f"Precio final de {game.nombre}: {precio_final}")
                # Comprobar si el usuario tiene suficiente dinero
                if user.dinero is None or Decimal(user.dinero) < precio_final:
                    logger.warning(f"El usuario {user_id} no tiene suficiente dinero para comprar el juego {game_id}.")
                    return jsonify({'success': False, 'message': 'Dinero insuficiente.'}), 402

                # Descontar el dinero
                user.dinero = Decimal(user.dinero) - precio_final
                logger.debug(f"Se ha restado el dinero al usuario  {user_id} - Dinero restante: {user.dinero}")

            
            # Aumentar el contador de jugadores que tienen el juego
            game.player_count += 1

            # Añadir el juego a la biblioteca del usuario
            new_entry = UsuarioJuego(id_usuario=user_id, id_juego=game_id)
            session.add(new_entry)
            session.commit()
            logger.info(f"El usuario {user_id} ha comprado el juego {game_id} ({game.nombre}) por {precio_final}.")
            return jsonify({
                                'success': True,
                                'message': f'Juego comprado exitosamente. Dinero restante: {user.dinero}',
                                'dinero_restante': str(user.dinero)
                            }), 200
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /store/buy?user={user_id}&game={game_id}: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500                
        
        
def game_to_dict_carrusel(session, juego: Juegos):
    price = session.query(PriceOverview).filter_by(juego_id=juego.steam_appid).first()



    juego_dict = {
                'app_id': juego.steam_appid,
                'nombre': juego.nombre,
                'precio': {
                    'precio_inicial': price.precio_inicial if price else None,
                    'descuento': price.descuento if price else None,
                },
                'header_image' : juego.header_image,
                'capturas_miniatura' : [cap.path_thumbnail for cap in juego.capturas[:4]],
                'f2p':juego.is_free,

            } 
    return juego_dict

def game_to_dict_ofertas(session, juego: Juegos):
    price = session.query(PriceOverview).filter_by(juego_id=juego.steam_appid).first()

    juego_dict = {
                'app_id': juego.steam_appid,
                'nombre': juego.nombre,
                'precio': {
                    'precio_inicial': price.precio_inicial if price else None,
                    'descuento': price.descuento if price else None,
                },
                'header_image': juego.header_image 
            } 
    return juego_dict

def game_to_dict_under_price(session, juego: Juegos):
    price = session.query(PriceOverview).filter_by(juego_id=juego.steam_appid).first()

    juego_dict = {
                'app_id': juego.steam_appid,
                'nombre': juego.nombre,
                'precio': {
                    'precio_inicial': price.precio_inicial if price else None,
                    'descuento': price.descuento if price else None,
                },
                'capsule_image': juego.capsule_image
            } 
    return juego_dict

def game_to_dict_last_releases(session, juego : Juegos):
    price = session.query(PriceOverview).filter_by(juego_id=juego.steam_appid).first()
    
    juego_dict = {
                'app_id': juego.steam_appid,
                'nombre': juego.nombre,
                'fecha_de_lanzamiento':juego.fecha_salida,
                'precio': {
                    'precio_inicial': price.precio_inicial if price else None,
                    'descuento': price.descuento if price else None,
                },
                'generos': [jg.genero.descripcion for jg in juego.generos],
                'capsule_image' :juego.capsule_image,
                'f2p':juego.is_free,
            } 
    return juego_dict


def parse_release_date(fecha_str):
    """
    Convierte una cadena de fecha a datetime, manejando múltiples formatos.
    Devuelve None para fechas no válidas, vacías, "To be announced" o formatos imprecisos como "Q2 2025".
    """
    if not fecha_str or fecha_str.strip().lower() in ["", "to be announced", "tba"]:
        return None
    
    fecha_str = fecha_str.strip()
    
    # Formatos soportados
    formatos = [
        '%d %b, %Y',      # "26 Mar, 2025"
        '%b %d, %Y',      # "Mar 25, 2025"
        '%d-%m-%Y',       # "06-10-2024"
        '%Y-%m-%d',       # "2007-03-06"
        '%m/%d/%Y',       # "03/06/2007"
        '%B %Y'           # "March 2025"
    ]
    
    for formato in formatos:
        try:
            fecha_dt = datetime.strptime(fecha_str, formato)
            # Para fechas sin día (como "March 2025"), asumimos el primer día del mes
            return fecha_dt
        except ValueError:
            continue
    
    # Si no coincide con ningún formato (por ejemplo, "Q2 2025"), devolver None
    return None