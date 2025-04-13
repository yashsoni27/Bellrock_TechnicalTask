using DotNetInterview.API;
using DotNetInterview.API.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DotNetInterview.Tests;

public class ItemServiceTests
{
    private DataContext _dataContext;
    private ItemService _itemService;
    private SqliteConnection _connection;

    [SetUp]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=DotNetInterview;Mode=Memory;Cache=Shared");
        _connection.Open();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(_connection)
            .Options;
        _dataContext = new DataContext(options);
        _dataContext.Database.EnsureCreated();
        _itemService = new ItemService(_dataContext, () => DateTime.UtcNow);
    }

    [TearDown]
    public void TearDown()
    {
        _dataContext.Database.EnsureDeleted();
        _connection?.Dispose();
    }

    [Test]
    public async Task GetAllItems_ReturnsAllItemsWithVariations()
    {
        var items = await _itemService.GetAllItems();

        Assert.That(items, Is.Not.Empty, "Should return at least one item");

        foreach (var item in items)
        {
            Assert.That(item.Variations, Is.Not.Null, "Each item should have variations collections initialized");

            if (item.Variations.Any())
            {
                foreach (var variation in item.Variations)
                {
                    Assert.That(variation.ItemId, Is.EqualTo(item.Id), "Variation should belong to the correct parent item");
                }
            }
        }

    }

    [Test]
    public async Task GetItemById_WithValidId_ReturnsItem()
    {
        var allItems = await _itemService.GetAllItems();
        var existingItem = allItems.First(i => i.Variations.Any());

        var item = await _itemService.GetItemById(existingItem.Id);

        Assert.Multiple(() =>
        {
            Assert.That(item, Is.Not.Null, "Should return an item for valid ID");
            Assert.That(item.Id, Is.EqualTo(existingItem.Id), "Should return the requested item");
            Assert.That(item.Variations, Is.Not.Null, "Should include variations");
            Assert.That(item.Variations.Count, Is.EqualTo(existingItem.Variations.Count),
                "Should have all variations loaded");
        });
    }

    [Test]
    public async Task GetItemById_WithInvalidId_ReturnsNull()
    {
        var item = await _itemService.GetItemById(Guid.NewGuid());

        Assert.That(item, Is.Null, "Should return null for non-existent ID");
    }

    [Test]
    public async Task CreateItem_WithValidData_CreatesNewItem()
    {
        var newItemReference = Guid.NewGuid().ToString("N");
        var variations = new List<Variation>
        {
            new() { Size = "S", Quantity = 5 },
            new() { Size = "M", Quantity = 3 }
        };

        var newItem = await _itemService.CreateItem(
            newItemReference,
            "Test Item",
            29.99m,
            "In Stock",
            null,
            variations
        );

        Assert.Multiple(() =>
        {
            Assert.That(newItem, Is.Not.Null, "Should return created item");
            Assert.That(newItem.Id, Is.Not.EqualTo(Guid.Empty), "Should assign new ID");
            Assert.That(newItem.Reference, Is.EqualTo(newItemReference), "Should save reference");
            Assert.That(newItem.Variations, Has.Count.EqualTo(variations.Count),
                "Should save all variations");
        });

        var savedItem = await _itemService.GetItemById(newItem.Id);
        Assert.That(savedItem, Is.Not.Null, "Should be able to retrieve saved item");
        Assert.That(savedItem.Variations, Has.Count.EqualTo(variations.Count),
            "Should persist all variations");
    }

    [Test]
    public async Task CreateItem_WithNullVariations_CreatesItemSuccessfully()
    {
        var reference = Guid.NewGuid().ToString("N");

        var newItem = await _itemService.CreateItem(
            reference,
            "Test Item",
            29.99m,
            "In Stock",
            null,
            null // null variations
        );

        Assert.That(newItem, Is.Not.Null);
        Assert.That(newItem.Variations, Is.Not.Null);
        Assert.That(newItem.Variations, Is.Empty);
    }

    [Test]
    public async Task UpdateItem_WithValidData_UpdatesAllFields()
    {
        var items = await _itemService.GetAllItems();
        var existingItem = items.First();
        var newName = "Updated " + Guid.NewGuid().ToString("N");
        var newPrice = 99.99m;
        var newVariations = new List<Variation>
        {
            new() { Size = "New Size", Quantity = 10 }
        };

        var updatedItem = await _itemService.UpdateItem(
            existingItem.Id,
            newName,
            newPrice,
            "In Stock",
            null,
            newVariations
        );

        Assert.Multiple(() =>
        {
            Assert.That(updatedItem, Is.Not.Null, "Should return updated item");
            Assert.That(updatedItem.Name, Is.EqualTo(newName), "Should update name");
            Assert.That(updatedItem.Price, Is.EqualTo(newPrice), "Should update price");
            Assert.That(updatedItem.Variations, Has.Count.EqualTo(newVariations.Count),
                "Should update variations");
        });

        var retrievedItem = await _itemService.GetItemById(existingItem.Id);
        Assert.That(retrievedItem.Name, Is.EqualTo(newName),
            "Changes should be persisted");
    }

    [Test]
    public async Task UpdateItem_WithInvalidId_ReturnsNull()
    {
        var result = await _itemService.UpdateItem(
            Guid.NewGuid(),
            "New Name",
            10.0m,
            "In Stock",
            null,
            new List<Variation>()
        );

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeleteItem_WithValidId_RemovesItem()
    {
        var newItem = await CreateTestItem();

        var result = await _itemService.DeleteItem(newItem.Id);
        var deletedItem = await _itemService.GetItemById(newItem.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Should return true on successful deletion");
            Assert.That(deletedItem, Is.Null, "Should not be able to retrieve deleted item");
        });
    }

    [Test]
    public async Task DeleteItem_WithInvalidId_ReturnsFalse()
    {
        var result = await _itemService.DeleteItem(Guid.NewGuid());

        Assert.That(result, Is.False, "Should return false when item doesn't exist");
    }

    [Test]
    public async Task GetAllItems_CalculatesCorrectDiscounts()
    {
        var item1 = await CreateTestItemWithVariations(new[]
        {
            new Variation { Size = "S", Quantity = 6 },
            new Variation { Size = "M", Quantity = 2 }
        });
        var item2 = await CreateTestItemWithVariations(new[]
        {
            new Variation { Size = "S", Quantity = 15 },
        });

        var items = await _itemService.GetAllItems();
        var retrievedItem1 = items.First(i => i.Id == item1.Id);
        var retrievedItem2 = items.First(i => i.Id == item2.Id);

        Assert.That(retrievedItem1.CurrentPrice, Is.EqualTo(retrievedItem1.Price * 0.9m),
            "Should apply 10% discount for total quantity > 5");
        Assert.That(retrievedItem2.CurrentPrice, Is.EqualTo(retrievedItem2.Price * 0.8m),
            "Should apply 20% discount for total quantity > 10");
    }

    [Test]
    public async Task GetAllItems_DoesNotApplyMondayDiscount_OutsideSpecifiedHours()
    {
        var item = await CreateTestItemWithVariations(new[]
        {
            new Variation { Size = "S", Quantity = 1 }
        });

        var mondayOutsideHours = new DateTime(2024, 1, 1, 11, 0, 0);
        var itemService = new ItemService(_dataContext, () => mondayOutsideHours);

        var items = await itemService.GetAllItems();
        var retrievedItem = items.First(i => i.Id == item.Id);

        Assert.That(retrievedItem.CurrentPrice, Is.EqualTo(retrievedItem.Price),
            "Should not apply Monday discount outside 12-5 PM");
    }

    [Test]
    public async Task GetAllItems_AppliesMondayDiscount_DuringSpecifiedHours()
    {
        var item = await CreateTestItemWithVariations(new[]
        {
            new Variation { Size = "S", Quantity = 1 }
        });

        var monday = new DateTime(2025, 4, 14, 14, 0, 0); // Monday
        var itemService = new ItemService(_dataContext, () => monday);

        var items = await itemService.GetAllItems();
        var retrievedItem = items.First(i => i.Id == item.Id);

        Console.WriteLine($"Original Price: {item.Price}");
        Console.WriteLine($"Current Price: {retrievedItem.CurrentPrice}");
        Console.WriteLine($"Day of Week: {monday.DayOfWeek}");
        Console.WriteLine($"Hour: {monday.Hour}");

        Assert.That(retrievedItem.CurrentPrice, Is.EqualTo(retrievedItem.Price * 0.5m),
            "Should apply 50% Monday discount between 12-5 PM");
    }

    [Test]
    public async Task GetAllItems_AppliesHighestDiscount_WhenMultipleDiscountsApply()
    {
        var item = await CreateTestItemWithVariations(new[]
        {
        new Variation { Size = "S", Quantity = 15 }
    });

        // Mock Monday at 2 PM (within 12-5 PM window) for 50% discount
        var monday = new DateTime(2025, 4, 14, 14, 0, 0); // Monday
        var itemService = new ItemService(_dataContext, () => monday);

        var items = await itemService.GetAllItems();
        var retrievedItem = items.First(i => i.Id == item.Id);

        Assert.That(retrievedItem.CurrentPrice, Is.EqualTo(retrievedItem.Price * 0.5m),
            "Should apply 50% Monday discount instead of 20% quantity discount");
    }


    [Test]
    public async Task GetAllItems_UpdatesStatusBasedOnTotalQuantity()
    {
        var itemWithStock = await CreateTestItemWithVariations(new[]
        {
            new Variation { Size = "S", Quantity = 1 }
        });

        var itemWithoutStock = await CreateTestItemWithVariations(new[]
        {
            new Variation { Size = "S", Quantity = 0 }
        });

        var items = await _itemService.GetAllItems();
        var retrievedWithStock = items.First(i => i.Id == itemWithStock.Id);
        var retrievedWithoutStock = items.First(i => i.Id == itemWithoutStock.Id);

        Assert.Multiple(() =>
        {
            Assert.That(retrievedWithStock.Status, Does.Contain("In Stock"),
                "Should show 'In Stock' when quantity > 0");
            Assert.That(retrievedWithoutStock.Status, Is.EqualTo("Sold Out"),
                "Should show 'Sold Out' when quantity = 0");
        });
    }




    private async Task<Item> CreateTestItem()
    {
        return await _itemService.CreateItem(
            Guid.NewGuid().ToString("N"),
            "Test Item",
            10.0m,
            null,
            null,
            new List<Variation>()
        );
    }

    private async Task<Item> CreateTestItemWithVariations(IEnumerable<Variation> variations)
    {
        return await _itemService.CreateItem(
            Guid.NewGuid().ToString("N"),
            "Test Item",
            10.0m,
            null,
            null,
            variations.ToList()
        );
    }
}