using DotNetInterview.API;
using DotNetInterview.API.Domain;
using Microsoft.EntityFrameworkCore;


public class ItemService
{
    private readonly DataContext _context;

    public ItemService(DataContext context)
    {
        _context = context;
    }

    public async Task<List<Item>> GetAllItems()
    {
        var items = await _context.Items
            .Include(i => i.Variations)
            .ToListAsync();

        foreach (var item in items)
        {
            UpdateItemPriceAndStatus(item);
        }

        return items;
    }

    public async Task<Item?> GetItemById(Guid id)
    {
        var item = await _context.Items
            .Include(i => i.Variations)
            .FirstOrDefaultAsync(i => i.Id == id);

        if(item != null)
        {
            UpdateItemPriceAndStatus(item);
        }
        return item;
    }

    public async Task<Item> CreateItem(string refrence, string name, decimal price)
    {
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Reference = refrence,
            Name = name,
            Price = price,
            Variations = new List<Variation>(),
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        UpdateItemPriceAndStatus(item);

        return item;
    }

    public async Task<Item> UpdateItem(Guid id, string name, decimal price)
    {
        var item = await _context.Items
            .Include(i => i.Variations)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            return null;
        }

        item.Name = name;
        item.Price = price;

        UpdateItemPriceAndStatus(item);
        await _context.SaveChangesAsync();

        return item;
    }

    public async Task<bool> DeleteItem(Guid id)
    {
        var item = await _context.Items
            .Include(i => i.Variations)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null) return false;

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }

    private void UpdateItemPriceAndStatus(Item item)
    {
        var discount = CalculateDiscount(item, DateTime.UtcNow);
        item.CurrentPrice = item.Price * (1 - discount);

        var totalQuantity = item.Variations.Sum(v => v.Quantity);
        item.Status = totalQuantity > 0 ? $"In Stock ({totalQuantity})" : "Sold Out";
    }

    //public decimal CalculateDiscountedPrice(Item item)
    //{
    //    var discount = CalculateDiscount(item, DateTime.UtcNow);
    //    return item.Price * (1 - discount);
    //}

    private decimal CalculateDiscount(Item item, DateTime currentTime)
    {
        decimal maxDiscount = 0;

        // Monday discount (50%)
        if (currentTime.DayOfWeek == DayOfWeek.Monday && currentTime.Hour >= 12 && currentTime.Hour < 17)
        {
            maxDiscount = Math.Max(maxDiscount, 0.5m);
        }

        // Check total quantity across all variations
        int totalQuantity = item.Variations.Sum(v => v.Quantity);

        if (totalQuantity > 10)
        {
            maxDiscount = Math.Max(maxDiscount, 0.2m); // 20% discount
        }
        else if (totalQuantity > 5)
        {
            maxDiscount = Math.Max(maxDiscount, 0.1m); // 10% discount
        }

        return maxDiscount;
    }

}

