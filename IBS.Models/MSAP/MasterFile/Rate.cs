using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class Rate
    {
        [Key]
        public int RateId { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string Type { get; set; } = null!;

        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        public DateOnly AsOf { get; set; }
    }
}
