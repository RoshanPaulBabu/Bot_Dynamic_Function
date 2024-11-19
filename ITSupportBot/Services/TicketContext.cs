using Microsoft.Bot.Schema.Teams;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ITSupportBot.Models;

namespace ITSupportBot.Services
{
    public class TicketContext : DbContext
    {
        public DbSet<Ticket> Tickets { get; set; }

        public TicketContext(DbContextOptions<TicketContext> options) : base(options) { }
    }
}
