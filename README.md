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

1. Abre **Files changed** y lee el código.
2. Verifica la definición de terminado (sección 7).
3. Ve a **Review changes** y elige **Approve**, o **Request changes** con comentarios concretos.

**Al integrar:** lo integra quien abrió el pull request, después de la aprobación, siempre con **Create a merge commit** (no *squash*). Así se conservan los commits de cada integrante.

---

## 7. Definición de terminado (DoD)

Una historia está terminada cuando:

- [ ] cumple todos sus criterios de aceptación;
- [ ] sus pruebas de éxito y de error se ejecutaron, y la evidencia quedó en el issue;
- [ ] las reglas que toca tienen pruebas unitarias y `dotnet test` pasa completo;
- [ ] compila sin errores ni advertencias nuevas;
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
- En el repositorio solo va `.env.example`, con las mismas variables y valores de ejemplo. Se crea cuando montemos el entorno con Docker.
- Si subiste un secreto por error, avísale al Scrum Master: hay que cambiar esa clave, aunque luego se borre del repositorio.
