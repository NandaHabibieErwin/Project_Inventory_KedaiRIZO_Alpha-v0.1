using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Models
{
    public class Detail_Transaksi
    {
        public int Id { get; set; }
        public int DataTransaksiId { get; set; }
        [ForeignKey("DataTransaksiId")]
        public virtual Data_Transaksi? DataTransaksi { get; set; }

        public Product? Product { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int total { get; set; }
    }
}

