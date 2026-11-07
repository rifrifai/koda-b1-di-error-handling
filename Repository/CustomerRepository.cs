using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using API.Models;
using Microsoft.Data.SqlClient;

namespace API.Repository;

public class CustomerRepository(IConfiguration config) : ICustomerRepository
{
	private readonly string _connString = config.GetConnectionString("ecommercedb") ?? throw new InvalidOperationException("Connection string ecommerce menghilang!");
	public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct)
	{
		const string sql = """
		SELECT c.CustomerId, c.Name, c.Email, c.Phone, c.Address, p.ProductId, p.ProductCode, p.Name AS ProductName, p.Price, p.Stock, p.Category FROM dbo.Customer c LEFT JOIN dbo.Product p ON c.CustomerId = p.CustomerId ORDER BY c.CustomerId;
		""";

		var customerDict = new Dictionary<int, Customer>();
		await using var conn = new SqlConnection(_connString);

		await conn.OpenAsync(ct);

		await using var command = new SqlCommand(sql, conn);

		await using var reader = await command.ExecuteReaderAsync(ct);

		while (await reader.ReadAsync(ct))
		{
			int customerId = reader.GetInt32(0);

			if (!customerDict.TryGetValue(customerId, out var customer))
			{
				customer = new Customer(
					customerId,
					reader.GetString(1),
					reader.GetString(2),
					reader.GetString(3),
					reader.GetString(4)
				);
				customerDict.Add(customerId, customer);
			}

			if (!reader.IsDBNull(5))
			{
				var product = new Product(
					reader.GetString(6),
					reader.GetString(7),
					reader.GetDecimal(8),
					reader.GetInt32(9),
					reader.GetString(10)
				);
				
				customer.Products.Add(product);
			}
		}
		return customerDict.Values.ToList();
	}

	public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
	{
		const string sql = """
			SELECT c.CustomerId, c.Name, c.Email, c.Phone, c.Address, p.ProductId, p.ProductCode, p.Name, p.Price, p.Stock, p.Category FROM dbo.Customer c LEFT JOIN dbo.Product p ON c.CustomerId = p.CustomerId WHERE c.CustomerId = @customerId;
		""";

		await using var conn = new SqlConnection(_connString);
		await conn.OpenAsync(ct);
		await using var command = new SqlCommand(sql, conn);
		command.Parameters.Add("@customerId", SqlDbType.Int).Value = id;
		await using var reader = await command.ExecuteReaderAsync(ct);
		Customer? customer = null;
		while (await reader.ReadAsync(ct))
		{
			customer ??= new Customer(
					reader.GetInt32(0),
					reader.GetString(1),
					reader.GetString(2),
					reader.GetString(3),
					reader.GetString(4)
				);

			if (!reader.IsDBNull(5))
			{
				var product = new Product(
					reader.GetString(6),
					reader.GetString(7),
					reader.GetDecimal(8),
					reader.GetInt32(9),
					reader.GetString(10)
				);
				
				customer.Products.Add(product);
			}
		}
		return customer;
	}

	public async Task<bool> TryAddAsync(Customer customer, CancellationToken ct)
	{
		const string sql = """
			INSERT INTO dbo.Customer(Name, Email, Phone, Address)OUTPUT INSERTED.CustomerId VALUES(@name, @email, @phone, @address);
		""";

		await using var conn = new SqlConnection(_connString);
		await conn.OpenAsync(ct);
		await using var command = new SqlCommand(sql, conn);

		command.Parameters.Add("@name", SqlDbType.NVarChar, 120).Value = customer.Name;
		command.Parameters.Add("@email", SqlDbType.NVarChar, 50).Value = customer.Email;
		command.Parameters.Add("@phone", SqlDbType.NVarChar, 50).Value = customer.Phone;
		command.Parameters.Add("@address", SqlDbType.NVarChar, 80).Value = customer.Address;
		
		try
		{
			var insertedId = await command.ExecuteScalarAsync(ct);

			if (insertedId != null)
			{
				customer.Id = (int)insertedId;
				return true;
			}
			return false;
		}
		catch (SqlException e) when(e.Number is 2601 or 2627)
		{
			return false;
		}catch (SqlException e)
		{
			Console.WriteLine($"SQL error tidak terduga: {e.Number} - {e.Message}");
			return false;
		}
	}

	public async Task<bool> TryDeleteAsync(int id, CancellationToken ct)
	{
		const string sql = """
			DELETE FROM dbo.Customer WHERE CustomerId = @customerId;
		""";

		await using var conn = new SqlConnection(_connString);
		await conn.OpenAsync(ct);
		await using var command = new SqlCommand(sql, conn);
		AddCustomerId(command, id);

		var affectedRows = await command.ExecuteNonQueryAsync(ct);
		return affectedRows == 1;
	}

	public async Task<bool> TryUpdateAsync(Customer customer, CancellationToken ct)
	{
		const string sql = """
			UPDATE dbo.Customer SET Name = @name, Email = @email, Phone = @phone, Address = @address WHERE CustomerId = @customerId; 
		""";
		await using var conn = new SqlConnection(_connString);
		await conn.OpenAsync(ct);
		await using var command = new SqlCommand(sql, conn);

		AddCustomerParameter(command, customer
		);
		var affectedRows = await command.ExecuteNonQueryAsync(ct);

		return affectedRows == 1;
	}

		// =========== METHOD ==========

	private static Customer ReadCustomer(SqlDataReader reader)
	{
		return new Customer(
			reader.GetInt32(0),
			reader.GetString(1),
			reader.GetString(2),
			reader.GetString(3),
			reader.GetString(4)
		);
	}
	private static void AddCustomerId(SqlCommand command, int id)
	{
		command.Parameters.Add("@customerId", System.Data.SqlDbType.Int).Value = id;
	}
	private static void AddCustomerParameter(SqlCommand command, Customer customer)
	{
		AddCustomerId(command, customer.Id);
		command.Parameters.Add("@name", SqlDbType.NVarChar, 120).Value = customer.Name;
		command.Parameters.Add("@email", SqlDbType.NVarChar, 50).Value = customer.Email;
		command.Parameters.Add("@phone", SqlDbType.NVarChar, 50).Value = customer.Phone;
		command.Parameters.Add("@address", SqlDbType.NVarChar, 80).Value = customer.Address;
	}
}