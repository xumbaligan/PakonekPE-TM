using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TM_PE.Model
{
    public enum RoleType
    {
        OfficeStaff,
        FieldTechnician
    }

    [Table("tbl_criteria")]
    public class Criteria
    {
        [Key]
        public int CriteriaId { get; set; }

        [Required, StringLength(150)]
        public string CriteriaName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public RoleType RoleType { get; set; }

        //[Range(1, 10)]
        //public int MaxScore { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }
}
