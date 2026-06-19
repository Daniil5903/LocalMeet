using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRole.Admin)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? sortOrder)
        {
            ViewData["CurrentSort"] = sortOrder;

            ViewData["IdSort"] = string.IsNullOrWhiteSpace(sortOrder) ? "id_desc" : "";
            ViewData["NameSort"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["EventsCountSort"] = sortOrder == "events_count" ? "events_count_desc" : "events_count";

            var query = _context.Categories
                .Include(c => c.Events)
                .AsQueryable();

            query = sortOrder switch
            {
                "id_desc" => query.OrderByDescending(c => c.Id),

                "name" => query.OrderBy(c => c.Name),
                "name_desc" => query.OrderByDescending(c => c.Name),

                "events_count" => query.OrderBy(c => c.Events.Count),
                "events_count_desc" => query.OrderByDescending(c => c.Events.Count),

                _ => query.OrderBy(c => c.Id)
            };

            var categories = await query.ToListAsync();

            return View(categories);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CategoryFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedName = model.Name.Trim();

            var exists = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == normalizedName.ToLower());

            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Категория с таким названием уже существует");
                return View(model);
            }

            var category = new Category
            {
                Name = normalizedName
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Категория успешно создана";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == model.Id);

            if (category == null)
            {
                return NotFound();
            }

            var normalizedName = model.Name.Trim();

            var exists = await _context.Categories
                .AnyAsync(c => c.Id != model.Id && c.Name.ToLower() == normalizedName.ToLower());

            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Категория с таким названием уже существует");
                return View(model);
            }

            category.Name = normalizedName;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Категория успешно обновлена";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Events)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Events)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Events.Any())
            {
                TempData["ErrorMessage"] = "Нельзя удалить категорию, к которой привязаны мероприятия";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Категория успешно удалена";

            return RedirectToAction(nameof(Index));
        }
    }
}