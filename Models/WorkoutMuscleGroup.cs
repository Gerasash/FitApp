using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitApp.Models
{
    [Table("WorkoutMuscleGroup")]
    public class WorkoutMuscleGroup
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int WorkoutId { get; set; }
        public int MuscleGroupId { get; set; }

    }
}
