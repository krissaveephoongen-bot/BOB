#!/bin/sh
# Starts Docker (if it is not running) and the local Nakama + Postgres. Safe to run again.
set -e
cd "$(dirname "$0")/server"
if ! docker info >/dev/null 2>&1; then
  (nohup dockerd >/tmp/dockerd.log 2>&1 &)
  for i in $(seq 1 30); do docker info >/dev/null 2>&1 && break; sleep 1; done
fi
node build-module.js
docker compose up -d
for i in $(seq 1 40); do
  [ "$(curl -s -o /dev/null -m 3 -w '%{http_code}' --noproxy '*' http://127.0.0.1:7350/healthcheck)" = "200" ] && break; sleep 1.5
done
echo "Nakama API http://127.0.0.1:7350  console http://127.0.0.1:7351 (admin / password, development only)"
