using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MuafaPlus.Models;
using MuafaPlus.Services;

namespace MuafaPlus.Controllers;

/// <summary>
/// Phase 1: Tenant management endpoints.
/// All routes require a valid JWT (Rule 6).
/// </summary>
[ApiController]
[Route("api/v1/tenants")]
[Authorize]
[Produces("application/json")]
public class TenantsController : ControllerBase
{
    private readonly TenantService              _tenants;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        TenantService              tenants,
        ILogger<TenantsController> logger)
    {
        _tenants = tenants;
        _logger  = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns all tenants ordered by creation date (newest first).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TenantResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<TenantResponse>>>> GetAll()
    {
        var tenants = await _tenants.GetAllTenantsAsync();
        return Ok(new ApiResponse<List<TenantResponse>> { Success = true, Data = tenants });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/tenants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new tenant with default settings, initial subscription,
    /// and an HA-XXXXXX invitation code for the first Hospital Admin.
    /// Returns 201 Created with the full TenantResponse.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TenantResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>),         StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> Create(
        [FromBody] CreateTenantRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Validation failed.",
                Metadata = new Dictionary<string, object>
                {
                    ["errors"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                }
            });

        try
        {
            var tenant = await _tenants.CreateTenantAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = tenant.TenantId },
                new ApiResponse<TenantResponse> { Success = true, Data = tenant });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tenant");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success   = false,
                Error     = "Failed to create tenant.",
                ErrorType = ex.GetType().Name
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants/{id}
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns a single tenant by ID. Returns 404 if not found.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),         StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantResponse>>> GetById(Guid id)
    {
        var tenant = await _tenants.GetTenantAsync(id);
        if (tenant == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Tenant {id} not found.",
                ErrorType = "NotFound"
            });

        return Ok(new ApiResponse<TenantResponse> { Success = true, Data = tenant });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants/{id}/settings
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns settings for the specified tenant.</summary>
    [HttpGet("{id:guid}/settings")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                 StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantSettingsResponse>>> GetSettings(Guid id)
    {
        var tenant = await _tenants.GetTenantAsync(id);
        if (tenant == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Tenant {id} not found.",
                ErrorType = "NotFound"
            });

        return Ok(new ApiResponse<TenantSettingsResponse> { Success = true, Data = tenant.Settings });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT /api/v1/tenants/{id}/settings
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates tenant settings. All fields are optional — only non-null values
    /// are applied.
    /// </summary>
    [HttpPut("{id:guid}/settings")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                 StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>),                 StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantSettingsResponse>>> UpdateSettings(
        Guid id,
        [FromBody] UpdateTenantSettingsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Validation failed.",
                Metadata = new Dictionary<string, object>
                {
                    ["errors"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                }
            });

        try
        {
            var settings = await _tenants.UpdateTenantSettingsAsync(id, request);
            return Ok(new ApiResponse<TenantSettingsResponse> { Success = true, Data = settings });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = ex.Message,
                ErrorType = "NotFound"
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/v1/tenants/{id}/subscription
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the active subscription for the specified tenant.
    /// Returns 404 if there is no active subscription.
    /// </summary>
    [HttpGet("{id:guid}/subscription")]
    [ProducesResponseType(typeof(ApiResponse<TenantSubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),                     StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantSubscriptionResponse>>> GetSubscription(Guid id)
    {
        var sub = await _tenants.GetTenantSubscriptionAsync(id);
        if (sub == null)
            return NotFound(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"No active subscription found for tenant {id}.",
                ErrorType = "NotFound"
            });

        return Ok(new ApiResponse<TenantSubscriptionResponse> { Success = true, Data = sub });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/v1/tenants/{id}/assistant-links
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Links an assistant to a physician within the specified tenant.
    /// Returns 409 Conflict if the link already exists and is active.
    /// </summary>
    [HttpPost("{id:guid}/assistant-links")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> LinkAssistant(
        Guid id,
        [FromBody] LinkAssistantRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error   = "Validation failed.",
                Metadata = new Dictionary<string, object>
                {
                    ["errors"] = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                }
            });

        var linked = await _tenants.LinkAssistantToPhysicianAsync(id, request);
        if (!linked)
            return Conflict(new ApiResponse<object>
            {
                Success   = false,
                Error     = $"Assistant {request.AssistantId} is already linked to physician {request.PhysicianId} in this tenant.",
                ErrorType = "Conflict"
            });

        return Ok(new ApiResponse<object>
        {
            Success  = true,
            Metadata = new Dictionary<string, object>
            {
                ["assistantId"] = request.AssistantId,
                ["physicianId"] = request.PhysicianId,
                ["tenantId"]    = id
            }
        });
    }
}
