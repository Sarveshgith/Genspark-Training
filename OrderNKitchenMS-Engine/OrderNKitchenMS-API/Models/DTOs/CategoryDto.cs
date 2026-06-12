namespace OrderNKitchenMS_API.Models.DTOs;

public class CategoryDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public bool IsNonVeg { get; set; }
	public bool IsDeleted { get; set; }
}

public class CategoryCreateDto
{
	public string Name { get; set; } = string.Empty;
	public bool IsNonVeg { get; set; }
}

public class CategoryUpdateDto
{
	public string Name { get; set; } = string.Empty;
	public bool IsNonVeg { get; set; }
}
