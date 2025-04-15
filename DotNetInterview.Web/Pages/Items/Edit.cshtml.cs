using DotNetInterview.Web.Models;
using DotNetInterview.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotNetInterview.Web.Pages.Items;

public class EditModel : PageModel
{
    private readonly ApiService _apiService;

    [BindProperty]
    public Item Item { get; set; }

    [BindProperty]
    public List<Variation> Variations { get; set; } = new();

    public EditModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        try
        {
            Item = await _apiService.GetAsync<Item>($"api/GetItem/{id}");
            if (Item == null)
            {
                return NotFound();
            }
            Variations = Item.Variations ?? new List<Variation>();
            return Page();
        }
        catch (Exception ex)
        {
            // Add logging here if needed
            return RedirectToPage("/Error");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Ensure variations are properly attached to the item
            if (Item.Variations == null)
            {
                Item.Variations = new List<Variation>();
            }

            // Add all variations from the form
            foreach (var variation in Variations.Where(v => !string.IsNullOrEmpty(v.Size)))
            {
                Item.Variations.Add(variation);
            }

            var updatedItem = await _apiService.PutAsync<Item, Item>($"api/UpdateItem/{Item.Id}", Item);
            
            if (updatedItem != null)
            {
                return RedirectToPage("/Index");
            }
            else
            {
                ModelState.AddModelError("", "Failed to update item");
                return Page();
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error updating item: {ex.Message}");
            return Page();
        }
    }
} 