using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class MessageUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_ApplicationUserId",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_ApplicationUserId1",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ApplicationUserId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ApplicationUserId1",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId1",
                table: "Messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId1",
                table: "Messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ApplicationUserId",
                table: "Messages",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ApplicationUserId1",
                table: "Messages",
                column: "ApplicationUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_ApplicationUserId",
                table: "Messages",
                column: "ApplicationUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_ApplicationUserId1",
                table: "Messages",
                column: "ApplicationUserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
