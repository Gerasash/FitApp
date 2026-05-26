

# ЗАКЛЮЧЕНИЕ

В рамках настоящей выпускной квалификационной работы разработана и реализована информационная система для учёта и планирования силовых спортивных тренировок — приложение FitApp. В ходе работы решены все поставленные задачи.

Проведён анализ предметной области, включающий обзор основных понятий силового тренинга (одноповторный максимум, субъективная оценка нагрузки, принцип прогрессивной нагрузки) и сравнительный анализ существующих аналогов (Strong, Hevy, FitNotes). Установлено, что ни одно из рассмотренных решений не объединяет в единой системе локальный прогноз на основе машинного обучения, алгоритм планирования нагрузки и автономный режим работы.

Спроектирована и реализована кроссплатформенная архитектура на базе .NET MAUI 9 с применением паттерна «модель — представление — модель представления», локальной базой данных SQLite и приоритетом автономной работы. Приложение поддерживает платформы Android и Windows и обеспечивает полную функциональность без сетевого подключения.

Разработан синтетический набор данных: 500 виртуальных атлетов, 26 недель, 763 322 подхода, откалиброванных по реальным силовым нормативам. На этих данных обучена модель градиентного бустинга LightGBM для прогнозирования одноповторного максимума на горизонте 28 дней. На тестовой выборке (22 967 пар) достигнута средняя абсолютная ошибка 1,437 кг — на 39,1 % ниже наивного предиктора и на 33,3 % ниже линейной регрессии. Модель преобразована в открытый формат ONNX (1,5 МБ) и встроена в приложение; одно вычисление прогноза занимает 4–8 мс и не требует обращения к внешним серверам.

Реализован алгоритм планирования следующей тренировки на основе якорной схемы. Алгоритм прошёл проверку на исторических данных (12 631 рекомендация): систематическое смещение отсутствует, 77,8 % рекомендаций укладываются в пределы ±10 % от фактически выполненного веса, каскадное снижение весов в цепной симуляции исключено.

Разработан сервер на ASP.NET Core 9 с двунаправленной синхронизацией данных по стратегии «побеждает последняя запись» и постоянным хранилищем PostgreSQL (платформа Neon). Выявлена и устранена нетривиальная проблема расхождения часов клиента и сервера при смешивании временны́х меток из разных источников. Сервис развёрнут на платформе Render.com с автоматической сборкой и развёртыванием при изменении кода.

**Основные результаты работы:**

1. реализовано функционально полное кроссплатформенное приложение FitApp (Android, Windows) с ведением дневника тренировок, каталогом упражнений, статистикой прогресса и шаблонами;
2. точность прогнозирования одноповторного максимума на 28 дней вперёд: средняя абсолютная ошибка 1,437 кг, средняя относительная ошибка 2,083 %;
3. алгоритм планирования нагрузки: 77,8 % рекомендаций в пределах ±10 % от факта, снижение веса менее чем в 0,1 % случаев;
4. время вычисления прогноза модели: 4–8 мс на целевых устройствах;
5. реализована двунаправленная облачная синхронизация с автоматическим разрешением конфликтов.

**Направления дальнейшего развития:** персональная адаптация модели (поправочная гребневая регрессия на остатках общей модели по мере накопления данных конкретного пользователя), классификатор вероятности выхода на плато, поддержка операционной системы iOS.

---

# СПИСОК ИСПОЛЬЗОВАННЫХ ИСТОЧНИКОВ

1. World Health Organization. Global action plan on physical activity 2018–2030: more active people for a healthier world. — Geneva: WHO, 2018. — 101 p.

2. Физическая культура и спорт в России. 2023: Стат. сб. / Росстат. — М.: Росстат, 2023. — 180 с.

3. Ke G., Meng Q., Finley T., Wang T., Chen W., Ma W., Ye Q., Liu T.Y. LightGBM: A Highly Efficient Gradient Boosting Decision Tree // Advances in Neural Information Processing Systems. — 2017. — Vol. 30. — P. 3146–3154.

4. Westcott W. L. Resistance training is medicine: effects of strength training on health // Current Sports Medicine Reports. — 2012. — Vol. 11, No. 4. — P. 209–216.

5. Borg G. A. Psychophysical bases of perceived exertion // Medicine and Science in Sports and Exercise. — 1982. — Vol. 14, No. 5. — P. 377–381.

6. Epley B. Poundage chart. — Lincoln, NE: Body Enterprises, 1985. — 86 p.

7. Mayhew J. L., Ball T. E., Arnold M. D., Bowen J. C. Relative muscular endurance performance as a predictor of bench press strength in college men and women // Journal of Applied Sport Science Research. — 1992. — Vol. 6, No. 4. — P. 200–206.

8. Brzycki M. Strength testing — predicting a one-rep max from reps-to-fatigue // Journal of Physical Education, Recreation and Dance. — 1993. — Vol. 64, No. 1. — P. 88–90.

9. Lander J. Maximums based on reps // National Strength and Conditioning Association Journal. — 1985. — Vol. 6. — P. 60–61.

10. Zatsiorsky V. M., Kraemer W. J. Science and Practice of Strength Training. — 2nd ed. — Champaign: Human Kinetics, 2006. — 264 p.

11. Haff G. G., Triplett N. T. Essentials of Strength Training and Conditioning. — 4th ed. — Champaign: Human Kinetics, 2015. — 752 p.

12. Strong Workout Tracker Gym Log [Электронный ресурс]. — URL: https://www.strong.app (дата обращения: 10.03.2026).

13. Hevy — Workout Tracker [Электронный ресурс]. — URL: https://www.hevyapp.com (дата обращения: 10.03.2026).

14. Jefit — Workout Tracker, Gym Log and Personal Trainer [Электронный ресурс]. — URL: https://www.jefit.com (дата обращения: 10.03.2026).

15. FitNotes — Gym Training Log [Электронный ресурс]. — URL: https://www.fitnotesapp.com (дата обращения: 10.03.2026).

16. LightGBM Documentation [Электронный ресурс]. — URL: https://lightgbm.readthedocs.io (дата обращения: 15.03.2026).

17. Chen T., Guestrin C. XGBoost: A scalable tree boosting system // Proceedings of the 22nd ACM SIGKDD International Conference on Knowledge Discovery and Data Mining. — 2016. — P. 785–794.

18. Schmidhuber J. Deep learning in neural networks: An overview // Neural Networks. — 2015. — Vol. 60. — P. 85–117.

19. Microsoft. .NET MAUI documentation [Электронный ресурс]. — URL: https://learn.microsoft.com/en-us/dotnet/maui (дата обращения: 20.03.2026).

20. Microsoft. CommunityToolkit.Mvvm documentation [Электронный ресурс]. — URL: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm (дата обращения: 20.03.2026).

21. Praeclarum. sqlite-net documentation [Электронный ресурс]. — URL: https://github.com/praeclarum/sqlite-net (дата обращения: 20.03.2026).

22. PostgreSQL Global Development Group. PostgreSQL 16 Documentation [Электронный ресурс]. — URL: https://www.postgresql.org/docs/16 (дата обращения: 25.03.2026).

23. Fowler M. Patterns of Enterprise Application Architecture. — Boston: Addison-Wesley, 2002. — 557 p.

24. Jones M., Bradley J., Sakimura N. JSON Web Token (JWT). RFC 7519 [Электронный ресурс]. — URL: https://tools.ietf.org/html/rfc7519 (дата обращения: 25.03.2026).

25. Provos N., Mazières D. A Future-Adaptable Password Scheme // USENIX Annual Technical Conference, FREENIX Track. — 1999. — P. 81–91.

26. Open Powerlifting Project [Электронный ресурс]. — URL: https://www.openpowerlifting.org (дата обращения: 10.03.2026).

27. Wiggins A. The Twelve-Factor App [Электронный ресурс]. — URL: https://12factor.net (дата обращения: 25.03.2026).

28. Lamport L. Time, clocks, and the ordering of events in a distributed system // Communications of the ACM. — 1978. — Vol. 21, No. 7. — P. 558–565.

---

# ПРИЛОЖЕНИЯ

## Приложение А — Листинги программного кода

Приведено в отдельном файле `vkr_Prilozhenie_A.md`. Содержит четыре ключевых фрагмента исходного кода:

- **А.1** — алгоритм планирования следующей тренировки (`WorkoutPlannerService.GetRecommendationAsync`), реализующий якорную схему расчёта рабочего веса;
- **А.2** — цикл двунаправленной синхронизации (`SyncService.RunOnceAsync`) с защитой от расхождения часов клиента и сервера;
- **А.3** — сборка признакового вектора для ONNX-инференса (`OnnxPredictionService.BuildFeatures`), воспроизводящая признаковый конвейер Python-этапа обучения;
- **А.4** — симулятор одной тренировки виртуального атлета (`VirtualAthlete.simulate_workout`) из подсистемы генерации синтетического датасета.

---

## Приложение Б — Схемы

### Б.1 ER-диаграмма базы данных клиента

*(Рисунок Б.1 — Полная ER-диаграмма с атрибутами)*

### Б.2 SQL-схема серверных таблиц

*(Листинг Б.1 — DDL-скрипт создания таблиц PostgreSQL с UPSERT-логикой)*

### Б.3 Диаграмма компонентов системы

*(Рисунок Б.2 — UML-диаграмма компонентов: клиент, сервер, ONNX Runtime, PostgreSQL)*

---

## Приложение В — Скриншоты приложения

*(Рисунок В.1 — Главный экран (список тренировок), Android)*

*(Рисунок В.2 — Страница активной тренировки с ML-подсказкой и рекомендацией планировщика)*

*(Рисунок В.3 — Страница прогресса: тепловая карта активности и график 1ПМ)*

*(Рисунок В.4 — Страница аккаунта: управление синхронизацией)*

---

## Приложение Г — Результаты обучения модели

*(Таблица Г.1 — Полная таблица метрик LightGBM в разрезе стажей)*

*(Рисунок Г.1 — График валидационной MAE в процессе обучения (early stopping на 326-й итерации))*

*(Рисунок Г.2 — Распределение ошибок прогноза (гистограмма residuals) на тестовой выборке)*
