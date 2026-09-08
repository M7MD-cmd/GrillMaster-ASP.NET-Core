using System.ComponentModel.DataAnnotations;

namespace GrillMaster.Models
{
    // ده الجدول اللي هيحفظ أنواع الأكل: Grilled Beef, Grilled Chicken, Seafood...
    public class Category
    {
        public int Id { get; set; } // الـ Primary Key بتاع الجدول (EF Core بتعرف Id تلقائي إنه المفتاح)

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // علاقة Category -> MenuItem : الكاتيجوري الواحدة فيها أكتر من صنف
        public List<MenuItem> MenuItems { get; set; } = new();
    }
}
