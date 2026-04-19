# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

FitApp is a .NET MAUI (net9.0) cross-platform fitness tracking app targeting Android, Windows, and MacCatalyst. The user-facing language is Russian.

## Build & Run

```bash
# Restore and build (all target frameworks)
dotnet restore
dotnet build

# Run for a specific platform (Windows on this machine)
dotnet build -t:Run -f net9.0-windows10.0.19041.0
dotnet build -t:Run -f net9.0-android35.0
```

There are no unit tests in the project.

## Architecture

Standard MAUI Shell app using the **MVVM pattern via CommunityToolkit.Mvvm** (source generators produce properties from `[ObservableProperty]` and commands from `[RelayCommand]`).

- **`MauiProgram.cs`** — DI composition root. All pages and ViewModels are registered here; `WorkoutDataBase` is a singleton, everything else `Transient`. New pages/VMs must be added here or DI resolution fails at runtime.
- **`AppShell.xaml`** — defines navigation routes. Each page used via `Shell.Current.GoToAsync` must be registered as `ShellContent` here.
- **`Data/WorkoutDataBase.cs`** — single SQLite persistence class wrapping `SQLiteAsyncConnection`. Tables are auto-created in the constructor via `Task.Run(...).Wait()` on `InitAsync`. All DB access goes through this class; there is no repository abstraction. Joins across `Workouts`/`Exercises`/`MuscleGroups` are done with raw `QueryAsync<T>` SQL or manual loops (the `sqlite-net-pcl` library doesn't support navigation properties).
- **`Models/`** — SQLite-net entities. Note table and column names are declared with `[Table]`/`[Column]` attributes; raw SQL in `WorkoutDataBase` must match those exact names (e.g., `Workouts`, `workout_id`, `muscle_group_id`).
- **`ViewModels/`** — `partial` classes inheriting `ObservableObject`. State lives as `[ObservableProperty]` fields; user actions are `[RelayCommand]` methods. Navigation and dialogs use `Shell.Current.DisplayPromptAsync` / `DisplayAlert` directly inside commands (no abstraction).
- **Data relationships** — `Workout` ↔ `MuscleGroup` is many-to-many via `WorkoutMuscleGroup`. `Workout` contains `WorkoutExercise` rows (exercise instances in a workout) which in turn contain `ExerciseSet` rows (weight/reps/RPE per set). Updates that change relationships (e.g. `UpdateWorkout`) **delete old join rows and re-insert** rather than diffing.

## Conventions specific to this repo

- Code comments, UI strings, and git commit messages are in Russian — match that style when editing.
- Nullable reference types are enabled (`<Nullable>enable</Nullable>`), but many model properties are declared without `?` and left unassigned — don't introduce new nullability warnings but don't refactor existing ones either.
- After modifying a ViewModel that uses `[ObservableProperty]` or `[RelayCommand]`, a rebuild is required so the source generator regenerates the partial class.
