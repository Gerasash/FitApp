const path = require("path");
const pptxgen = require(path.join("C:\\Users\\Gera1\\AppData\\Roaming\\npm\\node_modules", "pptxgenjs"));

const pptx = new pptxgen();
pptx.defineLayout({ name: "W", width: 13.333, height: 7.5 });
pptx.layout = "W";

const NAVY = "1E2761";
const ICE = "CADCFC";
const GRAY = "44474F";

const s = pptx.addSlide();
s.background = { color: "FFFFFF" };

// Заголовок
s.addText("Актуальность темы", {
  x: 0.6, y: 0.45, w: 11.5, h: 0.9, fontFace: "Georgia", fontSize: 36, bold: true, color: NAVY,
});

// Вводные тезисы
s.addText(
  [
    { text: "Силовой тренинг — массовая практика, требующая систематического учёта нагрузки.", options: { bullet: { code: "2022" }, paraSpaceAfter: 10 } },
    { text: "Ключевые задачи занимающегося — отслеживание прогресса 1ПМ и планирование рабочих весов.", options: { bullet: { code: "2022" }, paraSpaceAfter: 10 } },
    { text: "Существующие приложения (Strong, Hevy, FitNotes, Jefit) закрывают лишь часть задач: либо без локального ML, либо без автономного режима, либо по подписке.", options: { bullet: { code: "2022" }, paraSpaceAfter: 10 } },
  ],
  { x: 0.6, y: 1.6, w: 7.0, h: 3.2, fontFace: "Calibri", fontSize: 17, color: GRAY, align: "left", valign: "top", lineSpacingMultiple: 1.05 }
);

// Блок "Ниша FitApp"
s.addShape(pptx.ShapeType.roundRect, { x: 8.0, y: 1.6, w: 4.7, h: 4.6, fill: { color: NAVY }, rectRadius: 0.12 });
s.addText("НИША FITAPP", { x: 8.0, y: 1.8, w: 4.7, h: 0.5, fontFace: "Georgia", fontSize: 18, bold: true, color: "FFFFFF", align: "center" });
s.addText(
  [
    { text: "Автономная работа (приоритет локальных данных)", options: { bullet: { code: "2713" }, paraSpaceAfter: 8 } },
    { text: "Локальный ML-прогноз 1ПМ без подписки", options: { bullet: { code: "2713" }, paraSpaceAfter: 8 } },
    { text: "Планировщик нагрузки", options: { bullet: { code: "2713" }, paraSpaceAfter: 8 } },
    { text: "Android + Windows", options: { bullet: { code: "2713" }, paraSpaceAfter: 8 } },
    { text: "Синхронизация устройств", options: { bullet: { code: "2713" } } },
  ],
  { x: 8.3, y: 2.4, w: 4.1, h: 3.6, fontFace: "Calibri", fontSize: 15, color: ICE, align: "left", valign: "top" }
);

// Нижний колонтитул
s.addText("FitApp · ВКР · МТУСИ, 2026", { x: 0.6, y: 7.0, w: 8, h: 0.3, fontFace: "Calibri", fontSize: 10, color: "9AA0A6" });
s.addText("2 / 12", { x: 11.7, y: 7.0, w: 1.0, h: 0.3, fontFace: "Calibri", fontSize: 10, color: "9AA0A6", align: "right" });

pptx.writeFile({ fileName: "C:\\Users\\Gera1\\dev\\FitApp\\VKR\\presentation\\slide_02.pptx" }).then(() => console.log("OK slide_02.pptx"));
