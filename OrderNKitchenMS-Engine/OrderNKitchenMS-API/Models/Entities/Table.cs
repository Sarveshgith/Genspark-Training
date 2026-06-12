using System;
using System.ComponentModel.DataAnnotations;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Models.Entities;

public class Table : BaseEntity
{
    public int Id {get; set;}

    [Range(1, 100)]
    public required int Number {get; set;}

    public TableStatus Status {get; set;} = TableStatus.Available;

    [Range(1, 100)]
    public required int Capacity {get; set;}

    public bool IsDeleted {get; set;}
}
