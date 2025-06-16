from flask import Flask, jsonify, request, Blueprint
import logging
from BD.conexion_bd import get_session
from BD.models import *
from sqlalchemy import cast, Float,or_,func 
from sqlalchemy.inspection import inspect
from sqlalchemy.orm import aliased
from datetime import datetime


logger = logging.getLogger('Server.games')
# Blueprint para endpoints de la lista de amigos, no es necesario repetir el prefijo en los metodos
blueprint_games = Blueprint('games', __name__)

# Devuelve la lista de juegos
# Ejemplos de uso:
#     http://127.0.0.1:5000/games
#     http://127.0.0.1:5000/games/?short  # Te da solo nombre e id
#     http://127.0.0.1:5000/games/?page=4 # Te da la pagina 4
#     http://127.0.0.1:5000/games/?page=20&limit=100 # Te da la pagina 20 y salen 100 juegos por pagina
#     http://127.0.0.1:5000/games/?short&limit=100000 # Te salen todos los juegos en una pagina con solo el nombre e id
#     http://127.0.0.1:5000/games/?gender=Action&gender=RPG&category=Multi-player&category=Co-op&short # Filtras por generos y categorias
#     http://127.0.0.1:5000/games/?short&category=Multi-player&gender=Action&min_price=10&max_price=20&discount=1&name=Mortal
#     http://127.0.0.1:5000/games/?release_date=06-10-2024&short
@blueprint_games.route('/', methods=['GET'])
def get_games():
    try:
        logger.info("Se ha recibido una solicitud para obtener juegos")
        
        # Info paginada
        limit = request.args.get('limit', default=50, type=int)
        page = request.args.get('page', default=1, type=int)
        
        # Cantidad de datos
        short = request.args.get('short') is not None
        full = request.args.get('full') is not None

        # Filtros
        category_names = request.args.getlist('category',type=str)
        gender_names = request.args.getlist('gender',type=str)
        game_name = request.args.get('name',type=str)
        free = request.args.get('free', type=int)  # 0= de pago y 1 = gratis
        discounted = request.args.get('discount', type=int)  # 0= sin descuento 1 = en descuento
        max_price = request.args.get('max_price', type=float)
        min_price = request.args.get('min_price', type=float)
        release_date = request.args.get('release_date', type=str)

        offset = (page - 1) * limit
        
        # Solo devuelve juegos y dlcs, no devuelve trailers por ejemplo        
        available_types = ["game","dlc"]

        # Registrar argumentos para el logger
        args_dict = {
            "limit": limit,
            "page": page,
            "short": short,
            "full": full,
            "category_names": category_names,
            "gender_names": gender_names,
            "game_name": game_name,
            "free": free,
            "discounted": discounted,
            "max_price": max_price,
            "min_price": min_price,
            "release_date": release_date
        }
        logger.debug(f"Argumentos recibidos: {args_dict}")

        with get_session() as session:
            query = session.query(Juegos).order_by(Juegos.steam_appid)

            # FILTRO: Categorías
            if category_names:
                # Eliminar duplicados
                category_names = list(set(category_names))

                all_categories = session.query(Categorias).filter(
                    or_(*[Categorias.descripcion.ilike(f"%{cat}%") for cat in category_names])
                ).all()

                if len(all_categories) < len(category_names):
                    finded_categories = [c.descripcion.lower() for c in all_categories]
                    not_finded_categories = [cat for cat in category_names if all(cat.lower() not in desc.lower() for desc in finded_categories)]
                    return jsonify({'error': 'Categoría no encontrada', 'categorias': not_finded_categories}), 404

                # Hacer JOIN por cada categoría, asegurando que tenga TODAS
                for idx, category in enumerate(all_categories):
                    alias = aliased(JuegoCategoria)
                    query = query.join(alias, Juegos.categorias.of_type(alias)).filter(
                        alias.categoria_id == category.id
                    )
                logger.debug("Se ha aplicado el filtro correctamente por las siguientes categorias: "+str(category_names))

            # FILTRO: Géneros
            if gender_names:
                # Eliminar duplicados
                gender_names = list(set(gender_names))

                all_genders = session.query(Generos).filter(
                    or_(*[Generos.descripcion.ilike(f"%{g}%") for g in gender_names])
                ).all()

                if len(all_genders) < len(gender_names):
                    finded_genders = [g.descripcion.lower() for g in all_genders]
                    not_finded_genders = [g for g in gender_names if all(g.lower() not in gen.lower() for gen in finded_genders)]
                    return jsonify({'error': 'Género no encontrado', 'generos': not_finded_genders}), 404

                # Hacer JOIN por cada género, asegurando que tenga TODOS
                for idx, gender in enumerate(all_genders):
                    alias = aliased(JuegoGenero)
                    query = query.join(alias, Juegos.generos.of_type(alias)).filter(
                        alias.genero_id == gender.id
                    )
                logger.debug("Se ha aplicado el filtro correctamente por los siguientes géneros: "+str(gender_names))

            # FILTRO: Nombre del juego
            if game_name:
                query = query.filter(Juegos.nombre.ilike(f"%{game_name}%"))
                logger.debug("Se ha aplicado el filtro correctamente por el nombre",str(game_name))

            # FILTRO: Gratis
            if free == 1:
                query = query.filter(Juegos.is_free == True)
                logger.debug("Se ha aplicado el filtro '¿Es Gratis?' correctamente")

            # FILTRO: En descuento
            if discounted == 1:
                query = query.join(Juegos.price_overview).filter(
                    PriceOverview.descuento.isnot(None),
                    PriceOverview.descuento > "0"
                )
                logger.debug("Se ha aplicado el filtro 'En descuento' correctamente")

            try:
                # FILTRO: Precio máximo
                if max_price is not None:
                    query = query.join(Juegos.price_overview).filter(
                        cast(PriceOverview.precio_inicial, Float) / 100 <= max_price
                    )
                    logger.debug("Se ha aplicado correctamente el filtro 'Precio máximo' en", str(max_price))
                # FILTRO: Precio mínimo
                if min_price is not None:
                    query = query.join(Juegos.price_overview).filter(
                        cast(PriceOverview.precio_inicial, Float) / 100 >= min_price
                    )
                    logger.debug("Se ha aplicado correctamente el filtro 'Precio Minimo' en", str(min_price))
            except Exception as e:
                logger.warning(f"Error aplicando filtro de precio: {e}")
                return jsonify({'error': 'Error aplicando filtro de precio', 'detalle': str(e)}), 400

            try:
                if release_date:
                    try:
                        # Parsear la fecha del cliente
                        release_date_parsed = datetime.strptime(release_date, '%d-%m-%Y')
                        # Convertir al formato almacenado en BD ("6 Mar, 2007")
                        release_date_db_format = release_date_parsed.strftime('%d %b, %Y')
                        # Filtrar directamente en la consulta SQL
                        query = query.filter(Juegos.fecha_salida >= release_date_db_format)
                    except ValueError as ve:
                        logger.warning(f"Formato de fecha inválido: {ve}")
                        return jsonify({
                            'error': 'Formato de fecha inválido',
                            'ejemplos': [
                                '?release_date=06-10-2024 (día-mes-año)',
                                '?release_date=6 Mar, 2007'
                            ]
                        }), 400
                    except Exception as e:
                        logger.warning(f"Error aplicando filtro de fecha: {e}")
                        return jsonify({'error': 'Error aplicando filtro de fecha', 'detalle': str(e)}), 400

                    logger.debug("Se ha aplicado el filtro para saber los juegos lanzados después de",str(release_date))
                    # Filtramos los juegos que ya tenemos en la query
                    games_query = [
                        juego for juego in query.all()
                        if (juego.fecha_salida and 
                            convert_release_date(juego.fecha_salida) and 
                            convert_release_date(juego.fecha_salida) >= release_date_parsed)
                    ]
                    logger.debug("Se ha realizado la petición a la base de datos")
                    # Se agrega la paginacion
                    games_query = games_query[offset:offset+limit]
                else:
                    games_query = query.order_by(Juegos.steam_appid).filter(Juegos.tipo.in_(available_types)).limit(limit).offset(offset).all()
                    logger.debug("Se ha realizado la petición a la base de datos")
            except Exception as e:
                logger.warning(f"Error aplicando filtro de fecha: {e}")
                return jsonify({'error': 'Error aplicando filtro de fecha', 'detalle': str(e)}), 400

            # Se pasa la query a diccionario
            if short:
                json_string = [game_to_dict_short(juego) for juego in games_query]
                logger.debug("La petición se ha convertido en diccionario correctamente")
            elif full:
                json_string = [game_to_dict_full(session, juego) for juego in games_query]
                logger.debug("La petición se ha convertido en diccionario de formato entero correctamente")
            else:
                json_string = [game_to_dict(session, juego) for juego in games_query]
                logger.debug("La petición se ha convertido en diccionario de formato corto correctamente")
                
        game_count = len(json_string)
        if game_count > 0:
            logger.info("La solicitud se ha completado y será devuelta, numero de juegos encontrados: "+str(game_count))
        else:
            logger.warning("No se ha encontrado ningun juego en esta solicitud")

        # Se pasa el diccionario a json
        return jsonify({
            'pagina': page,
            'limite': limit,
            'total': len(json_string),
            'juegos': json_string
        })
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /games: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500

# Devuelve juegos que contengan el nombre que tu le hayas pasado
# Ejemplos de uso:
#       /games/counter           te da la info de todos los juegos que contengan counter
#       /games/counter?short     solo te da el nombre e id
@blueprint_games.route('/<string:game_name>', methods=['GET'])
def get_game_by_name(game_name):
    try:
        short  = request.args.get('short') is not None # esto devuelve true asique no hay que poner parametro
        full = request.args.get('full') is not None
        with get_session() as session:
            games_query = session.query(Juegos).order_by(Juegos.steam_appid).filter(Juegos.nombre.ilike(f"%{game_name}%"))
            
            if short:
                json_string = [game_to_dict_short(juego) for juego in games_query]
                logger.debug("La petición se ha convertido en diccionario correctamente")
            elif full:
                json_string = [game_to_dict_full(session, juego) for juego in games_query]
                logger.debug("La petición se ha convertido en diccionario de formato entero correctamente")
            else:
                json_string = [game_to_dict(session, juego) for juego in games_query]
                logger.debug("La petición se ha convertido en diccionario de formato corto correctamente")
                
                
            game_count = len(json_string)
            if game_count > 0:
                logger.info("La solicitud se ha completado y será devuelta, numero de juegos encontrados: "+str(game_count))
            else:
                logger.warning("No se ha encontrado ningun juego en esta solicitud")
                
            return jsonify({
                'juegos encontrados': game_count,
                'juegos': json_string
            })
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /games/<string:game_name>: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500
        
@blueprint_games.route('/<int:id_game>', methods=['GET'])
def get_game_by_id(id_game):
    try:
        short  = request.args.get('short') is not None # esto devuelve true asique no hay que poner parametro
        full = request.args.get('full') is not None
        with get_session() as session:
            games_query = session.query(Juegos).order_by(Juegos.steam_appid).filter(Juegos.steam_appid == id_game)
            
            if short:
                json_string = [game_to_dict_short(juego) for juego in games_query]
                logger.debug("La petición se ha convertido en diccionario correctamente")
            elif full:
                json_string = [game_to_dict_full(session, juego) for juego in games_query]
                logger.debug("La petición se ha convertido en diccionario de formato entero correctamente")
            else:
                json_string = [game_to_dict(session, juego) for juego in games_query]
                logger.debug("La petición se ha convertido en diccionario de formato corto correctamente")
                
            return jsonify({
                'juegos': json_string
            })
    except Exception as e:
        logger.error(f"Error inesperado en el endpoint /games/<int:id_game>: {e}")
        return jsonify({'error': 'Error inesperado en el servidor', 'detalle': str(e)}), 500


# vvv Métodos de utilidad vvv

# Función que devuelve datos sobre los juegos
def game_to_dict(session, juego : Juegos):
    price = session.query(PriceOverview).filter_by(juego_id=juego.steam_appid).first()
    juego_dict = {
                'app_id': juego.steam_appid,
                'nombre': juego.nombre,
                'precio': {
                    'precio_inicial': price.precio_inicial if price else None,
                    'descuento': price.descuento if price else None,
                },
                'imagen_capsula_v5' :juego.capsule_imagev5,
                'f2p':juego.is_free,
            } 
    return juego_dict

# Función que devuelve una cantidad minima de datos sobre los juegos
def game_to_dict_short(juego):
    juego_dict = {
            'app_id': juego.steam_appid,
            'nombre': juego.nombre,
        }  
    return juego_dict

# Función que devuelve todos los datos posibles sobre los juegos
def game_to_dict_full(session, juego : Juegos):
    price = session.query(PriceOverview).filter_by(juego_id=juego.steam_appid).first()
    ventas = session.query(func.count(UsuarioJuego.id_usuario)) \
    .filter(UsuarioJuego.id_juego == juego.steam_appid) \
    .scalar() #scalar lo devuelve como un int
    
    juego_dict = {
                'app_id': juego.steam_appid,
                'nombre': juego.nombre,
                'tipo': juego.tipo,
                'fecha_de_lanzamiento':juego.fecha_salida,
                'descripcion_corta': juego.short_description,
                'descripcion_detallada': juego.detailed_description,
                'precio': {
                    'precio_inicial': price.precio_inicial if price else None,
                    'descuento': price.descuento if price else None,
                },
                'generos': [jg.genero.descripcion for jg in juego.generos],
                'editores': [je.editor.nombre for je in juego.editores],
                'desarrolladores': [jd.desarrollador.nombre for jd in juego.desarrolladores],
                'categorias': [jc.categoria.descripcion for jc in juego.categorias],
                'capturas': [cap.path_full for cap in juego.capturas],
                'capturas_miniatura' : [cap.path_thumbnail for cap in juego.capturas],
                'imagen_cabecera': juego.header_image,
                'imagen_capsula' :juego.capsule_image,
                'imagen_capsula_v5' :juego.capsule_imagev5,
                'f2p':juego.is_free,
                'pagina_web': juego.website,
                'numero_de_ventas' : ventas
            } 
    return juego_dict

# Metodo generico que te devuelve una tabla como diccionario
def as_dict(obj):
    columns = [column.name for column in inspect(obj).mapper.columns]
    result = {column: getattr(obj, column) if getattr(obj, column) is not None else 'No disponible' for column in columns}
    
    for key, value in result.items():
        if value is None:
            result[key] = 'No disponible'
    return result


def convert_release_date(fecha_str):
    if not fecha_str:
        return None
    
    # Eliminar espacios en blanco al inicio/final
    fecha_str = fecha_str.strip()
    
    # Intentar con diferentes formatos
    formatos = [
        '%d %b, %Y',  # "6 Mar, 2007"
        '%d-%m-%Y',   # "06-10-2024"
        '%b %d, %Y',  # "Mar 6, 2007" (alternativo)
        '%Y-%m-%d',   # "2007-03-06" (formato ISO)
        '%m/%d/%Y'    # "03/06/2007" (formato americano)
    ]
    
    for formato in formatos:
        try:
            return datetime.strptime(fecha_str, formato)
        except ValueError:
            continue
    
    return None  # Si ningún formato coincide