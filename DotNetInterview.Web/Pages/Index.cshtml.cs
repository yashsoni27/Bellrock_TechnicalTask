using DotNetInterview.Web.Models;
using DotNetInterview.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotNetInterview.Web.Pages;

public class ItemsModel : PageModel
{
    private readonly ApiService _apiService;
    public List<Item> Items { get; set; } = new();

    public ItemsModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task OnGetAsync()
    {
        Items = await _apiService.GetAsync<List<Item>>("api/GetItems");
    }
}