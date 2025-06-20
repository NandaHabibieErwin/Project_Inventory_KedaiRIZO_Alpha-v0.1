using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Models
{
    public class Data_Transaksi
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual IdentityUser? ApplicationUser { get; set; }
        public DateTime Tanggal { get; set; }
        public int TotalAmount { get; set; }


        public virtual ICollection<Detail_Transaksi> Details { get; set; } = new List<Detail_Transaksi>();
    }
}
