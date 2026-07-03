using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderNKitchenMS_API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPendingApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPending",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPending",
                table: "Users");
        }
    }
}
