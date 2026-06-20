export interface CategoryModel {
    id: number;
	name: string;
	isNonVeg: boolean;
	isDeleted: boolean;
}

export interface MenuItemModel {
	id: number;
	name: string;
	description: string;
	price: number;
	categoryId: number;
	categoryName: string;
	imageUrl: string;
	preparationTime: number;
	isAvailable: boolean;
	isManuallyDisabled: boolean;
}

export interface QueryMenuItemModel {
	name?: string | null;
	categoryId?: number | null;
	minPrice?: number | null;
	maxPrice?: number | null;
	isAvailable?: boolean | null;
	maxPreparationTime?: number | null;
	pageNumber?: number;
	pageSize?: number;
}

// public class MenuItemCreateDto
// {
//     public string Name { get; set; } = string.Empty;
//     public string Description { get; set; } = string.Empty;
//     public decimal Price { get; set; }
//     public int CategoryId { get; set; }
//     public string ImageUrl { get; set; } = string.Empty;
//     public int PreparationTime { get; set; }
//     public bool IsAvailable { get; set; } = true;
// }

// public class MenuItemUpdateDto
// {
//     public string Name { get; set; } = string.Empty;
//     public string Description { get; set; } = string.Empty;
//     public decimal Price { get; set; }
//     public int CategoryId { get; set; }
//     public string ImageUrl { get; set; } = string.Empty;
//     public int PreparationTime { get; set; }
//     public bool IsAvailable { get; set; }
// }

// public class MenuItemAvailabilityDto
// {
//     public bool IsAvailable { get; set; }
// }
