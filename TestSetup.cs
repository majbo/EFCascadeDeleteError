using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace TestProject1
{
    public class TestContext : DbContext
    {
        private static readonly ILoggerFactory SomeLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
        });

        public DbSet<Tag> Tags { get; set; }
        
        public ITestOutputHelper OutputHelper { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("DataSource=:memory:")
                .UseLoggerFactory(SomeLoggerFactory)
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .LogTo(message => OutputHelper?.WriteLine(message));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tag>().HasMany(p => p.Synonyms)
                .WithOne(p => p.SynonymFor)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class Tag
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Tag? SynonymFor { get; set; }
        public Guid? SynonymForId { get; set; }
        public IEnumerable<Tag> Synonyms { get; set; } = null!;
    }

    public class UnitTestClass
    {
        private readonly ITestOutputHelper _outputHelper;

        public UnitTestClass(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }
        
        private void PrepareDatabase(TestContext context)
        {
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var t1 = new Tag {Id = Guid.NewGuid(), Name = "Foo"};
            var t2 = new Tag {Id = Guid.NewGuid(), Name = "Bar", SynonymFor = t1};

            context.Tags.AddRange(new[] {t1, t2});

            context.SaveChanges();
        }

        [Fact]
        public void Test1()
        {
            var context = new TestContext {OutputHelper = _outputHelper};

            PrepareDatabase(context);

            var barTag = context.Tags.Single(p => p.Name == "Bar");
            var barEntry = context.Entry(barTag);
            //barEntry.Property(nameof(Tag.SynonymForId)).CurrentValue = null;
            barTag.SynonymForId = null;

            context.SaveChangesAsync();


            barEntry.State = EntityState.Detached;
            //barEntry.Navigation(nameof(Tag.SynonymFor)).

            Assert.Null(context.Tags.Single(p => p.Name == "Bar").SynonymForId);
        }
        
        [Fact]
        public void Test2()
        {
            var context = new TestContext {OutputHelper = _outputHelper};

            PrepareDatabase(context);

            var temp1 = context.Set<Tag>().AsNoTracking().ToList();
            
            var barTag = context.Tags.Single(p => p.Name == "Bar");
            var barEntry = context.Entry(barTag);
            barEntry.Property(nameof(Tag.SynonymForId)).CurrentValue = null;
            context.SaveChangesAsync();

            barEntry.State = EntityState.Detached;

            var temp2 = context.Set<Tag>().AsNoTracking().ToList();
            
            Assert.Null(context.Tags.Single(p => p.Name == "Bar").SynonymForId);
        }
    }
}