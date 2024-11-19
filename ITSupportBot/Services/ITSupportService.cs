using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using ITSupportBot.Models;

namespace ITSupportBot.Services
{
    public class ITSupportService
    {
        private readonly IServiceProvider _serviceProvider;

        // Constructor accepts IServiceProvider to resolve scoped services dynamically
        public ITSupportService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task SaveTicketAsync(string title, string description)
        {
            // Create a scope to resolve scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TicketContext>();

                var ticket = new Ticket
                {
                    Title = title,
                    Description = description,
                    CreatedAt = DateTime.Now
                };

                context.Tickets.Add(ticket);
                await context.SaveChangesAsync();
            }
        }
    }
}
