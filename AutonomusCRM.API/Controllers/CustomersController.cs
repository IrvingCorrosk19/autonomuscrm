using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Application.Customers.Queries;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IRequestHandler<CreateCustomerCommand, Guid> _createHandler;
    private readonly IRequestHandler<UpdateCustomerStatusCommand, bool> _updateStatusHandler;
    private readonly IRequestHandler<GetCustomerByIdQuery, CustomerDto?> _getByIdHandler;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        IRequestHandler<CreateCustomerCommand, Guid> createHandler,
        IRequestHandler<UpdateCustomerStatusCommand, bool> updateStatusHandler,
        IRequestHandler<GetCustomerByIdQuery, CustomerDto?> getByIdHandler,
        ILogger<CustomersController> logger)
    {
        _createHandler = createHandler;
        _updateStatusHandler = updateStatusHandler;
        _getByIdHandler = getByIdHandler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateCustomer([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var customerId = await _createHandler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetCustomer), new { id = customerId }, customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var query = new GetCustomerByIdQuery(id, tenantId);
        var customer = await _getByIdHandler.HandleAsync(query, cancellationToken);
        
        if (customer == null)
            return NotFound();

        return Ok(customer);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] UpdateCustomerStatusCommand command, CancellationToken cancellationToken)
    {
        if (id != command.CustomerId)
            return BadRequest(new { error = "ID mismatch" });

        var result = await _updateStatusHandler.HandleAsync(command, cancellationToken);
        
        if (!result)
            return NotFound();

        return NoContent();
    }
}

