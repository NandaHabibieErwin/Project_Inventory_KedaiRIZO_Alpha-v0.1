using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Models
{
    public class Product
    {
        public int Id { get; set; }
        [DisplayName("Nama Produk")]
        public string Product_Name { get; set; }
        [DisplayName("Harga")]
        [Range(0, int.MaxValue, ErrorMessage = "Tidak boleh dibawah 0")]
        public int harga { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Tidak boleh dibawah 0")]
        [DisplayName("Stok")]
        
        public int stock { get; set; }
        [DisplayName("Kategori")]
        public int KategoriID { get; set; }
        [DisplayName("Kategori")]
        public Kategori? Kategori { get; set; }

    }
}
