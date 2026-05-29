-- Схема БД FitApp для построения EER-диаграммы в MySQL Workbench.
-- Импорт: Workbench → File → Import → Reverse Engineer MySQL Create Script…
-- Внешние ключи заданы явно, поэтому связи (crow's foot) построятся автоматически.
-- Имена таблиц и колонок соответствуют моделям приложения (Models/*.cs).

SET FOREIGN_KEY_CHECKS = 0;

-- Профиль пользователя (локальный + учётка синхронизации)
CREATE TABLE `users` (
  `id`               INT          NOT NULL AUTO_INCREMENT,
  `email`            VARCHAR(60)  NULL,
  `display_name`     VARCHAR(60)  NULL,
  `bodyweight`       DOUBLE       NOT NULL DEFAULT 0,
  `age`              INT          NOT NULL DEFAULT 0,
  `sex`              INT          NOT NULL DEFAULT 0,
  `experience_start` DATETIME     NULL,
  `target_rpe`       DOUBLE       NOT NULL DEFAULT 7.5,
  `created_at`       DATETIME     NOT NULL,
  `updated_at`       DATETIME     NOT NULL,
  `is_deleted`       TINYINT(1)   NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `uq_users_email` (`email`)
) ENGINE = InnoDB;

-- Группы мышц
CREATE TABLE `MuscleGroups` (
  `id`   INT          NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE = InnoDB;

-- Тренировки
CREATE TABLE `Workouts` (
  `id`          INT          NOT NULL AUTO_INCREMENT,
  `SyncId`      VARCHAR(36)  NOT NULL,
  `name`        VARCHAR(255) NULL,
  `Description` TEXT         NULL,
  `StartTime`   DATETIME     NOT NULL,
  `UserId`      INT          NOT NULL DEFAULT 1,
  `UpdatedAt`   DATETIME     NOT NULL,
  `IsDeleted`   TINYINT(1)   NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `uq_workouts_syncid` (`SyncId`),
  INDEX `ix_workouts_user` (`UserId`),
  CONSTRAINT `fk_workouts_users`
    FOREIGN KEY (`UserId`) REFERENCES `users` (`id`)
) ENGINE = InnoDB;

-- Справочник упражнений
CREATE TABLE `Exercises` (
  `id`                   INT          NOT NULL AUTO_INCREMENT,
  `Name`                 VARCHAR(255) NOT NULL,
  `NameEn`               VARCHAR(255) NULL,
  `PrimaryMuscleGroupId` INT          NOT NULL,
  `EquipmentType`        INT          NOT NULL DEFAULT 0,
  `Category`             INT          NOT NULL DEFAULT 0,
  `Mechanic`             INT          NOT NULL DEFAULT 0,
  `Instructions`         TEXT         NULL,
  `IsCustom`             TINYINT(1)   NOT NULL DEFAULT 0,
  `IsArchived`           TINYINT(1)   NOT NULL DEFAULT 0,
  `IsFavorite`           TINYINT(1)   NOT NULL DEFAULT 0,
  `CreatedAt`            DATETIME     NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `ix_exercises_muscle` (`PrimaryMuscleGroupId`),
  CONSTRAINT `fk_exercises_muscle`
    FOREIGN KEY (`PrimaryMuscleGroupId`) REFERENCES `MuscleGroups` (`id`)
) ENGINE = InnoDB;

-- Упражнения в составе тренировки
CREATE TABLE `WorkoutExercises` (
  `id`            INT         NOT NULL AUTO_INCREMENT,
  `SyncId`        VARCHAR(36) NOT NULL,
  `WorkoutId`     INT         NOT NULL,
  `WorkoutSyncId` VARCHAR(36) NULL,
  `ExerciseId`    INT         NOT NULL,
  `OrderIndex`    INT         NOT NULL DEFAULT 0,
  `UpdatedAt`     DATETIME    NOT NULL,
  `IsDeleted`     TINYINT(1)  NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `uq_we_syncid` (`SyncId`),
  INDEX `ix_we_workout` (`WorkoutId`),
  INDEX `ix_we_exercise` (`ExerciseId`),
  CONSTRAINT `fk_we_workout`
    FOREIGN KEY (`WorkoutId`) REFERENCES `Workouts` (`id`),
  CONSTRAINT `fk_we_exercise`
    FOREIGN KEY (`ExerciseId`) REFERENCES `Exercises` (`id`)
) ENGINE = InnoDB;

-- Подходы (вес/повторы/RPE)
CREATE TABLE `ExerciseSets` (
  `id`                    INT         NOT NULL AUTO_INCREMENT,
  `SyncId`                VARCHAR(36) NOT NULL,
  `WorkoutExerciseId`     INT         NOT NULL,
  `WorkoutExerciseSyncId` VARCHAR(36) NULL,
  `SetNumber`             INT         NOT NULL DEFAULT 0,
  `Weight`                DOUBLE      NOT NULL DEFAULT 0,
  `Reps`                  INT         NOT NULL DEFAULT 0,
  `RPE`                   DOUBLE      NOT NULL DEFAULT 0,
  `IsAssisted`            TINYINT(1)  NOT NULL DEFAULT 0,
  `Kind`                  INT         NOT NULL DEFAULT 0,
  `UpdatedAt`             DATETIME    NOT NULL,
  `IsDeleted`             TINYINT(1)  NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `uq_sets_syncid` (`SyncId`),
  INDEX `ix_sets_we` (`WorkoutExerciseId`),
  CONSTRAINT `fk_sets_we`
    FOREIGN KEY (`WorkoutExerciseId`) REFERENCES `WorkoutExercises` (`id`)
) ENGINE = InnoDB;

-- Связка «тренировка ↔ группы мышц» (многие-ко-многим)
CREATE TABLE `WorkoutMuscleGroups` (
  `id`              INT        NOT NULL AUTO_INCREMENT,
  `workout_id`      INT        NOT NULL,
  `muscle_group_id` INT        NOT NULL,
  `UpdatedAt`       DATETIME   NOT NULL,
  `IsDeleted`       TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  INDEX `ix_wmg_workout` (`workout_id`),
  INDEX `ix_wmg_muscle` (`muscle_group_id`),
  CONSTRAINT `fk_wmg_workout`
    FOREIGN KEY (`workout_id`) REFERENCES `Workouts` (`id`),
  CONSTRAINT `fk_wmg_muscle`
    FOREIGN KEY (`muscle_group_id`) REFERENCES `MuscleGroups` (`id`)
) ENGINE = InnoDB;

SET FOREIGN_KEY_CHECKS = 1;
