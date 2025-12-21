using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitApp.Models;


public class Exercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }

    // Связь с группой мышц (основная группа)
    public int PrimaryMuscleGroupId { get; set; }

    // Полезно для ИИ: тип упражнения
    // 0 - Штанга, 1 - Гантели, 2 - Тренажер, 3 - Свой вес
    public int EquipmentType { get; set; }
}

