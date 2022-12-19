using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using OlympicWords.Data;

namespace OlympicWords.Services
{
    public class MasterContext : DbContext
    {
        public MasterContext(DbContextOptions<MasterContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserPicture> UserPictures { get; set; }
        // public DbSet<UserRelation> UserRelations { get; set; }
        public DbSet<ProviderLink> ExternalIds { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region learning notes

            // modelBuilder.Entity<DisplayUser>(); this meas that I included this type in the database creation
            //despite it's not mentioned in a DbSet or explored by nav prop

            //you can ignore mentioned in exploring nav prop or even in a dbset by annotation or the fluent api
            //there's something called "exclude from migration" which is not the same as ignore, it's a bit advanced now

            //+++++++++++you need to learn
            //search for the schema in sql
            //also there's something called view in databases
            //nullable ref types, ms said it's recommended!
            //concurrency in ef core
            //optional: see indexer

            //configure prop by: moduleBuilder.Entity.Property

            //the key concept is the identifier for the row
            //the convention for pk is "Id" or "<TypeName>Id"

            //when the used pk type is not supported by the db, ef will create a temp one during the creation and tracking
            //then it will be replaced(in your code) with the real one after SaveChanges is called.

            //alternate key is a key that's not primary, has advanced usage

            //shadow prop is a prop not defined in the script model but defined in the database
            //it's the opposite of the ignored prop

            //I will use the value converter to create the comma separated id I want!

            #endregion

            SetMaxLength(modelBuilder);

            // modelBuilder.Entity<UserRelation>()
            // .HasKey(r => new { r.FollowerId, r.FollowingId });

            modelBuilder.Entity<ProviderLink>()
                .HasOne(u => u.User)
                .WithMany(u => u.Providers)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPicture>()
                .HasOne(u => u.User)
                .WithOne(u => u.Picture)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPicture>()
                .HasKey(p => p.UserId);

            // .OnDelete(DeleteBehavior.Cascade);

            #region pathetic tries

            // modelBuilder.Entity<UserRelation>().Property(u => u.FollowerId).HasMaxLength(64);
            // modelBuilder.Entity<UserRelation>().Property(u => u.FollowingId).HasMaxLength(64);
            //
            // modelBuilder.Entity<UserRelation>().HasKey(_ => new { _.FollowerId, _.FollowingId });

            // modelBuilder.Entity<UserRelation>()
            //     .HasOne(u => u.Follower)
            //     .WithMany(u => u.Followers)
            //     .HasForeignKey(u => u.FollowerId)
            //     .OnDelete(DeleteBehavior.Restrict);
            // // fluent api is harder then explicit sql?
            //
            // modelBuilder.Entity<UserRelation>()
            //     .HasOne(u => u.Following)
            //     .WithMany()
            //     .HasForeignKey(u => u.FollowingId);
            //
            // modelBuilder.Entity<UserRelation>()
            //     .HasKey(u => new { u.FollowerId, u.FollowingId });


            // modelBuilder.Entity<User>()
            //     .HasMany(u => u.Followers)
            //     .WithMany(u => u.Following)
            //     .UsingEntity<UserRelation>();

            modelBuilder.Entity<User>()
                .HasMany(u => u.Followers)
                .WithMany(u => u.Followings)
                .UsingEntity<UserRelation>(
                    userRelation => userRelation
                        .HasOne(r => r.Follower)
                        .WithMany(u => u.FollowingRelations)
                        .HasForeignKey(r => r.FollowerId),
                    userRelation => userRelation
                        .HasOne(r => r.Following)
                        .WithMany(u => u.FollowerRelations)
                        .HasForeignKey(r => r.FollowingId),
                    userRelation =>
                        userRelation.HasKey(r => new { r.FollowerId, r.FollowingId }));

            //many to many with no 
            // modelBuilder.Entity<User>().Property(u => u.XP).HasConversion(
            //     v => v + "xp",
            //     v => int.Parse(v.Remove(v.Length - 2)));
            //store xp as string formatted 123xp and deserialize it

            // modelBuilder.Entity<User>().Property(u => u.Wins).HasConversion<string>();
            //predefined conversion
            //or change column type

            // var c = new ValueConverter<int, char>(
            //     v => (char) v,
            //     v => (int) v
            // );
            //for converter reuse

            // var c = new ValueConverter<int, string>(
            //     v => v.ToString(),
            //     v => int.Parse(v),
            //     new ConverterMappingHints(size: 12, precision: 7) //you can override these on the property 
            // );
            //mapping hints

            #endregion

            IntListConversion(modelBuilder);

            Seeder.SeedData(modelBuilder);
        }

        private void SetMaxLength(ModelBuilder modelBuilder)
        {
            var stringProps = modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(string));

            foreach (var property in stringProps)
                property.SetMaxLength(128);
            //todo check if this is right set the string max length for all properties globally

            // foreach (var property in stringProps)
            //     property.AsProperty().Builder.HasMaxLength(128, ConfigurationSource.Convention);
            // //set string props maxlength as 128

            modelBuilder.Entity<User>().Property(u => u.PictureUrl).HasMaxLength(256);

            modelBuilder.Entity<User>().Property(u => u.Id).HasMaxLength(64);
            modelBuilder.Entity<UserPicture>().Property(u => u.UserId).HasMaxLength(64);

            modelBuilder.Entity<User>().Property(u => u.PictureUrl).HasMaxLength(512);

            //because facebook for example is very big
            modelBuilder.Entity<ProviderLink>().Property(id => id.Id).HasMaxLength(256);

            modelBuilder.Entity<ProviderLink>().Property(id => id.UserId).HasMaxLength(64);
        }

        private void IntListConversion(ModelBuilder modelBuilder)
        {
            Expression<Func<List<int>, List<int>, bool>> equalsExpression =
                (c1, c2) => c1.SequenceEqual(c2);
            Expression<Func<List<int>, int>> hashCodeExpression = hce =>
                hce.Aggregate((a, v) => HashCode.Combine(a, v.GetHashCode()));
            //this makes a hashcode to the list from it's elements
            //so 2 lists with the same values have the same hashcode

            var intListToStringComparer = new ValueComparer<List<int>>(
                equalsExpression,
                hashCodeExpression,
                c => c.ToList() //this is not used, because taking the snapshot doesn't have anything customized 
            );

            Expression<Func<List<int>, string>> serializeIntListExpression =
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null);
            Expression<Func<string, List<int>>> deserializeIntListExpression =
                v => JsonSerializer.Deserialize<List<int>>(v, (JsonSerializerOptions)null);

            var intListConverter = new ValueConverter<List<int>, string>(serializeIntListExpression,
                deserializeIntListExpression);

            modelBuilder.Entity<User>().Property(u => u.OwnedBackgroundIds)
                .HasConversion(intListConverter, intListToStringComparer);

            modelBuilder.Entity<User>().Property(u => u.OwnedCardBackIds)
                .HasConversion(intListConverter, intListToStringComparer);

            modelBuilder.Entity<User>().Property(u => u.OwnedTitleIds)
                .HasConversion(intListConverter, intListToStringComparer);
        }
    }
}