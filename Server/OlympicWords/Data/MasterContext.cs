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

namespace OlympicWords.Services
{
    public class MasterContext : DbContext
    {
        public MasterContext(DbContextOptions<MasterContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserRelation> UserRelations { get; set; }
        public DbSet<ExternalId> ExternalIds { get; set; }

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

            modelBuilder.Entity<UserRelation>()
                .HasKey(r => new { r.FollowerId, r.FollowingId });

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
            //     .WithMany(u => u.Followings)
            //     .UsingEntity<UserRelation>(
            //         j => j
            //             .HasOne(r => r.Follower)
            //             .WithMany(u => u.UserRelations)
            //             .HasForeignKey(r => r.FollowerId),
            //         j => j
            //             .HasOne(r => r.Following)
            //             .WithMany(u => u.UserRelations)
            //             .HasForeignKey(r => r.FollowingId),
            //         j =>
            //             j.HasKey(r => new { r.FollowerId, r.FollowingId }));

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

            SeedData(modelBuilder);
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

            modelBuilder.Entity<ExternalId>().Property(id => id.Id).HasMaxLength(256);
            modelBuilder.Entity<ExternalId>().Property(id => id.MainId).HasMaxLength(64);
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

        private void SeedData(ModelBuilder modelBuilder)
        {
            var user0 = new User()
            {
                Id = "0",
                PlayedRoomsCount = 3,
                WonRoomsCount = 4,
                Name = "hany",
                OwnedBackgroundIds = new List<int> { 1, 3 },
                OwnedTitleIds = new List<int> { 2, 4 },
                PictureUrl = "https://pbs.twimg.com/profile_images/592734306725933057/s4-h_LQC.jpg",
                Level = 13,
                Money = 22250,
                Xp = 806,
                OwnedCardBackIds = new List<int>() { 0, 2 },
                RequestedMoneyAidToday = 2,
                LastMoneyAimRequestTime = null,
                SelectedCardback = 2,
            };

            var bot999 = new User()
            {
                Id = "999",
                PlayedRoomsCount = 9,
                WonRoomsCount = 2,
                Name = "botA",
                PictureUrl =
                    "https://pbs.twimg.com/profile_images/723902674970750978/p8JWhWxP_400x400.jpg",
                OwnedBackgroundIds = new List<int> { 0, 3 },
                OwnedTitleIds = new List<int> { 1 },
                OwnedCardBackIds = new List<int> { 8 },
                Level = 7,
                Money = 1000,
                Xp = 34,
                RequestedMoneyAidToday = 0,
                LastMoneyAimRequestTime = null,
                SelectedCardback = 1
            };
            var bot9999 = new User()
            {
                Id = "9999",
                PlayedRoomsCount = 11,
                WonRoomsCount = 3,
                Name = "botB",
                PictureUrl = "https://pbs.twimg.com/profile_images/592734306725933057/s4-h_LQC.jpg",
                OwnedBackgroundIds = new List<int> { 3 },
                OwnedTitleIds = new List<int> { 0, 1 },
                OwnedCardBackIds = new List<int> { 0, 8 },
                Level = 8,
                Money = 1100,
                Xp = 44,
                RequestedMoneyAidToday = 0,
                LastMoneyAimRequestTime = null,
                SelectedCardback = 2
            };
            var bot99999 = new User()
            {
                Id = "99999",
                PlayedRoomsCount = 11,
                WonRoomsCount = 3,
                Name = "botC",
                PictureUrl =
                    "https://d3g9pb5nvr3u7.cloudfront.net/authors/57ea8955d8de1e1602f67ca0/1902081322/256.jpg",
                OwnedBackgroundIds = new List<int> { 3 },
                OwnedTitleIds = new List<int> { 0, 1 },
                OwnedCardBackIds = new List<int> { 0, 8 },
                Level = 8,
                Xp = 44,
                RequestedMoneyAidToday = 0,
                LastMoneyAimRequestTime = null,
                SelectedCardback = 2,
            };

            modelBuilder.Entity<User>().HasData(
                new List<User>
                {
                    user0,
                    bot999,
                    bot9999,
                    bot99999,
                    new()
                    {
                        Id = "1",
                        PlayedRoomsCount = 7,
                        WonRoomsCount = 11,
                        Name = "samy",
                        OwnedBackgroundIds = new List<int> { 0, 9 },
                        OwnedTitleIds = new List<int> { 11, 6 },
                        PictureUrl =
                            "https://d3g9pb5nvr3u7.cloudfront.net/authors/57ea8955d8de1e1602f67ca0/1902081322/256.jpg",
                        Level = 43,
                        Money = 89000,
                        Xp = 1983,
                        OwnedCardBackIds = new List<int>() { 0, 1, 2 },
                        RequestedMoneyAidToday = 0,
                        LastMoneyAimRequestTime = null,
                        SelectedCardback = 1,
                    },
                    new()
                    {
                        Id = "2",
                        PlayedRoomsCount = 973,
                        WonRoomsCount = 192,
                        Name = "anni",
                        OwnedBackgroundIds = new List<int> { 10, 8 },
                        OwnedTitleIds = new List<int> { 1, 3 },
                        OwnedCardBackIds = new List<int> { 4, 9 },
                        PictureUrl =
                            "https://pbs.twimg.com/profile_images/633661532350623745/8U1sJUc8_400x400.png",
                        Level = 139,
                        Money = 8500,
                        Xp = 8062,
                        RequestedMoneyAidToday = 4,
                        LastMoneyAimRequestTime = null,
                        SelectedCardback = 4,
                    },
                    new()
                    {
                        Id = "3",
                        PlayedRoomsCount = 6,
                        WonRoomsCount = 2,
                        Name = "ali",
                        PictureUrl =
                            "https://pbs.twimg.com/profile_images/723902674970750978/p8JWhWxP_400x400.jpg",
                        OwnedBackgroundIds = new List<int> { 10, 8 },
                        OwnedTitleIds = new List<int> { 1, 3 },
                        OwnedCardBackIds = new List<int> { 2, 4, 8 },
                        Level = 4,
                        Money = 3,
                        Xp = 12,
                        RequestedMoneyAidToday = 3,
                        LastMoneyAimRequestTime = null,
                        SelectedCardback = 2
                    },
                }
            );

            modelBuilder.Entity<UserRelation>().HasData(
                new List<UserRelation>()
                {
                    new()
                    {
                        FollowerId = "0",
                        FollowingId = "999",
                    },
                    new()
                    {
                        FollowerId = "0",
                        FollowingId = "9999",
                    },

                    new()
                    {
                        FollowerId = "9999",
                        FollowingId = "0",
                    }
                }
            );
        }
    }
}