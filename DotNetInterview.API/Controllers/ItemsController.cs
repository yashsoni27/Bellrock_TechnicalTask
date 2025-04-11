using DotNetInterview.API.Domain;
using Microsoft.AspNetCore.Mvc;

namespace DotNetInterview.API.Controllers
{
    [ApiController]
    [Route("api/[action]")]
    public class ItemsController : ControllerBase
    {
        private readonly ItemService _itemService;

        public ItemsController(ItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Item>>> GetItems()
        {
            var items = await _itemService.GetAllItems();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Item>> GetItem(Guid id)
        {
            var item = await _itemService.GetItemById(id);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Item>> CreateItem([FromBody] Item item)
        {
            if (item == null)
                return BadRequest("Invalid item data.");
            var createdItem = await _itemService.CreateItem(
                item.Reference,
                item.Name,
                item.Price
            );

            return CreatedAtAction(nameof(GetItem), new { id = createdItem.Id }, createdItem);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Item>> UpdateItem(Guid id, [FromBody] Item item)
        {
            if (item == null)
                return BadRequest("Invalid item data.");

            var updatedItem = await _itemService.UpdateItem(id, item.Name, item.Price);
            if (updatedItem == null)
                return NotFound();

            return Ok(updatedItem);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(Guid id)
        {
            var result = await _itemService.DeleteItem(id);
            if (!result)
                return NotFound();
            return NoContent();
        }
    }
}
