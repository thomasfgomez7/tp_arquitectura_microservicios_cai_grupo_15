"""Genera las capturas de Swagger UI con respuestas de error para docs/capturas/ (Etapa 10).

Requiere los cinco servicios levantados (scripts/start-all.sh) y Playwright:

    python3 -m venv .venv && .venv/bin/pip install playwright && .venv/bin/python -m playwright install chromium
    .venv/bin/python scripts/capture-swagger.py

Para cada caso abre Swagger UI, expande la operación, usa "Try it out" con datos que provocan un error
del catálogo y guarda una captura de la operación con la respuesta real (errorCode y errorMessage).
"""
import json
import urllib.request
from pathlib import Path

from playwright.sync_api import Page, sync_playwright

OUTPUT = Path(__file__).resolve().parent.parent / "docs" / "capturas"

USUARIO_MARIA = "a1b2c3d4-0000-0000-0000-111122223333"
NOTEBOOK = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
TALADRO = "b2c3d4e5-0000-0000-0000-000000000005"

# (archivo, puerto, método, ruta, parámetros de ruta, body)
CASOS = [
    ("01-products-404-PRD-001.png", 5001, "get", "/api/products/{id}", {"id": "99"}, None),
    ("02-products-409-PRD-004.png", 5001, "delete", "/api/products/{id}", {"id": NOTEBOOK}, None),
    ("03-users-403-USR-004.png", 5002, "post", "/api/users/login", {},
     {"email": "juan@email.com", "password": "MiPassword123!"}),
    ("04-users-400-USR-002.png", 5002, "post", "/api/users/register", {},
     {"nombre": "", "apellido": "González", "email": "no-es-un-email", "password": "MiPassword123!"}),
    ("05-orders-422-ORD-005.png", 5003, "post", "/api/orders", {},
     {"usuarioId": USUARIO_MARIA, "items": [{"productoId": TALADRO, "cantidad": 5}]}),
    ("06-cart-422-CRT-003.png", 5004, "post", "/api/cart/{userId}/items", {"userId": USUARIO_MARIA},
     {"productoId": TALADRO, "cantidad": 5}),
    ("07-notifications-404-NTF-001.png", 5005, "post", "/api/notifications/send", {},
     {"usuarioId": "11111111-1111-1111-1111-111111111111", "mensaje": "Su orden fue confirmada.", "tipo": "Email"}),
]


def crear_orden_pendiente_con_la_notebook() -> None:
    """PRD-004 necesita una orden activa con la Notebook en Orders.API."""
    body = json.dumps({"usuarioId": USUARIO_MARIA, "items": [{"productoId": NOTEBOOK, "cantidad": 1}]}).encode()
    request = urllib.request.Request("http://localhost:5003/api/orders", data=body,
                                     headers={"Content-Type": "application/json"}, method="POST")
    urllib.request.urlopen(request).close()


def capturar(page: Page, archivo: str, puerto: int, metodo: str, ruta: str, parametros: dict, body) -> None:
    page.goto(f"http://localhost:{puerto}/swagger/index.html")
    operacion = page.locator(f".opblock-{metodo}:has(.opblock-summary-path[data-path='{ruta}'])").first
    operacion.locator(".opblock-summary").click()
    operacion.locator(".try-out__btn").click()

    for nombre, valor in parametros.items():
        operacion.locator(f"tr[data-param-name='{nombre}'] input").fill(valor)
    if body is not None:
        operacion.locator("textarea.body-param__text").fill(json.dumps(body, indent=2, ensure_ascii=False))

    operacion.locator(".execute").click()
    respuesta = operacion.locator(".live-responses-table .response")
    respuesta.wait_for()

    # Se ocultan el comando curl y la URL del request para que la captura muestre lo importante:
    # el request, el status y el body de error.
    operacion.locator(".curl-command, .request-url").evaluate_all(
        "elements => elements.forEach(e => e.closest('div').style.display = 'none')")
    operacion.screenshot(path=OUTPUT / archivo)
    print(f"  {archivo}")


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    crear_orden_pendiente_con_la_notebook()

    with sync_playwright() as playwright:
        browser = playwright.chromium.launch()
        page = browser.new_page(viewport={"width": 1280, "height": 900}, device_scale_factor=2)
        for caso in CASOS:
            capturar(page, *caso)
        browser.close()


if __name__ == "__main__":
    main()
