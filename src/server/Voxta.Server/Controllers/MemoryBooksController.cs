using Microsoft.AspNetCore.Mvc;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Server.ViewModels.MemoryBooks;

namespace Voxta.Server.Controllers;

public class MemoryBooksController : Controller
{
    private readonly IMemoryRepository _repository;

    public MemoryBooksController(IMemoryRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("/characters/{id}/memory")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var memoryBook = await _repository.GetCharacterBookAsync(id);
        var vm = new MemoryBookViewModel
        {
            Id = memoryBook.CharacterId,
            Name = memoryBook.Name,
            Description = memoryBook.Description,
            Items = memoryBook.Items.Select(MemoryItemViewModel.Create).ToList()
        };
        return View(vm);
    }

    [HttpPost("/characters/{id}/memory")]
    public async Task<IActionResult> Edit([FromRoute] Guid id, MemoryBookViewModel data)
    {
        var book = await _repository.GetCharacterBookAsync(id);
        book.Name = data.Name;
        book.Description = data.Description;
        book.Items = data.Items.Select(x => x.ToModel()).ToList();
        await _repository.SaveBookAsync(book);
        return RedirectToAction("Edit", new { id });
    }

    [HttpPost("/characters/{id}/memory/add")]
    public async Task<IActionResult> AddItem(Guid id)
    {
        var memoryBook = await _repository.GetCharacterBookAsync(id);
        memoryBook.Items.Add(new MemoryItem
        {
            Id = Guid.NewGuid(),
            Keywords = Array.Empty<string>(),
            Text = "",
            Weight = 0,
        });
        await _repository.SaveBookAsync(memoryBook);
        return PartialView("_Items", memoryBook.Items.Select(MemoryItemViewModel.Create).ToList());
    }

    [HttpPost("/characters/{id}/memory/items/{entryId}/remove")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid entryId)
    {
        var memoryBook = await _repository.GetCharacterBookAsync(id);
        memoryBook.Items.Remove(memoryBook.Items.First(item => item.Id == entryId));
        await _repository.SaveBookAsync(memoryBook);
        return PartialView("_Items", memoryBook.Items.Select(MemoryItemViewModel.Create).ToList());
    }

    [HttpPost("/characters/{id}/memory/reorder")]
    public async Task<IActionResult> ReorderItems(Guid id, List<int> order)
    {
        var memoryBook = await _repository.GetCharacterBookAsync(id);
        memoryBook.Items = order.Select(index => memoryBook.Items[index]).ToList();
        await _repository.SaveBookAsync(memoryBook);
        return PartialView("_Items", memoryBook.Items.Select(MemoryItemViewModel.Create).ToList());
    }
}
