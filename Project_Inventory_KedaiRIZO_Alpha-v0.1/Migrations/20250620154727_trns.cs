using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Migrations
{
    /// <inheritdoc />
    public partial class trns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalAmount",
                table: "Data_Transaksi",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Detail_Transaksi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataTransaksiId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    total = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Detail_Transaksi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Detail_Transaksi_Data_Transaksi_DataTransaksiId",
                        column: x => x.DataTransaksiId,
                        principalTable: "Data_Transaksi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Detail_Transaksi_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Detail_Transaksi_DataTransaksiId",
                table: "Detail_Transaksi",
                column: "DataTransaksiId");

            migrationBuilder.CreateIndex(
                name: "IX_Detail_Transaksi_ProductId",
                table: "Detail_Transaksi",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Detail_Transaksi");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Data_Transaksi");
        }
    }
}
