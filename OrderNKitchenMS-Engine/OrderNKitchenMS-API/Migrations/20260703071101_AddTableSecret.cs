using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderNKitchenMS_API.Migrations
{
    /// <inheritdoc />
    public partial class AddTableSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Secret",
                table: "Tables",
                type: "text",
                nullable: false,
                defaultValue: "");

            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("UPDATE \"Tables\" SET \"Secret\" = md5(random()::text) WHERE \"Secret\" IS NULL OR \"Secret\" = '';");
            }
            else
            {
                migrationBuilder.Sql("UPDATE \"Tables\" SET \"Secret\" = hex(randomblob(16)) WHERE \"Secret\" IS NULL OR \"Secret\" = '';");
            }

            migrationBuilder.CreateIndex(
                name: "IX_Tables_Secret",
                table: "Tables",
                column: "Secret",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tables_Secret",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "Secret",
                table: "Tables");
        }
    }
}
