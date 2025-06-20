using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Models
{
    public class Data_Transaksi
    {
        public int Id { get; set; }
        public virtual IdentityApplicationUser ApplicationUser { get; set; }      
        public DateTime Tanggal { get; set; }        
    }
}
