
# 🎮 OSIRIS – Gestor de videojuegos multiplataforma

**OSIRIS** es una aplicación desarrollada como parte del **Proyecto Final de Grado en Desarrollo de Aplicaciones Multiplataforma**, cuyo objetivo principal es ofrecer una alternativa ligera y centralizada a plataformas como Steam, Epic Games, Ubisoft Connect, etc., permitiendo al usuario gestionar su biblioteca de videojuegos desde una única aplicación.

## 🧠 Descripción del proyecto

El proyecto consiste en una arquitectura **cliente-servidor** compuesta por:

- **Servidor** en Python con Flask y PostgreSQL: expone una API RESTful, modularizada mediante blueprints, que gestiona usuarios, juegos, compras, biblioteca, chat, y más.
- **Cliente** en C# usando WPF (Windows Presentation Foundation): aplicación de escritorio con interfaz intuitiva, capaz de consumir la API y permitir funcionalidades offline.
- **Base de datos** relacional en PostgreSQL, modelada con SQLAlchemy.

## 🚀 Funcionalidades principales

- 🛒 **Tienda virtual**: catálogo dinámico, filtros, simulación de compra.
- 🎮 **Biblioteca**: permite agregar, lanzar y gestionar juegos desde la app.
- 👥 **Sistema de usuarios**: registro, login, perfiles personalizables.
- 🧑‍🤝‍🧑 **Amigos y chat en tiempo real** con Socket.IO.
- ☁️ **Modo offline**: acceso local a la biblioteca sin conexión.
- 📈 **Vista web de administración** para logs y monitoreo en tiempo real.

## 📂 Estructura del repositorio

```
/Cliente/       # Aplicación en C# WPF (frontend)
/Servidor/      # API RESTful con Flask (backend)
/base_datos/    # Scripts SQL y modelo
/diagramas/     # Diagramas UML, ER, casos de uso, etc.
README.md
.gitignore
LICENSE
```

## 🛠️ Tecnologías utilizadas

### 🔧 Backend
- Python 3.11
- Flask + Blueprints
- SQLAlchemy
- PostgreSQL
- Socket.IO
- HTML/CSS + JS para vista web admin

### 💻 Cliente
- C# con WPF y XAML
- Visual Studio 2022
- Newtonsoft.Json
- HttpClient

### 📊 Herramientas externas
- GitHub
- draw.io / StarUML / dbDiagram.io
- Postman
- Adobe Photoshop

## 📦 Instalación y ejecución

### 📍 Requisitos

- Python 3.11
- PostgreSQL
- Visual Studio con soporte para C# WPF

### 🔌 Backend

```bash
cd Servidor
pip install -r requirements.txt
python main.py
```

Editar `config.json` para establecer la conexión con tu base de datos PostgreSQL.

### 🖥️ Cliente

1. Abre la carpeta `Cliente` en Visual Studio.
2. Establece la IP y puerto del servidor en `config.json`.
3. Ejecuta la aplicación.

## 📸 Capturas y diagramas

Se incluyen en la carpeta `/diagramas` los siguientes:

- Diagrama de arquitectura
- Diagrama de clases
- Casos de uso
- Entidad-Relación
- Diagrama de endpoints de la API
- Diagramas de pantallas

## 🧩 Reparto de tareas

Aunque todos los miembros colaboraron en múltiples fases del desarrollo, se establecieron responsabilidades principales basadas en las fortalezas de cada integrante:

- 👤**Jesús Gómez Villaboa**👤  
    ***Especialista en interfaces gráficas, asumió el liderazgo en el desarrollo del cliente WPF.***

    - Diseñó la tienda principal, incluyendo secciones destacadas, barra de búsqueda y fichas de juegos.

    - Implementó la biblioteca de juegos con soporte para datos reales.

    - Desarrolló el sistema de registro/login y la lógica de tokens de sesión automática.

    - Realizó ajustes y mejoras en el backend según las necesidades del cliente.

- 👤**Ignacio García García**👤  
    ***Encargado de varios módulos clave del backend y funciones del cliente.***

    - Desarrolló los endpoints relacionados con juegos, biblioteca de usuario y tienda destacada.

    - Implementó el sistema de logs del servidor.

    - Desarrolló junto a Rubén la vista web administrativa.

    - En el cliente, se encargó de las páginas de recarga de saldo, lista de amigos y chat.

- 👤**Rubén De Las Heras Silveira**👤  
  ***Desarrollador principal de la gestión de usuarios y la lógica del sistema social.***  

    - Implementó los módulos clave de la API relacionados con usuarios, perfiles, login, lista de amigos y chats.

    - Desarrolló junto a Ignacio la vista web administrativa del servidor.

    - Participó en la documentación técnica, el diseño modular del backend y tareas de integración y depuración generales.

    - En el cliente, diseñó la página de perfil de usuario y ventanas emergentes personalizadas.  

> Todos los integrantes participaron en la fase de análisis, diseño de diagramas, pruebas, depuración y revisión cruzada de funcionalidades.

## 👨‍💻 Autores

- [**Jesús Gómez Villaboa**](https://github.com/LightnigFast) · [LinkedIn](https://linkedin.com/in/jesús-gómez-274111351/)  
  _Cliente WPF: diseño de interfaz gráfica, tienda, biblioteca, login y mejoras en integración con backend._

- [**Ignacio García García**](https://github.com/Igg2000) · [LinkedIn](https://linkedin.com/in/ignacio-garcía-garcía-484b9a34b)  
  _Backend (Flask): juegos, biblioteca, tienda destacada, logs. Cliente: saldo, amigos, chat. Vista web admin._

- [**Rubén De Las Heras Silveira**](https://github.com/rubenhs9) · [LinkedIn](https://linkedin.com/in/rubenhs9/)  
  _Backend (Flask): usuarios, perfiles, login, sistema social. Cliente: perfil de usuario, ventanas emergentes. Vista web admin y documentación._


