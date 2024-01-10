namespace AspNetCoreIdentity.Web.Areas.Admin.Models
{
    public class AracKiralamaViewModel
    {
        public string UserId { get; set; }
        public string? UserName { get; set; }
        public string? Kategori { get; set; }
        public string? Marka { get; set; }
        public string? Model { get; set; }
        public string? Vites { get; set; }
        public DateTime? BasTar { get; set; }
        public DateTime? BitTar { get; set; }
        public int? ToplamUcret { get; set; }
    }
}
