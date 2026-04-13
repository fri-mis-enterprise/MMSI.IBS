using System.ComponentModel.DataAnnotations;

namespace IBS.Models.MasterFile
{
    public class Terms
    {
        [Key]
        [StringLength(10)]
        public string TermsCode { get; set; } = null!;

        public int NumberOfDays { get; set; }

        public int NumberOfMonths { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        [StringLength(100)]
        public string EditedBy { get; set; } = string.Empty;

        public DateTime EditedDate { get; set; }
    }
}
