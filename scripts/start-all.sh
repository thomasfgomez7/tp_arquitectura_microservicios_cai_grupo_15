#!/usr/bin/env bash
# Levanta los cinco microservicios juntos (macOS y Linux).
#
#   ./scripts/start-all.sh
#
# Compila la solución una sola vez, arranca cada API con su perfil "http" y muestra sus logs en esta
# terminal, cada línea con el nombre del servicio. Ctrl+C detiene los cinco.
set -euo pipefail

cd "$(dirname "$0")/.."

# Nombre y puerto de cada servicio (los de launchSettings.json).
SERVICES=(
  "Products:5001"
  "Users:5002"
  "Orders:5003"
  "Cart:5004"
  "Notifications:5005"
)

for entry in "${SERVICES[@]}"; do
  port="${entry#*:}"
  if lsof -iTCP:"$port" -sTCP:LISTEN >/dev/null 2>&1; then
    echo "El puerto $port ya está en uso (¿${entry%%:*}.API ya está levantado?). Detenelo y volvé a intentar." >&2
    exit 1
  fi
done

echo "Compilando la solución..."
dotnet build ECommerce.slnx -v q --nologo

# Al salir (Ctrl+C o error), se detienen todos los procesos que lanzó el script.
trap 'trap - INT TERM EXIT; echo; echo "Deteniendo los servicios..."; kill 0' INT TERM EXIT

for entry in "${SERVICES[@]}"; do
  name="${entry%%:*}"
  dotnet run --project "src/$name.API" --launch-profile http --no-build 2>&1 |
    while IFS= read -r line; do printf '[%-13s] %s\n' "$name" "$line"; done &
done

# Espera a que cada servicio responda su /health/live (hasta 60 segundos cada uno).
for entry in "${SERVICES[@]}"; do
  name="${entry%%:*}"
  port="${entry#*:}"
  for _ in $(seq 1 60); do
    curl -sf "http://localhost:$port/health/live" >/dev/null && break
    sleep 1
  done
  curl -sf "http://localhost:$port/health/live" >/dev/null ||
    { echo "$name.API no respondió en el puerto $port." >&2; exit 1; }
done

echo
echo "Los cinco servicios están levantados:"
for entry in "${SERVICES[@]}"; do
  name="${entry%%:*}"
  port="${entry#*:}"
  printf '  %-18s http://localhost:%s/swagger\n' "$name.API" "$port"
done
echo
echo "Ctrl+C para detenerlos."

wait
