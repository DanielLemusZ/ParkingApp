# ParkingApp

MVP del sistema de parqueadero con reconocimiento de placas (LPR) de la Universidad Autónoma de Manizales. El vehículo entra y sale por su placa, se cobra una vez por día según la franja (diurna o nocturna) del primer ingreso, el pago se registra en la caja y el administrador gestiona tarifas y cupos.

Proyecto del curso Ingeniería de Software II. Grupo **ParkingApp**.

## Equipo

| Integrante | Usuario de GitHub | Rol en Scrum |
|---|---|---|
| Daniel Lemus | [DanielLemusZ](https://github.com/DanielLemusZ) | Scrum Master y desarrollo |
| Brayan Escobar | [BDEscobarR](https://github.com/BDEscobarR) | Product Owner y desarrollo |
| Jacobo Patiño | [jacolopezp6](https://github.com/jacolopezp6) | Desarrollo |
| Jeronimo Gomez | [JeronimoGomez444](https://github.com/JeronimoGomez444) | Desarrollo |

## Stack

- **Lenguaje y plataforma:** C# con .NET 10 (LTS).
- **API:** ASP.NET Core con Controllers, documentada con Swagger.
- **Pantallas:** Blazor Web App (modo Interactive Server) con MudBlazor.
- **Base de datos:** PostgreSQL en Docker, con acceso mediante Entity Framework Core 10 y Npgsql.
- **Autenticación:** ASP.NET Core Identity, con los roles `Operador` y `Administrador`.
- **Pruebas:** xUnit v3, solo pruebas unitarias (mínimo 20).
- **Arquitectura:** monolito modular con arquitectura hexagonal. Las reglas de negocio viven en un dominio que no depende de la base de datos, la web ni el hardware.

## Estructura de la solución

| Proyecto | Qué contiene | Puede usar |
|---|---|---|
| `src/ParkingApp.Dominio` | Reglas del negocio | Nada: no depende de la base de datos, la web ni el hardware |
| `src/ParkingApp.Aplicacion` | Casos de uso y las interfaces que necesitan | Dominio |
| `src/ParkingApp.Infraestructura` | Base de datos y servicios externos | Dominio y Aplicación |
| `src/ParkingApp.Web` | API (Controllers y Swagger) y pantallas (Blazor con MudBlazor) | Aplicación e Infraestructura |
| `tests/ParkingApp.Pruebas` | Pruebas unitarias | Dominio y Aplicación |

- **Nombres en español**, sin tildes (`Configuracion`). Las palabras del framework siguen en inglés (`Controller`, `DbContext`).
- **Dentro de cada proyecto, el código va por módulo:** `Accesos`, `Cobros`, `Pagos`, `Configuracion`, `Reportes` e `Identidad`.
- **Documentación XML:** en el dominio y la aplicación, todo lo público lleva comentario. Si falta, la compilación muestra una advertencia.

## Cómo ejecutar

**Requisitos:** el SDK de .NET 10 o uno posterior. Para correr las pruebas desde un editor: Visual Studio 2026, VS Code con C# Dev Kit o Rider.

```powershell
dotnet tool restore                        # instala dotnet-ef en la versión del repositorio
dotnet build                               # compila; debe terminar con 0 advertencias
dotnet test                                # corre las pruebas unitarias
dotnet test --output Detailed              # lo mismo, con el nombre de cada prueba
dotnet format                              # corrige el formato según .editorconfig
dotnet run --project src/ParkingApp.Web    # levanta la aplicación
```

**La aplicación necesita la base de datos** (ver abajo). Si no puede conectarse, no arranca y explica por qué. Si hay migraciones sin aplicar, avisa con el comando para aplicarlas.

**Dirección de la aplicación:** la terminal la muestra en la línea `Now listening on`. Swagger está en esa misma dirección, terminada en `/swagger`.

- Con `dotnet run`, o con el perfil **http** de Visual Studio, es <http://localhost:5288>. El perfil **https** también abre <https://localhost:7083>.
- En Visual Studio, elige el perfil **http** o **https** junto al botón de ejecutar. Si Visual Studio agrega un perfil *IIS Express*, no lo subas: usa un puerto distinto en cada equipo.

### Base de datos (PostgreSQL 18.7 con Docker)

**Requisito:** Docker Desktop abierto (en Windows, con WSL 2).

1. **La primera vez:** copia `.env.example` como `.env` y cambia las contraseñas: la de la base de datos y las de los usuarios iniciales (ver [Usuarios y roles](#usuarios-y-roles)). Si en tu equipo ya usas el puerto 5432, cambia también `POSTGRES_PORT`.

   ```powershell
   Copy-Item .env.example .env
   ```

2. **Levantar, revisar y conectarse:**

   ```powershell
   docker compose up -d       # levanta PostgreSQL en segundo plano
   docker compose ps          # debe decir "healthy"
   docker compose exec postgres psql -U parkingapp -d parkingapp -c "select version();"
   ```

   El último comando usa el usuario y la base de `.env.example`. Si los cambiaste, usa los tuyos.

3. **Crear o actualizar las tablas** (la primera vez y cada vez que lleguen migraciones nuevas):

   ```powershell
   dotnet ef database update --project src/ParkingApp.Infraestructura --startup-project src/ParkingApp.Web
   ```

4. **Detener:** `docker compose stop` o `docker compose down`. Los dos conservan los datos.
5. **Recrear desde cero:** `docker compose down -v` borra el volumen **y todos los datos**. Después, `docker compose up -d` y el paso 3.

**Si falta una variable en `.env`**, Compose y la aplicación no arrancan y dicen cuál falta. La aplicación lee el mismo `.env`. `POSTGRES_HOST` es opcional: si no está, usa `localhost`.

**Migraciones** (código en `src/ParkingApp.Infraestructura/Persistencia/Migraciones`):

```powershell
# Crear una migración después de agregar o cambiar una entidad:
dotnet ef migrations add <Nombre> --project src/ParkingApp.Infraestructura --startup-project src/ParkingApp.Web --output-dir Persistencia/Migraciones
# Generar el script SQL completo, que se puede ejecutar varias veces sin duplicar nada (no lo subas al repositorio):
dotnet ef migrations script --idempotent --project src/ParkingApp.Infraestructura --startup-project src/ParkingApp.Web --output script.sql
```

**Sin Docker:** instala PostgreSQL 18.7 nativo, con el mismo puerto, usuario, contraseña y base de tu `.env`.

### Usuarios y roles

Todas las pantallas y la API piden iniciar sesión, en `/cuenta/iniciar-sesion`. No hay registro público: los usuarios iniciales los crea la aplicación al arrancar.

**Usuarios iniciales.** Al arrancar, la aplicación crea un usuario por rol con estas variables de `.env`, si todavía no existen:

| Variable | Para qué |
|---|---|
| `ADMIN_CORREO` y `ADMIN_CONTRASENA` | Usuario con el rol `Administrador` |
| `OPERADOR_CORREO` y `OPERADOR_CONTRASENA` | Usuario con el rol `Operador` |

- La contraseña necesita al menos 8 caracteres, con una mayúscula, una minúscula, un número y un símbolo.
- La terminal dice qué pasó con cada usuario: *creado*, *ya existía* o *no se creó* y por qué.
- Si el usuario ya existe, el arranque **no lo cambia**. Cambiar la contraseña en `.env` después no cambia la del usuario: para eso está *Cambiar contraseña*, en el menú de la cuenta.
- Si olvidaste la contraseña de un usuario inicial, bórralo y reinicia la aplicación, que lo vuelve a crear con la de `.env`:

  ```powershell
  docker compose exec postgres psql -U parkingapp -d parkingapp -c "delete from asp_net_users where email = 'admin@parkingapp.local';"
  ```

**Qué puede usar cada rol:**

| Rol | Pantallas | API |
|---|---|---|
| `Operador` | Carril y caja | `api/carril` |
| `Administrador` | Todo: carril, caja, tarifas, consultas y configuración | `api/carril` y `api/tarifas` |

- El menú muestra solo las opciones del rol. Si un operador escribe la dirección de otra pantalla, ve *Acceso denegado*.
- Sin sesión, la API responde **401**; con un rol que no alcanza, **403**.
- **Bloqueo:** después de 5 contraseñas incorrectas seguidas, la cuenta queda bloqueada 5 minutos y se muestra *Cuenta bloqueada temporalmente*.

**Probar la API desde Swagger:** inicia sesión en la aplicación y abre `/swagger` en el mismo navegador. Swagger envía la cookie de la sesión, así que cada endpoint responde según tu rol.

---

## 1. Ramas

| Rama | Para qué | Sale de | Se integra en |
|---|---|---|---|
| `main` | Solo versiones liberadas, cada una con su tag | — | — |
| `develop` | Integración del trabajo terminado | `main` | `release/x.y.z` |
| `feature/<n>-<descripcion>` | Una historia de usuario | `develop` | `develop`, por pull request |
| `release/x.y.z` | Preparar una versión | `develop` | `main` y `develop` |
| `hotfix/x.y.z` | Corrección urgente sobre una versión liberada | `main` | `main` y `develop` |

- `<n>` es el **número del issue** de la historia, y `<descripcion>` va en minúsculas y con guiones. Ejemplo: `feature/12-ingreso-por-placa`.
- **Nadie hace commits directos a `main` ni a `develop`.** Todo entra por pull request, con la aprobación de otro integrante. GitHub lo bloquea.
- No se usa `git push --force` en ramas compartidas.

---

## 2. Estados del tablero

| Estado | Significa |
|---|---|
| **Por hacer** | Está en el sprint y nadie la ha empezado |
| **En desarrollo** | Alguien la tiene asignada y su rama existe |
| **En revisión** | Su pull request está abierto, esperando aprobación |
| **Terminado** | Cumple la definición de terminado (sección 7) |

---

## 3. Estimación

Las historias se estiman en **puntos de historia**. El responsable de cada historia propone su estimación con la escala de abajo, y los dos integrantes con más experiencia en el stack (Daniel y Brayan) la revisan y la ajustan si hace falta, en revisión cruzada: ninguno aprueba solo su propia estimación. Las horas son solo una guía para calibrar: el esfuerzo total de la historia, incluidas las pruebas y la revisión.

| Puntos | Tamaño | Horas aproximadas |
|---|---|---|
| 1 | Un cambio puntual y conocido: un texto o una validación simple | Hasta 2 |
| 2 | Una pieza pequeña sin dudas: una regla con sus pruebas | 2 a 4 |
| 3 | Una regla o pantalla sencilla, con algo por aprender | 4 a 6 |
| 5 | Toca varias capas (dominio, datos y pantalla), sin grandes dudas | 6 a 10 |
| 8 | Varias capas con reglas complejas o algo nuevo para el equipo | 10 a 16 |
| 13 | Grande o con mucha incertidumbre | 16 a 24 |

Si una historia vale más de 13, se divide.

---

## 4. Trabajar una historia, paso a paso

1. **Asígnate el issue** y muévelo a **En desarrollo**.
2. **Crea tu rama** desde `develop` actualizado:

   ```bash
   git checkout develop
   git pull
   git checkout -b feature/12-ingreso-por-placa
   ```

3. **Trabaja y haz commits pequeños** (formato en la sección 5):

   ```bash
   git add .
   git commit -m "feat(accesos): registrar ingreso por placa (#12)"
   ```

4. **Antes de abrir el pull request**, trae lo último de `develop` y verifica que todo pase:

   ```bash
   git checkout develop
   git pull
   git checkout feature/12-ingreso-por-placa
   git merge develop
   dotnet test
   ```

   Si hay conflictos, resuélvelos, haz commit y vuelve a correr las pruebas.

5. **Sube tu rama:**

   ```bash
   git push -u origin feature/12-ingreso-por-placa
   ```

6. **Abre el pull request** en GitHub, hacia `develop` (sección 6), y mueve el issue a **En revisión**.
7. **Cuando lo aprueben**, intégralo con **Create a merge commit** y luego **Delete branch**. El issue se cierra solo y pasa a **Terminado**.
8. **Deja la evidencia** en el issue.

---

## 5. Commits

**Lo obligatorio:** que el mensaje diga qué cambió y, si pertenece a una historia, que incluya el número del issue (`#<n>`), para que GitHub lo enlace.

**Lo recomendado:** el formato de **Conventional Commits**:

```
<tipo>(<módulo>): <descripción> (#<n>)
```

| Tipo | Cuándo |
|---|---|
| `feat` | Funcionalidad nueva |
| `fix` | Corrección de un error |
| `test` | Agregar o cambiar pruebas |
| `refactor` | Cambio interno que no altera el comportamiento |
| `docs` | Documentación (README, comentarios, manuales) |
| `style` | Formato, sin cambios de lógica |
| `build` | Dependencias, proyecto o Docker |
| `ci` | Integración continua |
| `chore` | Tareas de mantenimiento que no encajan arriba |

- **Módulos:** `accesos`, `cobros`, `pagos`, `configuracion`, `reportes`, `identidad`, `web`, `infra`, `docs`.
- **Descripción:** con tus propias palabras, siempre que se entienda qué se hizo. Mejor si no pasa de unos 72 caracteres, porque GitHub corta lo que sigue.
- **`(#<n>)`:** el número del issue de la historia. GitHub enlaza el commit con el issue.
- **Cambios generales que no pertenecen a una historia** pueden omitir el módulo y el `(#<n>)`: `docs: Se agrega README.md con guia de trabajo del equipo`.
- **Un commit es un cambio lógico.** Mejor varios commits pequeños que uno gigante.
- **Al revisar**, el estilo del mensaje no es motivo para rechazar un pull request. Sí lo es que no se entienda qué cambió o que falte el número del issue.

Ejemplos válidos:

```
feat(accesos): registrar ingreso por placa (#12)
fix(pagos): Se rechaza la referencia de pago duplicada (#18)
test(cobros): cubrir tarifa nocturna a las 05:50 (#15)
docs: Se agrega escala de estimación en el README.md (#1)
```

---

## 6. Pull requests y revisión

**Al abrirlo:**

- **Base:** `develop`. **Compare:** tu rama.
- **Título:** el mismo formato de los commits, por ejemplo `feat(accesos): registrar ingreso por placa (#12)`.
- **Revisor:** en **Reviewers**, asigna a otro integrante.
- **Descripción:** copia y llena esta plantilla.

  ```markdown
  Closes #12

  ## Qué cambia
  -

  ## Cómo se probó
  - [ ] `dotnet test` pasa completo
  - Pruebas de éxito ejecutadas:
  - Pruebas de error ejecutadas:

  ## Capturas
  Solo si cambia una pantalla.
  ```

  `Closes #12` hace que el issue se cierre solo al integrar el pull request.

**Al revisar:**

1. Revisa que la verificación automática **Compilar, probar y revisar formato** esté en verde. Si está en rojo, en **Details** se ve qué paso falló.
2. Abre **Files changed** y lee el código.
3. Verifica la definición de terminado (sección 7).
4. Ve a **Review changes** y elige **Approve**, o **Request changes** con comentarios concretos.

**Al integrar:** lo integra quien abrió el pull request, después de la aprobación, siempre con **Create a merge commit** (no *squash*). Así se conservan los commits de cada integrante.

---

## 7. Definición de terminado (DoD)

Una historia está terminada cuando:

- [ ] cumple todos sus criterios de aceptación;
- [ ] sus pruebas de éxito y de error se ejecutaron, y la evidencia quedó en el issue;
- [ ] las reglas que toca tienen pruebas unitarias y `dotnet test` pasa completo;
- [ ] compila sin errores ni advertencias nuevas;
- [ ] la verificación automática del pull request está en verde;
- [ ] otro integrante revisó y aprobó el pull request;
- [ ] está integrada en `develop` y el issue quedó en **Terminado**;
- [ ] no hay secretos en el código (las credenciales van solo en `.env`);
- [ ] si cambió algo documentado (README, Swagger o manuales), la documentación quedó actualizada.

---

## 8. Versiones, release y hotfix

Usamos **versionado semántico**: `MAYOR.MENOR.PARCHE`. La entrega del MVP es la versión `v1.0.0`.

**Release (lo hace el Scrum Master el día del código congelado):**

1. Crea la rama de release desde `develop`:

   ```bash
   git checkout develop
   git pull
   git checkout -b release/1.0.0
   git push -u origin release/1.0.0
   ```

2. En `release/1.0.0` solo entran correcciones menores, por pull request.
3. Abre un pull request de `release/1.0.0` hacia `main` y, cuando lo aprueben, intégralo.
4. Crea el tag desde GitHub: **Releases → Draft a new release**. Escribe el tag `v1.0.0`, elige `main` como destino y publica.
5. Abre un pull request de `main` hacia `develop`, para que `develop` reciba las correcciones del release.

**Hotfix** (un error en una versión ya liberada):

1. Crea la rama `hotfix/1.0.1` desde `main`.
2. Corrige el error.
3. Abre un pull request hacia `main`, intégralo y publica el tag `v1.0.1`.
4. Lleva la corrección a `develop` con otro pull request.

---

## 9. Configuración y secretos

- Las credenciales y cadenas de conexión van en un archivo `.env`, que **nunca se sube** al repositorio.
- En el repositorio solo va `.env.example`, con las mismas variables, valores de ejemplo y una línea que explica cada una. Si agregas una variable a `.env`, agrégala también a `.env.example`.
- Si subiste un secreto por error, avísale al Scrum Master: hay que cambiar esa clave, aunque luego se borre del repositorio.
