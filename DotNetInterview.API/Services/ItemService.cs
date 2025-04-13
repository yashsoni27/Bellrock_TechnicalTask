using DotNetInterview.API;
using DotNetInterview.API.Domain;
using Microsoft.EntityFrameworkCore;


public class ItemService
{
    private readonly DataContext _context;
    private readonly Func<DateTime> _timeProvider;

    public ItemService(DataContext context, Func<DateTime> timeProvider = null)
    {
        _context = context;
        _timeProvider = timeProvider ?? (() => DateTime.UtcNow);
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

        if (item != null)
        {
            UpdateItemPriceAndStatus(item);
        }
        return item;
    }

    public async Task<Item> CreateItem(string reference, string name, decimal price, string? status, decimal? currentPrice, List<Variation> variations)
    {
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Reference = reference,
            Name = name,
            Price = price,
            Status = status,
            CurrentPrice = currentPrice,
            Variations = new List<Variation>()
        };

        if (variations != null)
        {
            foreach (var variation in variations)
            {
                item.Variations.Add(new Variation
                {
                    Size = variation.Size,
                    Quantity = variation.Quantity
                });
            }
        }

        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        UpdateItemPriceAndStatus(item);

        return item;
    }

    public async Task<Item> UpdateItem(Guid id, string name, decimal price, string status, decimal? currentPrice, List<Variation> variations)
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
        item.Status = status;
        item.CurrentPrice = currentPrice;

        item.Variations.Clear();
        if (variations != null)
        {
            foreach (var variation in variations)
            {
                item.Variations.Add(new Variation
                {
                    Size = variation.Size,
                    Quantity = variation.Quantity
                });
            }
        }

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
        var discount = CalculateDiscount(item, _timeProvider());
        item.CurrentPrice = item.Price * (1 - discount);

        var totalQuantity = item.Variations.Sum(v => v.Quantity);
        item.Status = totalQuantity > 0 ? $"In Stock ({totalQuantity})" : "Sold Out";
    }

    private decimal CalculateDiscount(Item item, DateTime? currentTime = null)
    {
        decimal maxDiscount = 0;
        var time = currentTime ?? _timeProvider();

        // Monday discount (50%)
        if (time.DayOfWeek == DayOfWeek.Monday && time.Hour >= 12 && time.Hour < 17)
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

