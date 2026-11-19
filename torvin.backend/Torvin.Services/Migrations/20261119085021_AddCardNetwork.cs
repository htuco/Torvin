using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Torvin.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddCardNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kartice dodane prije ove migracije nemaju mrežu, pa postaju Visa. Nula bi pala na ograničenju ispod
            migrationBuilder.AddColumn<int>(
                name: "Network",
                table: "Cards",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cards_Network",
                table: "Cards",
                sql: "[Network] BETWEEN 1 AND 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Cards_Network",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "Network",
                table: "Cards");
        }
    }
}
