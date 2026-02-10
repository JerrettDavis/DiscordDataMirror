using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordDataMirror.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentDownloadTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_hash",
                table: "attachments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "download_attempts",
                table: "attachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "download_status",
                table: "attachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "downloaded_at",
                table: "attachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_download_error",
                table: "attachments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "skip_reason",
                table: "attachments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_attachments_content_hash",
                table: "attachments",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "idx_attachments_download_status",
                table: "attachments",
                column: "download_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_attachments_content_hash",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "idx_attachments_download_status",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "content_hash",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "download_attempts",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "download_status",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "downloaded_at",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "last_download_error",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "skip_reason",
                table: "attachments");
        }
    }
}
