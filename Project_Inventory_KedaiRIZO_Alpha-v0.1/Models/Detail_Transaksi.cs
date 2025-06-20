namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Models
{
    public class Detail_Transaksi
    {
        public int Id { get; set; }
        public Product Id_Produk { get; set; }
        public Detail_Transaksi Id_Detail { get; set; }
        public int total {  get; set; }
    }
}
