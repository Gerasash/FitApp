"""
Запуск: python train.py
Обучает модель на синтетике + реальных данных FitApp (если есть).
"""
from data_loader import prepare_dataset
from predictor import train

if __name__ == "__main__":
    print("Подготовка данных...")
    df = prepare_dataset()
    print(f"Записей для обучения: {len(df)}")
    train(df)
    print("Готово!")
