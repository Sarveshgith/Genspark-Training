namespace OrderNKitchenMS_API.Models.DTOs;

public class TableDto
{
	public int Id { get; set; }
	public int Number { get; set; }
	public int Status { get; set; }
	public string StatusName { get; set; } = string.Empty;
	public int Capacity { get; set; }
	public bool IsDeleted { get; set; }
}

public class TableCreateDto
{
	public int Number { get; set; }
	public int Capacity { get; set; }
	public int? Status { get; set; }
}

public class TableUpdateDto
{
	public int Number { get; set; }
	public int Capacity { get; set; }
}

public class TableStatusUpdateDto
{
	public int Status { get; set; }
}
