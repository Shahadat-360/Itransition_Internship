using FormsApp.Data;
using FormsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FormsApp.Controllers
{
    [Authorize]
    public class TopicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TopicController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Topic
        public async Task<IActionResult> Index()
        {
            var topics = await _context.Topics
                .OrderBy(t => t.Name)
                .ToListAsync();
                
            // Get template counts for each topic
            foreach (var topic in topics)
            {
                var count = await _context.FormTemplates
                    .CountAsync(t => t.TopicId == topic.Id);
                ViewData[$"TemplateCount_{topic.Id}"] = count;
            }
                
            return View(topics);
        }

        // GET: Topic/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Topic/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Topic topic, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                topic.CreatedAt = DateTime.UtcNow;
                _context.Add(topic);
                await _context.SaveChangesAsync();
                
                // If AJAX request, return JSON response
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, topicId = topic.Id, topicName = topic.Name });
                }
                
                // Add TempData message for the global notification system
                TempData["SuccessMessage"] = "Topic created successfully! You can create another one below.";
                
                // If returnUrl is provided and it's a local URL, redirect to it
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    // Add the newly created topic ID as a parameter to help populate dropdown
                    if (returnUrl.Contains("?"))
                    {
                        returnUrl += $"&newTopicId={topic.Id}";
                    }
                    else
                    {
                        returnUrl += $"?newTopicId={topic.Id}";
                    }
                    return Redirect(returnUrl);
                }
                
                // Stay on the Create page instead of redirecting to Index
                ModelState.Clear();
                ViewData["TopicCreated"] = true;
                ViewData["CreatedTopic"] = new { Id = topic.Id, Name = topic.Name };
                
                // Preserve the referrer URL so the "Back to Template" button continues to work
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    ViewData["PreservedReferrer"] = returnUrl;
                }
                
                return View(new Topic());
            }
            
            // If AJAX request, return JSON response with errors
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(", ", errors) });
            }
            
            return View(topic);
        }

        // GET: Topic/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }
            return View(topic);
        }

        // POST: Topic/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Topic topic)
        {
            if (id != topic.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing topic to preserve CreatedAt
                    var existingTopic = await _context.Topics.FindAsync(id);
                    if (existingTopic == null)
                    {
                        return NotFound();
                    }
                    
                    // Update only specific properties
                    existingTopic.Name = topic.Name;
                    existingTopic.Description = topic.Description;
                    
                    _context.Update(existingTopic);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Topic updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TopicExists(topic.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(topic);
        }

        // GET: Topic/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (topic == null)
            {
                return NotFound();
            }

            // Get templates using this topic
            var templatesUsingTopic = await _context.FormTemplates
                .Where(t => t.TopicId == id)
                .Select(t => new { t.Id, t.Title })
                .ToListAsync();
                
            ViewData["TemplatesUsingTopic"] = templatesUsingTopic;
            ViewData["TemplateCount"] = templatesUsingTopic.Count;
            
            // Get other topics for reassignment
            ViewData["AvailableTopics"] = await _context.Topics
                .Where(t => t.Id != id)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return View(topic);
        }

        // POST: Topic/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            
            if (topic == null)
            {
                return NotFound();
            }
            
            // Check if topic is used in any templates
            var templateCount = await _context.FormTemplates
                .CountAsync(t => t.TopicId == id);
                
            if (templateCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete topic because it is used in {templateCount} templates. Please use the 'Reassign Templates' option first.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            
            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Topic deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        // POST: Topic/ReassignTemplates
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignTemplates(int sourceTopicId, int targetTopicId)
        {
            if (sourceTopicId == targetTopicId)
            {
                TempData["ErrorMessage"] = "Source and target topics cannot be the same.";
                return RedirectToAction(nameof(Delete), new { id = sourceTopicId });
            }
            
            var sourceTopic = await _context.Topics.FindAsync(sourceTopicId);
            var targetTopic = await _context.Topics.FindAsync(targetTopicId);
            
            if (sourceTopic == null || targetTopic == null)
            {
                return NotFound();
            }
            
            // Get templates using source topic
            var templates = await _context.FormTemplates
                .Where(t => t.TopicId == sourceTopicId)
                .ToListAsync();
                
            if (!templates.Any())
            {
                TempData["WarningMessage"] = "No templates found to reassign.";
                return RedirectToAction(nameof(Delete), new { id = sourceTopicId });
            }
            
            // Reassign templates to target topic
            foreach (var template in templates)
            {
                template.TopicId = targetTopicId;
            }
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Successfully reassigned {templates.Count} templates from '{sourceTopic.Name}' to '{targetTopic.Name}'.";
            return RedirectToAction(nameof(Delete), new { id = sourceTopicId });
        }

        // Helper method to check if topic exists
        private bool TopicExists(int id)
        {
            return _context.Topics.Any(e => e.Id == id);
        }
        
        // GET: Topic/GetTopicsForDropdown
        [HttpGet]
        public async Task<IActionResult> GetTopicsForDropdown()
        {
            var topics = await _context.Topics
                .OrderBy(t => t.Name)
                .Select(t => new { id = t.Id, name = t.Name })
                .ToListAsync();
                
            return Json(topics);
        }

        // POST: Topic/BatchDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchDelete(List<int> selectedTopics)
        {
            if (selectedTopics == null || !selectedTopics.Any())
            {
                TempData["ErrorMessage"] = "No topics were selected for deletion.";
                return RedirectToAction(nameof(Index));
            }
            
            int deletedCount = 0;
            List<string> errors = new List<string>();
            
            foreach (var topicId in selectedTopics)
            {
                var topic = await _context.Topics.FindAsync(topicId);
                if (topic == null)
                {
                    continue; // Skip if topic doesn't exist
                }
                
                // Check if topic is used in any templates
                var templateCount = await _context.FormTemplates.CountAsync(t => t.TopicId == topicId);
                
                if (templateCount > 0)
                {
                    errors.Add($"Topic '{topic.Name}' is used by {templateCount} templates and cannot be deleted.");
                    continue;
                }
                
                try
                {
                    _context.Topics.Remove(topic);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error deleting topic '{topic.Name}': {ex.Message}");
                }
            }
            
            if (deletedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} topic{(deletedCount != 1 ? "s" : "")}.";
            }
            
            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join("<br>", errors);
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
} 