using SQLite;

namespace TimoPrimesNow.Models
{
    [Table("Primenumbers")]
    public class Prime
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Primenumber { get; set; }
    }
}
