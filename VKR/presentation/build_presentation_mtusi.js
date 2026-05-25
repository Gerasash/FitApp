// Генератор презентации защиты ВКР FitApp по правилам МТУСИ
// Запуск: node build_presentation_mtusi.js
// Требует: npm install -g pptxgenjs

const pptxgen = require("pptxgenjs");

const pres = new pptxgen();
pres.layout = "LAYOUT_16x9"; // 10" × 5.625"
pres.author = "ВКР FitApp";
pres.title = "FitApp — защита ВКР (МТУСИ)";

// ═══════════════════════════════════════════════════════════
// ПАЛИТРА МТУСИ (из официального шаблона)
// ═══════════════════════════════════════════════════════════
const PURPLE_DARK = "24184E";   // глубокий фиолетовый
const PURPLE      = "372579";   // основной фиолетовый
const PURPLE_MED  = "47309C";   // средний фиолетовый
const PURPLE_LITE = "8C64D8";   // светлый фиолетовый
const PURPLE_BG   = "E8E0F7";   // фон-плашка
const CORAL       = "E85362";   // акцент 1 — коралл
const ORANGE      = "FA815F";   // акцент 2 — оранжевый
const YELLOW      = "FEED01";   // акцент 3 — жёлтый (редко)
const WHITE       = "FFFFFF";
const BLACK       = "000000";
const MUTED       = "6B6280";   // приглушённый текст

// ═══════════════════════════════════════════════════════════
// ТИПОГРАФИКА МТУСИ
// ═══════════════════════════════════════════════════════════
const FONT_H = "Montserrat SemiBold"; // для заголовков
const FONT_B = "Montserrat";          // для основного текста
// Размеры по регламенту: главный — 40, заголовок — 32, подзаголовок — 24/20,
// основной текст — 14/16/24, подписи — 12/14

// ═══════════════════════════════════════════════════════════
// ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
// ═══════════════════════════════════════════════════════════

// Стандартный заголовок контентного слайда
function addHeader(slide, title, slideNum, totalSlides) {
  // Боковая фиолетовая полоса
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0, w: 0.25, h: 5.625,
    fill: { color: PURPLE }, line: { color: PURPLE }
  });
  // Коралловая засечка
  slide.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0.4, w: 0.25, h: 0.65,
    fill: { color: CORAL }, line: { color: CORAL }
  });
  // Заголовок (32pt по регламенту)
  slide.addText(title, {
    x: 0.55, y: 0.35, w: 8.8, h: 0.7,
    fontSize: 28, bold: true, fontFace: FONT_H,
    color: PURPLE_DARK, valign: "middle", margin: 0
  });
  // Тонкая разделительная линия под заголовком
  slide.addShape(pres.shapes.LINE, {
    x: 0.55, y: 1.05, w: 9.05, h: 0,
    line: { color: PURPLE_BG, width: 1 }
  });
  // Нижний колонтитул
  slide.addText("FitApp · ВКР · МТУСИ, 2026", {
    x: 0.55, y: 5.3, w: 6, h: 0.25,
    fontSize: 10, color: MUTED, fontFace: FONT_B, italic: true,
    align: "left", valign: "middle"
  });
  slide.addText(`${slideNum} / ${totalSlides}`, {
    x: 8.5, y: 5.3, w: 1.3, h: 0.25,
    fontSize: 10, color: MUTED, fontFace: FONT_B,
    align: "right", valign: "middle"
  });
}

const TOTAL = 12;

// ═══════════════════════════════════════════════════════════
// СЛАЙД 1 — ТИТУЛЬНЫЙ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: PURPLE_DARK };

  // Декоративные коралловые полоски слева (3 штуки разной высоты)
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0, w: 0.6, h: 5.625,
    fill: { color: PURPLE }, line: { color: PURPLE }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 2.5, w: 0.6, h: 0.6,
    fill: { color: CORAL }, line: { color: CORAL }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 3.3, w: 0.6, h: 0.35,
    fill: { color: ORANGE }, line: { color: ORANGE }
  });

  // Шапка
  s.addText("МОСКОВСКИЙ ТЕХНИЧЕСКИЙ УНИВЕРСИТЕТ", {
    x: 1, y: 0.45, w: 8.5, h: 0.3,
    fontSize: 14, bold: true, color: WHITE, fontFace: FONT_H,
    align: "left", charSpacing: 2
  });
  s.addText("СВЯЗИ И ИНФОРМАТИКИ", {
    x: 1, y: 0.75, w: 8.5, h: 0.3,
    fontSize: 14, bold: true, color: WHITE, fontFace: FONT_H,
    align: "left", charSpacing: 2
  });
  s.addText("Выпускная квалификационная работа", {
    x: 1, y: 1.15, w: 8.5, h: 0.3,
    fontSize: 12, color: PURPLE_BG, fontFace: FONT_B, italic: true,
    align: "left"
  });

  // Основной заголовок (40pt по регламенту)
  s.addText(
    "Разработка кроссплатформенного приложения\n" +
    "для отслеживания силовых тренировок\n" +
    "с локальной ML-моделью прогнозирования 1ПМ\n" +
    "и облачной синхронизацией",
    {
      x: 1, y: 1.85, w: 8.5, h: 2.0,
      fontSize: 24, bold: true, color: WHITE, fontFace: FONT_H,
      align: "left", valign: "top",
      paraSpaceAfter: 4
    }
  );

  // Разделитель
  s.addShape(pres.shapes.LINE, {
    x: 1, y: 4.0, w: 2.5, h: 0,
    line: { color: CORAL, width: 3 }
  });

  // Автор + руководитель (24pt по регламенту)
  s.addText([
    { text: "Студент: ",     options: { color: PURPLE_BG, bold: true, fontSize: 13 } },
    { text: "[ФИО студента]", options: { color: WHITE, fontSize: 13, breakLine: true } },
    { text: "Группа: ",      options: { color: PURPLE_BG, bold: true, fontSize: 13 } },
    { text: "[номер группы]", options: { color: WHITE, fontSize: 13, breakLine: true } },
    { text: "Научный руководитель: ", options: { color: PURPLE_BG, bold: true, fontSize: 13 } },
    { text: "[ФИО, степень]", options: { color: WHITE, fontSize: 13 } }
  ], {
    x: 1, y: 4.2, w: 8.5, h: 1.0,
    fontFace: FONT_B, align: "left", valign: "top", paraSpaceAfter: 2
  });

  s.addText("Москва · 2026", {
    x: 1, y: 5.25, w: 8.5, h: 0.3,
    fontSize: 11, color: PURPLE_BG, fontFace: FONT_B, italic: true,
    align: "left"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 2 — АКТУАЛЬНОСТЬ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "Актуальность темы", 2, TOTAL);

  // Левая колонка — текст (24pt по регламенту для основного)
  s.addText([
    { text: "Силовой тренинг", options: { bold: true, color: PURPLE } },
    { text: " — массовая практика, требующая систематического учёта нагрузки.", options: { color: PURPLE_DARK, breakLine: true, paraSpaceAfter: 10 } },
    { text: "Ключевые задачи занимающегося — отслеживание прогресса ", options: { color: PURPLE_DARK } },
    { text: "1ПМ", options: { bold: true, color: CORAL } },
    { text: " и планирование рабочих весов.", options: { color: PURPLE_DARK, breakLine: true, paraSpaceAfter: 10 } },
    { text: "Существующие приложения (Strong, Hevy, FitNotes, Jefit) закрывают лишь часть задач: либо без ML, либо без офлайн-режима, либо с подпиской.", options: { color: PURPLE_DARK } }
  ], {
    x: 0.55, y: 1.3, w: 5.6, h: 3.8,
    fontSize: 14, fontFace: FONT_B, valign: "top"
  });

  // Правая карточка — «Ниша FitApp»
  s.addShape(pres.shapes.RECTANGLE, {
    x: 6.4, y: 1.4, w: 3.2, h: 3.6,
    fill: { color: PURPLE_BG }, line: { color: PURPLE, width: 1 }
  });
  // Цветная шапка карточки
  s.addShape(pres.shapes.RECTANGLE, {
    x: 6.4, y: 1.4, w: 3.2, h: 0.5,
    fill: { color: PURPLE }, line: { color: PURPLE }
  });
  s.addText("НИША FITAPP", {
    x: 6.4, y: 1.4, w: 3.2, h: 0.5,
    fontSize: 13, bold: true, color: WHITE, fontFace: FONT_H,
    align: "center", valign: "middle", charSpacing: 3
  });

  const niche = [
    "Offline-first архитектура",
    "Локальный ML без подписки",
    "Планировщик нагрузки",
    "Android + Windows",
    "Синхронизация устройств"
  ];
  s.addText(niche.map((t, i) => ({
    text: t,
    options: { bullet: { code: "25A0" }, breakLine: i < niche.length - 1, paraSpaceAfter: 8 }
  })), {
    x: 6.6, y: 2.05, w: 2.8, h: 2.85,
    fontSize: 13, fontFace: FONT_B, bold: true, color: PURPLE_DARK, valign: "top"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 3 — ЦЕЛЬ И ЗАДАЧИ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "Цель и задачи работы", 3, TOTAL);

  // Цель — большой блок
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 1.25, w: 9.05, h: 0.95,
    fill: { color: PURPLE }, line: { color: PURPLE }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 1.25, w: 0.12, h: 0.95,
    fill: { color: CORAL }, line: { color: CORAL }
  });
  s.addText([
    { text: "ЦЕЛЬ РАБОТЫ", options: { bold: true, color: CORAL, fontSize: 11, charSpacing: 3, breakLine: true } },
    { text: "Разработка кроссплатформенного приложения для учёта силовых тренировок с локальным ML-прогнозированием 1ПМ, планировщиком нагрузки и облачной синхронизацией.", options: { color: WHITE, fontSize: 13 } }
  ], {
    x: 0.85, y: 1.3, w: 8.75, h: 0.85,
    fontFace: FONT_B, valign: "middle", margin: 0
  });

  // Задачи — 6 нумерованных карточек 3x2
  const tasks = [
    "Проанализировать предметную область и аналоги",
    "Спроектировать архитектуру клиента и сервера",
    "Реализовать клиент: дневник, шаблоны, статистику",
    "Обучить ML-модель 1ПМ (LightGBM → ONNX)",
    "Реализовать протокол синхронизации (LWW)",
    "Провести функциональное и интеграционное тестирование"
  ];
  const cellW = 2.95, cellH = 1.35, gap = 0.1;
  tasks.forEach((t, i) => {
    const col = i % 3, row = Math.floor(i / 3);
    const x = 0.55 + col * (cellW + gap);
    const y = 2.45 + row * (cellH + gap);
    // Карточка
    s.addShape(pres.shapes.RECTANGLE, {
      x, y, w: cellW, h: cellH,
      fill: { color: PURPLE_BG }, line: { color: PURPLE_LITE, width: 0.5 }
    });
    // Кружок с номером
    s.addShape(pres.shapes.OVAL, {
      x: x + 0.15, y: y + 0.15, w: 0.45, h: 0.45,
      fill: { color: CORAL }, line: { color: CORAL }
    });
    s.addText(String(i + 1), {
      x: x + 0.15, y: y + 0.15, w: 0.45, h: 0.45,
      fontSize: 16, bold: true, color: WHITE, fontFace: FONT_H,
      align: "center", valign: "middle", margin: 0
    });
    // Текст задачи
    s.addText(t, {
      x: x + 0.7, y: y + 0.1, w: cellW - 0.8, h: cellH - 0.2,
      fontSize: 11, color: PURPLE_DARK, fontFace: FONT_B,
      valign: "middle", margin: 0
    });
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 4 — СРАВНИТЕЛЬНЫЙ АНАЛИЗ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "Сравнительный анализ аналогов", 4, TOTAL);

  // Таблица
  const H = (t, bg) => ({ text: t, options: { bold: true, color: WHITE, fill: { color: bg }, fontSize: 11, fontFace: FONT_H, align: "center" } });
  const cell = (t, opts = {}) => ({ text: t, options: { color: PURPLE_DARK, fontSize: 10, fontFace: FONT_B, align: "center", ...opts } });
  const yes  = { text: "✓", options: { align: "center", color: "0FA958", bold: true, fontSize: 13, fontFace: FONT_H } };
  const no   = { text: "—", options: { align: "center", color: MUTED, fontSize: 12, fontFace: FONT_B } };
  const yesA = { text: "✓", options: { align: "center", color: WHITE, bold: true, fontSize: 13, fontFace: FONT_H, fill: { color: CORAL } } };

  const tableData = [
    [
      H("Критерий", PURPLE),
      H("Strong", PURPLE),
      H("Hevy", PURPLE),
      H("FitNotes", PURPLE),
      H("Jefit", PURPLE),
      H("FitApp", CORAL)
    ],
    [cell("Поддержка Windows", { align: "left" }), no, no, no, no, yesA],
    [cell("Offline-first", { align: "left" }), yes, yes, yes, yes, yesA],
    [cell("Поддержка RPE", { align: "left" }), cell("частично"), yes, no, no, yesA],
    [cell("ML-прогноз 1ПМ", { align: "left", bold: true }), no, no, no, no, yesA],
    [cell("Планировщик нагрузки", { align: "left", bold: true }), cell("платно"), no, no, no, yesA],
    [cell("Облачная синхронизация", { align: "left" }), cell("iCloud"), yes, no, yes, yesA],
    [
      cell("Модель монетизации", { align: "left" }),
      cell("Усл.-бесплатная"),
      cell("Усл.-бесплатная"),
      cell("Бесплатная"),
      cell("Усл.-бесплатная"),
      cell("Open Source", { bold: true, fill: { color: PURPLE_BG } })
    ]
  ];

  s.addTable(tableData, {
    x: 0.55, y: 1.25, w: 9.05, colW: [2.45, 1.32, 1.32, 1.32, 1.32, 1.32],
    border: { type: "solid", pt: 0.5, color: "D5D0E0" },
    valign: "middle", autoPage: false
  });

  // Вывод
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 4.7, w: 9.05, h: 0.5,
    fill: { color: PURPLE_BG }, line: { color: PURPLE, width: 0.5 }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 4.7, w: 0.1, h: 0.5,
    fill: { color: CORAL }, line: { color: CORAL }
  });
  s.addText([
    { text: "Вывод: ", options: { bold: true, color: PURPLE } },
    { text: "FitApp — единственное решение, объединяющее все ключевые функции в одном бесплатном open-source продукте.", options: { color: PURPLE_DARK } }
  ], {
    x: 0.8, y: 4.72, w: 8.75, h: 0.45,
    fontSize: 11, fontFace: FONT_B, valign: "middle", margin: 0
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 5 — АРХИТЕКТУРА
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "Архитектура системы", 5, TOTAL);

  // ── Клиент ──
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 1.25, w: 4.3, h: 3.85,
    fill: { color: WHITE }, line: { color: PURPLE, width: 2 }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 1.25, w: 4.3, h: 0.5,
    fill: { color: PURPLE }, line: { color: PURPLE }
  });
  s.addText("КЛИЕНТ · .NET MAUI 9", {
    x: 0.55, y: 1.25, w: 4.3, h: 0.5,
    fontSize: 13, bold: true, color: WHITE, fontFace: FONT_H,
    align: "center", valign: "middle", charSpacing: 2
  });

  // Слой MVVM
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.75, y: 1.95, w: 3.9, h: 0.65,
    fill: { color: PURPLE_BG }, line: { color: PURPLE_LITE, width: 0.5 }
  });
  s.addText([
    { text: "MVVM", options: { bold: true, color: PURPLE, fontSize: 11, breakLine: true } },
    { text: "Views · ViewModels · Models", options: { color: PURPLE_DARK, fontSize: 10 } }
  ], {
    x: 0.75, y: 1.95, w: 3.9, h: 0.65,
    fontFace: FONT_B, align: "center", valign: "middle", margin: 0
  });

  // Сервисы
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.75, y: 2.7, w: 3.9, h: 1.1,
    fill: { color: PURPLE_BG }, line: { color: PURPLE_LITE, width: 0.5 }
  });
  s.addText([
    { text: "СЕРВИСНЫЙ СЛОЙ", options: { bold: true, color: PURPLE, fontSize: 11, charSpacing: 2, breakLine: true } },
    { text: "OnnxPredictionService", options: { color: PURPLE_DARK, fontSize: 10, breakLine: true } },
    { text: "WorkoutPlannerService", options: { color: PURPLE_DARK, fontSize: 10, breakLine: true } },
    { text: "SyncService · AuthClient", options: { color: PURPLE_DARK, fontSize: 10 } }
  ], {
    x: 0.75, y: 2.78, w: 3.9, h: 1.0,
    fontFace: FONT_B, align: "center", valign: "top", margin: 0
  });

  // Хранилище
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.75, y: 3.9, w: 3.9, h: 1.1,
    fill: { color: PURPLE_BG }, line: { color: PURPLE_LITE, width: 0.5 }
  });
  s.addText([
    { text: "ЛОКАЛЬНОЕ ХРАНИЛИЩЕ", options: { bold: true, color: PURPLE, fontSize: 11, charSpacing: 2, breakLine: true } },
    { text: "SQLite · ONNX-модель (1,5 МБ)", options: { color: PURPLE_DARK, fontSize: 10, breakLine: true } },
    { text: "SecureStorage (JWT)", options: { color: PURPLE_DARK, fontSize: 10 } }
  ], {
    x: 0.75, y: 3.98, w: 3.9, h: 1.0,
    fontFace: FONT_B, align: "center", valign: "top", margin: 0
  });

  // ── Связь клиент ↔ сервер ──
  s.addShape(pres.shapes.LINE, {
    x: 4.9, y: 2.5, w: 0.7, h: 0,
    line: { color: CORAL, width: 2.5, endArrowType: "triangle", beginArrowType: "triangle" }
  });
  s.addText([
    { text: "HTTPS", options: { color: CORAL, bold: true, fontSize: 9, breakLine: true } },
    { text: "JWT Bearer", options: { color: PURPLE, fontSize: 8 } }
  ], {
    x: 4.85, y: 1.9, w: 0.8, h: 0.5,
    fontFace: FONT_B, align: "center", valign: "middle"
  });

  // ── Сервер ──
  s.addShape(pres.shapes.RECTANGLE, {
    x: 5.65, y: 1.25, w: 4.0, h: 2.2,
    fill: { color: WHITE }, line: { color: PURPLE, width: 2 }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 5.65, y: 1.25, w: 4.0, h: 0.5,
    fill: { color: PURPLE }, line: { color: PURPLE }
  });
  s.addText("СЕРВЕР · ASP.NET Core 9", {
    x: 5.65, y: 1.25, w: 4.0, h: 0.5,
    fontSize: 13, bold: true, color: WHITE, fontFace: FONT_H,
    align: "center", valign: "middle", charSpacing: 2
  });
  s.addText([
    { text: "POST /auth/register", options: { color: PURPLE_DARK, fontSize: 11, breakLine: true, paraSpaceAfter: 3 } },
    { text: "POST /auth/login", options: { color: PURPLE_DARK, fontSize: 11, breakLine: true, paraSpaceAfter: 3 } },
    { text: "POST /sync ", options: { color: PURPLE_DARK, fontSize: 11, bold: true } },
    { text: "[Bearer JWT]", options: { color: CORAL, fontSize: 10, italic: true, breakLine: true, paraSpaceAfter: 3 } },
    { text: "BCrypt · JWT HS256 · 30 дней", options: { color: MUTED, fontSize: 10, italic: true } }
  ], {
    x: 5.9, y: 1.85, w: 3.5, h: 1.5,
    fontFace: FONT_B, valign: "top"
  });

  // ── Стрелка сервер ↔ БД ──
  s.addShape(pres.shapes.LINE, {
    x: 7.65, y: 3.5, w: 0, h: 0.45,
    line: { color: CORAL, width: 2.5, endArrowType: "triangle", beginArrowType: "triangle" }
  });

  // ── БД ──
  s.addShape(pres.shapes.OVAL, {
    x: 6.0, y: 4.0, w: 3.3, h: 1.0,
    fill: { color: PURPLE_BG }, line: { color: PURPLE, width: 1.5 }
  });
  s.addText([
    { text: "PostgreSQL / Neon", options: { bold: true, color: PURPLE, fontSize: 12, breakLine: true } },
    { text: "serverless, 512 МБ", options: { color: PURPLE_DARK, fontSize: 10, italic: true } }
  ], {
    x: 6.0, y: 4.0, w: 3.3, h: 1.0,
    fontFace: FONT_B, align: "center", valign: "middle", margin: 0
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 6 — СТЕК ТЕХНОЛОГИЙ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "Стек технологий", 6, TOTAL);

  const cards = [
    { title: "КЛИЕНТ",           color: PURPLE,     items: [".NET MAUI 9", "C# / XAML", "CommunityToolkit.Mvvm", "SQLite (sqlite-net-pcl)", "LiveChartsCore"] },
    { title: "СЕРВЕР",           color: PURPLE_MED, items: ["ASP.NET Core 9", "Npgsql (без EF)", "JWT HS256 + BCrypt", "Docker (multi-stage)", "Render.com Free"] },
    { title: "БАЗА ДАННЫХ",      color: CORAL,      items: ["Локально: SQLite", "Облако: PostgreSQL", "Хостинг: Neon", "Serverless, 512 МБ"] },
    { title: "ML-ИНФРАСТРУКТУРА", color: ORANGE,     items: ["Python 3.11", "LightGBM", "ONNX cross-platform", "OnnxRuntime 1.20.1"] }
  ];

  cards.forEach((card, i) => {
    const x = 0.55 + i * 2.3;
    const w = 2.15;
    // Карточка
    s.addShape(pres.shapes.RECTANGLE, {
      x, y: 1.35, w, h: 3.65,
      fill: { color: WHITE }, line: { color: card.color, width: 1.5 }
    });
    // Цветной заголовок
    s.addShape(pres.shapes.RECTANGLE, {
      x, y: 1.35, w, h: 0.6,
      fill: { color: card.color }, line: { color: card.color }
    });
    s.addText(card.title, {
      x, y: 1.35, w, h: 0.6,
      fontSize: 12, bold: true, color: WHITE, fontFace: FONT_H,
      align: "center", valign: "middle", charSpacing: 2
    });
    // Пункты
    s.addText(card.items.map((it, j) => ({
      text: it,
      options: { bullet: { code: "25A0" }, breakLine: j < card.items.length - 1, paraSpaceAfter: 6 }
    })), {
      x: x + 0.15, y: 2.1, w: w - 0.3, h: 2.8,
      fontSize: 11, fontFace: FONT_B, color: PURPLE_DARK, valign: "top"
    });
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 7 — ML-МОДУЛЬ: ДАТАСЕТ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "ML-модуль: датасет и признаки", 7, TOTAL);

  // Подзаголовок левой колонки (20pt)
  s.addText("ДАННЫЕ ОБУЧЕНИЯ", {
    x: 0.55, y: 1.3, w: 4.4, h: 0.35,
    fontSize: 13, bold: true, color: PURPLE, fontFace: FONT_H, charSpacing: 3
  });

  // Большие плашки-числа
  const stats = [
    { num: "763 322", label: "тренировочных подходов", color: PURPLE },
    { num: "500",     label: "спортсменов",            color: CORAL },
    { num: "118 350", label: "обучающих пар (X → y)",  color: ORANGE }
  ];
  stats.forEach((st, i) => {
    const y = 1.75 + i * 1.1;
    s.addShape(pres.shapes.RECTANGLE, {
      x: 0.55, y, w: 4.4, h: 0.95,
      fill: { color: WHITE }, line: { color: st.color, width: 1 }
    });
    s.addShape(pres.shapes.RECTANGLE, {
      x: 0.55, y, w: 0.12, h: 0.95,
      fill: { color: st.color }, line: { color: st.color }
    });
    s.addText(st.num, {
      x: 0.75, y: y + 0.05, w: 1.95, h: 0.85,
      fontSize: 26, bold: true, color: st.color, fontFace: FONT_H,
      valign: "middle", align: "left", margin: 0
    });
    s.addText(st.label, {
      x: 2.75, y: y + 0.05, w: 2.1, h: 0.85,
      fontSize: 11, color: PURPLE_DARK, fontFace: FONT_B,
      valign: "middle", align: "left", margin: 0
    });
  });

  // Правая колонка
  s.addText("29 ПРИЗНАКОВ · 4 ГРУППЫ", {
    x: 5.2, y: 1.3, w: 4.4, h: 0.35,
    fontSize: 13, bold: true, color: PURPLE, fontFace: FONT_H, charSpacing: 3
  });

  const groups = [
    { name: "История",            count: "13", desc: "лаги 1ПМ, средние, тренд" },
    { name: "Текущая тренировка", count: "7",  desc: "топ-вес, повторы, RPE" },
    { name: "Профиль",            count: "5",  desc: "вес, возраст, пол, стаж" },
    { name: "Упражнение",         count: "4",  desc: "тип, оборудование" }
  ];
  groups.forEach((g, i) => {
    const y = 1.75 + i * 0.8;
    s.addShape(pres.shapes.RECTANGLE, {
      x: 5.2, y, w: 4.4, h: 0.7,
      fill: { color: PURPLE_BG }, line: { color: PURPLE_LITE, width: 0.5 }
    });
    s.addShape(pres.shapes.OVAL, {
      x: 5.3, y: y + 0.1, w: 0.5, h: 0.5,
      fill: { color: PURPLE }, line: { color: PURPLE }
    });
    s.addText(g.count, {
      x: 5.3, y: y + 0.1, w: 0.5, h: 0.5,
      fontSize: 14, bold: true, color: WHITE, fontFace: FONT_H,
      align: "center", valign: "middle", margin: 0
    });
    s.addText([
      { text: g.name, options: { bold: true, color: PURPLE, fontSize: 12, breakLine: true } },
      { text: g.desc, options: { color: PURPLE_DARK, fontSize: 10 } }
    ], {
      x: 5.95, y: y + 0.05, w: 3.55, h: 0.6,
      fontFace: FONT_B, valign: "middle"
    });
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 8 — ML-МОДУЛЬ: РЕЗУЛЬТАТЫ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "ML-модуль: результаты обучения", 8, TOTAL);

  // Гистограмма MAE
  s.addChart(pres.charts.BAR, [{
    name: "MAE (кг)",
    labels: ["Naive baseline", "LinearRegression", "LightGBM"],
    values: [2.359, 2.154, 1.437]
  }], {
    x: 0.55, y: 1.3, w: 5.3, h: 3.7,
    barDir: "col",
    chartColors: [PURPLE_LITE, PURPLE_MED, CORAL],
    chartArea: { fill: { color: WHITE } },
    plotArea:  { fill: { color: WHITE } },
    catAxisLabelFontFace: FONT_B, catAxisLabelFontSize: 11, catAxisLabelColor: PURPLE_DARK,
    valAxisLabelFontFace: FONT_B, valAxisLabelFontSize: 10, valAxisLabelColor: MUTED,
    valGridLine: { color: "ECE6F5", size: 0.5 },
    catGridLine: { style: "none" },
    showValue: true,
    dataLabelFontFace: FONT_H, dataLabelFontSize: 12, dataLabelColor: PURPLE_DARK,
    dataLabelPosition: "outEnd",
    showLegend: false,
    showTitle: true, title: "Средняя ошибка прогноза 1ПМ (MAE, кг)",
    titleFontFace: FONT_H, titleFontSize: 13, titleColor: PURPLE
  });

  // Правая часть — большое число
  s.addShape(pres.shapes.RECTANGLE, {
    x: 6.15, y: 1.4, w: 3.5, h: 1.55,
    fill: { color: CORAL }, line: { color: CORAL }
  });
  s.addText([
    { text: "−39 %", options: { bold: true, fontSize: 40, color: WHITE, fontFace: FONT_H, breakLine: true } },
    { text: "снижение MAE\nLightGBM vs Naive", options: { color: WHITE, fontSize: 11, fontFace: FONT_B } }
  ], {
    x: 6.15, y: 1.45, w: 3.5, h: 1.45,
    align: "center", valign: "middle", margin: 0
  });

  // Соответствие ONNX
  s.addShape(pres.shapes.RECTANGLE, {
    x: 6.15, y: 3.1, w: 3.5, h: 1.9,
    fill: { color: PURPLE_BG }, line: { color: PURPLE, width: 1 }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 6.15, y: 3.1, w: 3.5, h: 0.4,
    fill: { color: PURPLE }, line: { color: PURPLE }
  });
  s.addText("СООТВЕТСТВИЕ ПАЙПЛАЙНОВ", {
    x: 6.15, y: 3.1, w: 3.5, h: 0.4,
    fontSize: 10, bold: true, color: WHITE, fontFace: FONT_H,
    align: "center", valign: "middle", charSpacing: 2
  });
  s.addText([
    { text: "Python (LightGBM) ↔ C# (ONNX):", options: { color: PURPLE_DARK, fontSize: 10, breakLine: true, paraSpaceAfter: 4 } },
    { text: "медиана: ", options: { color: MUTED, fontSize: 10 } },
    { text: "1,6 × 10⁻⁵ кг", options: { bold: true, color: CORAL, fontSize: 13, fontFace: FONT_H, breakLine: true, paraSpaceAfter: 4 } },
    { text: "p99: ", options: { color: MUTED, fontSize: 10 } },
    { text: "0,082 кг", options: { bold: true, color: CORAL, fontSize: 13, fontFace: FONT_H } }
  ], {
    x: 6.3, y: 3.55, w: 3.2, h: 1.4,
    fontFace: FONT_B, valign: "top"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 9 — ОБЛАЧНАЯ СИНХРОНИЗАЦИЯ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "Облачная синхронизация", 9, TOTAL);

  // Три лайфлайна
  const lanes = [
    { x: 0.8, label: "КЛИЕНТ\n.NET MAUI",    color: PURPLE },
    { x: 4.3, label: "СЕРВЕР\nASP.NET Core",  color: PURPLE_MED },
    { x: 7.8, label: "POSTGRESQL\nNeon",      color: CORAL }
  ];
  lanes.forEach(l => {
    s.addShape(pres.shapes.RECTANGLE, {
      x: l.x, y: 1.3, w: 1.7, h: 0.6,
      fill: { color: l.color }, line: { color: l.color }
    });
    s.addText(l.label, {
      x: l.x, y: 1.3, w: 1.7, h: 0.6,
      fontSize: 10, bold: true, color: WHITE, fontFace: FONT_H,
      align: "center", valign: "middle", charSpacing: 1
    });
    // вертикальная пунктирная линия жизни
    s.addShape(pres.shapes.LINE, {
      x: l.x + 0.85, y: 1.9, w: 0, h: 2.8,
      line: { color: l.color, width: 1, dashType: "dash" }
    });
  });

  // Шаги
  const steps = [
    { y: 2.25, from: 0, to: 1, label: "1. POST /sync  { pushBatch, lastSyncUtc }", solid: true },
    { y: 2.85, from: 1, to: 2, label: "2. UPSERT (LWW: UpdatedAt)",                solid: true },
    { y: 3.45, from: 2, to: 1, label: "3. COMMIT",                                  solid: false },
    { y: 4.05, from: 1, to: 0, label: "4. HTTP 200 { pullBatch, serverTimeUtc }",   solid: false }
  ];
  steps.forEach(st => {
    const x1 = lanes[st.from].x + 0.85;
    const x2 = lanes[st.to].x + 0.85;
    s.addShape(pres.shapes.LINE, {
      x: Math.min(x1, x2), y: st.y, w: Math.abs(x2 - x1), h: 0,
      line: {
        color: PURPLE_DARK, width: 1.5,
        dashType: st.solid ? "solid" : "dash",
        endArrowType: x2 > x1 ? "triangle" : "none",
        beginArrowType: x2 < x1 ? "triangle" : "none"
      }
    });
    s.addText(st.label, {
      x: Math.min(x1, x2), y: st.y - 0.3, w: Math.abs(x2 - x1), h: 0.25,
      fontSize: 10, color: PURPLE_DARK, fontFace: FONT_B,
      align: "center", valign: "middle", margin: 0
    });
  });

  // Плашка LWW
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 4.75, w: 9.05, h: 0.45,
    fill: { color: PURPLE_BG }, line: { color: PURPLE, width: 0.5 }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 4.75, w: 0.1, h: 0.45,
    fill: { color: CORAL }, line: { color: CORAL }
  });
  s.addText([
    { text: "LWW: ", options: { bold: true, color: PURPLE, fontFace: FONT_H } },
    { text: "ON CONFLICT(SyncId) DO UPDATE WHERE excluded.UpdatedAt > existing.UpdatedAt", options: { color: PURPLE_DARK, fontFace: "Consolas" } }
  ], {
    x: 0.8, y: 4.77, w: 8.75, h: 0.4,
    fontSize: 11, valign: "middle", margin: 0
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 10 — ТЕСТИРОВАНИЕ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "Тестирование и результаты", 10, TOTAL);

  // Левая колонка
  s.addText("ФУНКЦИОНАЛЬНЫЕ И ИНТЕГРАЦИОННЫЕ", {
    x: 0.55, y: 1.3, w: 4.5, h: 0.35,
    fontSize: 12, bold: true, color: PURPLE, fontFace: FONT_H, charSpacing: 2
  });
  const tests = [
    { n: "24", t: "функциональных теста" },
    { n: "8",  t: "ключевых сценариев пройдены" },
    { n: "4",  t: "сценария синхронизации" },
    { n: "2",  t: "платформы: Windows + Android" }
  ];
  tests.forEach((tst, i) => {
    const y = 1.75 + i * 0.65;
    s.addShape(pres.shapes.RECTANGLE, {
      x: 0.55, y, w: 4.5, h: 0.55,
      fill: { color: PURPLE_BG }, line: { color: PURPLE_LITE, width: 0.5 }
    });
    s.addText(tst.n, {
      x: 0.65, y: y + 0.05, w: 0.7, h: 0.45,
      fontSize: 22, bold: true, color: CORAL, fontFace: FONT_H,
      align: "center", valign: "middle", margin: 0
    });
    s.addText(tst.t, {
      x: 1.45, y: y + 0.05, w: 3.55, h: 0.45,
      fontSize: 11, color: PURPLE_DARK, fontFace: FONT_B,
      align: "left", valign: "middle", margin: 0
    });
  });

  // Правая колонка — производительность
  s.addText("ПРОИЗВОДИТЕЛЬНОСТЬ", {
    x: 5.2, y: 1.3, w: 4.4, h: 0.35,
    fontSize: 12, bold: true, color: PURPLE, fontFace: FONT_H, charSpacing: 2
  });

  const perfTable = [
    [
      { text: "Метрика", options: { bold: true, fill: { color: PURPLE }, color: WHITE, fontSize: 10, fontFace: FONT_H, align: "center" } },
      { text: "Win",     options: { bold: true, fill: { color: PURPLE }, color: WHITE, fontSize: 10, fontFace: FONT_H, align: "center" } },
      { text: "Android", options: { bold: true, fill: { color: PURPLE }, color: WHITE, fontSize: 10, fontFace: FONT_H, align: "center" } }
    ],
    [{ text: "Список тренировок", options: { fontSize: 10, fontFace: FONT_B } }, { text: "42 мс", options: { align: "center", fontSize: 10, fontFace: FONT_B } }, { text: "78 мс", options: { align: "center", fontSize: 10, fontFace: FONT_B } }],
    [{ text: "Inference ML", options: { fontSize: 10, fontFace: FONT_B, bold: true } }, { text: "4 мс", options: { align: "center", fontSize: 10, fontFace: FONT_H, bold: true, color: CORAL } }, { text: "8 мс", options: { align: "center", fontSize: 10, fontFace: FONT_H, bold: true, color: CORAL } }],
    [{ text: "Init ONNX-сервиса", options: { fontSize: 10, fontFace: FONT_B } }, { text: "87 мс", options: { align: "center", fontSize: 10, fontFace: FONT_B } }, { text: "143 мс", options: { align: "center", fontSize: 10, fontFace: FONT_B } }],
    [{ text: "Размер APK", options: { fontSize: 10, fontFace: FONT_B } }, { text: "—", options: { align: "center", fontSize: 10, fontFace: FONT_B } }, { text: "38 МБ", options: { align: "center", fontSize: 10, fontFace: FONT_H, bold: true } }]
  ];
  s.addTable(perfTable, {
    x: 5.2, y: 1.7, w: 4.4, colW: [2.4, 1.0, 1.0],
    color: PURPLE_DARK,
    border: { type: "solid", pt: 0.5, color: "D5D0E0" },
    valign: "middle"
  });

  // UX-плашка
  s.addShape(pres.shapes.RECTANGLE, {
    x: 5.2, y: 4.05, w: 4.4, h: 0.9,
    fill: { color: PURPLE_BG }, line: { color: PURPLE, width: 0.5 }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 5.2, y: 4.05, w: 0.1, h: 0.9,
    fill: { color: ORANGE }, line: { color: ORANGE }
  });
  s.addText([
    { text: "UX-ТЕСТ: ", options: { bold: true, color: ORANGE, fontFace: FONT_H, charSpacing: 1 } },
    { text: "6 добровольцев. ", options: { color: PURPLE_DARK, bold: true, breakLine: true } },
    { text: "Отмечены: интуитивный ввод подходов, полезность подсказки прогноза 1ПМ.", options: { color: PURPLE_DARK } }
  ], {
    x: 5.35, y: 4.1, w: 4.15, h: 0.8,
    fontSize: 10, fontFace: FONT_B, valign: "top"
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 11 — ВЫВОДЫ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: WHITE };
  addHeader(s, "Выводы", 11, TOTAL);

  // Большая плашка «Цель достигнута»
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 1.25, w: 9.05, h: 0.75,
    fill: { color: PURPLE }, line: { color: PURPLE }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0.55, y: 1.25, w: 0.12, h: 0.75,
    fill: { color: CORAL }, line: { color: CORAL }
  });
  s.addText([
    { text: "✓  ЦЕЛЬ РАБОТЫ ДОСТИГНУТА.  ", options: { bold: true, color: CORAL, fontSize: 14, charSpacing: 2 } },
    { text: "Все поставленные задачи выполнены, требования соблюдены.", options: { color: WHITE, fontSize: 12 } }
  ], {
    x: 0.85, y: 1.3, w: 8.75, h: 0.65,
    fontFace: FONT_B, valign: "middle", margin: 0
  });

  // 3 столбца
  const cols = [
    {
      title: "РЕАЛИЗОВАНО",
      color: PURPLE,
      items: [
        "Клиент: .NET MAUI 9, MVVM, 7 экранов",
        "Сервер: ASP.NET Core 9 на Render.com",
        "ML: LightGBM → ONNX (MAE 1,44 кг)",
        "Sync: LWW + offline-first"
      ]
    },
    {
      title: "НАУЧНАЯ ЗНАЧИМОСТЬ",
      color: PURPLE_MED,
      items: [
        "Адаптация формулы Эпли и ML",
        "Решение проблемы clock skew",
        "Валидация ONNX-портации (Δ < 0,1 кг)"
      ]
    },
    {
      title: "ПРАКТИЧЕСКАЯ ЦЕННОСТЬ",
      color: CORAL,
      items: [
        "Бесплатное решение на русском языке",
        "Единственное приложение с Windows",
        "Open Source, расширяемое сообществом"
      ]
    }
  ];
  cols.forEach((col, i) => {
    const x = 0.55 + i * 3.05;
    // Заголовок
    s.addShape(pres.shapes.RECTANGLE, {
      x, y: 2.2, w: 2.9, h: 0.55,
      fill: { color: col.color }, line: { color: col.color }
    });
    s.addText(col.title, {
      x, y: 2.2, w: 2.9, h: 0.55,
      fontSize: 11, bold: true, color: WHITE, fontFace: FONT_H,
      align: "center", valign: "middle", charSpacing: 2
    });
    // Тело
    s.addShape(pres.shapes.RECTANGLE, {
      x, y: 2.75, w: 2.9, h: 2.25,
      fill: { color: WHITE }, line: { color: col.color, width: 1 }
    });
    s.addText(col.items.map((it, j) => ({
      text: it,
      options: { bullet: { code: "25A0" }, breakLine: j < col.items.length - 1, paraSpaceAfter: 7 }
    })), {
      x: x + 0.15, y: 2.85, w: 2.6, h: 2.1,
      fontSize: 11, fontFace: FONT_B, color: PURPLE_DARK, valign: "top"
    });
  });
}

// ═══════════════════════════════════════════════════════════
// СЛАЙД 12 — СПАСИБО ЗА ВНИМАНИЕ
// ═══════════════════════════════════════════════════════════
{
  const s = pres.addSlide();
  s.background = { color: PURPLE_DARK };

  // Декоративные полосы
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 0, w: 0.6, h: 5.625,
    fill: { color: PURPLE }, line: { color: PURPLE }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 2.0, w: 0.6, h: 0.7,
    fill: { color: CORAL }, line: { color: CORAL }
  });
  s.addShape(pres.shapes.RECTANGLE, {
    x: 0, y: 2.85, w: 0.6, h: 0.4,
    fill: { color: ORANGE }, line: { color: ORANGE }
  });

  // Главный текст (40pt по регламенту для главного заголовка)
  s.addText("Спасибо за внимание!", {
    x: 1, y: 1.7, w: 8.5, h: 1.2,
    fontSize: 44, bold: true, color: WHITE, fontFace: FONT_H,
    align: "center", valign: "middle"
  });

  // Разделитель
  s.addShape(pres.shapes.LINE, {
    x: 3.5, y: 3.1, w: 3, h: 0,
    line: { color: CORAL, width: 3 }
  });

  // Подпись
  s.addText("Готов ответить на ваши вопросы", {
    x: 1, y: 3.3, w: 8.5, h: 0.6,
    fontSize: 18, color: PURPLE_BG, italic: true, fontFace: FONT_B,
    align: "center", valign: "middle"
  });

  // Нижняя плашка
  s.addText("FitApp · ВКР · МТУСИ · Москва 2026", {
    x: 1, y: 4.7, w: 8.5, h: 0.4,
    fontSize: 12, color: PURPLE_BG, fontFace: FONT_H, bold: true,
    align: "center", valign: "middle", charSpacing: 3
  });
}

// ═══════════════════════════════════════════════════════════
// СОХРАНЕНИЕ
// ═══════════════════════════════════════════════════════════
pres.writeFile({ fileName: "presentation_zaschita_mtusi.pptx" })
  .then(file => console.log("✓ Готово: " + file));
