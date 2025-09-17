using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class GroupController : Controller
{
    private readonly AppDbContext _context;
    
    public GroupController(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<IActionResult> Index()
    {
        var groups = await _context.Groups.ToListAsync();
        return View(groups);
    }
    
    public async Task<IActionResult> Details(int id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null) return NotFound();
        
        return View(group);
    }
    
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(Group group)
    {
        if (ModelState.IsValid)
        {
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        return View(group);
    }
}