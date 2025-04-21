using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitApp
{
    [Table("User")]
    public class User
    {
        [PrimaryKey]
        [AutoIncrement]
        [Column("id")]
        public int Id { get; set; }
        [Column("user_name")]
        public string UserName { get; set; } = "unknow_UserName";
        [Column("mobile")]
        public string Mobile { get; set; } = "unknow_mobile";
        [Column("email")]
        public string Email { get; set; } = "unknow_email";

    }
}
