using DotNetInterview.Web.Models;
using DotNetInterview.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotNetInterview.Web.Pages.Items;

public class ViewModel : PageModel
{
    private readonly ApiService _apiService;
    public Item Item { get; set; }

    public ViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Item = await _apiService.GetAsync<Item>($"api/GetItem/{id}");
        if (Item == null)
        {
            return NotFound();
        }
        return Page();
    }
} 