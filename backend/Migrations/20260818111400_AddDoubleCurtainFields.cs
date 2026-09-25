using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    public partial class AddDoubleCurtainFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDoubleCurtain",
                table: "QuotationWindows",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "SheerWidth",
                table: "QuotationWindows",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SheerHeight",
                table: "QuotationWindows",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SheerPanels",
                table: "QuotationWindows",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsDoubleCurtain", table: "QuotationWindows");
            migrationBuilder.DropColumn(name: "SheerWidth", table: "QuotationWindows");
            migrationBuilder.DropColumn(name: "SheerHeight", table: "QuotationWindows");
            migrationBuilder.DropColumn(name: "SheerPanels", table: "QuotationWindows");
        }
    }
}
