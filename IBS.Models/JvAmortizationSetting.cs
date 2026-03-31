using IBS.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models
{
    public class JvAmortizationSetting
    {
        [Key]
        public int Id { get; set; }

        public int JvId { get; set; }

        public JvFrequency JvFrequency { get; set; } = JvFrequency.Monthly;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public DateOnly? NextRunDate { get; set; }

        public DateOnly? LastRunDate { get; set; }

        public int OccurrenceTotal { get; set; }

        public int OccurrenceRemaining { get; set; }

        public bool IsActive { get; set; }

        public string ExpenseAccount { get; set; } = string.Empty;

        public string PrepaidAccount { get; set; } = string.Empty;
    }
}
