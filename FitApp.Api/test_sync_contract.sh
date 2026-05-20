#!/bin/bash
# Проверка совместимости JSON-контракта SyncDtos между клиентом и сервером.
# Отправляем именно те поля, которые формирует SyncService в MAUI, и
# проверяем что сервер их корректно обрабатывает.

set -e
API=http://localhost:5127

echo "=== Register ==="
REG=$(curl -s -X POST $API/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"contract@test.com","password":"secret123"}')
TOKEN=$(echo "$REG" | grep -o '"token":"[^"]*' | sed 's/"token":"//')

W_SID=$(powershell -Command "[guid]::NewGuid().ToString()" | tr -d '\r\n')
WE_SID=$(powershell -Command "[guid]::NewGuid().ToString()" | tr -d '\r\n')
S_SID=$(powershell -Command "[guid]::NewGuid().ToString()" | tr -d '\r\n')
NOW=$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")

echo ""
echo "=== Push with PascalCase field names (как сериализует System.Text.Json) ==="
RESP=$(curl -s -X POST $API/sync \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"LastSyncUtc\": null,
    \"Workouts\": [{
      \"SyncId\": \"$W_SID\",
      \"Name\": \"Contract test\",
      \"Description\": \"\",
      \"StartTime\": \"$NOW\",
      \"UpdatedAtUtc\": \"$NOW\",
      \"IsDeleted\": false
    }],
    \"WorkoutExercises\": [{
      \"SyncId\": \"$WE_SID\",
      \"WorkoutSyncId\": \"$W_SID\",
      \"ExerciseRefId\": 42,
      \"OrderIndex\": 1,
      \"UpdatedAtUtc\": \"$NOW\",
      \"IsDeleted\": false
    }],
    \"ExerciseSets\": [{
      \"SyncId\": \"$S_SID\",
      \"WorkoutExerciseSyncId\": \"$WE_SID\",
      \"SetNumber\": 1,
      \"Weight\": 80.5,
      \"Reps\": 5,
      \"RPE\": 8.5,
      \"IsAssisted\": false,
      \"Kind\": 0,
      \"UpdatedAtUtc\": \"$NOW\",
      \"IsDeleted\": false
    }]
  }")

echo "$RESP"
echo ""
echo "=== Check counts in response ==="
echo "$RESP" | grep -o '"syncId":"[^"]*"' | wc -l
echo "(expected 3: 1 workout + 1 exercise + 1 set)"
