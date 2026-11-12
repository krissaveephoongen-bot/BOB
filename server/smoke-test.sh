#!/bin/sh
# End-to-end check against the local Nakama: sign in as a device, then call the three game RPCs.
set -e
H=http://127.0.0.1:7350
TOKEN=$(curl -s --noproxy '*' -u defaultkey: -H 'Content-Type: application/json' \
  -d '{"id":"smoke-test-device-0001"}' "$H/v2/account/authenticate/device?create=true" | python3 -c 'import sys,json;print(json.load(sys.stdin)["token"])')
rpc(){ curl -s --noproxy '*' -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "$2" "$H/v2/rpc/$1?unwrap"; echo; }
echo "ping:";        rpc bob_ping '{}'
echo "make_round:";  rpc bob_make_round '{"seed":42,"round":1}'
echo "score_stack:"; rpc bob_score_stack '{"seed":42,"round":1,"stack":["sponge","soap","cloth"],"timeFrac":0.5}'
echo "cheat attempt (6 layers, must be refused):"; rpc bob_score_stack '{"seed":42,"round":1,"stack":["cloth","cloth","cloth","cloth","cloth","cloth"]}'
echo "log_event:";   rpc bob_log_event '{"event":"match_start","match":"smoke1","data":{"char":"sweep","seed":42}}'
echo "analytics (server key):"; curl -s --noproxy '*' -H 'Content-Type: application/json' -d '{"days":1}' "$H/v2/rpc/bob_analytics?http_key=defaulthttpkey&unwrap" | head -c 300; echo
echo "analytics as a player (must be refused):"; rpc bob_analytics '{}'
