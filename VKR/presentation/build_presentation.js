// Генератор презентации защиты ВКР FitApp
// Запуск: node build_presentation.js
// Требует: npm install -g pptxgenjs

const pptxgen = require("pptxgenjs");

const pres = new pptxgen();
pres.layout = "LAYOUT_16x9"; // 10" × 5.625"
pres.author = "ВКР FitApp";
pres.title = "FitApp — защита ВКР";

// ═══════════════════════════════════════════════════════════
// ЦВЕТОВАЯ ПАЛИТРА (Midnight Executive — академичный стиль)
// ═══════════════════════════════════════════════════════════
const NAVY = "1E2761";       // основной (заголовки, акценты)
const ICE = "CADCFC";        // светло-голубой (плашки)
const WHITE = "FFFFFF";
const TEXT = "1E2761";       // основной текст
const MUTED = "64748B";      // приглушённый
const CORAL = "F96167";      // акцент для ключевых цифр
const TEAL = "028090";       // второй акцент
const LIGHT_BG = "F7F9FC";   // светлый фон карточек

const FONT = "Calibri";
const FONT_TITLE = "Calibri";

// ═══════════════════════════════════════════════════════════
// ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
// ═══════════════════════════════════════════════════════════

// Заголовок слайда + полоска внизу заголовка
function addSlideHeader(slide, title, slideNum) {
  // Тонкая полоса сверху
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0, w: 10, h: 0.35,
    fill: { color: NAVY }, line: { color: NAVY }
  });
  // Текст заголовка
  slide.addText(title, {
    x: 0.4, y: 0.5, w: 8.8, h: 0.55,
    fontSize: 24, bold: true, fontFace: FONT_TITLE,
    color: NAVY, valign: "middle", margin: 0
  });
  // Номер слайда
  slide.addText(String(slideNum), {
    x: 9.2, y: 5.25, w: 0.6, h: 0.3,
    fontSize: 10, color: MUTED, fontFace: FONT,
    align: "right", valign: "middle"
  });
  // Подвал
  slide.addText("FitApp — ВКР, МТУСИ, 2026", {
    x: 0.4, y: 5.25, w: 6, h: 0.3,
    fontSize: 10, color: MUTED, fontFace: FONT,
    align: "left", valign: "middle"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 1 — ТИТУЛЬНЫЙ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: NAVY };

  // Декоративная вертикальная полоса
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0, w: 0.15, h: 5.625,
    fill: { color: CORAL }, line: { color: CORAL }
  });

  // Шапка МТУСИ
  s.addText("Московский технический университет связи и информатики", {
    x: 0.6, y: 0.5, w: 9, h: 0.4,
    fontSize: 14, color: ICE, fontFace: FONT, align: "left"
  });
  s.addText("Выпускная квалификационная работа", {
    x: 0.6, y: 0.9, w: 9, h: 0.35,
    fontSize: 12, color: ICE, italic: true, fontFace: FONT, align: "left"
  });

  // Основной заголовок
  s.addText(
    "Разработка кроссплатформенного приложения\n" +
    "для отслеживания силовых тренировок\n" +
    "с локальной ML-моделью прогнозирования 1ПМ\n" +
    "и облачной синхронизацией",
    {
      x: 0.6, y: 1.7, w: 8.8, h: 2.2,
      fontSize: 26, bold: true, color: WHITE, fontFace: FONT_TITLE,
      align: "left", valign: "top"
    }
  );

  // Автор / руководитель
  s.addText([
    { text: "Студент: ", options: { color: ICE, bold: true } },
    { text: "[ФИО студента]", options: { color: WHITE, breakLine: true } },
    { text: "Группа: ", options: { color: ICE, bold: true } },
    { text: "[номер группы]", options: { color: WHITE, breakLine: true } },
    { text: "Научный руководитель: ", options: { color: ICE, bold: true } },
    { text: "[ФИО, степень]", options: { color: WHITE } }
  ], {
    x: 0.6, y: 4.1, w: 8.8, h: 1.1,
    fontSize: 14, fontFace: FONT, align: "left", valign: "top"
  });

  s.addText("Москва, 2026", {
    x: 0.6, y: 5.2, w: 9, h: 0.3,
    fontSize: 12, color: ICE, fontFace: FONT, italic: true, align: "left"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 2 — АКТУАЛЬНОСТЬ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "Актуальность темы", 2);

  // Левая колонка — текст
  s.addText([
    { text: "Силовой тренинг — массовая практика, требующая систематического учёта нагрузки.", options: { bullet: true, breakLine: true, paraSpaceAfter: 8 } },
    { text: "Ключевые задачи занимающегося — отслеживание прогресса 1ПМ и планирование рабочих весов.", options: { bullet: true, breakLine: true, paraSpaceAfter: 8 } },
    { text: "Существующие приложения (Strong, Hevy, FitNotes, Jefit) закрывают только часть задач: либо без ML, либо без офлайн-режима, либо с подпиской.", options: { bullet: true, breakLine: true, paraSpaceAfter: 8 } },
    { text: "Ни одно из решений не объединяет: офлайн-first, локальное ML-прогнозирование без подписки, планировщик, кроссплатформенность (Android + Windows) и синхронизацию.", options: { bullet: true } }
  ], {
    x: 0.5, y: 1.3, w: 5.7, h: 3.7,
    fontSize: 14, fontFace: FONT, color: TEXT, valign: "top"
  });

  // Правая карточка — «ниша»
  s.addShape(pres.shapes.RECTANGLE, {
    x: 6.5, y: 1.5, w: 3.1, h: 3.3,
    fill: { color: LIGHT_BG }, line: { color: NAVY, width: 1 }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 6.5, y: 1.5, w: 0.08, h: 3.3,
    fill: { color: CORAL }, line: { color: CORAL }
  });
  s.addText("Ниша FitApp", {
    x: 6.75, y: 1.65, w: 2.7, h: 0.4,
    fontSize: 16, bold: true, color: NAVY, fontFace: FONT_TITLE
  });
  s.addText([
    { text: "Offline-first", options: { bullet: true, bold: true, breakLine: true } },
    { text: "ML локально, без подписки", options: { bullet: true, bold: true, breakLine: true } },
    { text: "Планировщик нагрузки", options: { bullet: true, bold: true, breakLine: true } },
    { text: "Android + Windows", options: { bullet: true, bold: true, breakLine: true } },
    { text: "Синхронизация между устройствами", options: { bullet: true, bold: true } }
  ], {
    x: 6.75, y: 2.15, w: 2.7, h: 2.5,
    fontSize: 13, fontFace: FONT, color: TEXT, valign: "top",
    paraSpaceAfter: 6
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 3 — ЦЕЛЬ И ЗАДАЧИ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "Цель и задачи работы", 3);

  // Цель — выделенный блок
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.4, y: 1.25, w: 9.2, h: 0.85,
    fill: { color: NAVY }, line: { color: NAVY }
  });
  s.addText([
    { text: "Цель работы: ", options: { bold: true, color: ICE } },
    { text: "разработка кроссплатформенного приложения для учёта силовых тренировок с локальным ML-прогнозированием 1ПМ, планировщиком нагрузки и облачной синхронизацией.", options: { color: WHITE } }
  ], {
    x: 0.6, y: 1.3, w: 8.8, h: 0.75,
    fontSize: 14, fontFace: FONT, valign: "middle", margin: 0
  });

  // Задачи
  s.addText("Задачи:", {
    x: 0.4, y: 2.3, w: 9, h: 0.35,
    fontSize: 16, bold: true, color: NAVY, fontFace: FONT_TITLE
  });

  s.addText([
    { text: "Проанализировать предметную область и существующие аналоги.", options: { bullet: { type: "number" }, breakLine: true, paraSpaceAfter: 6 } },
    { text: "Спроектировать архитектуру клиента (MVVM, .NET MAUI) и серверной части (ASP.NET Core 9, PostgreSQL).", options: { bullet: { type: "number" }, breakLine: true, paraSpaceAfter: 6 } },
    { text: "Реализовать клиентское приложение: дневник, шаблоны, статистику, каталог упражнений.", options: { bullet: { type: "number" }, breakLine: true, paraSpaceAfter: 6 } },
    { text: "Обучить ML-модель прогнозирования 1ПМ (LightGBM) и интегрировать её в приложение через ONNX Runtime.", options: { bullet: { type: "number" }, breakLine: true, paraSpaceAfter: 6 } },
    { text: "Реализовать протокол облачной синхронизации с разрешением конфликтов (Last-Write-Wins).", options: { bullet: { type: "number" }, breakLine: true, paraSpaceAfter: 6 } },
    { text: "Провести функциональное и интеграционное тестирование на Windows и Android.", options: { bullet: { type: "number" } } }
  ], {
    x: 0.6, y: 2.7, w: 9, h: 2.5,
    fontSize: 14, fontFace: FONT, color: TEXT, valign: "top"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 4 — СРАВНИТЕЛЬНЫЙ АНАЛИЗ АНАЛОГОВ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "Сравнительный анализ аналогов", 4);

  // Заголовок таблицы
  const headerRow = [
    { text: "Критерий", options: { bold: true, color: WHITE, fill: { color: NAVY }, fontSize: 11 } },
    { text: "Strong", options: { bold: true, color: WHITE, fill: { color: NAVY }, fontSize: 11, align: "center" } },
    { text: "Hevy", options: { bold: true, color: WHITE, fill: { color: NAVY }, fontSize: 11, align: "center" } },
    { text: "FitNotes", options: { bold: true, color: WHITE, fill: { color: NAVY }, fontSize: 11, align: "center" } },
    { text: "Jefit", options: { bold: true, color: WHITE, fill: { color: NAVY }, fontSize: 11, align: "center" } },
    { text: "FitApp", options: { bold: true, color: WHITE, fill: { color: CORAL }, fontSize: 11, align: "center" } }
  ];

  const yes = { text: "✓", options: { align: "center", color: "16A34A", bold: true } };
  const no  = { text: "—", options: { align: "center", color: MUTED } };
  const yesA = { text: "✓", options: { align: "center", color: WHITE, bold: true, fill: { color: CORAL } } };

  const tableData = [
    headerRow,
    ["Поддержка Windows", no, no, no, no, yesA],
    ["Offline-first", yes, yes, yes, yes, yesA],
    ["Поддержка RPE", { text: "частично", options: { align: "center", color: MUTED } }, yes, no, no, yesA],
    ["ML-прогноз 1ПМ", no, no, no, no, yesA],
    ["Планировщик нагрузки", { text: "платно", options: { align: "center", color: MUTED } }, no, no, no, yesA],
    ["Облачная синхронизация", { text: "iCloud", options: { align: "center", color: MUTED } }, yes, no, yes, yesA],
    ["Модель монетизации",
      { text: "Усл.-бесплатная", options: { align: "center", fontSize: 10 } },
      { text: "Усл.-бесплатная", options: { align: "center", fontSize: 10 } },
      { text: "Бесплатная", options: { align: "center", fontSize: 10 } },
      { text: "Усл.-бесплатная", options: { align: "center", fontSize: 10 } },
      { text: "Open Source", options: { align: "center", fontSize: 10, bold: true, fill: { color: ICE } } }
    ]
  ];

  s.addTable(tableData, {
    x: 0.3, y: 1.2, w: 9.4, colW: [2.4, 1.4, 1.4, 1.4, 1.4, 1.4],
    fontFace: FONT, fontSize: 12, color: TEXT,
    border: { type: "solid", pt: 0.5, color: "CBD5E1" },
    valign: "middle", autoPage: false
  });

  // Вывод
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.3, y: 4.7, w: 9.4, h: 0.5,
    fill: { color: LIGHT_BG }, line: { color: NAVY, width: 0.75 }
  });
  s.addText([
    { text: "Вывод: ", options: { bold: true, color: NAVY } },
    { text: "FitApp — единственное решение, объединяющее все ключевые функции в одном бесплатном open-source продукте.", options: { color: TEXT } }
  ], {
    x: 0.45, y: 4.72, w: 9.15, h: 0.45,
    fontSize: 12, fontFace: FONT, valign: "middle", margin: 0
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 5 — АРХИТЕКТУРА СИСТЕМЫ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "Архитектура системы", 5);

  // ─── Клиент ───
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.4, y: 1.2, w: 4.5, h: 3.7,
    fill: { color: "DAE8FC" }, line: { color: "6C8EBF", width: 1.5 }
  });
  s.addText("Клиент (.NET MAUI 9)", {
    x: 0.4, y: 1.25, w: 4.5, h: 0.4,
    fontSize: 14, bold: true, color: NAVY, fontFace: FONT_TITLE, align: "center"
  });

  // MVVM
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.6, y: 1.75, w: 4.1, h: 0.75,
    fill: { color: "FFF2CC" }, line: { color: "D6B656" }
  });
  s.addText("MVVM: Views · ViewModels · Models", {
    x: 0.6, y: 1.75, w: 4.1, h: 0.75,
    fontSize: 12, bold: true, color: TEXT, fontFace: FONT, align: "center", valign: "middle"
  });

  // Сервисы
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.6, y: 2.6, w: 4.1, h: 1.05,
    fill: { color: "D5E8D4" }, line: { color: "82B366" }
  });
  s.addText("Сервисный слой", {
    x: 0.6, y: 2.62, w: 4.1, h: 0.3,
    fontSize: 11, bold: true, color: TEXT, fontFace: FONT, align: "center"
  });
  s.addText("OnnxPredictionService · WorkoutPlannerService\nSyncService · AuthClient", {
    x: 0.6, y: 2.9, w: 4.1, h: 0.75,
    fontSize: 10, color: TEXT, fontFace: FONT, align: "center", valign: "top"
  });

  // Хранилище
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.6, y: 3.75, w: 4.1, h: 1.05,
    fill: { color: "F8CECC" }, line: { color: "B85450" }
  });
  s.addText("Локальное хранилище", {
    x: 0.6, y: 3.77, w: 4.1, h: 0.3,
    fontSize: 11, bold: true, color: TEXT, fontFace: FONT, align: "center"
  });
  s.addText("SQLite · ONNX-модель (1,5 МБ) · SecureStorage", {
    x: 0.6, y: 4.05, w: 4.1, h: 0.7,
    fontSize: 10, color: TEXT, fontFace: FONT, align: "center", valign: "top"
  });

  // ─── Стрелка клиент ↔ сервер ───
  s.addShape(pres.shapes.LINE, {
    x: 4.9, y: 2.1, w: 0.7, h: 0,
    line: { color: NAVY, width: 2, endArrowType: "triangle", beginArrowType: "triangle" }
  });
  s.addText("HTTPS\nJWT", {
    x: 4.85, y: 1.6, w: 0.85, h: 0.45,
    fontSize: 9, color: NAVY, italic: true, fontFace: FONT, align: "center"
  });

  // ─── Сервер ───
  s.addShape(pres.shapes.RECTANGLE, {
    x: 5.7, y: 1.2, w: 4.0, h: 2.2,
    fill: { color: "E1D5E7" }, line: { color: "9673A6", width: 1.5 }
  });
  s.addText("Сервер (ASP.NET Core 9)", {
    x: 5.7, y: 1.25, w: 4.0, h: 0.4,
    fontSize: 14, bold: true, color: NAVY, fontFace: FONT_TITLE, align: "center"
  });
  s.addText([
    { text: "POST /auth/register", options: { breakLine: true } },
    { text: "POST /auth/login", options: { breakLine: true } },
    { text: "POST /sync  [Bearer JWT]", options: { breakLine: true, bold: true } },
    { text: " ", options: { breakLine: true } },
    { text: "BCrypt · JWT HS256", options: { italic: true, color: MUTED } }
  ], {
    x: 5.9, y: 1.7, w: 3.7, h: 1.6,
    fontSize: 11, fontFace: FONT, color: TEXT, valign: "top"
  });

  // ─── PostgreSQL / Neon ───
  s.addShape(pres.shapes.LINE, {
    x: 7.7, y: 3.45, w: 0, h: 0.4,
    line: { color: NAVY, width: 2, endArrowType: "triangle", beginArrowType: "triangle" }
  });
  s.addShape(pres.shapes.OVAL, {
    x: 6.2, y: 3.9, w: 3.0, h: 0.95,
    fill: { color: "F5F5F5" }, line: { color: "555555", width: 1.5 }
  });
  s.addText("PostgreSQL / Neon (serverless)", {
    x: 6.2, y: 3.95, w: 3.0, h: 0.85,
    fontSize: 12, bold: true, color: TEXT, fontFace: FONT, align: "center", valign: "middle"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 6 — СТЕК ТЕХНОЛОГИЙ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "Стек технологий", 6);

  // 4 карточки в одном ряду
  const cards = [
    { title: "Клиент",   items: [".NET MAUI 9", "C# / XAML", "CommunityToolkit.Mvvm", "SQLite (sqlite-net-pcl)", "LiveChartsCore"], color: "DAE8FC", border: "6C8EBF" },
    { title: "Сервер",   items: ["ASP.NET Core 9", "Npgsql (без EF)", "JWT HS256 + BCrypt", "Docker (multi-stage)", "Render.com Free"], color: "E1D5E7", border: "9673A6" },
    { title: "База данных", items: ["Локально: SQLite", "Облако: PostgreSQL", "Хостинг: Neon serverless", "Постоянное хранилище 512 МБ"], color: "F8CECC", border: "B85450" },
    { title: "ML-инфраструктура", items: ["Python 3.11", "LightGBM", "ONNX (cross-platform)", "Microsoft.ML.OnnxRuntime 1.20.1"], color: "D5E8D4", border: "82B366" }
  ];

  cards.forEach((card, i) => {
    const x = 0.3 + i * 2.4;
    const w = 2.25;
    s.addShape(pres.shapes.RECTANGLE, {
      x, y: 1.3, w, h: 3.7,
      fill: { color: card.color }, line: { color: card.border, width: 1.5 }
    });
    s.addText(card.title, {
      x, y: 1.4, w, h: 0.45,
      fontSize: 14, bold: true, color: NAVY, fontFace: FONT_TITLE, align: "center"
    });
    s.addText(card.items.map((it, j) => ({
      text: it,
      options: { bullet: true, breakLine: j < card.items.length - 1, paraSpaceAfter: 4 }
    })), {
      x: x + 0.15, y: 1.95, w: w - 0.3, h: 2.95,
      fontSize: 11, fontFace: FONT, color: TEXT, valign: "top"
    });
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 7 — ML-МОДУЛЬ: ДАТАСЕТ И ПРИЗНАКИ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "ML-модуль: датасет и признаки", 7);

  // Левая колонка — статистика датасета
  s.addText("Данные обучения", {
    x: 0.4, y: 1.25, w: 4.7, h: 0.4,
    fontSize: 16, bold: true, color: NAVY, fontFace: FONT_TITLE
  });

  // Большие плашки-числа
  const stats = [
    { num: "763 322", label: "тренировочных подходов" },
    { num: "500",     label: "спортсменов" },
    { num: "118 350", label: "обучающих пар (X → y)" }
  ];
  stats.forEach((st, i) => {
    const y = 1.75 + i * 1.05;
    s.addShape(pres.shapes.RECTANGLE, {
      x: 0.4, y, w: 4.7, h: 0.95,
      fill: { color: LIGHT_BG }, line: { color: NAVY, width: 0.75 }
    });
    s.addShape(pres.shapes.RECTANGLE, {
      x: 0.4, y, w: 0.08, h: 0.95,
      fill: { color: CORAL }, line: { color: CORAL }
    });
    s.addText(st.num, {
      x: 0.55, y: y + 0.05, w: 2.0, h: 0.85,
      fontSize: 28, bold: true, color: NAVY, fontFace: FONT_TITLE,
      valign: "middle", align: "left", margin: 0
    });
    s.addText(st.label, {
      x: 2.6, y: y + 0.05, w: 2.5, h: 0.85,
      fontSize: 12, color: TEXT, fontFace: FONT,
      valign: "middle", align: "left", margin: 0
    });
  });

  // Правая колонка — группы признаков
  s.addText("29 признаков, 4 группы", {
    x: 5.4, y: 1.25, w: 4.3, h: 0.4,
    fontSize: 16, bold: true, color: NAVY, fontFace: FONT_TITLE
  });

  const groups = [
    { name: "История",           count: "13", desc: "лаги 1ПМ, скользящие средние, тренд" },
    { name: "Текущая тренировка", count: "7",  desc: "топ-вес, повторы, RPE, объём" },
    { name: "Профиль",            count: "5",  desc: "вес тела, возраст, пол, стаж" },
    { name: "Упражнение",         count: "4",  desc: "тип, оборудование, изоляция" }
  ];
  groups.forEach((g, i) => {
    const y = 1.75 + i * 0.75;
    s.addShape(pres.shapes.RECTANGLE, {
      x: 5.4, y, w: 4.3, h: 0.65,
      fill: { color: WHITE }, line: { color: TEAL, width: 1 }
    });
    s.addText(g.count, {
      x: 5.45, y, w: 0.6, h: 0.65,
      fontSize: 18, bold: true, color: TEAL, fontFace: FONT_TITLE,
      align: "center", valign: "middle", margin: 0
    });
    s.addText([
      { text: g.name, options: { bold: true, color: NAVY, breakLine: true } },
      { text: g.desc, options: { color: MUTED, fontSize: 10 } }
    ], {
      x: 6.1, y: y + 0.02, w: 3.55, h: 0.6,
      fontSize: 11, fontFace: FONT, valign: "middle"
    });
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 8 — ML-МОДУЛЬ: РЕЗУЛЬТАТЫ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "ML-модуль: результаты обучения", 8);

  // Гистограмма MAE
  s.addChart(pres.charts.BAR, [{
    name: "MAE (кг)",
    labels: ["Naive baseline", "LinearRegression", "LightGBM"],
    values: [2.359, 2.154, 1.437]
  }], {
    x: 0.4, y: 1.2, w: 5.5, h: 3.6,
    barDir: "col",
    chartColors: ["94A3B8", "94A3B8", "F96167"],
    chartArea: { fill: { color: WHITE } },
    plotArea:  { fill: { color: WHITE } },
    catAxisLabelFontFace: FONT, catAxisLabelFontSize: 11, catAxisLabelColor: TEXT,
    valAxisLabelFontFace: FONT, valAxisLabelFontSize: 10, valAxisLabelColor: MUTED,
    valGridLine: { color: "E2E8F0", size: 0.5 },
    catGridLine: { style: "none" },
    showValue: true,
    dataLabelFontFace: FONT, dataLabelFontSize: 11, dataLabelColor: TEXT,
    dataLabelPosition: "outEnd",
    showLegend: false,
    showTitle: true, title: "Средняя ошибка прогноза 1ПМ (MAE, кг)",
    titleFontFace: FONT_TITLE, titleFontSize: 13, titleColor: NAVY
  });

  // Правая часть — итог
  s.addShape(pres.shapes.RECTANGLE, {
    x: 6.2, y: 1.4, w: 3.5, h: 1.4,
    fill: { color: NAVY }, line: { color: NAVY }
  });
  s.addText([
    { text: "−39 %", options: { bold: true, fontSize: 36, color: WHITE, breakLine: true } },
    { text: "снижение MAE\nLightGBM vs Naive", options: { color: ICE, fontSize: 12 } }
  ], {
    x: 6.3, y: 1.45, w: 3.3, h: 1.3,
    fontFace: FONT_TITLE, align: "center", valign: "middle", margin: 0
  });

  // Соответствие ONNX
  s.addShape(pres.shapes.RECTANGLE, {
    x: 6.2, y: 2.95, w: 3.5, h: 1.85,
    fill: { color: LIGHT_BG }, line: { color: NAVY, width: 0.75 }
  });
  s.addText("Соответствие пайплайнов", {
    x: 6.3, y: 3.05, w: 3.3, h: 0.3,
    fontSize: 13, bold: true, color: NAVY, fontFace: FONT_TITLE
  });
  s.addText([
    { text: "Python (LightGBM) ↔ C# (ONNX Runtime):", options: { breakLine: true, paraSpaceAfter: 4 } },
    { text: "медиана расхождения:", options: { color: MUTED, breakLine: true } },
    { text: "1,6 × 10⁻⁵ кг", options: { bold: true, color: CORAL, fontSize: 14, breakLine: true, paraSpaceAfter: 4 } },
    { text: "p99 расхождения:", options: { color: MUTED, breakLine: true } },
    { text: "0,082 кг", options: { bold: true, color: CORAL, fontSize: 14 } }
  ], {
    x: 6.3, y: 3.4, w: 3.3, h: 1.4,
    fontSize: 11, fontFace: FONT, color: TEXT, valign: "top"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 9 — ОБЛАЧНАЯ СИНХРОНИЗАЦИЯ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "Облачная синхронизация", 9);

  // Три лайфлайна
  const laneY = 1.3, laneH = 3.1;
  const lanes = [
    { x: 0.9, label: "Клиент\n(.NET MAUI)", color: "DAE8FC", border: "6C8EBF" },
    { x: 4.2, label: "Сервер\n(ASP.NET Core 9)", color: "E1D5E7", border: "9673A6" },
    { x: 7.5, label: "PostgreSQL\n(Neon)", color: "F5F5F5", border: "666666" }
  ];
  lanes.forEach(l => {
    s.addShape(pres.shapes.RECTANGLE, {
      x: l.x, y: laneY, w: 1.7, h: 0.55,
      fill: { color: l.color }, line: { color: l.border, width: 1 }
    });
    s.addText(l.label, {
      x: l.x, y: laneY, w: 1.7, h: 0.55,
      fontSize: 11, bold: true, color: TEXT, fontFace: FONT, align: "center", valign: "middle"
    });
    // вертикальная пунктирная линия жизни
    s.addShape(pres.shapes.LINE, {
      x: l.x + 0.85, y: laneY + 0.55, w: 0, h: laneH,
      line: { color: l.border, width: 0.75, dashType: "dash" }
    });
  });

  // Стрелки шагов
  const steps = [
    { y: 2.15, from: 0, to: 1, label: "1. POST /sync  { pushBatch, lastSyncUtc }", solid: true },
    { y: 2.75, from: 1, to: 2, label: "2. UPSERT (LWW: UpdatedAt)", solid: true },
    { y: 3.35, from: 2, to: 1, label: "3. COMMIT", solid: false },
    { y: 3.95, from: 1, to: 0, label: "4. HTTP 200 { pullBatch, serverTimeUtc }", solid: false }
  ];
  steps.forEach(st => {
    const x1 = lanes[st.from].x + 0.85;
    const x2 = lanes[st.to].x + 0.85;
    s.addShape(pres.shapes.LINE, {
      x: Math.min(x1, x2), y: st.y, w: Math.abs(x2 - x1), h: 0,
      line: {
        color: NAVY, width: 1.5,
        dashType: st.solid ? "solid" : "dash",
        endArrowType: x2 > x1 ? "triangle" : "none",
        beginArrowType: x2 < x1 ? "triangle" : "none"
      }
    });
    s.addText(st.label, {
      x: Math.min(x1, x2), y: st.y - 0.3, w: Math.abs(x2 - x1), h: 0.25,
      fontSize: 10, color: TEXT, fontFace: FONT, align: "center", valign: "middle", margin: 0
    });
  });

  // Подпись внизу
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.4, y: 4.7, w: 9.2, h: 0.5,
    fill: { color: LIGHT_BG }, line: { color: NAVY, width: 0.75 }
  });
  s.addText([
    { text: "LWW: ", options: { bold: true, color: NAVY } },
    { text: "ON CONFLICT(SyncId) DO UPDATE WHERE excluded.UpdatedAt > existing.UpdatedAt", options: { color: TEXT, fontSize: 11 } }
  ], {
    x: 0.55, y: 4.72, w: 9.05, h: 0.45,
    fontSize: 12, fontFace: FONT, valign: "middle", margin: 0
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 10 — ТЕСТИРОВАНИЕ И РЕЗУЛЬТАТЫ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "Тестирование и результаты", 10);

  // Левая колонка — функциональное и интеграционное
  s.addText("Функциональное тестирование", {
    x: 0.4, y: 1.25, w: 4.7, h: 0.35,
    fontSize: 14, bold: true, color: NAVY, fontFace: FONT_TITLE
  });
  s.addText([
    { text: "24 тестовых случая", options: { bullet: true, breakLine: true } },
    { text: "8 ключевых сценариев — все пройдены", options: { bullet: true, breakLine: true } },
    { text: "Платформы: Windows 10 + Android 12", options: { bullet: true } }
  ], {
    x: 0.5, y: 1.6, w: 4.6, h: 1.1,
    fontSize: 12, fontFace: FONT, color: TEXT, valign: "top", paraSpaceAfter: 4
  });

  s.addText("Интеграционная синхронизация", {
    x: 0.4, y: 2.85, w: 4.7, h: 0.35,
    fontSize: 14, bold: true, color: NAVY, fontFace: FONT_TITLE
  });
  s.addText([
    { text: "Базовая синхронизация двух устройств", options: { bullet: true, breakLine: true } },
    { text: "Конфликт UpdatedAt (LWW)", options: { bullet: true, breakLine: true } },
    { text: "Мягкое удаление", options: { bullet: true, breakLine: true } },
    { text: "Смена аккаунта + пересинхронизация", options: { bullet: true } }
  ], {
    x: 0.5, y: 3.2, w: 4.6, h: 1.6,
    fontSize: 12, fontFace: FONT, color: TEXT, valign: "top", paraSpaceAfter: 4
  });

  // Правая колонка — производительность
  s.addText("Производительность", {
    x: 5.3, y: 1.25, w: 4.4, h: 0.35,
    fontSize: 14, bold: true, color: NAVY, fontFace: FONT_TITLE
  });

  // Таблица производительности
  const perfTable = [
    [
      { text: "Метрика", options: { bold: true, fill: { color: NAVY }, color: WHITE, fontSize: 11 } },
      { text: "Win", options: { bold: true, fill: { color: NAVY }, color: WHITE, fontSize: 11, align: "center" } },
      { text: "Android", options: { bold: true, fill: { color: NAVY }, color: WHITE, fontSize: 11, align: "center" } }
    ],
    [
      "Загрузка списка тренировок",
      { text: "42 мс", options: { align: "center" } },
      { text: "78 мс", options: { align: "center" } }
    ],
    [
      "Inference ML (1 упражнение)",
      { text: "4 мс", options: { align: "center", bold: true, color: CORAL } },
      { text: "8 мс", options: { align: "center", bold: true, color: CORAL } }
    ],
    [
      "Init ONNX-сервиса",
      { text: "87 мс", options: { align: "center" } },
      { text: "143 мс", options: { align: "center" } }
    ],
    [
      "Размер APK",
      { text: "—", options: { align: "center" } },
      { text: "38 МБ", options: { align: "center", bold: true } }
    ]
  ];
  s.addTable(perfTable, {
    x: 5.3, y: 1.65, w: 4.4, colW: [2.4, 1.0, 1.0],
    fontFace: FONT, fontSize: 11, color: TEXT,
    border: { type: "solid", pt: 0.5, color: "CBD5E1" },
    valign: "middle"
  });

  // Карточка UX
  s.addShape(pres.shapes.RECTANGLE, {
    x: 5.3, y: 3.85, w: 4.4, h: 1.0,
    fill: { color: LIGHT_BG }, line: { color: NAVY, width: 0.75 }
  });
  s.addText([
    { text: "Пилотное UX-тестирование: ", options: { bold: true, color: NAVY, breakLine: true } },
    { text: "6 добровольцев. Отмечены: интуитивный ввод, полезность подсказки прогноза 1ПМ, удобство применения рекомендации.", options: { color: TEXT } }
  ], {
    x: 5.45, y: 3.9, w: 4.15, h: 0.9,
    fontSize: 11, fontFace: FONT, valign: "top"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 11 — ВЫВОДЫ / НАУЧНО-ПРАКТИЧЕСКАЯ ЗНАЧИМОСТЬ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addSlideHeader(s, "Выводы", 11);

  // Цель достигнута — крупная плашка
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.4, y: 1.25, w: 9.2, h: 0.7,
    fill: { color: NAVY }, line: { color: NAVY }
  });
  s.addText([
    { text: "✓ Цель работы достигнута. ", options: { bold: true, color: ICE } },
    { text: "Все поставленные задачи выполнены, требования соблюдены.", options: { color: WHITE } }
  ], {
    x: 0.6, y: 1.3, w: 8.8, h: 0.6,
    fontSize: 14, fontFace: FONT, valign: "middle", margin: 0
  });

  // 3 столбца результатов
  const cols = [
    {
      title: "Реализовано",
      items: [
        "Клиент: .NET MAUI 9, MVVM, 7 экранов",
        "Сервер: ASP.NET Core 9 на Render",
        "ML: LightGBM → ONNX (MAE 1,44 кг)",
        "Sync: LWW + offline-first"
      ],
      color: NAVY
    },
    {
      title: "Научная значимость",
      items: [
        "Адаптация формульного подхода Эпли и ML",
        "Решение проблемы clock skew",
        "Валидация ONNX-портации (Δ < 0,1 кг)"
      ],
      color: TEAL
    },
    {
      title: "Практическая ценность",
      items: [
        "Бесплатное решение для русскоязычных пользователей",
        "Единственное приложение с поддержкой Windows",
        "Open Source, расширяемое сообществом"
      ],
      color: CORAL
    }
  ];
  cols.forEach((col, i) => {
    const x = 0.4 + i * 3.1;
    s.addShape(pres.shapes.RECTANGLE, {
      x, y: 2.15, w: 2.95, h: 0.55,
      fill: { color: col.color }, line: { color: col.color }
    });
    s.addText(col.title, {
      x, y: 2.15, w: 2.95, h: 0.55,
      fontSize: 14, bold: true, color: WHITE, fontFace: FONT_TITLE,
      align: "center", valign: "middle"
    });
    s.addShape(pres.shapes.RECTANGLE, {
      x, y: 2.7, w: 2.95, h: 2.15,
      fill: { color: WHITE }, line: { color: col.color, width: 1 }
    });
    s.addText(col.items.map((it, j) => ({
      text: it,
      options: { bullet: true, breakLine: j < col.items.length - 1, paraSpaceAfter: 6 }
    })), {
      x: x + 0.15, y: 2.8, w: 2.65, h: 2.0,
      fontSize: 11, fontFace: FONT, color: TEXT, valign: "top"
    });
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 12 — СПАСИБО ЗА ВНИМАНИЕ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: NAVY };

  // Декоративная коралловая полоса слева
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0, w: 0.15, h: 5.625,
    fill: { color: CORAL }, line: { color: CORAL }
  });

  s.addText("Спасибо за внимание!", {
    x: 0.6, y: 2.0, w: 8.8, h: 1.0,
    fontSize: 48, bold: true, color: WHITE, fontFace: FONT_TITLE,
    align: "center", valign: "middle"
  });

  s.addText("Готов ответить на ваши вопросы", {
    x: 0.6, y: 3.1, w: 8.8, h: 0.6,
    fontSize: 20, color: ICE, italic: true, fontFace: FONT,
    align: "center", valign: "middle"
  });

  // Контактная плашка
  s.addShape(pres.shapes.RECTANGLE, {
    x: 2.5, y: 4.3, w: 5, h: 0.7,
    fill: { color: WHITE, transparency: 90 }, line: { color: ICE, width: 1 }
  });
  s.addText([
    { text: "FitApp · ВКР · МТУСИ 2026", options: { color: WHITE, bold: true } }
  ], {
    x: 2.5, y: 4.3, w: 5, h: 0.7,
    fontSize: 14, fontFace: FONT, align: "center", valign: "middle"
  });
}

// ═══════════════════════════════════════════════════════════
// СОХРАНЕНИЕ
// ═══════════════════════════════════════════════════════════
pres.writeFile({ fileName: "presentation_zaschita.pptx" })
  .then(file => console.log("✓ Готово: " + file));
