using System.ComponentModel.DataAnnotations;

namespace AspNetCoreIdentity.Web.Models
{
    public class Arac
    {
        [Key]
        public int Id { get; set; }
        public string Kategori { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public string Vites { get; set; }
        public string Ucret { get; set; }
        public bool KiradaMi { get; set; }
    }
}
