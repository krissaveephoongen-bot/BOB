# Windows: sign in as a device, then call the three game RPCs on the local Nakama.
$H = 'http://127.0.0.1:7350'
$basic = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('defaultkey:'))
$auth = Invoke-RestMethod -Method Post -Uri "$H/v2/account/authenticate/device?create=true" -Headers @{Authorization = "Basic $basic"} -ContentType 'application/json' -Body '{"id":"smoke-test-device-0001"}'
function Rpc($id, $json) {
  # Decode as UTF-8 ourselves: Windows PowerShell 5.1 would read the reply as Latin-1 and garble the Thai text.
  try { $r = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$H/v2/rpc/$id`?unwrap" -Headers @{Authorization = "Bearer $($auth.token)"} -ContentType 'application/json' -Body $json
        [Text.Encoding]::UTF8.GetString($r.RawContentStream.ToArray()) }
  catch { "REFUSED: " + $_.ErrorDetails.Message }
}
"ping:";        Rpc bob_ping '{}'
"make_round:";  Rpc bob_make_round '{"seed":42,"round":1}'
"score_stack:"; Rpc bob_score_stack '{"seed":42,"round":1,"stack":["sponge","soap","cloth"],"timeFrac":0.5}'
"cheat attempt (6 layers, must be refused):"; Rpc bob_score_stack '{"seed":42,"round":1,"stack":["cloth","cloth","cloth","cloth","cloth","cloth"]}'
