//Файл Workout.cs
using FitApp;
using System;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
//using System.ComponentModel.DataAnnotations.Schema;

namespace FitApp.Models
{
    [Table("Workouts")]
    public class Workout
    {
        [PrimaryKey]
        [AutoIncrement]
        [Column("id")]
        public int Id { get; set; }
        [Column("Workout_name")]
        public string Name { get; set; }
        
        public DateTime StartTime { get; set; }
        public Workout()
        {
            
        }
        public Workout(string name, DateTime time)
        {
            Name = string.IsNullOrEmpty(name) ? "Unknown" : name;
            /*if (String.IsNullOrEmpty(name)) Name = "Unknown";
            else Name = name;*/

            StartTime = time;
        }
        [Ignore]
        public List<MuscleGroup> MuscleGroups { get; set; } = new();
    }
}
