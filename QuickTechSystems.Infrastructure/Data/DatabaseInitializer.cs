using QuickTechSystems.Domain.Entities;

namespace QuickTechSystems.Infrastructure.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            // Add seed data if the database is empty
            if (!context.Categories.Any())
            {
                var categories = new[]
                {
                    new Category { Name = "General", Description = "General items" },
                    new Category { Name = "Electronics", Description = "Electronic items" },
                    new Category { Name = "Groceries", Description = "Grocery items" }
                };

                context.Categories.AddRange(categories);
                context.SaveChanges();
            }
        }
    }
}