using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderNKitchenMS_API.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquenessConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tables_Number",
                table: "Tables");

            migrationBuilder.DropIndex(
                name: "IX_MenuItemIngredients_MenuItemId",
                table: "MenuItemIngredients");

            migrationBuilder.CreateIndex(
                name: "IX_Tables_Number",
                table: "Tables",
                column: "Number",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_Name",
                table: "MenuItems",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemIngredients_MenuItemId_ItemId",
                table: "MenuItemIngredients",
                columns: new[] { "MenuItemId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Name",
                table: "Items",
                column: "Name",
                unique: true,
                filter: "\"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tables_Number",
                table: "Tables");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_Name",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItemIngredients_MenuItemId_ItemId",
                table: "MenuItemIngredients");

            migrationBuilder.DropIndex(
                name: "IX_Items_Name",
                table: "Items");

            migrationBuilder.CreateIndex(
                name: "IX_Tables_Number",
                table: "Tables",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemIngredients_MenuItemId",
                table: "MenuItemIngredients",
                column: "MenuItemId");
        }
    }
}
