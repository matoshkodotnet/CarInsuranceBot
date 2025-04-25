using CarInsuranceBot.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarInsuranceBot.Data
{
    public class BotDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserStep> UserSteps { get; set; }
        public DbSet<Step> Steps { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }

        public BotDbContext(DbContextOptions<BotDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.UserId);

            modelBuilder.Entity<UserStep>()
                .HasKey(us => us.UserStepId);

            modelBuilder.Entity<Step>()
                .HasKey(s => s.StepId);

            modelBuilder.Entity<Document>()
                .HasKey(d => d.DocumentId);

            modelBuilder.Entity<DocumentType>()
                .HasKey(dt => dt.DocumentTypeId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.CurrentStep)
                .WithOne(us => us.User)
                .HasForeignKey<UserStep>(us => us.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Documents)
                .WithOne(d => d.User)
                .HasForeignKey(d => d.UserId);

            modelBuilder.Entity<UserStep>()
                .HasOne(us => us.Step)
                .WithMany(s => s.UserSteps)
                .HasForeignKey(us => us.StepId);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.DocumentType)
                .WithMany(dt => dt.Documents)
                .HasForeignKey(d => d.DocumentTypeId);

            modelBuilder.Entity<Step>()
                .HasData(
                    new Step { StepId = 1, StepName = "PassportUpload", Description = "Очікування завантаження фото паспорта" },
                    new Step { StepId = 2, StepName = "VehicleDocUpload", Description = "Очікування завантаження фото документа транспортного засобу" },
                    new Step { StepId = 3, StepName = "DataConfirmation", Description = "Очікування підтвердження витягнутих даних" },
                    new Step { StepId = 4, StepName = "PriceConfirmation", Description = "Очікування підтвердження ціни" },
                    new Step { StepId = 5, StepName = "Completed", Description = "Процес завершено" }
                );

            modelBuilder.Entity<DocumentType>()
                .HasData(
                    new DocumentType { DocumentTypeId = 1, TypeName = "Passport", Description = "Паспорт користувача" },
                    new DocumentType { DocumentTypeId = 2, TypeName = "VehicleDoc", Description = "Документ транспортного засобу" }
                );
        }
    }
}