using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IBS.Models.MMSI.MasterFile
{
    public class Module
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // If NUM is manually assigned
        public int ModuleNumber { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string ModuleName { get; set; } = null!;

        [Column(TypeName = "varchar(100)")]
        public string Description { get; set; } = null!;
    }
}
