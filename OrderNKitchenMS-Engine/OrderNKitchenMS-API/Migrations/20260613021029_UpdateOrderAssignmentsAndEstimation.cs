using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderNKitchenMS_API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderAssignmentsAndEstimation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_AssignedUserId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "AssignedUserId",
                table: "Orders",
                newName: "AssignedWaiterId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_AssignedUserId",
                table: "Orders",
                newName: "IX_Orders_AssignedWaiterId");

            migrationBuilder.AddColumn<int>(
                name: "AssignedChefId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { 5, new DateTime(2026, 5, 28, 11, 4, 45, 313, DateTimeKind.Utc).AddTicks(3780), 5, null });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AssignedChefId",
                table: "Orders",
                column: "AssignedChefId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_AssignedChefId",
                table: "Orders",
                column: "AssignedChefId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_AssignedWaiterId",
                table: "Orders",
                column: "AssignedWaiterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_AssignedChefId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_AssignedWaiterId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AssignedChefId",
                table: "Orders");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "AssignedChefId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "AssignedWaiterId",
                table: "Orders",
                newName: "AssignedUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_AssignedWaiterId",
                table: "Orders",
                newName: "IX_Orders_AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_AssignedUserId",
                table: "Orders",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
