using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project_Inventory_KedaiRIZO_Alpha_v0._1.Migrations
{
    /// <inheritdoc />
    public partial class transact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Data_Transaksi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Tanggal = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data_Transaksi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Data_Transaksi_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Data_Transaksi_UserId",
                table: "Data_Transaksi",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Data_Transaksi");
        }
    }
}
