using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiscordDataMirror.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guilds",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon_url = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    owner_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guilds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_state",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_message_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_state", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    discriminator = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    global_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    is_bot = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "channels",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    guild_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parent_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    topic = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    is_nsfw = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channels", x => x.id);
                    table.ForeignKey(
                        name: "FK_channels_channels_parent_id",
                        column: x => x.parent_id,
                        principalTable: "channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_channels_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    guild_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    permissions = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_hoisted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_mentionable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_managed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_roles_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guild_members",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    guild_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nickname = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_pending = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    role_ids = table.Column<string>(type: "jsonb", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_guild_members_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_guild_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_maps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_user_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mapped_user_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    mapping_type = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_maps", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_maps_users_canonical_user_id",
                        column: x => x.canonical_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_maps_users_mapped_user_id",
                        column: x => x.mapped_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    channel_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    author_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    clean_content = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_tts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    referenced_message_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    raw_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_messages_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_messages_referenced_message_id",
                        column: x => x.referenced_message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_messages_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "threads",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parent_channel_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    owner_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    message_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    member_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    archive_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    auto_archive_duration = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_threads", x => x.id);
                    table.ForeignKey(
                        name: "FK_threads_channels_id",
                        column: x => x.id,
                        principalTable: "channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_threads_channels_parent_channel_id",
                        column: x => x.parent_channel_id,
                        principalTable: "channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_threads_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    proxy_url = table.Column<string>(type: "text", nullable: true),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    local_path = table.Column<string>(type: "text", nullable: true),
                    is_cached = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_attachments_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "embeds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    title = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    url = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    color = table.Column<int>(type: "integer", nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_embeds", x => x.id);
                    table.ForeignKey(
                        name: "FK_embeds_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reactions",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    message_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    emote_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    user_ids = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_reactions_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_attachments_message",
                table: "attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "idx_channels_guild",
                table: "channels",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_channels_parent_id",
                table: "channels",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_embeds_message",
                table: "embeds",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "idx_guild_members_guild",
                table: "guild_members",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_guild_members_user_id_guild_id",
                table: "guild_members",
                columns: new[] { "user_id", "guild_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_messages_author",
                table: "messages",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_channel_timestamp",
                table: "messages",
                columns: new[] { "channel_id", "timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_messages_referenced_message_id",
                table: "messages",
                column: "referenced_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_reactions_message_id_emote_key",
                table: "reactions",
                columns: new[] { "message_id", "emote_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_guild_id",
                table: "roles",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_state_entity_type_entity_id",
                table: "sync_state",
                columns: new[] { "entity_type", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_threads_owner_id",
                table: "threads",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_threads_parent_channel_id",
                table: "threads",
                column: "parent_channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_maps_canonical_user_id_mapped_user_id",
                table: "user_maps",
                columns: new[] { "canonical_user_id", "mapped_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_maps_mapped_user_id",
                table: "user_maps",
                column: "mapped_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachments");

            migrationBuilder.DropTable(
                name: "embeds");

            migrationBuilder.DropTable(
                name: "guild_members");

            migrationBuilder.DropTable(
                name: "reactions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "sync_state");

            migrationBuilder.DropTable(
                name: "threads");

            migrationBuilder.DropTable(
                name: "user_maps");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "channels");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "guilds");
        }
    }
}
