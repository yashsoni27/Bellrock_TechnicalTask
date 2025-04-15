using DotNetInterview.Web.Models;
using DotNetInterview.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotNetInterview.Web.Pages.Items;

public class CreateModel : PageModel
{
    private readonly ApiService _apiService;

    [BindProperty]
    public Item Item { get; set; } = new();

    [BindProperty]
    public List<Variation> Variations { get; set; } = new();

    public CreateModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void OnGet()
    {
        Variations.Add(new Variation());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Item.Variations = Variations
                .Where(v => !string.IsNullOrEmpty(v.Size))
                .ToList();

            var createdItem = await _apiService.PostAsync<Item, Item>("api/CreateItem", Item);
            
            if (createdItem != null)
            {
                return RedirectToPage("/Index");
            }
            else
            {
                ModelState.AddModelError("", "Failed to create item");
                return Page();
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error creating item: {ex.Message}");
            return Page();
        }
    }
} 