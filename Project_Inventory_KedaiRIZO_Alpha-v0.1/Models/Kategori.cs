using System.ComponentModel.DataAnnotations;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Models
{
    public class Kategori
    {
        [Key]
        public int Id { get; set; }
        public string Nama_Kategori { get; set; }
    }
}
