"""
Запуск: uvicorn main:app --host 0.0.0.0 --port 8000
"""
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from predictor import predict, get_trend

app = FastAPI(title="FitApp AI API")


class SetRecord(BaseModel):
    weight: float
    reps: int
    rpe: float
    date: str


class PredictRequest(BaseModel):
    exercise_id: int
    history: list[SetRecord]


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/predict")
def predict_next(req: PredictRequest):
    if not req.history:
        raise HTTPException(status_code=400, detail="История пуста")
    history = [s.model_dump() for s in req.history]
    try:
        result = predict(history)
    except FileNotFoundError as e:
        raise HTTPException(status_code=503, detail=str(e))
    return result


@app.get("/trend/{exercise_id}")
def trend(exercise_id: int, weights: str):
    """weights — числа через запятую, например: 80,82.5,82.5,85"""
    try:
        w = [float(x) for x in weights.split(",")]
    except ValueError:
        raise HTTPException(status_code=400, detail="Неверный формат weights")
    return {"trend": get_trend(w)}
