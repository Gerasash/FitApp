#!/bin/bash
set -e
API=http://localhost:5127

echo "=== 1. Register user ==="
REG=$(curl -s -X POST $API/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"sync@test.com","password":"secret123"}')
echo "$REG"
TOKEN=$(echo "$REG" | grep -o '"token":"[^"]*' | sed 's/"token":"//')
echo "TOKEN=${TOKEN:0:30}..."

echo ""
echo "=== 2. Sync without auth (expect 401) ==="
curl -s -o /dev/null -w "HTTP %{http_code}\n" -X POST $API/sync \
  -H "Content-Type: application/json" \
  -d '{"workouts":[],"workoutExercises":[],"exerciseSets":[]}'

echo ""
echo "=== 3. First sync — push 1 workout + 1 exercise + 1 set ==="
W_SID="w-$(powershell -Command "[guid]::NewGuid().ToString()" | tr -d '\r\n')"
WE_SID="we-$(powershell -Command "[guid]::NewGuid().ToString()" | tr -d '\r\n')"
S_SID="s-$(powershell -Command "[guid]::NewGuid().ToString()" | tr -d '\r\n')"
NOW=$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")
curl -s -X POST $API/sync \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"lastSyncUtc\": null,
    \"workouts\": [{\"syncId\":\"$W_SID\",\"name\":\"Leg Day\",\"description\":\"\",\"startTime\":\"$NOW\",\"updatedAtUtc\":\"$NOW\",\"isDeleted\":false}],
    \"workoutExercises\": [{\"syncId\":\"$WE_SID\",\"workoutSyncId\":\"$W_SID\",\"exerciseRefId\":1,\"orderIndex\":1,\"updatedAtUtc\":\"$NOW\",\"isDeleted\":false}],
    \"exerciseSets\": [{\"syncId\":\"$S_SID\",\"workoutExerciseSyncId\":\"$WE_SID\",\"setNumber\":1,\"weight\":100,\"reps\":5,\"rpe\":7,\"isAssisted\":false,\"kind\":0,\"updatedAtUtc\":\"$NOW\",\"isDeleted\":false}]
  }"

echo ""
echo ""
echo "=== 4. Second sync — pull with lastSyncUtc=null (expect 1+1+1) ==="
curl -s -X POST $API/sync \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"lastSyncUtc":null,"workouts":[],"workoutExercises":[],"exerciseSets":[]}'

echo ""
echo ""
echo "=== 5. Sync with lastSyncUtc=2099 (expect empty) ==="
curl -s -X POST $API/sync \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"lastSyncUtc":"2099-01-01T00:00:00Z","workouts":[],"workoutExercises":[],"exerciseSets":[]}'

echo ""
echo ""
echo "=== 6. Update workout (newer UpdatedAt) ==="
LATER=$(date -u -d '+1 hour' +"%Y-%m-%dT%H:%M:%S.000Z" 2>/dev/null || date -u -v+1H +"%Y-%m-%dT%H:%M:%S.000Z")
curl -s -X POST $API/sync \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"lastSyncUtc\":\"$NOW\",
    \"workouts\":[{\"syncId\":\"$W_SID\",\"name\":\"Leg Day (updated)\",\"description\":\"\",\"startTime\":\"$NOW\",\"updatedAtUtc\":\"$LATER\",\"isDeleted\":false}],
    \"workoutExercises\":[],
    \"exerciseSets\":[]
  }"

echo ""
echo ""
echo "=== 7. Try stale update (older UpdatedAt — should be ignored) ==="
PAST="2020-01-01T00:00:00.000Z"
curl -s -X POST $API/sync \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"lastSyncUtc\":null,
    \"workouts\":[{\"syncId\":\"$W_SID\",\"name\":\"OLD name should be ignored\",\"description\":\"\",\"startTime\":\"$NOW\",\"updatedAtUtc\":\"$PAST\",\"isDeleted\":false}],
    \"workoutExercises\":[],
    \"exerciseSets\":[]
  }" > /dev/null

# pull — должны увидеть имя из шага 6, а не из 7
curl -s -X POST $API/sync \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"lastSyncUtc":null,"workouts":[],"workoutExercises":[],"exerciseSets":[]}' | grep -o '"name":"[^"]*"'
echo ""
