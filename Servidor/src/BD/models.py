from sqlalchemy import Column, Integer, String, Text, DateTime, ForeignKey, Boolean, Numeric
from sqlalchemy.orm import relationship, sessionmaker
from sqlalchemy.orm import declarative_base
from sqlalchemy import create_engine
from datetime import datetime

# Esta es la forma de declarar el modelo de datos de la base de datos
# para que sqlalchemy pueda usarlo como si fuesen objetos
Base = declarative_base()


# === JUEGOS ===
class Juegos(Base):
    __tablename__ = 'juegos'

    steam_appid = Column(Integer, primary_key=True)
    nombre = Column(Text)
    tipo = Column(Text)
    required_age = Column(Integer)
    is_free = Column(Boolean)
    detailed_description = Column(Text)
    short_description = Column(Text)
    fecha_salida = Column(Text)
    header_image = Column(Text)
    capsule_image = Column(Text)
    capsule_imagev5 = Column(Text)
    website = Column(Text)
    player_count = Column(Integer)

    price_overview = relationship("PriceOverview", back_populates="juego")
    generos = relationship("JuegoGenero", back_populates="juego")
    editores = relationship("JuegoEditor", back_populates="juego")
    desarrolladores = relationship("JuegoDesarrollador", back_populates="juego")
    categorias = relationship("JuegoCategoria", back_populates="juego")
    capturas = relationship("Capturas", back_populates="juego")
    usuarios_juego = relationship("UsuarioJuego", back_populates="juego")


# === PRICE OVERVIEW ===
class PriceOverview(Base):
    __tablename__ = 'price_overview'

    id = Column(Integer, primary_key=True)
    juego_id = Column(Integer, ForeignKey('juegos.steam_appid'))
    precio_inicial = Column(String)
    descuento = Column(String)

    juego = relationship("Juegos", back_populates="price_overview")


# === JUEGO - GÉNERO ===
class JuegoGenero(Base):
    __tablename__ = 'juego_genero'

    juego_id = Column(Integer, ForeignKey('juegos.steam_appid'), primary_key=True)
    genero_id = Column(String, ForeignKey('generos.id'), primary_key=True)

    juego = relationship("Juegos", back_populates="generos")
    genero = relationship("Generos", back_populates="juegos")


# === JUEGO - EDITOR ===
class JuegoEditor(Base):
    __tablename__ = 'juego_editor'

    juego_id = Column(Integer, ForeignKey('juegos.steam_appid'), primary_key=True)
    editor_id = Column(Integer, ForeignKey('editores.id'), primary_key=True)

    juego = relationship("Juegos", back_populates="editores")
    editor = relationship("Editores", back_populates="juegos")


# === JUEGO - DESARROLLADOR ===
class JuegoDesarrollador(Base):
    __tablename__ = 'juego_desarrollador'

    juego_id = Column(Integer, ForeignKey('juegos.steam_appid'), primary_key=True)
    desarrollador_id = Column(Integer, ForeignKey('desarrolladores.id'), primary_key=True)

    juego = relationship("Juegos", back_populates="desarrolladores")
    desarrollador = relationship("Desarrolladores", back_populates="juegos")


# === JUEGO - CATEGORÍA ===
class JuegoCategoria(Base):
    __tablename__ = 'juego_categoria'

    juego_id = Column(Integer, ForeignKey('juegos.steam_appid'), primary_key=True)
    categoria_id = Column(Integer, ForeignKey('categorias.id'), primary_key=True)

    juego = relationship("Juegos", back_populates="categorias")
    categoria = relationship("Categorias", back_populates="juegos")


# === CAPTURAS ===
class Capturas(Base):
    __tablename__ = 'capturas'

    id = Column(Integer, primary_key=True)
    juego_id = Column(Integer, ForeignKey('juegos.steam_appid'))
    path_thumbnail = Column(String)
    path_full = Column(String)

    juego = relationship("Juegos", back_populates="capturas")


# === CATEGORÍAS ===
class Categorias(Base):
    __tablename__ = 'categorias'

    id = Column(Integer, primary_key=True)
    descripcion = Column(Text)

    juegos = relationship("JuegoCategoria", back_populates="categoria")


# === GÉNEROS ===
class Generos(Base):
    __tablename__ = 'generos'

    id = Column(String, primary_key=True)
    descripcion = Column(Text)

    juegos = relationship("JuegoGenero", back_populates="genero")


# === EDITORES ===
class Editores(Base):
    __tablename__ = 'editores'

    id = Column(Integer, primary_key=True)
    nombre = Column(String, unique=True)

    juegos = relationship("JuegoEditor", back_populates="editor")


# === DESARROLLADORES ===
class Desarrolladores(Base):
    __tablename__ = 'desarrolladores'

    id = Column(Integer, primary_key=True)
    nombre = Column(String, unique=True)

    juegos = relationship("JuegoDesarrollador", back_populates="desarrollador")


class Usuario(Base):
    __tablename__ = 'Usuario'

    id_usuario = Column(Integer, primary_key=True)
    nombre_usuario = Column(String(50))
    contrasena = Column(String(255))
    descripcion = Column(Text)
    foto_perfil = Column(String(255))
    codigo_amigo = Column(String(100))
    correo = Column(String(100), unique=True)
    nombre_cuenta = Column(String(100), unique=True)
    token = Column(String(20), unique=True)
    dinero = Column(Numeric(10, 2), default=0.00)
    estado = Column(String(50))
    
    # Relaciones Amistades Confirmadas
    amistades_enviadas = relationship(
        "Amigos",
        foreign_keys="[Amigos.usuario1_id]",
        back_populates="usuario1",
        cascade="all, delete-orphan"
    )
    amistades_recibidas = relationship(
        "Amigos",
        foreign_keys="[Amigos.usuario2_id]",
        back_populates="usuario2",
        cascade="all, delete-orphan"
    )

    # Relaciones Solicitudes de Amistad con nombre correcto de la clase
    solicitudes_enviadas = relationship(
        "SolicitudAmistad",
        foreign_keys="[SolicitudAmistad.de_usuario_id]",
        back_populates="de_usuario",
        cascade="all, delete-orphan"
    )
    solicitudes_recibidas = relationship(
        "SolicitudAmistad",
        foreign_keys="[SolicitudAmistad.para_usuario_id]",
        back_populates="para_usuario",
        cascade="all, delete-orphan"
    )

    juegos_usuario = relationship(
        "UsuarioJuego",
        back_populates="usuario",
        
        passive_deletes=True  # 
    )
    
    # Otras relaciones
    mensajes_enviados = relationship("Mensaje", foreign_keys="[Mensaje.id_usuario_remitente]", back_populates="remitente")
    grupos = relationship("UsuarioGrupoChat", back_populates="usuario", cascade="all, delete")

class Amigos(Base):
    __tablename__ = 'amigos'

    id = Column(Integer, primary_key=True)
    usuario1_id = Column(Integer, ForeignKey('Usuario.id_usuario', ondelete='CASCADE'), nullable=False)
    usuario2_id = Column(Integer, ForeignKey('Usuario.id_usuario', ondelete='CASCADE'), nullable=False)
    fecha_amistad = Column(DateTime, default=datetime.utcnow)

    usuario1 = relationship("Usuario", foreign_keys=[usuario1_id], back_populates="amistades_enviadas")
    usuario2 = relationship("Usuario", foreign_keys=[usuario2_id], back_populates="amistades_recibidas")


# SOLICITUDES ANTES DE QUE DOS USUARIOS SEAN AMIGOS || SOLICITUDES PENDIENTES, ACEPTADAS O RECHAZAS
# === SOLICITUDES DE AMISTAD ===
class SolicitudAmistad(Base):
    __tablename__ = 'solicitudes_amigos'

    id = Column(Integer, primary_key=True, autoincrement=True)
    de_usuario_id = Column(Integer, ForeignKey('Usuario.id_usuario', ondelete='CASCADE'), nullable=False)
    para_usuario_id = Column(Integer, ForeignKey('Usuario.id_usuario', ondelete='CASCADE'), nullable=False)
    estado = Column(String(50))
    fecha_creacion = Column(DateTime, default=datetime.utcnow)

    de_usuario = relationship("Usuario", foreign_keys=[de_usuario_id], back_populates="solicitudes_enviadas")
    para_usuario = relationship("Usuario", foreign_keys=[para_usuario_id], back_populates="solicitudes_recibidas")

# === GRUPO DE CHAT ===
class GrupoChat(Base):
    __tablename__ = 'GrupoChat'

    id_chat = Column(Integer, primary_key=True)

    mensajes = relationship("Mensaje", back_populates="grupo_chat")
    usuarios_grupo = relationship("UsuarioGrupoChat", back_populates="grupo_chat")
    es_privado = Column(Boolean, default=False)


# === MENSAJE ===
class Mensaje(Base):
    __tablename__ = 'Mensaje'

    id_mensaje = Column(Integer, primary_key=True, autoincrement=True)
    mensaje = Column(Text)
    timestamp = Column(DateTime, default=datetime.utcnow)
    id_usuario_remitente = Column(Integer, ForeignKey('Usuario.id_usuario'))
    id_chat = Column(Integer, ForeignKey('GrupoChat.id_chat'))

    remitente = relationship("Usuario", foreign_keys=[id_usuario_remitente], back_populates="mensajes_enviados")
    grupo_chat = relationship("GrupoChat", back_populates="mensajes")


# === USUARIO - GRUPO CHAT (intermedia) ===
class UsuarioGrupoChat(Base):
    __tablename__ = 'Usuario_GrupoChat'

    id_usuario = Column(Integer, ForeignKey('Usuario.id_usuario', ondelete="CASCADE"), primary_key=True)
    id_chat = Column(Integer, ForeignKey('GrupoChat.id_chat', ondelete="CASCADE"), primary_key=True)

    usuario = relationship("Usuario", back_populates="grupos")
    grupo_chat = relationship("GrupoChat", back_populates="usuarios_grupo")


# === USUARIO - JUEGO (intermedia) ===
class UsuarioJuego(Base):
    __tablename__ = 'Usuario_Juego'

    id_usuario = Column(Integer, ForeignKey('Usuario.id_usuario', ondelete="CASCADE"), primary_key=True)
    id_juego = Column(Integer, ForeignKey('juegos.steam_appid'), primary_key=True)
    fecha_compra = Column(DateTime, default=datetime.utcnow) #FECHA DE COMPRA DE JUEGO

    usuario = relationship(
        "Usuario",
        back_populates="juegos_usuario",
        passive_deletes=True  # <-- Esto es clave
    )
    juego = relationship("Juegos", back_populates="usuarios_juego")