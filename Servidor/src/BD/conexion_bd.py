from sqlalchemy import create_engine,text
from sqlalchemy.exc import SQLAlchemyError
from sqlalchemy.orm import sessionmaker
from contextlib import contextmanager
import json

# Configuración de la base de datos por defecto
USUARIO = "postgres"
CONTRASENA = "1234"
HOST = "localhost"
PUERTO = "5432"
DB = "BD_TFG"

# Cargar la configuración desde un archivo JSON en caso de que exista
CONFIG_FILE = 'config.json'
try:
    with open(CONFIG_FILE, 'r') as file:
        json_data = json.load(file)
        USUARIO = json_data.get("USUARIO", USUARIO)
        CONTRASENA = json_data.get("CONTRASENA", CONTRASENA)
        HOST = json_data.get("HOST", HOST)
        PUERTO = json_data.get("PUERTO", PUERTO)
        DB = json_data.get("DB", DB)
except FileNotFoundError:
    print(f"[Error] El archivo de configuración '{CONFIG_FILE}' no se encontró. Usando valores por defecto.")

DATABASE_URL = f"postgresql+psycopg2://{USUARIO}:{CONTRASENA}@{HOST}:{PUERTO}/{DB}"

engine = create_engine(
    DATABASE_URL + "?client_encoding=utf8",
    connect_args={"options": "-c client_encoding=utf8"}
)

Session = sessionmaker(bind=engine)

# Esto del contextmanager hace que la sesion se cierre de forma automatica o que si hay un error vuelva atras
@contextmanager
def get_session():
    db = Session()
    try:
        yield db
        db.commit()
    except Exception as e:
        db.rollback()
        raise e
    finally:
        db.close()
        
def is_db_online():
        try:
            with get_session() as db:
                db.execute(text("SELECT 1"))
                return True
        except Exception as e:
            print(f"[Error] No se pudo conectar a la base de datos: {e}")
            return False
        except SQLAlchemyError as e:
            print(f"[Error] Error general de SQLAlchemy: {e}")
            return False