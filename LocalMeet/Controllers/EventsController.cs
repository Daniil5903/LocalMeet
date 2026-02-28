using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocalMeet.Data;
using LocalMeet.Services.Interfaces;
using LocalMeet.ViewModels;

namespace LocalMeet.Controllers
{
    public class EventsController : Controller
    {
        private readonly IEventService _eventService;
        private readonly ApplicationDbContext _context;

        public EventsController(
            IEventService eventService,
            ApplicationDbContext context)
        {
            _eventService = eventService;
            _context = context;
        }

        public async Task<IActionResult> Index(
            int? categoryId,
            string? search,
            bool showPast = false,
            int page = 1)
        {
            int pageSize = 5;

            var (events, totalCount) =
                await _eventService.GetFilteredEventsAsync(
                    categoryId,
                    search,
                    showPast,
                    page,
                    pageSize);

            var categories = await _context.Categories.ToListAsync();

            var vm = new EventListViewModel
            {
                Events = events,
                Categories = categories,
                SelectedCategoryId = categoryId,
                Search = search,
                ShowPast = showPast,
                Page = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return View(vm);
        }
    }
}