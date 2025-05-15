using FitApp;
using SQLite;
using SQLiteNetExtensions.Attributes;
namespace FitApp.Models
{
    [Table("WorkoutMuscleGroups")]
    public class WorkoutMuscleGroup
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("workout_id")]
        [ForeignKey(typeof(Workout))] // Указываем, что это внешний ключ к таблице Workouts
        public int WorkoutId { get; set; }

        [Column("muscle_group_id")]
        [ForeignKey(typeof(MuscleGroup))] // Указываем, что это внешний ключ к таблице MuscleGroups
        public int MuscleGroupId { get; set; }

        public WorkoutMuscleGroup() { }

        public WorkoutMuscleGroup(int workoutId, int muscleGroupId)
        {
            WorkoutId = workoutId;
            MuscleGroupId = muscleGroupId;
        }


    }
}
