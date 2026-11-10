"""Prueba integral del sistema (Etapa 11): levanta los cinco servicios y verifica, contra el sistema real,
todo el catálogo de errores, los health checks, la propagación del Correlation ID y los logs en archivo.

    python3 scripts/verify-system.py

Solo usa la biblioteca estándar de Python. Necesita los puertos 5001 a 5005 libres: levanta sus propias
instancias, con los datos semilla intactos, y las detiene al terminar. Para probar los errores 500 apaga
una dependencia a la vez (Orders, Users y Products). Termina con código 1 si algún chequeo falla.
"""
import json
import os
import signal
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PORTS = {"Products": 5001, "Users": 5002, "Orders": 5003, "Cart": 5004, "Notifications": 5005}
DEPENDENCIES = {"Products": ["Orders"], "Users": [], "Orders": ["Users", "Products"],
                "Cart": ["Products"], "Notifications": ["Users"]}
ERROR_FIELDS = ["type", "title", "status", "detail", "instance", "errorCode", "errorMessage", "correlationId"]

MARIA = "a1b2c3d4-0000-0000-0000-111122223333"
NOTEBOOK = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
PELOTA = "b2c3d4e5-0000-0000-0000-000000000004"
TALADRO = "b2c3d4e5-0000-0000-0000-000000000005"
ORDEN_SEMILLA = "f1e2d3c4-0000-0000-0000-aabbccddeeff"
NO_EXISTE = "11111111-1111-1111-1111-111111111111"

RUN_ID = uuid.uuid4().hex[:6]
results: list[tuple[str, bool, str]] = []
processes: dict[str, subprocess.Popen] = {}


# ---------- Infraestructura ----------

def port_in_use(port: int) -> bool:
    with socket.socket() as s:
        return s.connect_ex(("localhost", port)) == 0


def start(name: str) -> None:
    processes[name] = subprocess.Popen(
        ["dotnet", "run", "--project", f"src/{name}.API", "--launch-profile", "http", "--no-build"],
        cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, start_new_session=True)


def stop(name: str) -> None:
    process = processes.pop(name, None)
    if process and process.poll() is None:
        os.killpg(process.pid, signal.SIGTERM)  # el grupo incluye la API que lanza "dotnet run"
        process.wait(timeout=30)
    for _ in range(30):
        if not port_in_use(PORTS[name]):
            return
        time.sleep(0.5)


def wait_until_live(name: str) -> None:
    for _ in range(90):
        if call("GET", name, "/health/live")[0] == 200:
            return
        time.sleep(1)
    sys.exit(f"{name}.API no respondió en el puerto {PORTS[name]}.")


def call(method: str, service: str, path: str, body=None, correlation_id: str | None = None):
    """Devuelve (status, headers, body JSON o None). Un error de conexión devuelve status 0."""
    request = urllib.request.Request(
        f"http://localhost:{PORTS[service]}{path}", method=method,
        data=json.dumps(body).encode() if body is not None else None,
        headers={"Content-Type": "application/json", **({"X-Correlation-Id": correlation_id} if correlation_id else {})})
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            status, headers, raw = response.status, response.headers, response.read()
    except urllib.error.HTTPError as error:
        status, headers, raw = error.code, error.headers, error.read()
    except (urllib.error.URLError, ConnectionError, TimeoutError):
        return 0, {}, None
    try:
        return status, headers, json.loads(raw) if raw else None
    except json.JSONDecodeError:
        return status, headers, None


def check(name: str, ok: bool, detail: str = "") -> None:
    results.append((name, ok, detail))
    print(f"  {'✅' if ok else '❌'} {name}" + (f"  ({detail})" if detail and not ok else ""))


# ---------- Chequeos ----------

def expect_error(code: str, status: int, method: str, service: str, path: str, body=None) -> None:
    """Provoca un error del catálogo y verifica el contrato completo (sección 3.1 + correlationId)."""
    correlation_id = f"verif-{RUN_ID}-{code}"
    actual, headers, data = call(method, service, path, body, correlation_id)
    problems = []
    if actual != status:
        problems.append(f"status {actual}, se esperaba {status}")
    if not isinstance(data, dict):
        problems.append("el body no es JSON")
    else:
        if data.get("errorCode") != code:
            problems.append(f"errorCode {data.get('errorCode')}")
        missing = [field for field in ERROR_FIELDS if not data.get(field)]
        if missing:
            problems.append(f"faltan {', '.join(missing)}")
        if data.get("instance") != path.split("?")[0]:
            problems.append(f"instance {data.get('instance')}")
        if data.get("correlationId") != correlation_id:
            problems.append("el correlationId del body no es el enviado")
    if headers and "application/problem+json" not in headers.get("Content-Type", ""):
        problems.append(f"content-type {headers.get('Content-Type')}")
    if headers and headers.get("X-Correlation-Id") != correlation_id:
        problems.append("el header X-Correlation-Id no es el enviado")
    message = data.get("errorMessage", "") if isinstance(data, dict) else ""
    check(f"{code} {status}  {method} {path}" + (f"  → {message}" if message and not problems else ""),
          not problems, "; ".join(problems))


def expect_health(service: str, endpoint: str, expected: str, unhealthy_checks: tuple = ()) -> None:
    status, _, data = call("GET", service, endpoint)
    expected_status = 503 if expected == "Unhealthy" else 200
    ok = (status == expected_status and isinstance(data, dict) and data.get("status") == expected
          and data.get("servicio") == f"{service}.API")
    if ok and unhealthy_checks:
        states = {c["name"]: c["status"] for c in data.get("checks", [])}
        ok = all(states.get(f"{dep}.API") == "Degraded" for dep in unhealthy_checks)
    got = f"status {status}, {data.get('status') if isinstance(data, dict) else 'sin JSON'}"
    check(f"{service}.API {endpoint} → {expected}", ok, got)


def log_entries(service: str) -> list[dict]:
    """Líneas del archivo de log de hoy (Serilog JSON) del servicio."""
    path = ROOT / f"src/{service}.API/logs/{service.lower()}-{datetime.now():%Y%m%d}.json"
    if not path.exists():
        return []
    return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]


def expect_propagation(code: str, services: list[str]) -> None:
    """El Correlation ID del request aparece en el archivo de log de cada servicio que participó."""
    correlation_id = f"verif-{RUN_ID}-{code}"
    found = [s for s in services
             if any(e.get("Properties", {}).get("CorrelationId") == correlation_id for e in log_entries(s))]
    check(f"Correlation ID de {code} en los logs de {' y '.join(services)}", found == services,
          f"solo en {', '.join(found) or 'ninguno'}")


def expect_log_fields(service: str) -> None:
    """Sección 5.3: Timestamp, Nivel, Servicio, Endpoint, Correlation ID y errorCode cuando aplica."""
    entries = [e for e in log_entries(service) if RUN_ID in str(e.get("Properties", {}).get("CorrelationId"))]
    end_lines = [e for e in entries if e.get("MessageTemplate", "").startswith("Fin del request")]
    problems = []
    if not end_lines:
        problems.append("no hay logs de esta corrida en el archivo")
    for entry in entries:
        props = entry.get("Properties", {})
        if not (entry.get("Timestamp") and entry.get("Level") and props.get("Servicio") == f"{service}.API"
                and props.get("Endpoint") and props.get("CorrelationId")):
            problems.append(f"línea incompleta: {entry.get('RenderedMessage', '')[:60]}")
            break
    errors = [e for e in end_lines if e.get("Properties", {}).get("StatusCode", 0) >= 400]
    if any(not e["Properties"].get("ErrorCode") for e in errors):
        problems.append("hay respuestas de error sin ErrorCode en el log de fin")
    if not any(e.get("Level") == "Warning" for e in entries):
        problems.append("no hay Warning de errores de negocio")
    check(f"{service}.API: logs en archivo con Timestamp, Nivel, Servicio, Endpoint, CorrelationId y ErrorCode",
          not problems, "; ".join(problems))


# ---------- Guion de la prueba ----------

def main() -> None:
    os.chdir(ROOT)
    busy = [f"{n} ({p})" for n, p in PORTS.items() if port_in_use(p)]
    if busy:
        sys.exit(f"Puertos ocupados: {', '.join(busy)}. Detené los servicios y volvé a intentar.")

    print("Compilando la solución...")
    subprocess.run(["dotnet", "build", "ECommerce.slnx", "-v", "q", "--nologo"], check=True,
                   stdout=subprocess.DEVNULL)
    try:
        for name in PORTS:
            start(name)
        for name in PORTS:
            wait_until_live(name)
        run_checks()
    finally:
        for name in list(processes):
            stop(name)

    failed = [r for r in results if not r[1]]
    print(f"\n{len(results) - len(failed)} de {len(results)} chequeos correctos.")
    sys.exit(1 if failed else 0)


def run_checks() -> None:
    nuevo = f"verif-{RUN_ID}@email.com"

    print("\n1. Health checks con los cinco servicios levantados")
    for service in PORTS:
        for endpoint in ("/health", "/health/ready", "/health/live"):
            expect_health(service, endpoint, "Healthy")

    print("\n2. Catálogo de errores (4xx), con los cinco servicios levantados")
    expect_error("PRD-001", 404, "GET", "Products", "/api/products/99")
    expect_error("PRD-002", 400, "POST", "Products", "/api/products", {})
    expect_error("PRD-003", 409, "POST", "Products", "/api/products",
                 {"nombre": "notebook dell xps 15", "precio": 1500, "stock": 1, "categoria": "Electrónica"})
    expect_error("PRD-004", 409, "DELETE", "Products", f"/api/products/{NOTEBOOK}")

    expect_error("USR-001", 409, "POST", "Users", "/api/users/register",
                 {"nombre": "María", "apellido": "González", "email": "maria@email.com", "password": "x"})
    expect_error("USR-002", 400, "POST", "Users", "/api/users/register", {})
    call("POST", "Users", "/api/users/register",
         {"nombre": "Prueba", "apellido": "Integral", "email": nuevo, "password": "Correcta123!"})
    expect_error("USR-003", 401, "POST", "Users", "/api/users/login", {"email": nuevo, "password": "incorrecta"})
    expect_error("USR-004", 403, "POST", "Users", "/api/users/login",
                 {"email": "juan@email.com", "password": "MiPassword123!"})
    expect_error("USR-005", 403, "POST", "Users", "/api/users/login",
                 {"email": "carlos@email.com", "password": "MiPassword123!"})
    expect_error("USR-007", 404, "GET", "Users", "/api/users/99")

    expect_error("ORD-001", 404, "GET", "Orders", "/api/orders/99")
    expect_error("ORD-002", 400, "POST", "Orders", "/api/orders", {"usuarioId": MARIA, "items": []})
    expect_error("ORD-003", 404, "POST", "Orders", "/api/orders",
                 {"usuarioId": NO_EXISTE, "items": [{"productoId": NOTEBOOK, "cantidad": 1}]})
    expect_error("ORD-004", 404, "POST", "Orders", "/api/orders",
                 {"usuarioId": MARIA, "items": [{"productoId": NO_EXISTE, "cantidad": 1}]})
    expect_error("ORD-005", 422, "POST", "Orders", "/api/orders",
                 {"usuarioId": MARIA, "items": [{"productoId": TALADRO, "cantidad": 5}]})
    expect_error("ORD-006", 409, "PUT", "Orders", f"/api/orders/{ORDEN_SEMILLA}/status", {"estado": "Entregada"})

    expect_error("CRT-001", 404, "GET", "Cart", f"/api/cart/{NO_EXISTE}")
    expect_error("CRT-002", 404, "POST", "Cart", f"/api/cart/{MARIA}/items", {"productoId": NO_EXISTE, "cantidad": 1})
    expect_error("CRT-003", 422, "POST", "Cart", f"/api/cart/{MARIA}/items", {"productoId": TALADRO, "cantidad": 5})
    expect_error("CRT-004", 400, "POST", "Cart", f"/api/cart/{MARIA}/items", {"productoId": NOTEBOOK, "cantidad": 0})

    expect_error("NTF-001", 404, "POST", "Notifications", "/api/notifications/send",
                 {"usuarioId": NO_EXISTE, "mensaje": "Hola", "tipo": "Email"})
    expect_error("NTF-002", 400, "POST", "Notifications", "/api/notifications/send",
                 {"usuarioId": MARIA, "mensaje": "Hola", "tipo": "Fax"})
    expect_error("NTF-003", 404, "GET", "Notifications", f"/api/notifications/{NO_EXISTE}")

    print("\n3. Correlation ID propagado entre servicios (archivos de log)")
    time.sleep(1)  # Serilog escribe el archivo de forma asíncrona
    expect_propagation("PRD-004", ["Products", "Orders"])
    expect_propagation("ORD-005", ["Orders", "Users", "Products"])
    expect_propagation("CRT-003", ["Cart", "Products"])
    expect_propagation("NTF-001", ["Notifications", "Users"])

    print("\n4. Errores 500 y health checks con una dependencia apagada")
    stop("Orders")
    expect_health("Products", "/health/ready", "Degraded", ("Orders",))
    expect_health("Products", "/health/live", "Healthy")
    expect_error("PRD-005", 500, "DELETE", "Products", f"/api/products/{PELOTA}")
    check("PRD-005 no borró el producto (D-39)", call("GET", "Products", f"/api/products/{PELOTA}")[0] == 200)
    start("Orders")
    wait_until_live("Orders")

    stop("Users")
    expect_health("Orders", "/health/ready", "Degraded", ("Users",))
    expect_health("Notifications", "/health/ready", "Degraded", ("Users",))
    expect_error("ORD-007", 500, "POST", "Orders", "/api/orders",
                 {"usuarioId": MARIA, "items": [{"productoId": NOTEBOOK, "cantidad": 1}]})
    expect_error("NTF-004", 500, "POST", "Notifications", "/api/notifications/send",
                 {"usuarioId": MARIA, "mensaje": "Hola", "tipo": "Email"})
    check("Orders sigue respondiendo consultas sin Users",
          call("GET", "Orders", f"/api/orders/{ORDEN_SEMILLA}")[0] == 200)
    start("Users")
    wait_until_live("Users")

    stop("Products")
    expect_health("Cart", "/health/ready", "Degraded", ("Products",))
    expect_error("CRT-005", 500, "POST", "Cart", f"/api/cart/{MARIA}/items", {"productoId": NOTEBOOK, "cantidad": 1})
    check("Cart sigue mostrando el carrito sin Products", call("GET", "Cart", f"/api/cart/{MARIA}")[0] == 200)
    start("Products")
    wait_until_live("Products")
    print("  ➖ USR-006 500  Users.API no depende de otro servicio: no se puede provocar desde afuera."
          " Lo cubre UnexpectedErrorTests.")

    print("\n5. Logs en archivo (sección 5.3)")
    time.sleep(1)
    for service in PORTS:
        expect_log_fields(service)


if __name__ == "__main__":
    main()
