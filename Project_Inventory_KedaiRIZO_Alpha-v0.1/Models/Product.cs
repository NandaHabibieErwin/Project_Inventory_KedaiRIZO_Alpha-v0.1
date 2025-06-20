namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Product_Name { get; set; }
        public int harga { get; set; }
        public int stock { get; set; }      
        public int KategoriID { get; set; }
        public Kategori Id_Kategori { get; set; }

    }
}
