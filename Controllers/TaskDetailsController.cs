using client.Data;
using client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace client.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaskDetailsController : ControllerBase
{
    private readonly AppDbContext _context;
    public TaskDetailsController(AppDbContext context)
    { _context = context; }

    [HttpGet]
    public async Task<IActionResult> GetTaskDetails(int taskId)
    {
        var details = await _context.TaskDetails .Where(x => x.TaskId == taskId) .OrderBy(x => x.TaskDetailId) .ToListAsync();
        return Ok(details);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskDetail(int id)
    {
        var detail = await _context.TaskDetails .FirstOrDefaultAsync(x =>  x.TaskDetailId == id);
        if (detail == null) return NotFound("Task detail not found");
        return Ok(detail);
    }

    [HttpPost]
    public async Task<IActionResult> AddTaskDetail( TaskDetail detail)
    {
        var task = await _context.Tasks .FindAsync(detail.TaskId);
        if (task == null)  return BadRequest("Task not found");
        detail.Amount = detail.Qty * detail.Rate;
        _context.TaskDetails.Add(detail);
        await _context.SaveChangesAsync();
        return Ok(detail);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> AddTaskDetails( List<TaskDetail> details)
    { foreach (var detail in details)
        { detail.Amount = detail.Qty * detail.Rate; }
        _context.TaskDetails.AddRange(details); await _context.SaveChangesAsync();
        return Ok(details);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTaskDetail(int id,TaskDetail detail)
    {
        if (id != detail.TaskDetailId) return BadRequest();
        var existing = await _context.TaskDetails .FindAsync(id);
        if (existing == null) return NotFound();
        existing.Description = detail.Description;
        existing.Qty = detail.Qty;
        existing.Rate = detail.Rate;
        existing.Amount = detail.Qty * detail.Rate;
        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTaskDetail( int id)
    {
        var detail = await _context.TaskDetails .FindAsync(id);
        if (detail == null) return NotFound();
        _context.TaskDetails.Remove(detail);
        await _context.SaveChangesAsync();
        return Ok(new
        { message = "Task detail deleted successfully" });
    }
}