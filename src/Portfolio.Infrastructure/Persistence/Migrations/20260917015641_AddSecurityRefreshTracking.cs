using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityRefreshTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPriceRefreshAttemptAt",
                table: "Securities",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPriceRefreshAttemptAt",
                table: "Securities");
        }
    }
}
