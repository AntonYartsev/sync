using Microsoft.AspNetCore.Mvc;
using Sync.Backend.Models;
using Sync.Backend.Services;

namespace Sync.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EditorController : ControllerBase
{
    private readonly IEditorService _editorService;
    private readonly ILogger<EditorController> _logger;

    public EditorController(IEditorService editorService, ILogger<EditorController> logger)
    {
        _editorService = editorService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EditorState>> GetEditor(string id)
    {
        try
        {
            var editor = await _editorService.GetEditorStateAsync(id);
            return Ok(editor);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Editor with ID {id} not found");
        }
    }

    [HttpPost]
    public async Task<ActionResult<EditorState>> CreateEditor()
    {
        var editor = await _editorService.CreateEditorStateAsync();
        return CreatedAtAction(nameof(GetEditor), new { id = editor.Id }, editor);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EditorState>> UpdateEditor(string id, [FromBody] string content)
    {
        try
        {
            var editor = await _editorService.UpdateEditorStateAsync(id, content);
            return Ok(editor);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Editor with ID {id} not found");
        }
    }
} 