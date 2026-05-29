const path = require("path");
const G = "C:\\Users\\Gera1\\AppData\\Roaming\\npm\\node_modules";
const pptxgen = require(path.join(G, "pptxgenjs"));
const OUT = "C:\\Users\\Gera1\\dev\\FitApp\\VKR\\presentation\\drafts\\";
const ST = new pptxgen().ShapeType;

const NAVY = "1E2761", ICE = "CADCFC", GRAY = "44474F", MUTE = "9AA0A6", ACC = "F96167";
const HEAD = "Georgia", BODY = "Calibri";

function newDeck() {
  const p = new pptxgen();
  p.defineLayout({ name: "W", width: 13.333, height: 7.5 });
  p.layout = "W";
  return p;
}
function footer(s, n) {
  s.addText("FitApp · ВКР · МТУСИ, 2026", { x: 0.6, y: 7.05, w: 8, h: 0.3, fontFace: BODY, fontSize: 10, color: MUTE });
  s.addText(n + " / 12", { x: 11.7, y: 7.05, w: 1.0, h: 0.3, fontFace: BODY, fontSize: 10, color: MUTE, align: "right" });
}
function title(s, t) {
  s.addText(t, { x: 0.6, y: 0.4, w: 12, h: 0.9, fontFace: HEAD, fontSize: 34, bold: true, color: NAVY });
}
function bullets(items, code) {
  return items.map((t, i) => ({ text: t, options: { bullet: { code: code || "2022" }, paraSpaceAfter: 8 } }));
}
const decks = [];
function slide(n, build, notes) {
  const p = newDeck();
  const s = p.addSlide();
  s.background = { color: "FFFFFF" };
  build(s);
  footer(s, n);
  if (notes) s.addNotes(notes);
  decks.push(p.writeFile({ fileName: OUT + "slide_" + String(n).padStart(2, "0") + ".pptx" }));
}

// ---------- Slide 1 — Титул ----------
slide(1, (s) => {
  s.background = { color: NAVY };
  s.addText("МОСКОВСКИЙ ТЕХНИЧЕСКИЙ УНИВЕРСИТЕТ\nСВЯЗИ И ИНФОРМАТИКИ", { x: 0.8, y: 0.6, w: 11.7, h: 1.0, fontFace: BODY, fontSize: 16, color: ICE, align: "center", bold: true });
  s.addText("Выпускная квалификационная работа", { x: 0.8, y: 2.0, w: 11.7, h: 0.5, fontFace: BODY, fontSize: 15, color: ICE, align: "center" });
  s.addText("Разработка кроссплатформенного приложения\nдля отслеживания силовых тренировок с локальной\nML-моделью прогнозирования 1ПМ и облачной синхронизацией",
    { x: 0.8, y: 2.6, w: 11.7, h: 1.9, fontFace: HEAD, fontSize: 28, bold: true, color: "FFFFFF", align: "center", lineSpacingMultiple: 1.05 });
  s.addText([
    { text: "Студент: Герасимов Александр Евгеньевич   ·   Группа: БЭИ2202\n", options: {} },
    { text: "Научный руководитель: доцент к.т.н. Карташов Д. А.", options: {} },
  ], { x: 0.8, y: 5.3, w: 11.7, h: 1.0, fontFace: BODY, fontSize: 15, color: ICE, align: "center" });
  s.addText("Москва · 2026", { x: 0.8, y: 6.5, w: 11.7, h: 0.4, fontFace: BODY, fontSize: 13, color: MUTE, align: "center" });
},
"Здравствуйте, уважаемые члены комиссии. Тема моей работы — разработка кроссплатформенного приложения для отслеживания силовых тренировок с локальной моделью машинного обучения для прогнозирования одноповторного максимума и облачной синхронизацией. Научный руководитель — доцент Карташов Дмитрий Александрович.");

// ---------- Slide 2 — Актуальность ----------
slide(2, (s) => {
  title(s, "Актуальность темы");
  s.addText(bullets([
    "Силовой тренинг — массовая практика, требующая систематического учёта нагрузки.",
    "Ключевые задачи занимающегося — отслеживание прогресса 1ПМ и планирование рабочих весов.",
    "Существующие приложения (Strong, Hevy, FitNotes, Jefit) закрывают лишь часть задач: либо без локального ML, либо без автономного режима, либо по подписке.",
  ]), { x: 0.6, y: 1.6, w: 7.0, h: 3.5, fontFace: BODY, fontSize: 17, color: GRAY, valign: "top", lineSpacingMultiple: 1.05 });
  s.addShape(ST.roundRect, { x: 8.0, y: 1.6, w: 4.7, h: 4.6, fill: { color: NAVY }, rectRadius: 0.12 });
  s.addText("НИША FITAPP", { x: 8.0, y: 1.8, w: 4.7, h: 0.5, fontFace: HEAD, fontSize: 18, bold: true, color: "FFFFFF", align: "center" });
  s.addText(bullets(["Автономная работа (приоритет локальных данных)", "Локальный ML-прогноз 1ПМ без подписки", "Планировщик нагрузки", "Android + Windows", "Синхронизация устройств"], "2713"),
    { x: 8.3, y: 2.45, w: 4.1, h: 3.6, fontFace: BODY, fontSize: 15, color: ICE, valign: "top" });
},
"Силовой тренинг — массовое занятие, и тем, кто тренируется, важно отслеживать прогресс и грамотно планировать веса. Я проанализировал популярные приложения — Strong, Hevy, FitNotes, Jefit — и увидел, что каждое закрывает лишь часть задач: где-то нет локального машинного обучения, где-то нет полноценной автономной работы, где-то функции спрятаны за подпиской. Ниша FitApp — объединить автономную работу, локальный прогноз без подписки, планировщик нагрузки, поддержку и Android, и Windows, и синхронизацию между устройствами.");

// ---------- Slide 3 — Цель и задачи ----------
slide(3, (s) => {
  title(s, "Цель и задачи работы");
  s.addShape(ST.roundRect, { x: 0.6, y: 1.45, w: 12.1, h: 1.25, fill: { color: ICE }, rectRadius: 0.1 });
  s.addText([
    { text: "ЦЕЛЬ РАБОТЫ.  ", options: { bold: true, color: NAVY } },
    { text: "Разработка кроссплатформенного приложения для учёта силовых тренировок с локальным ML-прогнозированием 1ПМ, планировщиком нагрузки и облачной синхронизацией.", options: { color: GRAY } },
  ], { x: 0.9, y: 1.55, w: 11.5, h: 1.05, fontFace: BODY, fontSize: 16, valign: "middle" });
  const tasks = [
    "Проанализировать предметную область и аналоги",
    "Спроектировать архитектуру клиента и сервера, базу данных",
    "Реализовать клиент: дневник, шаблоны, статистику",
    "Обучить ML-модель прогноза 1ПМ (LightGBM → ONNX)",
    "Реализовать протокол синхронизации (побеждает последняя запись)",
    "Провести функциональное и интеграционное тестирование",
  ];
  tasks.forEach((t, i) => {
    const col = i % 2, row = Math.floor(i / 2);
    const x = 0.6 + col * 6.15, y = 3.05 + row * 1.25;
    s.addShape(ST.ellipse, { x, y, w: 0.6, h: 0.6, fill: { color: NAVY } });
    s.addText(String(i + 1), { x, y, w: 0.6, h: 0.6, fontFace: HEAD, fontSize: 22, bold: true, color: "FFFFFF", align: "center", valign: "middle" });
    s.addText(t, { x: x + 0.75, y, w: 5.2, h: 0.9, fontFace: BODY, fontSize: 14.5, color: GRAY, valign: "middle" });
  });
},
"Цель работы — разработать кроссплатформенное приложение для учёта силовых тренировок с локальным прогнозом одноповторного максимума, планировщиком нагрузки и облачной синхронизацией. Для её достижения я поставил шесть задач: проанализировать аналоги, спроектировать архитектуру и базу данных, реализовать клиентское приложение, обучить ML-модель и перевести её в формат ONNX, реализовать протокол синхронизации, и провести тестирование. Дальше покажу, что получилось по каждому направлению.");

// ---------- Slide 4 — Анализ аналогов ----------
slide(4, (s) => {
  title(s, "Сравнительный анализ аналогов");
  const rows = [
    ["Критерий", "Strong", "Hevy", "FitNotes", "Jefit", "FitApp"],
    ["Поддержка Windows", "—", "—", "—", "—", "✓"],
    ["Автономная работа", "✓", "✓", "✓", "✓", "✓"],
    ["Поддержка RPE", "частично", "✓", "—", "—", "✓"],
    ["ML-прогноз 1ПМ", "—", "—", "—", "—", "✓"],
    ["Планировщик нагрузки", "платно", "—", "—", "—", "✓"],
    ["Облачная синхронизация", "iCloud", "✓", "—", "✓", "✓"],
    ["Монетизация", "усл.-беспл.", "усл.-беспл.", "беспл.", "усл.-беспл.", "Open Source"],
  ];
  const tbl = rows.map((r, ri) =>
    r.map((c, ci) => ({
      text: c,
      options: {
        fontFace: BODY, fontSize: ri === 0 ? 13 : 12.5, align: ci === 0 ? "left" : "center",
        bold: ri === 0 || ci === 5, color: ri === 0 ? "FFFFFF" : (ci === 5 ? NAVY : GRAY),
        fill: { color: ri === 0 ? NAVY : (ci === 5 ? "EAF0FF" : "FFFFFF") }, valign: "middle",
      },
    }))
  );
  s.addTable(tbl, { x: 0.6, y: 1.5, w: 12.1, colW: [3.1, 1.8, 1.8, 1.8, 1.8, 1.8], rowH: 0.5, border: { type: "solid", color: "D8DCE3", pt: 0.5 } });
  s.addText("FitApp — единственное решение, объединяющее все ключевые функции в одном бесплатном продукте с открытым исходным кодом.",
    { x: 0.6, y: 6.0, w: 12.1, h: 0.7, fontFace: BODY, fontSize: 15, italic: true, color: NAVY, align: "left" });
},
"Сравнение по ключевым критериям свёл в таблицу. Главное отличие FitApp — это единственное решение с поддержкой Windows и единственное с локальным прогнозом одноповторного максимума. Автономную работу и RPE поддерживают и некоторые аналоги, но планировщик нагрузки либо платный, либо отсутствует. Вывод: FitApp объединяет все ключевые функции в одном бесплатном продукте с открытым исходным кодом.");

// ---------- Slide 5 — Архитектура ----------
slide(5, (s) => {
  title(s, "Архитектура системы");
  // Клиент
  s.addShape(ST.roundRect, { x: 0.6, y: 1.5, w: 5.6, h: 4.9, fill: { color: "EAF0FF" }, line: { color: NAVY, width: 1 }, rectRadius: 0.1 });
  s.addText("КЛИЕНТ · .NET MAUI 9", { x: 0.6, y: 1.6, w: 5.6, h: 0.5, fontFace: HEAD, fontSize: 16, bold: true, color: NAVY, align: "center" });
  s.addText([
    { text: "MVVM: Views · ViewModels · Models\n", options: { bold: true, paraSpaceAfter: 6 } },
    { text: "Сервисный слой:\n", options: { bold: true } },
    { text: "OnnxPredictionService · WorkoutPlannerService\nSyncService · AuthClient\n", options: { paraSpaceAfter: 6 } },
    { text: "Локальное хранилище:\n", options: { bold: true } },
    { text: "SQLite · ONNX-модель (1,5 МБ) · SecureStorage (JWT)", options: {} },
  ], { x: 0.85, y: 2.2, w: 5.1, h: 4.0, fontFace: BODY, fontSize: 13.5, color: GRAY, valign: "top", lineSpacingMultiple: 1.04 });
  // Стрелка
  s.addText("HTTPS\nJWT Bearer", { x: 6.25, y: 3.4, w: 1.0, h: 0.9, fontFace: BODY, fontSize: 11, color: ACC, align: "center", bold: true });
  s.addShape(ST.rightArrow, { x: 6.25, y: 4.1, w: 0.95, h: 0.4, fill: { color: ACC } });
  // Сервер
  s.addShape(ST.roundRect, { x: 7.3, y: 1.5, w: 5.4, h: 4.9, fill: { color: NAVY }, rectRadius: 0.1 });
  s.addText("СЕРВЕР · ASP.NET Core 9", { x: 7.3, y: 1.6, w: 5.4, h: 0.5, fontFace: HEAD, fontSize: 16, bold: true, color: "FFFFFF", align: "center" });
  s.addText([
    { text: "POST /auth/register\nPOST /auth/login\nPOST /sync  [Bearer JWT]\n", options: { color: ICE, paraSpaceAfter: 8 } },
    { text: "BCrypt · JWT HS256 · срок 30 дней\n", options: { color: "FFFFFF", paraSpaceAfter: 8 } },
    { text: "PostgreSQL / Neon\nserverless, 512 МБ", options: { color: ICE } },
  ], { x: 7.55, y: 2.2, w: 4.9, h: 4.0, fontFace: BODY, fontSize: 13.5, valign: "top", lineSpacingMultiple: 1.04 });
},
"Система состоит из двух частей. Клиент на .NET MAUI 9 построен по паттерну MVVM и работает по принципу приоритета локальных данных: все операции идут через локальную базу SQLite, рядом лежит ONNX-модель и защищённое хранилище токена. Сервисный слой — это прогноз на ONNX, планировщик, синхронизация и клиент аутентификации. Сервер — минимальный REST API на ASP.NET Core 9 с тремя конечными точками: регистрация, вход и синхронизация. Пароли хранятся хешами BCrypt, доступ — по JWT-токену. Постоянное хранилище — PostgreSQL на платформе Neon. Связь — по HTTPS с токеном в заголовке.");

// ---------- Slide 6 — Стек ----------
slide(6, (s) => {
  title(s, "Стек технологий");
  const cards = [
    ["КЛИЕНТ", [".NET MAUI 9", "C# / XAML", "CommunityToolkit.Mvvm", "SQLite (sqlite-net-pcl)", "LiveChartsCore"]],
    ["СЕРВЕР", ["ASP.NET Core 9", "Npgsql (без EF)", "JWT HS256 + BCrypt", "Docker (multi-stage)", "Render.com (Free)"]],
    ["БАЗА ДАННЫХ", ["Локально: SQLite", "Облако: PostgreSQL", "Хостинг: Neon", "Serverless, 512 МБ"]],
    ["ML-ИНФРАСТРУКТУРА", ["Python 3.11", "LightGBM", "ONNX (кроссплатформенно)", "OnnxRuntime 1.20.1"]],
  ];
  cards.forEach((c, i) => {
    const col = i % 2, row = Math.floor(i / 2);
    const x = 0.6 + col * 6.15, y = 1.6 + row * 2.55;
    s.addShape(ST.roundRect, { x, y, w: 5.9, h: 2.35, fill: { color: i % 2 ? "EAF0FF" : NAVY }, rectRadius: 0.1 });
    s.addText(c[0], { x: x + 0.25, y: y + 0.15, w: 5.4, h: 0.45, fontFace: HEAD, fontSize: 15, bold: true, color: i % 2 ? NAVY : "FFFFFF" });
    s.addText(bullets(c[1]), { x: x + 0.3, y: y + 0.65, w: 5.3, h: 1.6, fontFace: BODY, fontSize: 13, color: i % 2 ? GRAY : ICE, valign: "top" });
  });
},
"Технологический стек. Клиент — .NET MAUI 9 на C# и XAML, паттерн MVVM через CommunityToolkit, локальная база SQLite, графики на LiveChartsCore. Сервер — ASP.NET Core 9, работа с PostgreSQL напрямую через Npgsql без Entity Framework, упакован в Docker и развёрнут на Render. База данных: локально SQLite, в облаке PostgreSQL на Neon. ML-инфраструктура: модель обучается на Python с помощью LightGBM, переводится в формат ONNX и исполняется в приложении через ONNX Runtime — одинаково на Android и Windows. Единый язык C# и на клиенте, и на сервере упростил совместное использование классов передачи данных.");

// ---------- Slide 7 — ML датасет и признаки ----------
slide(7, (s) => {
  title(s, "ML-модуль: набор данных и признаки");
  const stats = [["762 318", "тренировочных подхода"], ["500", "спортсменов"], ["113 434", "обучающих пар (X → y)"]];
  stats.forEach((st, i) => {
    const x = 0.6 + i * 4.05;
    s.addShape(ST.roundRect, { x, y: 1.5, w: 3.8, h: 1.7, fill: { color: NAVY }, rectRadius: 0.1 });
    s.addText(st[0], { x, y: 1.6, w: 3.8, h: 0.9, fontFace: HEAD, fontSize: 32, bold: true, color: "FFFFFF", align: "center" });
    s.addText(st[1], { x, y: 2.5, w: 3.8, h: 0.6, fontFace: BODY, fontSize: 13, color: ICE, align: "center" });
  });
  s.addText("30 ПРИЗНАКОВ · 4 ГРУППЫ", { x: 0.6, y: 3.5, w: 12.1, h: 0.5, fontFace: HEAD, fontSize: 18, bold: true, color: NAVY });
  const grp = [["14", "История", "лаги 1ПМ, тренд, пик и просадка"], ["7", "Текущая тренировка", "топ-вес, повторы, RPE"], ["5", "Профиль", "вес, возраст, пол, стаж"], ["4", "Упражнение", "тип, оборудование"]];
  grp.forEach((g, i) => {
    const x = 0.6 + i * 3.05;
    s.addShape(ST.roundRect, { x, y: 4.1, w: 2.85, h: 1.95, fill: { color: "EAF0FF" }, rectRadius: 0.1 });
    s.addText(g[0], { x, y: 4.18, w: 2.85, h: 0.55, fontFace: HEAD, fontSize: 26, bold: true, color: ACC, align: "center" });
    s.addText(g[1], { x: x + 0.15, y: 4.75, w: 2.55, h: 0.45, fontFace: BODY, fontSize: 14, bold: true, color: NAVY, align: "center" });
    s.addText(g[2], { x: x + 0.15, y: 5.2, w: 2.55, h: 0.8, fontFace: BODY, fontSize: 12, color: GRAY, align: "center" });
  });
  s.addText("Данные моделируют не только рост, но и травмы, паузы и восстановление — модель учится прогнозировать возврат к прежнему уровню, а не застой.",
    { x: 0.6, y: 6.25, w: 12.1, h: 0.6, fontFace: BODY, fontSize: 13, italic: true, color: NAVY, valign: "top", lineSpacingMultiple: 1.0 });
},
"Для обучения модели я сгенерировал реалистичный набор данных: более 760 тысяч подходов по 500 виртуальным спортсменам, из которых сформировано около 113 тысяч обучающих пар «вход — целевое значение». Целевое значение — одноповторный максимум через 28 дней. Модель использует 30 признаков в четырёх группах: история (лаги одноповторного максимума, тренд, исторический пик и просадка от него) — самая важная группа из 14 признаков; параметры текущей тренировки; профиль спортсмена; и характеристики упражнения. Важная особенность: данные моделируют не только рост силы, но и спады — травмы и длительные паузы с последующим восстановлением, поэтому модель учится прогнозировать возврат к прежнему уровню.");

// ---------- Slide 8 — ML результаты ----------
slide(8, (s) => {
  title(s, "ML-модуль: результаты");
  // --- Левая панель: сравнение MAE трёх моделей (ручные бары) ---
  s.addShape(ST.roundRect, { x: 0.6, y: 1.5, w: 6.0, h: 3.9, fill: { color: "EAF0FF" }, rectRadius: 0.1 });
  s.addText("Средняя ошибка прогноза 1ПМ (MAE), кг", { x: 0.85, y: 1.62, w: 5.5, h: 0.45, fontFace: HEAD, fontSize: 14.5, bold: true, color: NAVY });
  const bars = [["Наивный (последнее значение)", 2.54, MUTE], ["Лин. регрессия (5 точек)", 2.43, "7E9AD6"], ["LightGBM", 1.82, ACC]];
  const bx = 0.95, bmaxw = 4.4, bden = 2.6;
  bars.forEach((b, i) => {
    const by = 2.45 + i * 0.92;
    s.addText(b[0], { x: bx, y: by - 0.34, w: 5.4, h: 0.3, fontFace: BODY, fontSize: 12, color: GRAY });
    const w = bmaxw * (b[1] / bden);
    s.addShape(ST.roundRect, { x: bx, y: by, w, h: 0.38, fill: { color: b[2] }, rectRadius: 0.04 });
    s.addText(b[1].toFixed(2).replace(".", ","), { x: bx + w + 0.08, y: by - 0.05, w: 1.0, h: 0.48, fontFace: HEAD, fontSize: 15, bold: true, color: NAVY, valign: "middle" });
  });
  s.addText("LightGBM — на 29 % точнее наивного предиктора", { x: 0.85, y: 5.0, w: 5.5, h: 0.35, fontFace: BODY, fontSize: 13, italic: true, color: NAVY });
  // --- Правая панель: устойчивость к просадкам ---
  s.addShape(ST.roundRect, { x: 6.8, y: 1.5, w: 5.9, h: 3.9, fill: { color: NAVY }, rectRadius: 0.1 });
  s.addText("УСТОЙЧИВОСТЬ К ПРОСАДКАМ\n(травмы, длительные паузы)", { x: 7.0, y: 1.68, w: 5.5, h: 0.8, fontFace: HEAD, fontSize: 15, bold: true, color: "FFFFFF", align: "center" });
  s.addText("−43 %", { x: 6.8, y: 2.5, w: 5.9, h: 1.15, fontFace: HEAD, fontSize: 58, bold: true, color: "FFFFFF", align: "center" });
  s.addText("ошибки на сессиях со спадом 1ПМ:\n2,23 → 1,26 кг против наивного", { x: 7.0, y: 3.7, w: 5.5, h: 0.7, fontFace: BODY, fontSize: 14, color: ICE, align: "center" });
  s.addText("Модель прогнозирует восстановление, а не застой на просевшем уровне.", { x: 7.1, y: 4.5, w: 5.3, h: 0.8, fontFace: BODY, fontSize: 13, italic: true, color: ICE, align: "center" });
  // --- Нижняя полоса: корректность переноса в ONNX ---
  s.addShape(ST.roundRect, { x: 0.6, y: 5.65, w: 12.1, h: 0.9, fill: { color: ICE }, rectRadius: 0.1 });
  s.addText([
    { text: "Перенос Python (LightGBM) → C# (ONNX Runtime) без потери точности: ", options: { color: GRAY } },
    { text: "медиана расхождения 1,6 × 10⁻⁵ кг", options: { bold: true, color: NAVY } },
    { text: ", исполнение прямо на устройстве за 4–8 мс.", options: { color: GRAY } },
  ], { x: 0.9, y: 5.65, w: 11.5, h: 0.9, fontFace: BODY, fontSize: 13.5, valign: "middle" });
},
"По качеству я сравнил три модели. Наивный предиктор просто переносит текущее значение — его ошибка 2,5 килограмма. Линейная регрессия по последним точкам — чуть лучше. Градиентный бустинг LightGBM даёт ошибку 1,8 килограмма, это на 29 процентов точнее наивного. Но самое важное — справа: на сессиях, где силовые показатели резко просели из-за травмы или паузы, модель снижает ошибку уже на 43 процента и, главное, прогнозирует именно восстановление, а не застой на низком уровне — раньше такие случаи были для модели неизвестной территорией. Отдельно я убедился, что перенос модели из Python в приложение через формат ONNX не теряет точности: расхождение исчезающе мало, а сам прогноз считается на устройстве за единицы миллисекунд, без сети.");

// ---------- Slide 9 — Синхронизация ----------
slide(9, (s) => {
  title(s, "Облачная синхронизация");
  const nodes = [["КЛИЕНТ", ".NET MAUI", "EAF0FF", NAVY], ["СЕРВЕР", "ASP.NET Core", NAVY, "FFFFFF"], ["POSTGRESQL", "Neon", "EAF0FF", NAVY]];
  nodes.forEach((nd, i) => {
    const x = 0.6 + i * 4.25;
    s.addShape(ST.roundRect, { x, y: 1.5, w: 3.8, h: 1.2, fill: { color: nd[2] }, line: { color: NAVY, width: 1 }, rectRadius: 0.1 });
    s.addText([{ text: nd[0] + "\n", options: { bold: true, fontSize: 16 } }, { text: nd[1], options: { fontSize: 12 } }],
      { x, y: 1.5, w: 3.8, h: 1.2, fontFace: BODY, color: nd[3], align: "center", valign: "middle" });
    if (i < 2) s.addShape(ST.rightArrow, { x: x + 3.85, y: 1.95, w: 0.38, h: 0.3, fill: { color: ACC } });
  });
  s.addText(bullets([
    "Клиент отправляет POST /sync: пакет исходящих изменений и метку последней синхронизации.",
    "Сервер применяет вставку-или-обновление по стратегии «побеждает последняя запись» — по метке времени UpdatedAt.",
    "Транзакция фиксируется (COMMIT).",
    "В ответе клиент получает пакет входящих изменений и текущее время сервера.",
  ]), { x: 0.6, y: 3.2, w: 12.1, h: 2.6, fontFace: BODY, fontSize: 16, color: GRAY, valign: "top", lineSpacingMultiple: 1.05 });
  s.addShape(ST.roundRect, { x: 0.6, y: 5.95, w: 12.1, h: 0.8, fill: { color: ICE }, rectRadius: 0.1 });
  s.addText("Глобальные идентификаторы (SyncId, GUID) и мягкое удаление (IsDeleted) обеспечивают корректное слияние данных без коллизий между устройствами.",
    { x: 0.85, y: 5.95, w: 11.6, h: 0.8, fontFace: BODY, fontSize: 14, italic: true, color: NAVY, valign: "middle" });
},
"Синхронизация двунаправленная и запускается по явному действию пользователя. Один цикл устроен так: клиент отправляет на сервер пакет своих изменений и метку времени последней синхронизации; сервер применяет их по стратегии «побеждает последняя запись» — выигрывает запись с более поздней меткой времени, поэтому порядок устройств не важен; транзакция фиксируется; и в ответе клиент получает изменения с сервера и его текущее время. Чтобы записи не сталкивались между устройствами, каждая сущность имеет глобальный идентификатор GUID, а удаление сделано мягким — через флаг, который тоже корректно распространяется.");

// ---------- Slide 10 — Тестирование ----------
slide(10, (s) => {
  title(s, "Тестирование и результаты");
  const stats = [["24", "функциональных теста"], ["8", "ключевых сценариев"], ["4", "сценария синхронизации"], ["2", "платформы: Windows + Android"]];
  stats.forEach((st, i) => {
    const x = 0.6 + i * 3.05;
    s.addShape(ST.roundRect, { x, y: 1.5, w: 2.85, h: 1.5, fill: { color: NAVY }, rectRadius: 0.1 });
    s.addText(st[0], { x, y: 1.55, w: 2.85, h: 0.8, fontFace: HEAD, fontSize: 30, bold: true, color: "FFFFFF", align: "center" });
    s.addText(st[1], { x: x + 0.1, y: 2.35, w: 2.65, h: 0.6, fontFace: BODY, fontSize: 12, color: ICE, align: "center" });
  });
  s.addText("ПРОИЗВОДИТЕЛЬНОСТЬ", { x: 0.6, y: 3.3, w: 12, h: 0.5, fontFace: HEAD, fontSize: 17, bold: true, color: NAVY });
  const rows = [
    ["Метрика", "Windows", "Android"],
    ["Список тренировок", "42 мс", "78 мс"],
    ["Вычисление прогноза (ML)", "4 мс", "8 мс"],
    ["Инициализация ONNX-сервиса", "87 мс", "143 мс"],
    ["Размер установочного пакета (APK)", "—", "38 МБ"],
  ];
  const tbl = rows.map((r, ri) => r.map((c, ci) => ({ text: c, options: { fontFace: BODY, fontSize: 13, bold: ri === 0, align: ci === 0 ? "left" : "center", color: ri === 0 ? "FFFFFF" : GRAY, fill: { color: ri === 0 ? NAVY : "FFFFFF" }, valign: "middle" } })));
  s.addTable(tbl, { x: 0.6, y: 3.85, w: 8.0, colW: [4.4, 1.8, 1.8], rowH: 0.45, border: { type: "solid", color: "D8DCE3", pt: 0.5 } });
  s.addShape(ST.roundRect, { x: 8.9, y: 3.85, w: 3.8, h: 2.25, fill: { color: "EAF0FF" }, rectRadius: 0.1 });
  s.addText([
    { text: "UX-тест: 6 добровольцев\n\n", options: { bold: true, color: NAVY, fontSize: 15 } },
    { text: "Отмечены: интуитивный ввод подходов, полезность подсказки прогноза 1ПМ.", options: { color: GRAY, fontSize: 13 } },
  ], { x: 9.15, y: 4.05, w: 3.3, h: 1.9, fontFace: BODY, valign: "top" });
},
"Тестирование шло по трём направлениям. Функциональное — 24 теста, восемь ключевых сценариев пройдены на обеих платформах. Интеграционное — четыре сценария синхронизации, включая конфликт одновременных изменений и мягкое удаление, все пройдены. По производительности: вычисление одного прогноза занимает 4 миллисекунды на Windows и 8 на Android при требовании в 100 — то есть с запасом более чем в десять раз; список тренировок открывается за десятки миллисекунд; установочный пакет под Android — 38 мегабайт. Также провёл пилотный тест с шестью добровольцами: отметили удобный ввод подходов и полезность подсказки прогноза.");

// ---------- Slide 11 — Выводы ----------
slide(11, (s) => {
  title(s, "Выводы");
  s.addShape(ST.roundRect, { x: 0.6, y: 1.4, w: 12.1, h: 0.9, fill: { color: NAVY }, rectRadius: 0.1 });
  s.addText("Цель работы достигнута: все поставленные задачи выполнены, требования соблюдены.",
    { x: 0.9, y: 1.4, w: 11.5, h: 0.9, fontFace: BODY, fontSize: 17, bold: true, color: "FFFFFF", valign: "middle" });
  const cols = [
    ["РЕАЛИЗОВАНО", ["Клиент: .NET MAUI 9, MVVM, 7 экранов", "Сервер: ASP.NET Core 9 на Render.com", "ML: LightGBM → ONNX (MAE 1,44 кг)", "Синхронизация: «последняя запись» + автономный режим"]],
    ["НАУЧНАЯ ЗНАЧИМОСТЬ", ["Адаптация формулы Эпли и ML-модели", "Решение проблемы рассогласования системных часов", "Проверка корректности переноса в ONNX (Δ < 0,1 кг)"]],
    ["ПРАКТИЧЕСКАЯ ЦЕННОСТЬ", ["Бесплатное решение на русском языке", "Единственное приложение с поддержкой Windows", "Открытый код, расширяемый сообществом"]],
  ];
  cols.forEach((c, i) => {
    const x = 0.6 + i * 4.05;
    s.addShape(ST.roundRect, { x, y: 2.6, w: 3.8, h: 0.55, fill: { color: ICE }, rectRadius: 0.08 });
    s.addText(c[0], { x: x + 0.15, y: 2.6, w: 3.6, h: 0.55, fontFace: HEAD, fontSize: 14, bold: true, color: NAVY, valign: "middle" });
    s.addText(bullets(c[1], "2713"), { x, y: 3.35, w: 3.8, h: 3.4, fontFace: BODY, fontSize: 13.5, color: GRAY, valign: "top", lineSpacingMultiple: 1.04 });
  });
},
"Подведу итог. Цель достигнута, все шесть задач выполнены. Реализован клиент на .NET MAUI с семью экранами, сервер на ASP.NET Core, развёрнутый в облаке, обученная ML-модель со средней ошибкой около полутора килограммов и протокол синхронизации с автономным режимом. Научная значимость — в сочетании классической формулы Эпли с машинным обучением, в решении проблемы рассогласования системных часов между устройствами и в подтверждённой корректности переноса модели в ONNX. Практическая ценность — это бесплатное решение на русском языке, единственное с поддержкой Windows, с открытым исходным кодом.");

// ---------- Slide 12 — Спасибо ----------
slide(12, (s) => {
  s.background = { color: NAVY };
  s.addText("Спасибо за внимание!", { x: 0.8, y: 2.7, w: 11.7, h: 1.2, fontFace: HEAD, fontSize: 44, bold: true, color: "FFFFFF", align: "center" });
  s.addText("Готов ответить на ваши вопросы", { x: 0.8, y: 4.0, w: 11.7, h: 0.7, fontFace: BODY, fontSize: 20, color: ICE, align: "center" });
  s.addText("FitApp · ВКР · МТУСИ · Москва, 2026", { x: 0.8, y: 6.7, w: 11.7, h: 0.4, fontFace: BODY, fontSize: 13, color: MUTE, align: "center" });
},
"На этом доклад завершён. Спасибо за внимание, готов ответить на ваши вопросы.");

Promise.all(decks).then(() => console.log("ALL DONE: 12 files"));
