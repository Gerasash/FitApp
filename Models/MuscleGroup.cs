using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitApp.Models
{
    public class MuscleGroup
    {
        [AutoIncrement, PrimaryKey]

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

    }
}
