namespace SapVendaDraft.Models
{
    public class VendaDto
    {
        public required string CardCode { get; set; }
        public required List<ItemVenda> Itens { get; set; }
    }

    public class ItemVenda
    {
        public required string ItemCode { get; set; }
        public double Quantity { get; set; }
        public double Price { get; set; }
    }
}
