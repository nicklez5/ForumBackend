using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceImageWithBanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Forums");

            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Forums",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "Forums",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Forums");

            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "Forums");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Forums",
                type: "TEXT",
                nullable: true);
        }
    }
}
