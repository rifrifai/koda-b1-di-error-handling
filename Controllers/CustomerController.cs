using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs.Customers;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class CustomerController(ICustomerService service) : ControllerBase
{
    private readonly ICustomerService _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Customer>>> GetAll(CancellationToken ct)
    {
        var customers = await _service.GetAllAsync(ct);
        return Ok(customers);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetById([FromRoute] int id, CancellationToken ct)
    {
        var customer = await _service.GetByIdAsync(id, ct);

        return customer is not null ? Ok(customer) : Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Customer does not exist"
        );
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CustomerRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(request, ct);

            if (created is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Gagal membuat data. Email atau nomor telepon mungkin sudah terdaftar!"
                );
            }
            return CreatedAtAction(
                nameof(GetById),
                new { id =  created.Id },
                created
            );
        }
        catch (ArgumentException e)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Product Data",
                detail: e.Message
            );
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
    
        return deleted ? NoContent() : Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Customer not found!",
            detail: $"No customer was found for id {id}."
        );
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> Update(
    [FromRoute] int id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        try
        {
            var existingProduct = await _service.UpdateAsync(id, request, ct);

            return existingProduct is null ? 
            Problem(
                statusCode: StatusCodes.Status404NotFound, 
                title: "Customer not found", 
                detail: $"No customer was found for ID {id}.") : Ok(existingProduct);
        }
        catch (ArgumentException e)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid product data!",
                detail: e.Message);
        }
    }
}