using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserIdToForum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Forums",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Forums_ApplicationUserId",
                table: "Forums",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Forums_Users_ApplicationUserId",
                table: "Forums",
                column: "ApplicationUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Forums_Users_ApplicationUserId",
                table: "Forums");

            migrationBuilder.DropIndex(
                name: "IX_Forums_ApplicationUserId",
                table: "Forums");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Forums");
        }
    }
}
